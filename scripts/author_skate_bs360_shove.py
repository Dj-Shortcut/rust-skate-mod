#!/usr/bin/env python3
"""Bake the SHA-bound Kroetkov BS360 Shove-it; no flip or body-spin substitution."""
import argparse
import copy
import hashlib
import gzip
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
CONTROLS = ROOT / 'assets/authored/animation/bs360-shove-keyed/constraints.json'
SURFACE = ROOT / 'assets/authored/animation/treflip-keyed/own-board-surface.json'


def scalar(keys, sf):
    return float(curve([v[0] for v in keys], [[v[1]] for v in keys], sf)[0])


def source_local_root(source, sf, derivative=False):
    frames = source['frames']; times = [r['sourceFrame'] for r in frames]
    i = max(0, min(len(frames)-2, int(np.searchsorted(times, sf, side='right'))-1))
    a, b = frames[i:i+2]; span = times[i+1]-times[i]
    root = lambda row: np.asarray(row['joints']['Pelvis'])-np.asarray(row['board']['origin'])+UP*.092
    ra, rb = root(a), root(b)
    if derivative: return (rb-ra)/span
    u = float(np.clip((sf-times[i])/span, 0, 1))
    return ra*(1-u)+rb*u


def source_catch_root(source, sf, spec, ordinary):
    start, end = spec['start'], spec['end']
    if not start < sf < end: return ordinary
    a, b = source_local_root(source, start), source_local_root(source, end)
    va = source_local_root(source, start, derivative=True)
    vb = source_local_root(source, end, derivative=True)
    span = end-start; u = (sf-start)/span
    # Preserve the original interpolated positions and both actual source-time derivatives. Only the
    # old contact-prior discontinuity inside this own take is reconstructed.
    return ((2*u**3-3*u*u+1)*a+(u**3-2*u*u+u)*span*va
            +(-2*u**3+3*u*u)*b+(u**3-u*u)*span*vb)


def source_body(source, sf, spec):
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
    root_shift = source_catch_root(source, sf, spec, raw['Pelvis'])-raw['Pelvis']
    raw = {k: value+root_shift for k, value in raw.items()}
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


def source_rear_arm(s, sf, joints):
    spec = s['rearArmImageGuide']
    weight = min(smooth((sf-spec['start'])/(spec['full']-spec['start'])),
                 1-smooth((sf-spec['returnStart'])/(spec['end']-spec['returnStart'])))
    if weight <= 0: return
    anchors = []
    for row in s['_rawFrames']:
        if row['frame_index'] in spec['authoredGapFrames']: continue
        points = row['image_landmarks'][row['target_selection']['selectedCandidateIndex']]
        shoulders = (points[11]['y']+points[12]['y'])*.5
        hips = (points[23]['y']+points[24]['y'])*.5
        span = hips-shoulders
        if span <= 0 or min(points[k].get('visibility', 0) for k in (11,13,15)) < .5:
            continue
        wrist_y = points[15]['y']
        for mark in spec['wristPixelOverrides']:
            if row['frame_index'] == mark['frame']: wrist_y = mark['pixel'][1]/row['height']
        anchors.append((row['frame_index'], [-(points[13]['y']-points[11]['y'])/span,
                                             -(wrist_y-points[13]['y'])/span]))
    if len(anchors) < 2 or not anchors[0][0] <= sf <= anchors[-1][0]:
        raise ValueError('Source rear-arm guide lacks actual bracketing anchors')
    # Rejected94–96 and the uncertain wrist93 mark remain explicit authored guidance.
    vertical_rise = (joints['LeftShoulder'][1]+joints['RightShoulder'][1])*.5-joints['Pelvis'][1]
    dy = curve([a[0] for a in anchors], [a[1] for a in anchors], sf)*vertical_rise
    side = 'Right'
    vectors = [joints[side+'Elbow']-joints[side+'Shoulder'], joints[side+'Hand']-joints[side+'Elbow']]
    for n, (root, tip, length) in enumerate((('Shoulder','Elbow',s['_rig']['upperArm']), ('Elbow','Hand',s['_rig']['forearm']))):
        old = vectors[n]; horizontal = old-UP*old[1]; y = float(dy[n])
        if abs(y) >= length*.998 or np.linalg.norm(horizontal) < 1e-8:
            raise ValueError('Source arm image guide exceeds its measured chain at '+str(sf))
        goal = unit(horizontal)*math.sqrt(length*length-y*y)+UP*y
        direction = unit(old*(1-weight)+goal*weight)
        joints[side+tip] = joints[side+root]+direction*length


def reference_height(s, sf):
    p = s['phases']
    f = float(np.clip((sf - p['pop']) / (p['land'] - p['pop']), 0, 1))
    return .5 * s['reference']['gravity'] * s['reference']['flightSeconds'] ** 2 * f * (1 - f)


