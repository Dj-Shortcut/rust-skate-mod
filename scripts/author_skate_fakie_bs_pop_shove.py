#!/usr/bin/env python3
"""Bake the own BATB13 Fakie BS Pop Shove-it, with one board-only turn.

Only fixed-rig/geometry mathematics are shared with the original Ollie/shove
bakers. The actor, body directions, source clock, feet and reconstruction
controls belong to this take; no Cole or Halfcab motion is read.
"""
import argparse
import copy
import json
import math
from pathlib import Path

import numpy as np
import author_skate_ollie as geometry
from author_skate_bs_pop_shove import scalar
from author_skate_kickflip import curve, limb
from author_skate_kickflip_hybrid import body_at
from author_skate_treflip import basis, deck_shoe, free_foot_clearance, smooth
from capture_math import (DIRS, JOINTS, UP, contact_events, encode_pack,
                          euler_quat, matrix_quat, qmatrix, slerp, unit)

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'assets/authored/animation/fakie-bs-pop-shove-keyed'
SURFACE = ROOT / 'assets/authored/animation/treflip-keyed'


def board_at(s, sf):
    yaw = scalar(s['yawKeys'], sf)
    pitch = scalar(s['pitchKeys'], sf) * s['_strikePitch'] / 46.
    rotation = qmatrix(euler_quat([0., yaw, 0.])) @ qmatrix(euler_quat([-pitch, 0., 0.]))
    floor = -float((np.asarray(s['_geometry']['support']['board']) @ rotation.T)[:, 1].min())
    rise = geometry.reference_height(s, sf)
    free = smooth((sf - s['tailRelease']) / (s['pressEnd'] - s['tailRelease']))
    world_y = max(floor, floor * (1 - free) + (.092 + rise) * free)
    if sf <= s['pop'] or sf >= s['land']:
        world_y = .092
    # Source carrier/jump is absent from the body. The reference rise is only
    # subtracted from this board pressure offset, then the live carrier owns it.
    return np.array([0., world_y - rise, 0.]), rotation


def planted(s, sf, side, catch=False):
    origin, rotation = board_at(s, sf)
    p = s['catchFeet' if catch else 'setupFeet'][side]
    return deck_shoe(s['_geometry'], origin, rotation, p['x'], p['z'], p['yaw'])


def foot(s, sf, side):
    release = s['frontRelease'] if side == 'Left' else s['tailRelease']
    if sf <= release:
        return planted(s, sf, side)
    start, start_basis = planted(s, release, side)
    keys = s['frontFreeKeys' if side == 'Left' else 'rearFreeKeys']
    target = curve([r[0] for r in keys], [r[1:] for r in keys], sf)
    # These are body/nose-frame free-foot paths. A turning board never drags
    # the rider through its 180, and the front foot has no flip/instep flick.
    yaw = math.radians(s['freeFootYaw'][side])
    free_basis = basis(np.array([math.sin(yaw), 0., math.cos(yaw)]), UP)
    weight = smooth((sf - release) / s['releaseBlendFrames'])
    ankle = start * (1 - weight) + target * weight
    frame = qmatrix(slerp(matrix_quat(start_basis), matrix_quat(free_basis), weight))
    caught_at = s['firstCatch'] if side == 'Left' else s['catch']
    caught = smooth((sf - (caught_at - s['catchBlendFrames'])) / s['catchBlendFrames'])
    if caught:
        goal, goal_basis = planted(s, sf, side, True)
        ankle = ankle * (1 - caught) + goal * caught
        frame = qmatrix(slerp(matrix_quat(frame), matrix_quat(goal_basis), caught))
    if sf < caught_at:
        origin, rotation = board_at(s, sf)
        ankle[1] += free_foot_clearance(s['_geometry'], origin, rotation, ankle, frame)
    return ankle, frame


