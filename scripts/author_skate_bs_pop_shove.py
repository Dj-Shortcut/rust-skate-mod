#!/usr/bin/env python3
"""Bake the SHA-bound Cole BS Pop Shove-it; no flip or body-spin substitution."""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from author_skate_kickflip import curve, limb
from author_skate_kickflip_hybrid import basis, deck_shoe, smooth
from author_skate_treflip import free_foot_clearance
from capture_math import (JOINTS, DIRS, UP, contact_events, encode_pack,
                          euler_quat, matrix_quat, qmatrix, slerp, unit)
from capture_retarget import transport_leg_pole

ROOT = Path(__file__).resolve().parents[1]
CONTROLS = ROOT / 'assets/authored/animation/bs-pop-shove-keyed/constraints.json'
SURFACE = ROOT / 'assets/authored/animation/treflip-keyed/own-board-surface.json'


def scalar(keys, sf):
    return float(curve([v[0] for v in keys], [[v[1]] for v in keys], sf)[0])


def source_body(source, sf):
    frames = source['frames']
    times = [r['sourceFrame'] for r in frames]
    i = max(0, min(len(frames) - 2, int(np.searchsorted(times, sf, side='right')) - 1))
    a, b = frames[i:i + 2]
    w = float(np.clip((sf - times[i]) / (times[i + 1] - times[i]), 0, 1))
    vector = lambda x, y: np.asarray(x) * (1 - w) + np.asarray(y) * w
    # The old recipe contains both forward travel and a vertical board/root
    # trajectory. Remove that translation once; the live carrier owns both.
    translation = vector(a['board']['origin'], b['board']['origin']) - UP * .092
    raw = {k: vector(a['joints'][k], b['joints'][k]) - translation for k in JOINTS}
    dirs = {k: unit(vector(a['dirs'][k], b['dirs'][k])) for k in DIRS}
    soles = {k: unit(vector(a['soleUp'][k], b['soleUp'][k])) for k in ('Left', 'Right')}
    rig = source['metadata']['rig']
    j = {k: v.copy() for k, v in raw.items()}
    torso = unit(raw['Neck'] - raw['Pelvis'])
    hips = unit(raw['RightHip'] - raw['LeftHip'])
    shoulders = unit(raw['RightShoulder'] - raw['LeftShoulder'])
    shoulders = unit(shoulders - torso * np.dot(shoulders, torso))
    j['Chest'] = j['Pelvis'] + torso * (rig['spine'] / 1.12)
    j['Neck'] = j['Pelvis'] + torso * rig['spine']
    j['Head'] = j['Neck'] + unit(raw['Head'] - raw['Neck']) * rig['head']
    for side, sign in (('Left', -1), ('Right', 1)):
        hip = j['Pelvis'] + hips * (sign * rig['hipHalfWidth'])
        shoulder = j['Pelvis'] + torso * rig['shoulderRise'] + shoulders * (sign * rig['shoulderHalfWidth'])
        j[side + 'Hip'] = hip
        j[side + 'Shoulder'] = shoulder
        j[side + 'Elbow'] = shoulder + unit(raw[side + 'Elbow'] - raw[side + 'Shoulder']) * rig['upperArm']
        j[side + 'Hand'] = j[side + 'Elbow'] + unit(raw[side + 'Hand'] - raw[side + 'Elbow']) * rig['forearm']
        j[side + 'Knee'] = hip + unit(raw[side + 'Knee'] - raw[side + 'Hip']) * rig['thigh']
        j[side + 'Ankle'] = j[side + 'Knee'] + unit(raw[side + 'Ankle'] - raw[side + 'Knee']) * rig['shin']
        j[side + 'Toe'] = j[side + 'Ankle'] + unit(raw[side + 'Toe'] - raw[side + 'Ankle']) * rig['foot']
    return j, dirs, soles