def board_at(s, sf):
    p = s['phases']
    yaw, pitch = scalar(s['yawKeys'], sf), scalar(s['pitchKeys'], sf)
    # A backside360 scoop in canonical coordinates; Up never turns underneath
    # the board. There is deliberately no longitudinal roll component.
    # The filmed deck keeps its carrier-plane tilt while the board sweeps
    # through the shove. Rotating that tilt with the nose reverses the visible
    # Air slope after half a turn. This is authored camera-limited geometry.
    r = qmatrix(euler_quat([-pitch, 0, 0])) @ qmatrix(euler_quat([0, yaw, 0]))
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
    # The source front sole may release before Pop, but the live clip is
    # unknown during ground Load. Begin its free guide at the observed Pop
    # footing, avoiding a hidden forward step on the first captured frame.
    release = max(p['frontRelease'], p['pop']) if side == 'Left' else p['rearRelease']
    if sf <= release:
        return attached(s, sf, side)
    start, old_basis = attached(s, release, side)
    target = raw[side + 'Ankle'].copy()
    if side == 'Right': target[1] += scalar(s['rearPickupKeys'], sf)
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
    catch_start = caught - 1.5
    catch_weight = smooth((sf - catch_start) / (caught - catch_start))
    # Full360 restores the original nose/tail order. Canonical Left remains
    # the front anatomical foot; there is no body turn or half-turn foot swap.
    z = .24 if side == 'Left' else -.24
    local_yaw = 80 if side == 'Left' else 85
    goal, goal_basis = deck_shoe(s, origin, rotation, .04, z, local_yaw, s['soleGap'])
    return (ankle * (1 - catch_weight) + goal * catch_weight,
            qmatrix(slerp(matrix_quat(frame), matrix_quat(goal_basis), catch_weight)))


def sample(source, s, sf):
    j, dirs, soles = source_body(source, sf, s['catchRootJoin'])
    source_rear_arm(s, sf, j)
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
        if side == 'Right' and sf > s['rearLandingKnee']['start']:
            spec = s['rearLandingKnee']
            weight = smooth((sf-spec['start'])/(spec['full']-spec['start']))
            toe = frame[:, 2]-UP*frame[1, 2]
            pole = unit(unit(pole)*(1-weight)+unit(toe)*weight)
        j[side + 'Knee'] = limb(j[side + 'Hip'], ankle, s['_rig']['thigh'], s['_rig']['shin'], pole)
        j[side + 'Ankle'] = ankle
        j[side + 'Toe'] = ankle + frame[:, 2] * math.sqrt(s['_rig']['foot'] ** 2 - s['_rig']['toeDrop'] ** 2) - frame[:, 1] * s['_rig']['toeDrop']
        soles[side] = frame[:, 1]
    origin, rotation = board_at(s, sf)
    return dict(t=(sf - s['firstSourceFrame']) / s['sourceFps'], sourceFrame=sf,
                sourceCacheTime=sf / s['sourceFps'],
                sourceVideoTime=s['sourceCacheOriginalOffsetSeconds'] + sf / s['sourceFps'],
                joints={k: j[k].tolist() for k in JOINTS}, dirs={k: dirs[k].tolist() for k in DIRS},
                soleUp={k: soles[k].tolist() for k in soles},
                board=dict(origin=origin.tolist(), quaternion=matrix_quat(rotation).tolist()))


