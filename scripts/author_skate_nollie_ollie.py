#!/usr/bin/env python3
"""Bake the own normal P4C Nollie with nose pressure and a rear instep guide.

Published inputs are compact fixed-rig controls, not footage or raw detector
output. The native physical carrier supplies the jump once at playback.
"""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from author_skate_kickflip import curve, limb
from author_skate_kickflip_hybrid import body_at
from author_skate_treflip import basis, deck_shoe, free_foot_clearance, smooth
from capture_math import (DIRS, JOINTS, UP, contact_events, encode_pack,
                          euler_quat, matrix_quat, qmatrix, slerp, unit)
from capture_retarget import transport_leg_pole

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'assets/authored/animation/nollie-ollie-keyed'
GEOMETRY = ROOT / 'assets/authored/animation/treflip-keyed'


def digest(data):
    return hashlib.sha256(data).hexdigest()


def scalar(keys, sf):
    return float(curve([r[0] for r in keys], [[r[1]] for r in keys], sf)[0])


def reference_height(s, sf):
    fraction = float(np.clip((sf-s['pop'])/(s['land']-s['pop']), 0, 1))
    return .5*s['reference']['gravity']*s['reference']['flightSeconds']**2*fraction*(1-fraction)


def nose_strike_pitch(s):
    geometry = s['_geometry']
    tail = np.asarray(geometry['support']['tail'])*[1, 1, -1]
    wheels = np.asarray(geometry['support']['wheel'])
    lo, hi = 0., 75.
    for _ in range(45):
        angle = (lo+hi)/2
        rotation = qmatrix(euler_quat([angle, 0, 0]))
        gap = (tail@rotation.T)[:, 1].min()-(wheels@rotation.T)[:, 1].min()+s['reference']['wheelClearance']
        if gap > 0:
            lo = angle
        else:
            hi = angle
    return (lo+hi)/2


def board_at(s, sf):
    # +X presses the +Z nose down. No roll, source yaw or body spin.
    pitch = scalar(s['pitchKeys'], sf)*s['_strikePitch']/46.
    roll = 0.
    rotation = qmatrix(euler_quat([pitch, 0, 0])) @ qmatrix(euler_quat([0, 0, roll]))
    floor = -float((np.asarray(s['_geometry']['support']['board'])@rotation.T)[:, 1].min())
    height = reference_height(s, sf)
    free = smooth((sf-s['noseRelease'])/(s['pressEnd']-s['noseRelease']))
    world_y = max(floor, floor*(1-free)+(.092+height)*free)
    if sf <= s['pop'] or sf >= s['land']:
        world_y = .092
    return np.array([0., world_y-height, 0.]), rotation


def planted(s, sf, side, catch=False):
    origin, rotation = board_at(s, sf)
    target = s['catchFeet' if catch else 'setupFeet'][side]
    return deck_shoe(s['_geometry'], origin, rotation, target['x'], target['z'], target['yaw'])


def guide_contact(s, sf):
    patch = s['guide']
    origin, rotation = board_at(s, sf)
    x, z = curve([r[0] for r in patch['deckKeys']], [r[1:] for r in patch['deckKeys']], sf)
    shoe = s['_geometry']['shoe']
    y = shoe['kickRise']*(max(0., abs(z)-shoe['kickStart'])/shoe['kickSpan'])**2 + shoe['concave']*(x/shoe['deckHalfWidth'])**2
    normal = unit(np.array([-2*shoe['concave']*x/shoe['deckHalfWidth']**2, 1.,
        -2*shoe['kickRise']*max(0., abs(z)-shoe['kickStart'])/shoe['kickSpan']**2*np.sign(z)]))
    contact_normal = -rotation@normal
    yaw = math.radians(patch['toeHeadingDegrees'])
    toe = rotation@np.array([math.sin(yaw), 0., math.cos(yaw)])
    tangent = unit(toe-contact_normal*np.dot(toe, contact_normal))
    local_normal = unit(np.array(patch['instepSurfaceNormalLocal']))
    local_tangent = unit(np.array([0., 0., 1.])-local_normal*local_normal[2])
    frame = (np.column_stack((contact_normal, tangent, unit(np.cross(contact_normal, tangent)))) @
             np.column_stack((local_normal, local_tangent, unit(np.cross(local_normal, local_tangent)))).T)
    ankle = origin+rotation@np.array([x, y, z])-frame@np.array(patch['shoeLocal'])
    attached, old_frame = planted(s, sf, 'Right')
    weight = smooth((sf-patch['start'])/(patch['full']-patch['start']))
    return (attached*(1-weight)+ankle*weight,
            qmatrix(slerp(matrix_quat(old_frame), matrix_quat(frame), weight)))