def reference_height(s, sf):
    p = s['phases']
    f = float(np.clip((sf - p['pop']) / (p['land'] - p['pop']), 0, 1))
    return .5 * s['reference']['gravity'] * s['reference']['flightSeconds'] ** 2 * f * (1 - f)


def board_at(s, sf):
    p = s['phases']
    yaw, pitch = scalar(s['yawKeys'], sf), scalar(s['pitchKeys'], sf)
    # A backside180 scoop in canonical coordinates; Up never turns underneath
    # the board. There is deliberately no longitudinal roll component.
    r = qmatrix(euler_quat([0, yaw, 0])) @ qmatrix(euler_quat([-pitch, 0, 0]))
    h = reference_height(s, sf)
    press = 1 - smooth((sf - p['rearRelease']) / (p['pressEnd'] - p['rearRelease']))
    floor = -float((np.asarray(s['support']['board']) @ r.T)[:, 1].min())
    world_y = max(floor, floor * press + (.092 + h) * (1 - press))
    if sf <= p['pop'] or sf >= p['land']:
        world_y = .092
    return np.array([0., world_y - h, 0.]), r


def attached(s, sf, side):
    origin, rotation = board_at(s, sf)
    e = s['entryFeet'][side]
    forward = np.asarray(e['forward'])
    yaw = math.degrees(math.atan2(forward[0], forward[2]))
    return deck_shoe(s, origin, rotation, e['ankle'][0], e['ankle'][2], yaw, s['soleGap'])


def foot(s, sf, side, raw, soles):
    p = s['phases']
    release = p['frontRelease'] if side == 'Left' else p['rearRelease']
    if sf <= release:
        return attached(s, sf, side)
    start, old_basis = attached(s, release, side)
    target = raw[side + 'Ankle'].copy()
    forward = unit(raw[side + 'Toe'] - raw[side + 'Ankle'] + soles[side] * s['_rig']['toeDrop'])
    free_basis = basis(forward, soles[side])
    weight = smooth((sf - release) / s['freeFootBlendFrames'])
    ankle = start * (1 - weight) + target * weight
    frame = qmatrix(slerp(matrix_quat(old_basis), matrix_quat(free_basis), weight))
    origin, rotation = board_at(s, sf)
    # Only triangles beneath this actual shoe footprint can lift a free foot;
    # no remote-nose/full-deck projection or source-confidence inflation.
    ankle[1] += free_foot_clearance(s, origin, rotation, ankle, frame)
    caught = p['firstCatch'] if side == 'Left' else p['bothCatch']
    catch_weight = smooth((sf - (caught - 1.5)) / 1.5)
    # A board-only half turn reverses deck-local front/back; anatomy does not
    # turn180 with it. Canonical Left stays over world+nose and Right behind.
    z = -.24 if side == 'Left' else .24
    local_yaw = -100 if side == 'Left' else -95
    goal, goal_basis = deck_shoe(s, origin, rotation, .04, z, local_yaw, s['soleGap'])
    return (ankle * (1 - catch_weight) + goal * catch_weight,
            qmatrix(slerp(matrix_quat(frame), matrix_quat(goal_basis), catch_weight)))


