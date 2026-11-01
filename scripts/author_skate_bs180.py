#!/usr/bin/env python3
"""Bake the own Wes plain BS180 reference, with one live-carrier body turn.

The source is a genuine non-flipping BS180, not a shove or flip combination.
Compact anatomy, monocular depth, cropped head/hands and contacts are authored.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from author_skate_kickflip import curve, limb
from author_skate_treflip import basis, deck_shoe, smooth, strike_pitch
from capture_math import JOINTS, DIRS, UP, unit, matrix_quat, qmatrix, slerp, encode_pack, contact_events

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'assets/authored/animation/bs180-keyed'
GEOMETRY = ROOT / 'assets/authored/animation/treflip-keyed'
sha = lambda data: hashlib.sha256(data).hexdigest()


def scalar(keys, frame):
    return float(curve([x[0] for x in keys], [[x[1]] for x in keys], frame)[0])


def vector(keys, frame):
    return curve([x[0] for x in keys], [x[1:] for x in keys], frame)


def turn_y(degrees):
    a = math.radians(degrees)
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])


def turn_x(degrees):
    a = math.radians(degrees)
    c, s = math.cos(a), math.sin(a)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])


def orientation(a, b, weight):
    return qmatrix(slerp(matrix_quat(a), matrix_quat(b), weight))


def board(s, sf):
    yaw = turn_y(scalar(s['sourceYawKeys'], sf))
    pitch = (scalar(s['earlyPitchKeys'], sf) if 'earlyPitchKeys' in s and sf < 54
             else scalar(s['pitchKeys'], sf) * s['_strikePitch'] / 46)
    rotation = yaw @ turn_x(-pitch)
    f = float(np.clip((sf-s['pop'])/(s['land']-s['pop']), 0, 1))
    height = .5*s['referenceFlight']**2*s['gravity']*f*(1-f)
    free = smooth((sf-s['tailRelease'])/(s['pressEnd']-s['tailRelease']))
    floor = -float((np.asarray(s['_geometry']['support']['board']) @ rotation.T)[:, 1].min())
    y = floor*(1-free) + (.092+height)*free
    y = max(y, floor)
    if sf <= s['pop'] or sf >= s['land']:
        y = .092
    return np.array([0., y, 0.]), rotation, height, yaw


def planted(s, sf, side, caught=False):
    origin, rotation, _, _ = board(s, sf)
    target = s['catchFeet' if caught else 'setupFeet'][side]
    return deck_shoe(s['_geometry'], origin, rotation,
                     target['x'], target['z'], target['yaw'])


def guide_contact(s, sf):
    # Shared original contact-frame math only; BS timing/path are this take's.
    # The outer upper drags along the grip toward the nose, never off an edge.
    patch = s['guide']
    origin, rotation, _, _ = board(s, sf)
    x, z = vector(patch['deckKeys'], sf)
    shoe = s['_geometry']['shoe']
    y = shoe['kickRise']*(max(0., abs(z)-shoe['kickStart'])/shoe['kickSpan'])**2 + shoe['concave']*(x/shoe['deckHalfWidth'])**2
    normal = unit(np.array([-2*shoe['concave']*x/shoe['deckHalfWidth']**2, 1.,
                           -2*shoe['kickRise']*max(0., abs(z)-shoe['kickStart'])/shoe['kickSpan']**2*np.sign(z)]))
    contact_normal = -rotation @ normal
    angle = math.radians(patch['toeHeadingDegrees'])
    toe = rotation @ np.array([math.sin(angle), 0., math.cos(angle)])
    tangent = unit(toe-contact_normal*np.dot(toe, contact_normal))
    local_normal = unit(np.array(patch['instepSurfaceNormalLocal']))
    local_tangent = unit(np.array([0., 0., 1.])-local_normal*local_normal[2])
    frame = (np.column_stack((contact_normal, tangent, unit(np.cross(contact_normal, tangent)))) @
             np.column_stack((local_normal, local_tangent, unit(np.cross(local_normal, local_tangent)))).T)
    ankle = origin + rotation @ np.array([x, y, z]) - frame @ np.array(patch['shoeLocal'])
    previous, previous_frame = planted(s, sf, 'Left')
    weight = smooth((sf-patch['start'])/(patch['full']-patch['start']))
    return previous*(1-weight)+ankle*weight, orientation(previous_frame, frame, weight)


def foot(s, sf, side):
    release = s['guide']['release'] if side == 'Left' else s['tailRelease']
    if sf <= release:
        return guide_contact(s, sf) if side == 'Left' else planted(s, sf, side)
    start, first_frame = guide_contact(s, release) if side == 'Left' else planted(s, release, side)
    origin, rotation, height, yaw = board(s, sf)
    release_origin, _, release_height, release_yaw = board(s, release)
    # Carry the released foot with the body/board half-turn, not a shove. Its
    # local release remains continuous while the small free clearance develops.
    relative = release_yaw.T @ (start-release_origin)
    start = origin + yaw @ relative
    first_frame = yaw @ release_yaw.T @ first_frame
    target = s['catchFeet'][side]
    free, flat = deck_shoe(s['_geometry'], origin, rotation,
                          target['x'], target['z'], target['yaw'])
    free += UP*s['freeClearance']
    weight = smooth((sf-release)/(s['pressEnd']-release))
    ankle = start*(1-weight)+free*weight
    frame = orientation(first_frame, flat, weight)
    caught = smooth((sf-s['catchStart'])/(s['catch']-s['catchStart']))
    if caught:
        goal, goal_frame = planted(s, sf, side, True)
        ankle = ankle*(1-caught)+goal*caught
        frame = orientation(frame, goal_frame, caught)
    return ankle, frame


def arms(controls, s, sf):
    rows = controls['frames']
    times = [r['frame'] for r in rows]
    a, b, hold, end = s['balance']['window']
    weight = smooth((sf-a)/(b-a))*(1-smooth((sf-hold)/(end-hold)))
    result = {}
    for side in ['Left', 'Right']:
        result[side] = []
        for part in ['upper', 'fore']:
            observed = unit(curve(times, [r['arms'][side][part] for r in rows], sf))
            authored = unit(s['balance'][side][part])
            direction = unit(observed*(1-weight)+authored*weight)
            # Cropped source hands cannot define a believable pop wind-up.
            # This authored correction ends before the accepted Air pose.
            pop_balance = s.get('popBalance')
            if pop_balance and side in pop_balance:
                start, full, held, finish = pop_balance['window']
                pop_weight = smooth((sf-start)/(full-start))*(1-smooth((sf-held)/(finish-held)))
                target = unit(pop_balance[side][part])
                if pop_weight > 0:
                    direction = unit(direction*(1-pop_weight)+target*pop_weight)
            result[side].append(direction)
    return result


def sample(controls, s, sf):
    origin, rotation, height, source_yaw = board(s, sf)
    hip_turn = turn_y(scalar(s['sourceYawKeys'], sf)+scalar(s['hipWindupKeys'], sf))
    chest_turn = turn_y(scalar(s['sourceYawKeys'], sf)+scalar(s['chestWindupKeys'], sf))
    extension = s.get('popExtension')
    lift = 0.
    if extension:
        start, full, held, finish = extension['window']
        lift = extension['metres']*smooth((sf-start)/(full-start))*(1-smooth((sf-held)/(finish-held)))
    pelvis = origin + UP*(scalar(s['pelvisHeightKeys'], sf)+lift)
    feet = {side: foot(s, sf, side) for side in ['Left', 'Right']}
    hips = {'Left': hip_turn @ np.array([0., 0., .09]),
            'Right': hip_turn @ np.array([0., 0., -.09])}
    low, high = -float('inf'), float('inf')
    for side, (ankle, _) in feet.items():
        hip = pelvis+hips[side]
        horizontal = float(np.sum((hip[[0, 2]]-ankle[[0, 2]])**2))
        reach = .87*.995
        if horizontal >= reach**2:
            raise ValueError(f'Unreachable BS180 {side} target at {sf}')
        half = math.sqrt(reach**2-horizontal)
        low = max(low, ankle[1]-half)
        high = min(high, ankle[1]+half)
    if low > high:
        raise ValueError(f'No common BS180 hip/foot interval at {sf}')
    pelvis[1] = float(np.clip(pelvis[1], low, high))
    joints = {'Pelvis': pelvis}
    for side, (ankle, frame) in feet.items():
        hip = pelvis+hips[side]
        joints[side+'Hip'] = hip
        pole = np.array([1., 0., .1 if side == 'Left' else -.1])
        if side == 'Left' and 'frontKneeGuide' in s:
            guide = s['frontKneeGuide']
            start, full, held, finish = guide['window']
            weight = smooth((sf-start)/(full-start))*(1-smooth((sf-held)/(finish-held)))
            pole = pole*(1-weight)+np.array(guide['pole'])*weight
        joints[side+'Knee'] = limb(hip, ankle, .44, .43, hip_turn @ pole)
        joints[side+'Ankle'] = ankle
        joints[side+'Toe'] = ankle+frame[:, 2]*math.sqrt(.14**2-.07**2)-frame[:, 1]*.07
    lean = math.radians(scalar(s['leanKeys'], sf))
    up = unit(UP*math.cos(lean)+(source_yaw @ np.array([0., 0., 1.]))*math.sin(lean))
    forward = chest_turn @ np.array([1., 0., 0.])
    forward = unit(forward-up*np.dot(forward, up))
    right = unit(np.cross(up, forward))
    joints['Chest'] = pelvis+up*(.52/1.12)
    joints['Neck'] = pelvis+up*.52
    gaze = source_yaw @ unit(np.array(s['headGaze']))
    head_up = unit(UP-gaze*np.dot(UP, gaze))
    joints['Head'] = joints['Neck']+head_up*.12
    arm = arms(controls, s, sf)
    body_basis = np.column_stack((forward, up, right))
    for side, sign in [('Left', -1), ('Right', 1)]:
        shoulder = pelvis+up*.44+right*(sign*.18)
        upper, fore = [unit(body_basis @ v) for v in arm[side]]
        joints[side+'Shoulder'] = shoulder
        joints[side+'Elbow'] = shoulder+upper*.29
        joints[side+'Hand'] = joints[side+'Elbow']+fore*.27
    directions = dict(PelvisForward=hip_turn @ np.array([1., 0., 0.]),
                      PelvisUp=UP, ChestForward=forward, ChestUp=up, HeadForward=gaze)
    # Every source-frame vector is expressed in the source world above. Remove
    # its BOARD heading once, for every joint/direction/sole and the full board.
    # The live NoseYaw carrier already owns actual SpinYaw and adds one turn.
    inverse = source_yaw.T
    joints = {key: inverse @ (value-UP*height) for key, value in joints.items()}
    directions = {key: inverse @ value for key, value in directions.items()}
    soles = {side: inverse @ frame[:, 1] for side, (_, frame) in feet.items()}
    return dict(t=(sf-s['first'])/s['fps'], sourceFrame=sf, sourceCacheTime=sf/s['fps'],
                joints={k: joints[k].tolist() for k in JOINTS},
                dirs={k: directions[k].tolist() for k in DIRS},
                soleUp={k: v.tolist() for k, v in soles.items()},
                board=dict(origin=(inverse @ (origin-UP*height)).tolist(),
                           quaternion=matrix_quat(inverse @ rotation).tolist()))


def bake(body_raw, s):
    if sha(body_raw) != s['sourceBodySha256']:
        raise ValueError('Own BS180 body controls changed')
    controls = json.loads(body_raw)
    if controls['canonicalStance'] != 'regular':
        raise ValueError('Explicit filmed-Goofy canonicalization required')
    raw = (GEOMETRY/'constraints.json').read_bytes()
    if sha(raw) != s['geometryControlsSha256']:
        raise ValueError('Pinned original deck/shoe geometry changed')
    geometry = json.loads(raw)
    s['_geometry'] = geometry
    s['_strikePitch'] = strike_pitch(geometry)
    phases = [(s['first'], 'load', ['LeftFoot', 'RightFoot']),
              (s['pop'], 'pop', ['LeftFoot', 'RightFoot']),
              (s['frontSoleRelease'], 'pop', ['RightFoot']),
              (s['tailRelease'], 'air', []), (s['pressEnd'], 'air', []),
              (s['catch'], 'catch', ['LeftFoot', 'RightFoot']),
              (s['land'], 'land', ['LeftFoot', 'RightFoot']),
              (s['recovery'], 'recovery', ['LeftFoot', 'RightFoot'])]
    frames = np.unique(np.round(np.r_[np.arange(s['first'], s['last']+1e-7, 1/6),
                                     [p[0] for p in phases], s['guide']['release'], s['catchStart']], 9))
    rows = [sample(controls, s, float(sf)) for sf in frames]
    duration = (s['last']-s['first'])/s['fps']
    metadata = dict(id='bs-180-source-keyed-v1', movementKind='bs-180',
                    label='BS180 — own Wes plain take, reconstructed depth/contact [UNREVIEWED]',
                    sourceUrl=controls['sourceUrl'], sourceAuthor='The Berrics',
                    reviewed=False, authored=True, capturedBody=True, filmedPlainTake=True,
                    canonicalStance='regular', sourceFilmedStance=controls['sourceFilmedStance'],
                    sourceFps=s['fps'], firstSourceFrame=s['first'], lastSourceFrame=s['last'],
                    sourceBodySha256=s['sourceBodySha256'], sourcePoseSha256=controls['sourcePoseSha256'],
                    sourceVideoSha256=controls['sourceVideoSha256'], sourceFramePts=controls['sourceFramePts'],
                    sourceCanonicalization=controls['canonicalization'],
                    sourceHeadingRemoval='After reconstructing source-world body/windup: inverse source BOARD yaw applied to ALL joints including toes, directions, sole frames and full board; live native NoseYaw owns exactly one body+board turn.',
                    sourceRootMotion='Reference ballistic carrier removed once; actual physics owns flight, spin, landing and scoring.',
                    provenance=s['provenance'], reconstructionLimits=controls['limits'],
                    sourceUnreliableHandFrames=controls['unreliableHandFrames'],
                    signature=dict(pop='Ollie', switch=False, fakie=False, flip=0, shove=0, spin=-180, body=0))
    clip = dict(format='skate-capture-source-bs180/1', metadata=metadata, stance='regular',
                fps=s['fps']*6, duration=duration, frames=rows,
                events=contact_events([dict(t=(sf-s['first'])/s['fps'], phase=p, contacts=c)
                                       for sf, p, c in phases], duration))
    return clip, encode_pack([clip])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--body', type=Path, default=ASSET/'captured-body.json')
    parser.add_argument('--controls', type=Path, default=ASSET/'constraints.json')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.resolve() == ASSET.resolve():
        raise ValueError('Generated output must not replace committed source controls')
    clip, packed = bake(args.body.read_bytes(), json.loads(args.controls.read_text()))
    args.output.mkdir(parents=True, exist_ok=True)
    payloads = [('bs-180-source-keyed-v1.json', (json.dumps(clip, indent=2)+'\n').encode()),
                ('skate_captures.skmc', packed)]
    temporary = []
    try:
        for name, data in payloads:
            target = args.output/name
            temp = target.with_suffix(target.suffix+'.tmp')
            temp.write_bytes(data)
            temporary.append((temp, target))
        for temp, target in temporary:
            temp.replace(target)
    finally:
        for temp, _ in temporary:
            if temp.exists():
                temp.unlink()
    print('BS180_SOURCE_READY', len(clip['frames']), sha(packed))


if __name__ == '__main__':
    main()
