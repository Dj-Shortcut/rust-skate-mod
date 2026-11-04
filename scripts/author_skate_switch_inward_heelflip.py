#!/usr/bin/env python3
"""Bake the own BATB12 Switch Inward Heelflip with direct Regular anatomy and its own BS scoop.

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
from author_skate_kickflip import curve, limb, foot_basis
from author_skate_kickflip_hybrid import body_at
from author_skate_treflip import basis, deck_shoe, free_foot_clearance, smooth
from capture_math import (DIRS, JOINTS, UP, contact_events, encode_pack,
                          matrix_quat, qmatrix, slerp, unit)

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'assets/authored/animation/switch-inward-heelflip-keyed'
SURFACE = ROOT / 'assets/authored/animation/treflip-keyed'


def turn(axis, degrees):
    radians = math.radians(degrees)
    c, sn = math.cos(radians), math.sin(radians)
    return {'x': np.array([[1,0,0],[0,c,-sn],[0,sn,c]]),
            'y': np.array([[c,0,sn],[0,1,0],[-sn,0,c]]),
            'z': np.array([[c,-sn,0],[sn,c,0],[0,0,1]])}[axis]


def board_at(s, sf):
    rotation = turn('y', scalar(s['yawKeys'], sf)) @ turn('x', -scalar(s['pitchKeys'], sf)) @ turn('z', scalar(s['rollKeys'], sf))
    floor = -float((np.asarray(s['_geometry']['support']['board']) @ rotation.T)[:, 1].min())
    rise = geometry.reference_height(s, sf)
    free = smooth((sf - s['tailRelease']) / (s['pressEnd'] - s['tailRelease']))
    world_y = floor * (1 - free) + max(floor + .01, .092 + rise) * free
    if sf <= s['pop'] or sf >= s['land']:
        world_y = .092
    # Own board floor/air envelope is expressed relative to one reference carrier.
    # Own hip-relative body has no source translation/jump; never subtract it twice.
    return np.array([0., world_y - rise, 0.]), rotation


def planted(s, sf, side, catch=False):
    origin, rotation = board_at(s, sf)
    p = s['catchFeet' if catch else 'setupFeet'][side]
    return deck_shoe(s['_geometry'], origin, rotation, p['x'], p['z'], p['yaw'])


def heel(s, sf):
    """Own LEFT heel corner at the toe-side nose edge; never a sole substitute."""
    origin, rotation = board_at(s, sf)
    keys = s['heelDeckKeys']
    x, z, yaw, inversion, toes_up = curve([row[0] for row in keys], [row[1:] for row in keys], sf)
    shape = s['_geometry']['shoe']
    kick = max(0., abs(z) - shape['kickStart'])
    level = shape['concave'] * (x / shape['deckHalfWidth']) ** 2 + shape['kickRise'] * (kick / shape['kickSpan']) ** 2
    frame = rotation @ foot_basis(float(yaw), float(inversion), float(toes_up))
    # Reuse only the symmetric original heel geometry. Position/depth/angles
    # follow this take's own heel-led toe-side path, not another Heel motion.
    point = np.array([x, level + shape['soleGap'], z])
    return origin + rotation @ point - frame @ np.asarray(s['heelShoeFeature']), frame


def foot(s, sf, side):
    release = s['heelRelease'] if side == 'Left' else s['tailRelease']
    if side == 'Right' and sf <= release:
        return planted(s, sf, side)
    if side == 'Left' and sf <= s['frontRelease']:
        return planted(s, sf, side)
    if side == 'Left' and sf <= release:
        start, start_frame = planted(s, sf, side)
        upper, upper_frame = heel(s, sf)
        weight = smooth((sf - s['frontRelease']) / (s['heelContactFull'] - s['frontRelease']))
        return start * (1 - weight) + upper * weight, qmatrix(slerp(matrix_quat(start_frame), matrix_quat(upper_frame), weight))
    start, start_basis = heel(s, release) if side == 'Left' else planted(s, release, side)
    keys = s['frontFreeKeys'] if side == 'Left' else s['rearFreeKeys']
    target = curve([row[0] for row in keys], [row[1:] for row in keys], sf)
    yaw = math.radians(s['freeFootYaw'][side])
    free_basis = basis(np.array([math.sin(yaw), 0., math.cos(yaw)]), UP)
    weight = smooth((sf - release) / s['releaseBlendFrames'][side])
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
    pelvis = curve([r[0] for r in s['pelvisWorldKeys']], [r[1:] for r in s['pelvisWorldKeys']], sf)
    # All own pelvis keys are WORLD heights, including the caught/landed
    # absorption. The actual native carrier is added once by the game.
    pelvis[1] -= geometry.reference_height(s, sf)
    delta = pelvis - joints['Pelvis']
    for k in joints:
        joints[k] += delta
    # Source arms follow this own take; hidden/ambiguous depth may receive
    # only explicitly labelled own balance windows, never another trick body.
    for balance in s['balanceWindows']:
        start, full, held, end = balance['window']
        weight = smooth((sf - start) / (full - start)) * (1 - smooth((sf - held) / (end - held)))
        if weight <= 0:
            continue
        facing = unit(np.asarray(dirs['ChestForward']) * [1., 0., 1.])
        left = unit((joints['LeftHip'] - joints['RightHip']) * [1., 0., 1.])
        local = lambda v: unit(facing * v[0] + UP * v[1] + left * v[2])
        side = balance['side']
        shoulder = joints[side + 'Shoulder']
        upper = unit(joints[side + 'Elbow'] - shoulder)
        fore = unit(joints[side + 'Hand'] - joints[side + 'Elbow'])
        # Own high diagonals are deliberately lowered under the owner rule;
        # the partly occluded Left forearm receives explicitly authored depth.
        # Both windows return smoothly to this take's own upright source.
        upper_rows, fore_rows = balance['upperKeys'], balance['foreKeys']
        upper_target = curve([r[0] for r in upper_rows], [r[1:] for r in upper_rows], sf)
        fore_target = curve([r[0] for r in fore_rows], [r[1:] for r in fore_rows], sf)
        elbow = shoulder + unit(upper * (1 - weight) + local(upper_target) * weight) * .29
        joints[side + 'Elbow'] = elbow
        joints[side + 'Hand'] = elbow + unit(fore * (1 - weight) + local(fore_target) * weight) * .27
    feet = {side: foot(s, sf, side) for side in ('Left', 'Right')}
    lo, hi = -float('inf'), float('inf')
    reach = .87 * s['maximumReachShare']
    for side, (ankle, _) in feet.items():
        hip = joints[side + 'Hip']
        offset = hip - joints['Pelvis']
        horizontal = float(np.sum((hip[[0, 2]] - ankle[[0, 2]]) ** 2))
        if horizontal >= reach * reach:
            raise ValueError('Own Switch Inward Heelflip foot exceeds fixed-leg horizontal reach at ' + str(sf))
        half = math.sqrt(reach * reach - horizontal)
        lo, hi = max(lo, ankle[1] - offset[1] - half), min(hi, ankle[1] - offset[1] + half)
    if lo > hi:
        raise ValueError('Own Switch Inward Heelflip has no common fixed-foot root interval at ' + str(sf))
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
                sourceVideoTime=(s['sourceContainerFirstPts'] + min(sf, s['lastGenuineSourceFrame']) * s['sourcePtsStep']) * s['sourceTimeBase'][0] / s['sourceTimeBase'][1],
                sourceSampleAuthored=sf > s['lastGenuineSourceFrame'],
                joints={k: joints[k].tolist() for k in JOINTS},
                dirs={k: dirs[k].tolist() for k in DIRS},
                soleUp={k: soles[k].tolist() for k in soles},
                board=dict(origin=origin.tolist(), quaternion=matrix_quat(rotation).tolist()))


def bake(body_raw, controls):
    s = copy.deepcopy(controls)
    if geometry.digest(body_raw) != s['sourceBodySha256']:
        raise ValueError('Own Switch Inward Heelflip body binding differs')
    c = json.loads(body_raw)
    if (c['firstSourceFrame'], c['lastSourceFrame'], c['filmedStance'], c['canonicalStance'], c['reviewed']) != (16575, 16660, 'regular', 'regular', False):
        raise ValueError('Use only the own unreviewed BATB12 striped-shirt Switch Inward Heelflip take')
    if c['sourceVideoSha256'] != s['sourceVideoSha256'] or c['sourceReflectionAppliedOnce'] or c.get('sourceReflectionCount') != 0:
        raise ValueError('Own video / zero-reflection Regular anatomical binding differs')
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
    ordered = [s[k] for k in ('firstSourceFrame', 'pop', 'frontRelease', 'heelContactFull', 'tailStrike', 'tailRelease', 'heelRelease', 'pressEnd', 'firstCatch', 'catch', 'land', 'recovery', 'lastSourceFrame')]
    if not all(a < b for a, b in zip(ordered, ordered[1:])):
        raise ValueError('Expected own Right-tail sole release, later free Air and catch order')
    for name in ('yawKeys', 'pitchKeys', 'rollKeys', 'pelvisWorldKeys', 'frontFreeKeys', 'rearFreeKeys', 'gazeKeys'):
        if not all(a[0] < b[0] for a, b in zip(s[name], s[name][1:])):
            raise ValueError('Own source keys must increase: ' + name)
    times = np.unique(np.round(np.r_[s['firstSourceFrame'] + np.arange(round((s['lastSourceFrame'] - s['firstSourceFrame']) * 6) + 1) / 6, ordered], 9))
    frames = [sample(c, s, float(sf)) for sf in times]
    if abs(s['rollKeys'][-1][1] + 360) > .01 or abs(s['yawKeys'][-1][1] - 180) > .01:
        raise ValueError('Own Inward needs its complete -360 heel roll / +180 BS yaw')
    phases = [(s['firstSourceFrame'], 'load', ['LeftFoot', 'RightFoot']),
              (s['pop'], 'pop', ['LeftFoot', 'RightFoot']),
              (s['frontRelease'], 'pop', ['RightFoot']),
              (s['tailRelease'], 'pop', []), (s['pressEnd'], 'air', []),
              (s['firstCatch'], 'catch', ['LeftFoot']),
              (s['catch'], 'catch', ['LeftFoot', 'RightFoot']),
              (s['land'], 'land', ['LeftFoot', 'RightFoot']),
              (s['recovery'], 'recovery', ['LeftFoot', 'RightFoot'])]
    meta = dict(id=s['id'], movementKind=s['movementKind'], reviewed=False,
                label='Switch Inward Heelflip — own BATB12 body / front HEEL / BS scoop / full board winding [UNREVIEWED]',
                authored=True, capturedBody=True, canonicalStance='regular', filmedStance='regular',
                sourceReflectionAppliedOnce=False, sourceReflectionCount=0, nativeSignature=s['nativeSignature'],
                firstSourceFrame=s['firstSourceFrame'], lastSourceFrame=s['lastSourceFrame'], lastGenuineSourceFrame=s['lastGenuineSourceFrame'], sourceFps=s['sourceFps'],
                sourceRawFrameCount=c['sourceRawFrameCount'], sourceSelectedFrameCount=c['sourceSelectedFrameCount'], authoredRecoverySuffix=False, authoredSourceSuffix=False,
                sourceUrl=s['sourceUrl'], sourceAuthor=s['sourceAuthor'],
                sourceVideoOffsetSeconds=s['sourceOriginalRequestedOffsetSeconds'] + (s['sourceContainerFirstPts'] + s['firstSourceFrame'] * s['sourcePtsStep']) / s['sourceTimeBase'][1],
                sourceCacheOriginalOffsetSeconds=s['sourceOriginalRequestedOffsetSeconds'], sourceVideoCacheOffsetSeconds=(s['sourceContainerFirstPts'] + s['firstSourceFrame'] * s['sourcePtsStep']) / s['sourceTimeBase'][1],
                sourceBodyControlSha256=s['sourceBodySha256'], sourceVideoSha256=s['sourceVideoSha256'],
                fullSourceVideoSha256=s['sourceVideoSha256'], sourceFrameSpace='Cached full Pq decoder16575–16660, sourcePTS=frame*1001,timebase1/30000, firstPTS0. Genuine upright16657 before later celebration; own stabilization stays inside this take.',
                sourcePoseSha256=c['sourcePoseSha256'], sourceTiming=c['sourceTiming'],
                sourceRawMissingFrames=c['sourceRawMissingFrames'], rejectedAnatomicalAssociationFrames=c['rejectedAnatomicalAssociationFrames'], authoredInterpolationAtMissingFrames=c['authoredInterpolationAtMissingFrames'], lowerChainReconstructedFrames=c['lowerChainReconstructedFrames'],
                sourceTailStrikeFrame=s['tailStrike'], referenceGroundPress=s['reference'],
                sourceRootMotion=s['sourceRootMotion'], provenance=s['provenance'], uncertainty=s['uncertainty'],
                sourcePhaseLimit=s['phaseClockLimit'],
                contactLimit='Own Right-tail sole BS scoop and Left HEEL-corner toe-side flick, not a sole or outer-instep contact. Left sole releases before heel guide; both feet free before laterAir. Deck-local catch ends AND toe headings reverse after complete -360heelroll/+180yaw; anatomical feet remain the same. Timing/depth/angles are authored estimates, not measured3D.',
                combinedWinding=dict(nativeFlip=-360, nativeShove=-180, bodySpin=0, canonicalRoll=-360, canonicalYaw=180),
                reconstruction=dict(pelvisWorldKeys=s['pelvisWorldKeys'], kneePlane=s['kneePlane'], gazeKeys=s['gazeKeys'], balanceWindows=s['balanceWindows'], authoredRecoveryStabilization=s['recoveryWindow'], ownUprightReferenceFrame=s['recoverySourceFrame'], maximumReachShare=s['maximumReachShare']),
                rig=dict(thigh=.44, shin=.43, upperArm=.29, forearm=.27, spine=.52, shoulderRise=.44, hipHalfWidth=.09, shoulderHalfWidth=.18, head=.12, foot=.14, toeDrop=.07))
    duration = (s['lastSourceFrame'] - s['firstSourceFrame']) / s['sourceFps']
    clip = dict(format='skate-capture-source-keyed-switch-inward-heelflip/1', metadata=meta,
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
    print('SWITCH_INWARD_HEELFLIP_SOURCE_READY', len(clip['frames']), geometry.digest(packed))


if __name__ == '__main__':
    main()