def sample(source, s, sf):
    j, dirs, soles = source_body(source, sf)
    original = {k: v.copy() for k, v in j.items()}
    feet = {side: foot(s, sf, side, original, soles) for side in ('Left', 'Right')}
    # Constrain only root height against both unchanged foot targets. This is
    # authored retargeting of the old source root, not extra physical airtime.
    lo, hi = -float('inf'), float('inf')
    reach = (s['_rig']['thigh'] + s['_rig']['shin']) * .998
    for side, (ankle, _) in feet.items():
        offset = j[side + 'Hip'] - j['Pelvis']
        horizontal = (j[side + 'Hip'][0] - ankle[0]) ** 2 + (j[side + 'Hip'][2] - ankle[2]) ** 2
        if horizontal >= reach * reach:
            raise ValueError('Source shove foot exceeds fixed-leg horizontal reach at ' + str(sf))
        half = math.sqrt(reach * reach - horizontal)
        lo = max(lo, ankle[1] - offset[1] - half)
        hi = min(hi, ankle[1] - offset[1] + half)
    if lo > hi:
        raise ValueError('No common source-root height for shove feet at ' + str(sf))
    shift = UP * (float(np.clip(j['Pelvis'][1], lo, hi)) - j['Pelvis'][1])
    for k in j:
        j[k] += shift
    for side, (ankle, frame) in feet.items():
        pole = transport_leg_pole(original[side + 'Hip'], original[side + 'Knee'], original[side + 'Ankle'], j[side + 'Hip'], ankle)
        j[side + 'Knee'] = limb(j[side + 'Hip'], ankle, s['_rig']['thigh'], s['_rig']['shin'], pole)
        j[side + 'Ankle'] = ankle
        j[side + 'Toe'] = ankle + frame[:, 2] * math.sqrt(s['_rig']['foot'] ** 2 - s['_rig']['toeDrop'] ** 2) - frame[:, 1] * s['_rig']['toeDrop']
        soles[side] = frame[:, 1]
    origin, rotation = board_at(s, sf)
    return dict(t=(sf - s['firstSourceFrame']) / s['sourceFps'], sourceFrame=sf,
                sourceCacheTime=sf / s['sourceFps'],
                sourceVideoTime=s['sourceVideoOffsetSeconds'] + sf / s['sourceFps'],
                joints={k: j[k].tolist() for k in JOINTS}, dirs={k: dirs[k].tolist() for k in DIRS},
                soleUp={k: soles[k].tolist() for k in soles},
                board=dict(origin=origin.tolist(), quaternion=matrix_quat(rotation).tolist()))