def sample(c, s, sf):
    t = (sf - s['firstSourceFrame']) / s['sourceFps']
    joints, dirs, soles = body_at(c, t)
    recovery = smooth((sf - s['recoveryWindow'][0]) / (s['recoveryWindow'][1] - s['recoveryWindow'][0]))
    if recovery:
        final, final_dirs, _ = body_at(c, (s['recoverySourceFrame'] - s['firstSourceFrame']) / s['sourceFps'])
        for k in joints:
            joints[k] = joints[k] * (1 - recovery) + final[k] * recovery
        for k in dirs:
            dirs[k] = unit(dirs[k] * (1 - recovery) + final_dirs[k] * recovery)
        for side in ('Left', 'Right'):
            joints[side + 'Elbow'] = joints[side + 'Shoulder'] + unit(joints[side + 'Elbow'] - joints[side + 'Shoulder']) * .29
            joints[side + 'Hand'] = joints[side + 'Elbow'] + unit(joints[side + 'Hand'] - joints[side + 'Elbow']) * .27
    pelvis = curve([r[0] for r in s['pelvisKeys']], [r[1:] for r in s['pelvisKeys']], sf)
    delta = pelvis - joints['Pelvis']
    for k in joints:
        joints[k] += delta
    feet = {side: foot(s, sf, side) for side in ('Left', 'Right')}
    lo, hi = -float('inf'), float('inf')
    reach = .87 * s['maximumReachShare']
    for side, (ankle, _) in feet.items():
        hip = joints[side + 'Hip']
        offset = hip - joints['Pelvis']
        horizontal = float(np.sum((hip[[0, 2]] - ankle[[0, 2]]) ** 2))
        if horizontal >= reach * reach:
            raise ValueError('Own Fakie shove foot exceeds fixed-leg horizontal reach at ' + str(sf))
        half = math.sqrt(reach * reach - horizontal)
        lo, hi = max(lo, ankle[1] - offset[1] - half), min(hi, ankle[1] - offset[1] + half)
    if lo > hi:
        raise ValueError('Own Fakie shove has no common fixed-foot root interval at ' + str(sf))
    shift = UP * (float(np.clip(joints['Pelvis'][1], lo, hi)) - joints['Pelvis'][1])
    for k in joints:
        joints[k] += shift
    for side, (ankle, frame) in feet.items():
        hip = joints[side + 'Hip']
        pole = unit(np.asarray(s['kneePlane']['forward']))
        joints[side + 'Knee'] = limb(hip, ankle, .44, .43, pole)
        joints[side + 'Ankle'] = ankle
        joints[side + 'Toe'] = ankle + frame[:, 2] * math.sqrt(.14 ** 2 - .07 ** 2) - frame[:, 1] * .07
        soles[side] = frame[:, 1]
    geometry.head_balance(joints, dirs, s, sf)
    origin, rotation = board_at(s, sf)
    return dict(t=t, sourceFrame=sf,
                sourceVideoTime=s['sourceCacheOriginalOffsetSeconds'] + sf / s['sourceFps'],
                joints={k: joints[k].tolist() for k in JOINTS},
                dirs={k: dirs[k].tolist() for k in DIRS},
                soleUp={k: soles[k].tolist() for k in soles},
                board=dict(origin=origin.tolist(), quaternion=matrix_quat(rotation).tolist()))