def foot(s, sf, side):
    patch = s['guide']
    release = patch['release'] if side == 'Right' else s['noseRelease']
    if sf <= release:
        return guide_contact(s, sf) if side == 'Right' else planted(s, sf, side)
    start, old_frame = guide_contact(s, release) if side == 'Right' else planted(s, release, side)
    keys = s['frontFreeKeys' if side == 'Left' else 'rearFreeKeys']
    target = curve([r[0] for r in keys], [r[1:] for r in keys], sf)
    yaw = math.radians(s['catchFeet'][side]['yaw'])
    free_frame = basis(np.array([math.sin(yaw), 0., math.cos(yaw)]), UP)
    weight = smooth((sf-release)/(patch['end']-release if side == 'Right' else 2.))
    ankle = start*(1-weight)+target*weight
    frame = qmatrix(slerp(matrix_quat(old_frame), matrix_quat(free_frame), weight))
    caught_at = s['catch']
    caught = smooth((sf-(caught_at-2))/2.)
    origin, rotation = board_at(s, sf)
    if caught:
        goal, goal_frame = planted(s, sf, side, catch=True)
        ankle = ankle*(1-caught)+goal*caught
        frame = qmatrix(slerp(matrix_quat(frame), matrix_quat(goal_frame), caught))
    if sf < caught_at:
        ankle[1] += free_foot_clearance(s['_geometry'], origin, rotation, ankle, frame)
    return ankle, frame


def head_balance(joints, dirs, s, sf):
    rows = s['gazeKeys']
    gaze = unit(curve([r[0] for r in rows], [r[1:] for r in rows], sf))
    head_up = unit(UP-gaze*np.dot(UP, gaze))
    joints['Head'] = joints['Neck']+head_up*.12
    dirs['HeadForward'] = gaze


def sample(c, s, sf):
    t = (sf-s['firstSourceFrame'])/s['sourceFps']
    joints, dirs, soles = body_at(c, t)
    recovery = smooth((sf-s['recoveryWindow'][0])/(s['recoveryWindow'][1]-s['recoveryWindow'][0]))
    if recovery:
        # Retain the same attempt's final settling frame, suppressing monocular
        # ankle/knee depth wobble after its visibly stable landed roll-away.
        final, final_dirs, _ = body_at(c, (s['recoverySourceFrame']-s['firstSourceFrame'])/s['sourceFps'])
        for key in joints:
            joints[key] = joints[key]*(1-recovery)+final[key]*recovery
        for key in dirs:
            dirs[key] = unit(dirs[key]*(1-recovery)+final_dirs[key]*recovery)
        # Re-establish fixed chains after the source pose interpolation.
        for side in ('Left', 'Right'):
            joints[side+'Elbow'] = joints[side+'Shoulder']+unit(joints[side+'Elbow']-joints[side+'Shoulder'])*.29
            joints[side+'Hand'] = joints[side+'Elbow']+unit(joints[side+'Hand']-joints[side+'Elbow'])*.27
    target = curve([r[0] for r in s['pelvisKeys']], [r[1:] for r in s['pelvisKeys']], sf)
    delta = target-joints['Pelvis']
    for key in joints:
        joints[key] += delta
    original = {key: value.copy() for key, value in joints.items()}
    feet = {side: foot(s, sf, side) for side in ('Left', 'Right')}
    # Fixed shoe guides remain unchanged. Project one coherent root height into
    # the common canonical-leg reach interval, with the existing .998 reserve.
    lo, hi = -float('inf'), float('inf')
    reach = .87*s['maximumReachShare']
    for side, (ankle, _) in feet.items():
        hip = joints[side+'Hip']
        offset = hip-joints['Pelvis']
        horizontal = float(np.sum((hip[[0, 2]]-ankle[[0, 2]])**2))
        if horizontal >= reach*reach:
            raise ValueError('Own Ollie foot has no horizontal canonical reach at '+str(sf))
        half = math.sqrt(reach*reach-horizontal)
        lo, hi = max(lo, ankle[1]-offset[1]-half), min(hi, ankle[1]-offset[1]+half)
    if lo > hi:
        raise ValueError('Own Ollie has no common fixed-foot root interval at '+str(sf))
    shift = UP*(float(np.clip(joints['Pelvis'][1], lo, hi))-joints['Pelvis'][1])
    for key in joints:
        joints[key] += shift
    for side, (ankle, frame) in feet.items():
        hip = joints[side+'Hip']
        pole = transport_leg_pole(original[side+'Hip'], original[side+'Knee'], original[side+'Ankle'], hip, ankle)
        if recovery:
            toe = unit(frame[:, 2]*[1, 0, 1])
            pole = unit(unit(pole)*(1-recovery)+toe*recovery)
        joints[side+'Knee'] = limb(hip, ankle, .44, .43, pole)
        joints[side+'Ankle'] = ankle
        joints[side+'Toe'] = ankle+frame[:, 2]*math.sqrt(.14**2-.07**2)-frame[:, 1]*.07
        soles[side] = frame[:, 1]
    head_balance(joints, dirs, s, sf)
    origin, rotation = board_at(s, sf)
    return dict(t=t, sourceFrame=sf, sourceVideoTime=s['sourceCacheOriginalOffsetSeconds']+sf/s['sourceFps'],
                joints={k: joints[k].tolist() for k in JOINTS}, dirs={k: dirs[k].tolist() for k in DIRS},
                soleUp={k: soles[k].tolist() for k in soles},
                board=dict(origin=origin.tolist(), quaternion=matrix_quat(rotation).tolist()))