def bake(source, controls):
    s = copy.deepcopy(controls)
    m = source['metadata']
    if m['id'] != s['originalId'] or m['movementKind'] != s['movementKind'] or m['reviewed']:
        raise ValueError('Use the exact original unreviewed Cole shove capture')
    if hashlib.sha256(encode_pack([source])).hexdigest() != s['originalPackSha256']:
        raise ValueError('The original shove capture differs from its pinned pack')
    if m['firstSourceFrame'] != s['originalSourceWindow'][0] or m['lastSourceFrame'] != s['originalSourceWindow'][1]:
        raise ValueError('Unexpected original shove source window')
    if not m['firstSourceFrame'] <= s['firstSourceFrame'] < s['lastSourceFrame'] <= m['lastSourceFrame']:
        raise ValueError('Output must stay within the original filmed take')
    surface_bytes = SURFACE.read_bytes()
    if hashlib.sha256(surface_bytes).hexdigest() != s['sourceSurfaceSha256']:
        raise ValueError('Own board surface differs from the pinned geometry')
    surface = json.loads(surface_bytes)
    s['_surfacePoints'], s['_surfaceTriangles'] = np.asarray(surface['points']), np.asarray(surface['triangles'])
    s['_rig'] = m['rig']
    s['shoe'] = dict(s['hybrid']['popContact'], soleGap=s['soleGap'])
    s['authored'] = dict(minimumAirSoleClearance=s['soleGap'])
    p = s['phases']
    ordered = [s['firstSourceFrame'], p['frontRelease'], p['pop'], p['strike'], p['rearRelease'], p['pressEnd'], p['firstCatch'], p['bothCatch'], p['land'], p['recovery'], s['lastSourceFrame']]
    if not all(a < b for a, b in zip(ordered, ordered[1:])):
        raise ValueError('Expected ordered source-specific shove phases')
    times = np.unique(np.r_[np.linspace(s['firstSourceFrame'], s['lastSourceFrame'], 145), ordered])
    frames = [sample(source, s, float(sf)) for sf in times]
    if any(qmatrix(r['board']['quaternion'])[1, 1] <= 0 for r in frames):
        raise ValueError('A BS shove must not acquire a longitudinal board flip')
    events = [(s['firstSourceFrame'], 'load', ['LeftFoot', 'RightFoot']),
              (p['frontRelease'], 'load', ['RightFoot']), (p['pop'], 'pop', ['RightFoot']),
              (p['rearRelease'], 'air', []), (p['pressEnd'], 'air', []),
              (p['firstCatch'], 'catch', ['LeftFoot']), (p['bothCatch'], 'catch', ['LeftFoot', 'RightFoot']),
              (p['land'], 'land', ['LeftFoot', 'RightFoot']), (p['recovery'], 'recovery', ['LeftFoot', 'RightFoot'])]
    meta = dict(m, id=s['id'], label='BS Pop Shove-it — own source body and 180 scoop [UNREVIEWED]',
                reviewed=False, authored=True, capturedBody=True,
                filmedStance='Unverified; original canonical mapping retained',
                footOrientationLimit='Authored shaped-deck contact fit; interpolated source-based free-foot heading/sole estimates, not measured shoe orientation.',
                resamplingLimit='Dense authored interpolation does not add source observations or increase confidence',
                coordinateFrame='Y up,+Z nose,+X right; upright physical carrier at0; rest deck top+0.092; quaternionXYZW',
                lastSourceFrame=s['lastSourceFrame'], sourceRootMotion='Original board translation removed once; actual physical carrier added once at playback.',
                capturedSourceProvenance=copy.deepcopy(m.get('provenance', {})),
                sourceContactRootFit=dict(method='Authored vertical root projection into the intersection of both fixed-foot outer reach spheres; source upper directions and foot targets retained.', maximumReachShare=.998),
                boardProvenance='Authored source-led 180 yaw and tail pressure; no longitudinal flip; own whole-board support hull.',
                provenance=s['provenance'], uncertainty=s['uncertainty'], libraryEdit=s['libraryEdit'],
                sourceOriginalPackSha256=s['originalPackSha256'], sourceBodyInputSha256=hashlib.sha256(json.dumps(source, sort_keys=True).encode()).hexdigest(),
                sourceVideoOffsetSeconds=s['firstSourceFrame'] / s['sourceFps'],
                sourceCacheOriginalOffsetSeconds=s['sourceCacheOriginalOffsetSeconds'], referenceGroundPress=s['reference'],
                combinedWinding=dict(nativeFlip=0, nativeShove=-180, canonicalYaw=180),
                contactLimit='Monocular source-role/contact estimates and authored shaped-deck reconstruction; no measured contact confidence.')
    duration = (s['lastSourceFrame'] - s['firstSourceFrame']) / s['sourceFps']
    clip = dict(format='skate-source-keyed-shove/1', metadata=meta, stance='regular', fps=s['sourceFps'] * 6, duration=duration,
                events=contact_events([dict(t=(sf-s['firstSourceFrame'])/s['sourceFps'], phase=phase, contacts=c) for sf, phase, c in events], duration), frames=frames)
    return clip, encode_pack([clip])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, required=True)
    parser.add_argument('--controls', type=Path, default=CONTROLS)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.resolve() in args.input.resolve().parents:
        raise ValueError('Keep authored output separate from its immutable source')
    clip, pack = bake(json.loads(args.input.read_text()), json.loads(args.controls.read_text()))
    payloads = [('bs-pop-shove-it-source-keyed-v1.json', (json.dumps(clip, indent=2)+'\n').encode()), ('skate_captures.skmc', pack)]
    args.output.mkdir(parents=True, exist_ok=True)
    for name, data in payloads:
        target = args.output / name
        temporary = target.with_suffix(target.suffix + '.tmp')
        temporary.write_bytes(data)
        temporary.replace(target)
    print('BS_SHOVE_SOURCE_READY', len(clip['frames']), hashlib.sha256(pack).hexdigest())


if __name__ == '__main__':
    main()
