#!/usr/bin/env python3
"""Author a full frontside360 shove from its SHA-bound shared FS180 body.

The reference is explicitly FS180, not filmed FS360. No native physics authority.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from author_skate_bs_pop_shove import source_body
from author_skate_kickflip import curve, limb
from author_skate_treflip import (basis, deck_shoe, entry_foot,
                                 free_foot_clearance, smooth)
from capture_math import (JOINTS, DIRS, UP, contact_events, encode_pack,
                          euler_quat, matrix_quat, qmatrix, slerp, unit)

ROOT = Path(__file__).resolve().parents[1]
CONTROLS = ROOT / 'assets/authored/animation/fs360-shove-keyed/constraints.json'
GEOMETRY = ROOT / 'assets/authored/animation/treflip-keyed'
sha = lambda data: hashlib.sha256(data).hexdigest()


def scalar(keys, sf):
    return float(curve([v[0] for v in keys], [[v[1]] for v in keys], sf)[0])


def board_at(s, sf):
    # Canonical frontside is negative yaw. No longitudinal flip or body spin.
    yaw, pitch = scalar(s['yawKeys'], sf), scalar(s['pitchKeys'], sf)
    rotation = qmatrix(euler_quat([0, yaw, 0])) @ qmatrix(euler_quat([-pitch, 0, 0]))
    fraction = float(np.clip((sf-s['pop'])/(s['land']-s['pop']), 0, 1))
    height = .5*8*.6**2*fraction*(1-fraction)
    floor = -float((np.asarray(s['_geometry']['support']['board']) @ rotation.T)[:, 1].min())
    press = 1-smooth((sf-s['rearRelease'])/(s['pressEnd']-s['rearRelease']))
    world_y = max(floor, floor*press+(.092+height)*(1-press))
    if sf <= s['pop'] or sf >= s['land']:
        world_y = .092
    return np.array([0., world_y-height, 0.]), rotation


def attached(s, sf, side):
    origin, rotation = board_at(s, sf)
    geometry = s['_geometry']
    # Neutral measured tail/mid charge is the entry; the FS rear pressure
    # shifts toward the heel-side pocket during the source pop, not in Load.
    weight = smooth((sf-s['pop'])/(s['rearRelease']-s['pop']))
    x, z, yaw = (-.075, -.32, 85) if side == 'Right' else (-.04, .06, 65)
    return entry_foot(geometry, side, origin, rotation, weight, x, z, yaw)


def catch_frame(s, origin, rotation, side):
    # Transform the anatomical catch footprint and toe heading into the final
    # deck basis. Its zero Y datum leaves ankle height to the shaped-deck fit.
    final = qmatrix(euler_quat([0, -360, 0]))
    target = s['catchFeet'][side]
    world = np.asarray(target['xyz'])
    angle = math.radians(target['toeHeadingDegrees'])
    local, forward = final.T@world, final.T@np.array([math.sin(angle), 0, math.cos(angle)])
    return deck_shoe(s['_geometry'], origin, rotation, float(local[0]), float(local[2]),
                     math.degrees(math.atan2(forward[0], forward[2])))


def foot(s, sf, side):
    release = s['frontRelease'] if side == 'Left' else s['rearRelease']
    if sf <= release:
        return attached(s, sf, side)
    start, start_basis = attached(s, release, side)
    keys = s['frontFreeKeys'] if side == 'Left' else s['rearFreeKeys']
    target = curve([r[0] for r in keys], [r[1:] for r in keys], sf)+UP*.092
    yaw = math.radians(s['catchFeet'][side]['toeHeadingDegrees'])
    free_basis = basis(np.array([math.sin(yaw), 0, math.cos(yaw)]), UP)
    weight = smooth((sf-release)/2)
    ankle = start*(1-weight)+target*weight
    frame = qmatrix(slerp(matrix_quat(start_basis), matrix_quat(free_basis), weight))
    origin, rotation = board_at(s, sf)
    catch = s['catchFeet'][side]['sourceFrame']
    caught = smooth((sf-s['catchStart'])/(catch-s['catchStart']))
    if caught:
        goal, goal_basis = catch_frame(s, origin, rotation, side)
        ankle = ankle*(1-caught)+goal*caught
        frame = qmatrix(slerp(matrix_quat(frame), matrix_quat(goal_basis), caught))
    if sf < catch:
        ankle[1] += free_foot_clearance(s['_geometry'], origin, rotation, ankle, frame)
    return ankle, frame


def sample(source, s, sf):
    # This generic measured-chain reconstruction reads only the FS source;
    # none of the BS source body, arm or foot controls is imported.
    joints, dirs, soles = source_body(source, sf)
    rig = source['metadata']['rig']
    original = {k: value.copy() for k, value in joints.items()}
    feet = {side: foot(s, sf, side) for side in ('Left', 'Right')}
    begin, end = s['recoveryKneeWindow']
    recovery = smooth((sf-begin)/(end-begin))
    if recovery > 0:
        # Balance the recovery over real sole footprints, not ankle points.
        shoe = s['_geometry']['shoe']
        rows = shoe['soleRows']
        outline = np.array([[w, z] for z, w in rows]+[[-w, z] for z, w in reversed(rows)])
        following = np.roll(outline, -1, axis=0)
        cross = outline[:, 0]*following[:, 1]-following[:, 0]*outline[:, 1]
        centroid = ((outline+following)*cross[:, None]).sum(axis=0)/(3*cross.sum())
        local = np.array([centroid[0], shoe['soleLocalY'], centroid[1]])
        centres = [ankle+frame@local for ankle, frame in feet.values()]
        toe = unit(sum((frame[:, 2] for _, frame in feet.values()), np.zeros(3))*[1, 0, 1])
        target = np.mean(centres, axis=0)-toe*rig['hipHalfWidth']
        shift = (target-joints['Pelvis'])*[recovery, 0, recovery]
        for key in joints:
            joints[key] += shift
    lo, hi = -float('inf'), float('inf')
    reach = (rig['thigh']+rig['shin'])*s['maximumReachShare']
    for side, (ankle, _) in feet.items():
        offset = joints[side+'Hip']-joints['Pelvis']
        horizontal = float(np.sum((joints[side+'Hip'][[0, 2]]-ankle[[0, 2]])**2))
        if horizontal >= reach*reach:
            raise ValueError('FS360 foot exceeds measured horizontal reach at '+str(sf))
        half = math.sqrt(reach*reach-horizontal)
        lo, hi = max(lo, ankle[1]-offset[1]-half), min(hi, ankle[1]-offset[1]+half)
    if lo > hi:
        raise ValueError('FS360 has no common fixed-foot root interval at '+str(sf))
    delta = UP*(float(np.clip(joints['Pelvis'][1], lo, hi))-joints['Pelvis'][1])
    for key in joints:
        joints[key] += delta
    for side, (ankle, frame) in feet.items():
        hip, old_hip, old_end = joints[side+'Hip'], original[side+'Hip'], original[side+'Ankle']
        axis = old_end-old_hip
        pole = original[side+'Knee']-old_hip-axis*np.dot(original[side+'Knee']-old_hip, axis)/np.dot(axis, axis)
        if side == 'Right':
            # The FS recovery witness keeps the rear shin steep, rather than
            # kneeling tailward. Pre-Land chains and all foot targets stay unchanged.
            if recovery > 0:
                pole = unit(pole)*(1-recovery)+unit(frame[:, 2])*recovery
        joints[side+'Knee'] = limb(hip, ankle, rig['thigh'], rig['shin'], pole)
        joints[side+'Ankle'] = ankle
        joints[side+'Toe'] = ankle+frame[:, 2]*math.sqrt(rig['foot']**2-rig['toeDrop']**2)-frame[:, 1]*rig['toeDrop']
        soles[side] = frame[:, 1]
    origin, rotation = board_at(s, sf)
    return dict(t=(sf-s['firstSourceFrame'])/s['sourceFps'], sourceFrame=sf,
                joints={k: joints[k].tolist() for k in JOINTS},
                dirs={k: dirs[k].tolist() for k in DIRS},
                soleUp={k: soles[k].tolist() for k in soles},
                board=dict(origin=origin.tolist(), quaternion=matrix_quat(rotation).tolist()))


def bake(raw, s):
    if sha(raw) != s['sourceJsonSha256']:
        raise ValueError('Pinned FS source reconstruction differs')
    helper = ROOT/'scripts/author_skate_bs_pop_shove.py'
    if sha(helper.read_bytes()) != s['retargetHelperSha256']:
        raise ValueError('Measured-chain helper differs')
    source = json.loads(raw)
    if source['metadata']['id'] != s['sourceId'] or source['metadata']['reviewed'] is not False:
        raise ValueError('The exact unreviewed FS180 source is required')
    if s['frontRelease'] != s['pop']:
        raise ValueError('The FS360 front release must coincide with its Pop event')
    for side in ('Left', 'Right'):
        target = s['catchFeet'][side]
        if target['sourceFrame'] != s['catch'] or target['xyz'][1] != 0:
            raise ValueError('FS360 feet must share the catch event and fitted deck-plane datum')
    geometry_raw = (GEOMETRY/'constraints.json').read_bytes()
    if sha(geometry_raw) != s['geometrySha256']:
        raise ValueError('Own shoe and entry geometry differs')
    geometry = json.loads(geometry_raw)
    surface_raw = (GEOMETRY/'own-board-surface.json').read_bytes()
    if sha(surface_raw) != geometry['sourceSurfaceSha256']:
        raise ValueError('Own shaped deck differs')
    surface = json.loads(surface_raw)
    geometry['_surfacePoints'], geometry['_surfaceTriangles'] = np.asarray(surface['points']), np.asarray(surface['triangles'])
    geometry['authored']['minimumAirSoleClearance'] = .025
    s['_geometry'] = geometry
    phases = [(s['firstSourceFrame'], 'load', ['LeftFoot', 'RightFoot']),
              (s['pop'], 'pop', ['RightFoot']), (s['rearRelease'], 'air', []),
              (s['pressEnd'], 'air', []), (s['catch'], 'catch', ['LeftFoot', 'RightFoot']),
              (s['land'], 'land', ['LeftFoot', 'RightFoot']), (s['recovery'], 'recovery', ['LeftFoot', 'RightFoot'])]
    times = np.unique(np.round(np.r_[np.arange(s['firstSourceFrame'], s['lastSourceFrame']+1e-7, 1/6),
                                      [r[0] for r in phases], s['rotationEnd'], s['catchStart']], 9))
    frames = [sample(source, s, float(sf)) for sf in times]
    duration = (s['lastSourceFrame']-s['firstSourceFrame'])/s['sourceFps']
    metadata = dict(id=s['id'], movementKind=s['movementKind'],
                    label='Frontside360 Shove-it — authored shared-FS180 derivation [UNREVIEWED]',
                    reviewed=False, authored=True, capturedBody=True,
                    sourceUrl=source['metadata']['sourceUrl'], sourceAuthor=source['metadata']['sourceAuthor'],
                    firstSourceFrame=s['firstSourceFrame'], lastSourceFrame=s['lastSourceFrame'], sourceFps=s['sourceFps'],
                    sharedSourceId=s['sourceId'], sharedSourceJsonSha256=s['sourceJsonSha256'],
                    referenceFootageMovementKind='fs-pop-shove-it', filmedFs360Take=False,
                    provenance=s['provenance'], combinedWinding=dict(nativeFlip=0, nativeShove=360, canonicalYaw=-360, bodySpin=0),
                    sourceRootMotion='Source carrier translation removed; actual live carrier added once',
                    referenceGroundPress=dict(flightSeconds=.6, gravity=8),
                    catchTransform='Complete world position and toe direction transformed by final360 deck basis')
    clip = dict(format='skate-capture-authored-fs360/1', metadata=metadata, stance='regular',
                fps=s['sourceFps']*6, duration=duration,
                events=contact_events([dict(t=(sf-s['firstSourceFrame'])/s['sourceFps'], phase=p, contacts=c)
                                       for sf, p, c in phases], duration), frames=frames)
    return clip, encode_pack([clip])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, required=True)
    parser.add_argument('--controls', type=Path, default=CONTROLS)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.input.resolve().parent == args.output.resolve():
        raise ValueError('Preserve the original source directory')
    clip, packed = bake(args.input.read_bytes(), json.loads(args.controls.read_text()))
    payloads = [('fs-360-shove-it-source-keyed-v1.json', (json.dumps(clip, indent=2)+'\n').encode()),
                ('skate_captures.skmc', packed)]
    args.output.mkdir(parents=True, exist_ok=True)
    temporary = []
    try:
        for name, data in payloads:
            target = args.output/name
            tmp = target.with_suffix(target.suffix+'.tmp')
            tmp.write_bytes(data)
            temporary.append((tmp, target))
        for tmp, target in temporary:
            tmp.replace(target)
    finally:
        for tmp, _ in temporary:
            if tmp.exists():
                tmp.unlink()
    print('FS360_DERIVED_SOURCE_READY', len(clip['frames']), sha(packed))


if __name__ == '__main__':
    main()