def bake(body_raw, controls):
    s = copy.deepcopy(controls)
    if digest(body_raw) != s['sourceBodySha256']:
        raise ValueError('The pinned own normal Nollie body controls differ')
    c = json.loads(body_raw)
    if (c['firstSourceFrame'], c['lastSourceFrame'], c['filmedStance'], c['reviewed']) != (95, 175, 'regular', False):
        raise ValueError('Use only the own unreviewed P4C bounded95..175 normal Nollie')
    if c['sourceVideoSha256'] != s['sourceVideoSha256']:
        raise ValueError('Own bounded Ollie video binding differs')
    geometry_raw = (GEOMETRY/'constraints.json').read_bytes()
    if digest(geometry_raw) != s['geometrySha256']:
        raise ValueError('Own board/shoe geometry differs')
    geometry = json.loads(geometry_raw)
    surface_raw = (GEOMETRY/'own-board-surface.json').read_bytes()
    if digest(surface_raw) != geometry['sourceSurfaceSha256']:
        raise ValueError('Own shaped board surface differs')
    surface = json.loads(surface_raw)
    geometry['_surfacePoints'] = np.asarray(surface['points'])
    geometry['_surfaceTriangles'] = np.asarray(surface['triangles'])
    geometry['authored']['minimumAirSoleClearance'] = s['minimumAirSoleClearance']
    s['_geometry'] = geometry
    s['_strikePitch'] = nose_strike_pitch(s)
    ordered = [s[k] for k in ('firstSourceFrame', 'pop', 'guideSoleRelease', 'noseStrike', 'noseRelease', 'pressEnd', 'catch', 'land', 'recovery', 'lastSourceFrame')]
    if not all(a < b for a, b in zip(ordered, ordered[1:])):
        raise ValueError('Expected own nose-pressure/rear-guide/free-Air/catch/landing order')
    phases = [(s['firstSourceFrame'], 'load', ['LeftFoot', 'RightFoot']),
              (s['pop'], 'pop', ['LeftFoot', 'RightFoot']),
              (s['guideSoleRelease'], 'pop', ['LeftFoot']),
              (s['noseRelease'], 'pop', []), (s['pressEnd'], 'air', []),
              (s['catch'], 'catch', ['LeftFoot', 'RightFoot']),
              (s['land'], 'land', ['LeftFoot', 'RightFoot']),
              (s['recovery'], 'recovery', ['LeftFoot', 'RightFoot'])]
    times = np.unique(np.round(np.r_[np.arange(s['firstSourceFrame'], s['lastSourceFrame']+1e-7, 1/6), ordered, s['guide']['full'], s['guide']['release']], 9))
    frames = [sample(c, s, float(sf)) for sf in times]
    metadata = dict(id=s['id'], movementKind=s['movementKind'], reviewed=False,
        label='Nollie — own P4C / reconstructed depth, contact and gap142 [UNREVIEWED]',
        authored=True, capturedBody=True, canonicalStance='regular', capturedStance='regular', filmedStance='regular',
        firstSourceFrame=s['firstSourceFrame'], lastSourceFrame=s['lastSourceFrame'], sourceFps=s['sourceFps'],
        sourceUrl=s['sourceUrl'], sourceAuthor=s['sourceAuthor'],
        sourceVideoOffsetSeconds=s['sourceCacheOriginalOffsetSeconds']+s['firstSourceFrame']/s['sourceFps'],
        sourceCacheOriginalOffsetSeconds=s['sourceCacheOriginalOffsetSeconds'],
        sourceBodyControlSha256=s['sourceBodySha256'], sourceVideoSha256=s['sourceVideoSha256'],
        fullSourceVideoSha256=None, sourceFrameSpace='Bounded64..78s transcoded preview decoder frames, NOT original global frames',
        sourcePoseSha256=c['sourcePoseSha256'], sourceTiming=c['sourceTiming'], sourceRootMotion=s['sourceRootMotion'],
        sourceIdentitySelectedFrames=80, sourceIdentityTotalFrames=81,
        authoredMissingActorControlFrames=c['authoredMissingActorControlFrames'],
        authoredLowerBodyDepthControlFrames=c['authoredLowerBodyDepthControlFrames'],
        sourceIdentityLimit=c['confidenceLimit'],
        provenance=s['provenance'], uncertainty=s['uncertainty'], sourcePhaseLimit=s['phaseClockLimit'],
        contactLimit='Authored Left nose sole and Right tailward outer-upper griptape guide against original deck/shoe; no measured3D contact confidence.',
        sourceNoseStrikeFrame=s['noseStrike'], referenceGroundPress=s['reference'],
        combinedWinding=dict(nativeFlip=0, nativeShove=0, bodySpin=0, canonicalRoll=0, canonicalYaw=0),
        reconstruction=dict(pelvisKeys=s['pelvisKeys'], recoveryWindow=s['recoveryWindow'], guide=s['guide'], maximumReachShare=s['maximumReachShare'], gazeKeys=s['gazeKeys']),
        rig=dict(thigh=.44, shin=.43, upperArm=.29, forearm=.27, spine=.52, shoulderRise=.44, hipHalfWidth=.09, shoulderHalfWidth=.18, head=.12, foot=.14, toeDrop=.07))
    duration = (s['lastSourceFrame']-s['firstSourceFrame'])/s['sourceFps']
    clip = dict(format='skate-capture-source-keyed-nollie-ollie/1', metadata=metadata, stance='regular',
                duration=duration, fps=s['sourceFps']*6, frames=frames,
                events=contact_events([dict(t=(sf-s['firstSourceFrame'])/s['sourceFps'], phase=p, contacts=c)
                                       for sf, p, c in phases], duration))
    return clip, encode_pack([clip])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--controls', type=Path, default=ASSET/'constraints.json')
    args = parser.parse_args()
    if args.output.resolve() == ASSET.resolve():
        raise ValueError('Generated output must not replace committed source controls')
    clip, packed = bake((ASSET/'captured-body.json').read_bytes(), json.loads(args.controls.read_text()))
    args.output.mkdir(parents=True, exist_ok=True)
    for name, data in [(clip['metadata']['id']+'.json', (json.dumps(clip, indent=2)+'\n').encode()), ('skate_captures.skmc', packed)]:
        target = args.output/name
        temporary = target.with_suffix(target.suffix+'.tmp')
        temporary.write_bytes(data)
        temporary.replace(target)
    print('NOLLIE_OLLIE_SOURCE_READY', len(clip['frames']), digest(packed))


if __name__ == '__main__':
    main()