def bake(body_raw, controls):
    s = copy.deepcopy(controls)
    if geometry.digest(body_raw) != s['sourceBodySha256']:
        raise ValueError('Own Fakie BS shove body binding differs')
    c = json.loads(body_raw)
    if (c['firstSourceFrame'], c['lastSourceFrame'], c['filmedStance'], c['canonicalStance'], c['reviewed']) != (923, 1006, 'goofy', 'regular', False):
        raise ValueError('Use only the own unreviewed BATB13 black Fakie take')
    if c['sourceVideoSha256'] != s['sourceVideoSha256'] or not c['sourceReflectionAppliedOnce']:
        raise ValueError('Own bounded video / one-time anatomical reflection differs')
    for name, expected in s['geometryCodeSha256'].items():
        if geometry.digest((ROOT / 'scripts' / name).read_bytes()) != expected:
            raise ValueError('Read-only geometry/math helper differs: ' + name)
    graw = (SURFACE / 'constraints.json').read_bytes()
    if geometry.digest(graw) != s['geometrySha256']:
        raise ValueError('Original board/shoe geometry differs')
    g = json.loads(graw)
    surface_raw = (SURFACE / 'own-board-surface.json').read_bytes()
    if geometry.digest(surface_raw) != g['sourceSurfaceSha256']:
        raise ValueError('Original shaped deck surface differs')
    surface = json.loads(surface_raw)
    g['_surfacePoints'], g['_surfaceTriangles'] = np.asarray(surface['points']), np.asarray(surface['triangles'])
    g['authored']['minimumAirSoleClearance'] = s['minimumAirSoleClearance']
    s['_geometry'] = g
    s['_strikePitch'] = geometry.tail_strike_pitch(s)
    ordered = [s[k] for k in ('firstSourceFrame', 'pop', 'frontRelease', 'tailStrike', 'tailRelease', 'pressEnd', 'firstCatch', 'catch', 'land', 'recovery', 'lastSourceFrame')]
    if not all(a < b for a, b in zip(ordered, ordered[1:])):
        raise ValueError('Expected own Right-tail sole release, later free Air and catch order')
    for name in ('yawKeys', 'pitchKeys', 'pelvisKeys', 'frontFreeKeys', 'rearFreeKeys', 'gazeKeys'):
        if not all(a[0] < b[0] for a, b in zip(s[name], s[name][1:])):
            raise ValueError('Own source keys must increase: ' + name)
    times = np.unique(np.round(np.r_[np.arange(s['firstSourceFrame'], s['lastSourceFrame'] + 1e-7, 1 / 6), ordered], 9))
    frames = [sample(c, s, float(sf)) for sf in times]
    if any(qmatrix(f['board']['quaternion'])[1, 1] <= 0 for f in frames):
        raise ValueError('A Fakie BS shove must stay grip-up with no longitudinal flip')
    phases = [(s['firstSourceFrame'], 'load', ['LeftFoot', 'RightFoot']),
              (s['pop'], 'pop', ['LeftFoot', 'RightFoot']),
              (s['frontRelease'], 'pop', ['RightFoot']),
              (s['tailRelease'], 'pop', []), (s['pressEnd'], 'air', []),
              (s['firstCatch'], 'catch', ['LeftFoot']),
              (s['catch'], 'catch', ['LeftFoot', 'RightFoot']),
              (s['land'], 'land', ['LeftFoot', 'RightFoot']),
              (s['recovery'], 'recovery', ['LeftFoot', 'RightFoot'])]
    meta = dict(id=s['id'], movementKind=s['movementKind'], reviewed=False,
                label='Fakie BS Pop Shove-it — own BATB13 body / board-only180 [UNREVIEWED]',
                authored=True, capturedBody=True, canonicalStance='regular', filmedStance='goofy',
                sourceReflectionAppliedOnce=True, nativeSignature=s['nativeSignature'],
                firstSourceFrame=s['firstSourceFrame'], lastSourceFrame=s['lastSourceFrame'], sourceFps=s['sourceFps'],
                sourceUrl=s['sourceUrl'], sourceAuthor=s['sourceAuthor'],
                sourceVideoOffsetSeconds=s['sourceCacheOriginalOffsetSeconds'] + s['firstSourceFrame'] / s['sourceFps'],
                sourceCacheOriginalOffsetSeconds=s['sourceCacheOriginalOffsetSeconds'],
                sourceBodyControlSha256=s['sourceBodySha256'], sourceVideoSha256=s['sourceVideoSha256'],
                fullSourceVideoSha256=None, sourceFrameSpace='Bounded transcoded preview decoder indices, not original full-video indices',
                sourcePoseSha256=c['sourcePoseSha256'], sourceTiming=c['sourceTiming'],
                sourceRawMissingFrames=c['sourceRawMissingFrames'], authoredInterpolationAtMissingFrames=c['authoredInterpolationAtMissingFrames'],
                sourceTailStrikeFrame=s['tailStrike'], referenceGroundPress=s['reference'],
                sourceRootMotion=s['sourceRootMotion'], provenance=s['provenance'], uncertainty=s['uncertainty'],
                sourcePhaseLimit=s['phaseClockLimit'],
                contactLimit='Own authored Right-tail sole/scoop, front foot lifts without flick; independent body-frame free shoes return to reversed deck-local ends after180. No measured3D confidence.',
                combinedWinding=dict(nativeFlip=0, nativeShove=-180, bodySpin=0, canonicalRoll=0, canonicalYaw=180),
                reconstruction=dict(pelvisKeys=s['pelvisKeys'], kneePlane=s['kneePlane'], gazeKeys=s['gazeKeys'], maximumReachShare=s['maximumReachShare']),
                rig=dict(thigh=.44, shin=.43, upperArm=.29, forearm=.27, spine=.52, shoulderRise=.44, hipHalfWidth=.09, shoulderHalfWidth=.18, head=.12, foot=.14, toeDrop=.07))
    duration = (s['lastSourceFrame'] - s['firstSourceFrame']) / s['sourceFps']
    clip = dict(format='skate-capture-source-keyed-fakie-bs-pop-shove/1', metadata=meta,
                stance='regular', duration=duration, fps=s['sourceFps'] * 6, frames=frames,
                events=contact_events([dict(t=(sf - s['firstSourceFrame']) / s['sourceFps'], phase=p, contacts=cs)
                                       for sf, p, cs in phases], duration))
    return clip, encode_pack([clip])


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--output', type=Path, required=True)
    p.add_argument('--controls', type=Path, default=ASSET / 'constraints.json')
    args = p.parse_args()
    clip, packed = bake((ASSET / 'captured-body.json').read_bytes(), json.loads(args.controls.read_text()))
    args.output.mkdir(parents=True, exist_ok=True)
    for name, data in [(clip['metadata']['id'] + '.json', (json.dumps(clip, indent=2) + '\n').encode()), ('skate_captures.skmc', packed)]:
        dest = args.output / name
        temp = dest.with_suffix(dest.suffix + '.tmp')
        temp.write_bytes(data)
        temp.replace(dest)
    print('FAKIE_BS_SHOVE_SOURCE_READY', len(clip['frames']), geometry.digest(packed))


if __name__ == '__main__':
    main()