def bake(source, controls, raw):
    s = copy.deepcopy(controls)
    if hashlib.sha256(raw).hexdigest() != s['originalRawPoseSha256']: raise ValueError('Wrong raw actor track')
    s['_rawFrames'] = json.loads(gzip.decompress(raw))['frames']
    m = source['metadata']
    if m['id'] != s['originalId'] or m['movementKind'] != s['movementKind'] or m['reviewed']:
        raise ValueError('Use the exact original unreviewed Kroetkov360 capture')
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
    ordered = [s['firstSourceFrame'], p['frontRelease'], p['pop'], p['strike'], p['rearRelease'], p['pressEnd'], p['airJoin'], p['firstCatch'], p['bothCatch'], p['land'], p['recovery'], s['lastSourceFrame']]
    if not all(a < b for a, b in zip(ordered, ordered[1:])):
        raise ValueError('Expected ordered source-specific shove phases')
    times = np.unique(np.round(np.r_[np.arange(s['firstSourceFrame'], s['lastSourceFrame'] + 1e-7, 1.0 / 6), ordered], 9))
    frames = [sample(source, s, float(sf)) for sf in times]
    if any(qmatrix(r['board']['quaternion'])[1, 1] <= 0 for r in frames):
        raise ValueError('A BS shove must not acquire a longitudinal board flip')
    events = [(s['firstSourceFrame'], 'load', ['LeftFoot', 'RightFoot']),
              (p['frontRelease'], 'load', ['RightFoot']), (p['pop'], 'pop', ['RightFoot']),
              (p['rearRelease'], 'air', []), (p['pressEnd'], 'air', []), (p['airJoin'], 'air', []),
              (p['firstCatch'], 'catch', ['LeftFoot']), (p['bothCatch'], 'catch', ['LeftFoot', 'RightFoot']),
              (p['land'], 'land', ['LeftFoot', 'RightFoot']), (p['recovery'], 'recovery', ['LeftFoot', 'RightFoot'])]
    meta = dict(m, id=s['id'], label='BS360 Shove-it — own source body and full360 scoop [UNREVIEWED]',
                reviewed=False, authored=True, capturedBody=True,
                filmedStance='Goofy source role mapping from original image audit; canonical source normalized; preview anatomy requires actualGLB audit',
                footOrientationLimit='Authored shaped-deck contact fit; interpolated source-based free-foot heading/sole estimates, not measured shoe orientation.',
                resamplingLimit='Dense authored interpolation does not add source observations or increase confidence',
                coordinateFrame='Y up,+Z nose,+X right; upright physical carrier at0; rest deck top+0.092; quaternionXYZW',
                firstSourceFrame=s['firstSourceFrame'], lastSourceFrame=s['lastSourceFrame'], sourceRootMotion='Original board translation removed once; actual physical carrier added once at playback.',
                capturedSourceProvenance=copy.deepcopy(m.get('provenance', {})),
                capturedSourceCamera=copy.deepcopy(m.get('camera', {})),
                camera=dict(mode='already-canonical', referenceFrame=93, referenceCameraOffset=[.907, .10, -.421], provenance='Fixed preview approximation from the original estimated source93 camera basis; source poses are already canonical, no second camera transform.'),
                sourceContactRootFit=dict(method='Authored vertical root projection into the intersection of both fixed-foot outer reach spheres; source upper directions and foot targets retained.', maximumReachShare=.998),
                boardProvenance='Authored source-led 360 yaw and tail pressure; no longitudinal flip; own whole-board support hull.',
                provenance=s['provenance'], uncertainty=s['uncertainty'], libraryEdit=s['libraryEdit'],
                sourceOriginalPackSha256=s['originalPackSha256'], originalRawPoseSha256=s['originalRawPoseSha256'], sourceJobSha256=s['sourceJobSha256'], sourceBodyInputSha256=hashlib.sha256(json.dumps(source, sort_keys=True).encode()).hexdigest(),
                sourceVideoOffsetSeconds=s['firstSourceFrame'] / s['sourceFps'],
                sourceCacheOriginalOffsetSeconds=s['sourceCacheOriginalOffsetSeconds'], referenceGroundPress=s['reference'],
                combinedWinding=dict(nativeFlip=0, nativeShove=-360, canonicalYaw=360),
                sourcePhaseLimit=s['sourcePhaseLimit'], sourceRootJoinFrame=p['airJoin'], authoredRearPickup=s['rearPickupKeys'], rearArmImageGuide=s['rearArmImageGuide'], rearLandingKnee=s['rearLandingKnee'], catchRootJoin=s['catchRootJoin'],
                contactLimit='Monocular source-role/contact estimates and authored shaped-deck reconstruction; no measured contact confidence.')
    duration = (s['lastSourceFrame'] - s['firstSourceFrame']) / s['sourceFps']
    clip = dict(format='skate-source-keyed-bs360/1', metadata=meta, stance='regular', fps=s['sourceFps'] * 6, duration=duration,
                events=contact_events([dict(t=(sf-s['firstSourceFrame'])/s['sourceFps'], phase=phase, contacts=c) for sf, phase, c in events], duration), frames=frames)
    return clip, encode_pack([clip])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, required=True)
    parser.add_argument('--raw', type=Path, required=True)
    parser.add_argument('--controls', type=Path, default=CONTROLS)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.resolve() in args.input.resolve().parents:
        raise ValueError('Keep authored output separate from its immutable source')
    clip, pack = bake(json.loads(args.input.read_text()), json.loads(args.controls.read_text()), args.raw.read_bytes())
    payloads = [('bs-360-shove-it-source-keyed-v1.json', (json.dumps(clip, indent=2)+'\n').encode()), ('skate_captures.skmc', pack)]
    args.output.mkdir(parents=True, exist_ok=True)
    for name, data in payloads:
        target = args.output / name
        temporary = target.with_suffix(target.suffix + '.tmp')
        temporary.write_bytes(data)
        temporary.replace(target)
    print('BS360_SOURCE_READY', len(clip['frames']), hashlib.sha256(pack).hexdigest())


if __name__ == '__main__':
    main()
