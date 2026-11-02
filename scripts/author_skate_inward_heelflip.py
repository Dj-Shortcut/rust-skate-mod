#!/usr/bin/env python3
"""Author an explicitly derived Inward Heel from the pinned normal Heel body.

This is Heel timing/body plus an authored backside half-shove, not a filmed
Inward take. Geometry helpers fit the original shaped deck and shoe.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

import numpy as np
import author_skate_heelflip_hybrid as heel
from author_skate_kickflip import foot_basis, limb
from author_skate_kickflip_hybrid import deck_shoe, smooth
from capture_math import JOINTS, UP, unit, matrix_quat, encode_pack, contact_events

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'assets/authored/animation/inward-heelflip-derived'
HEEL = ROOT / 'assets/authored/animation/heelflip-keyed'
sha = lambda data: hashlib.sha256(data).hexdigest()


def turn_y(degrees):
    a = math.radians(degrees)
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])


def yaw(s, sf):
    return heel.scalar(s['yawKeys'], sf)


def board(s, sf):
    origin, rotation, carrier = heel.board(s['_heel'], sf)
    return origin, turn_y(yaw(s, sf)) @ rotation, carrier


def pressure_foot(s, sf, side):
    """Only the contacted sole/heel follows the early backside scoop."""
    b = s['_heel']
    origin, _, _ = heel.board(b, sf)
    ankle, frame = heel.foot(b, sf, side)
    rotation = turn_y(yaw(s, sf))
    return origin + rotation @ (ankle - origin), rotation @ frame


def catch_foot(s, sf, side):
    # A half-shove reverses the deck, not the rider's anatomical feet. Transform
    # position AND heading into that reversed deck before measured sole fitting.
    origin, rotation, _ = board(s, sf)
    point, forward = s['_worldCatch'][side]
    local = s['_catchRotation'].T @ point
    heading = s['_catchRotation'].T @ forward
    angle = math.degrees(math.atan2(heading[0], heading[2]))
    return deck_shoe(s['_heel'], origin, rotation,
                     float(local[0]), float(local[2]), angle, .003)


def free_foot(s, sf, side):
    b = s['_heel']
    # Do not inherit the source Heel's early catch on the unturned deck.
    frame = min(sf, s['catchStart'])
    ankle, rotation = heel.foot(b, frame, side)
    origin, _, carrier = heel.board(b, sf)
    prior_origin, _, _ = heel.board(b, frame)
    ankle = ankle + origin - prior_origin
    return ankle, rotation


def foot(s, sf, side):
    b = s['_heel']
    release = b['heelFlickFrame'] if side == 'Left' else b['rearSoleReleaseFrame']
    if sf <= release:
        return pressure_foot(s, sf, side)
    # The pressure endpoint and the free anatomical trajectory are separate.
    # Gradually release the scoop displacement rather than rotating free feet
    # with the board or snapping from the heel contact into the old source.
    ankle, rotation = free_foot(s, sf, side)
    source_a, source_r = heel.foot(b, release, side)
    pressed_a, pressed_r = pressure_foot(s, release, side)
    weight = smooth((sf - release) / (s['pressEnd'] - release))
    ankle += (pressed_a - source_a) * (1 - weight)
    rotation = heel.orientation(pressed_r, rotation, weight)
    caught = smooth((sf - s['catchStart']) / (b['catchFrame'] - s['catchStart']))
    if caught:
        target, target_r = catch_foot(s, sf, side)
        ankle = ankle * (1 - caught) + target * caught
        rotation = heel.orientation(rotation, target_r, caught)
    return ankle, rotation


def balance(row, s, sf):
    """Own bent balance arms; the shared Heel is not an Inward arm capture."""
    c = s['authoredBalance']
    a, b, hold, end = c['window']
    weight = smooth((sf - a) / (b - a)) * (1 - smooth((sf - hold) / (end - hold)))
    if not weight:
        return
    joints = row['joints']
    for side in ['Left', 'Right']:
        shoulder = np.asarray(joints[side + 'Shoulder'])
        elbow = np.asarray(joints[side + 'Elbow'])
        hand = np.asarray(joints[side + 'Hand'])
        upper = unit(unit(elbow - shoulder) * (1 - weight) +
                     unit(c[side]['upper']) * weight)
        fore = unit(unit(hand - elbow) * (1 - weight) +
                    unit(c[side]['fore']) * weight)
        elbow = shoulder + upper * .29
        joints[side + 'Elbow'] = elbow.tolist()
        joints[side + 'Hand'] = (elbow + fore * .27).tolist()


def sample(controls, s, sf):
    b = s['_heel']
    row = heel.sample(controls, b, sf)
    joints = {k: np.asarray(v) for k, v in row['joints'].items()}
    original = {k: v.copy() for k, v in joints.items()}
    origin, rotation, carrier = board(s, sf)
    # Keep the body low until the heel/tail pressure has actually released.
    # The normal route interpolates toward the free-Air entry endpoint; that
    # endpoint must also remain compact while the earlier rear foot is on tail.
    # This is a source pose, not an extra jump or a runtime reach exemption.
    lowered = UP * heel.scalar(s['pressPelvisOffsetKeys'], sf)
    for key in joints:
        joints[key] += lowered
    feet = {side: foot(s, sf, side) for side in ['Left', 'Right']}
    feet = {side: (ankle - UP * carrier, frame)
            for side, (ankle, frame) in feet.items()}
    # Preserve the shared body's pose unless the new contact needs a small
    # common-height correction. Never shorten bones to reach an ankle.
    low, high = -float('inf'), float('inf')
    reach = .87 * .995
    for side, (ankle, _) in feet.items():
        hip = joints[side + 'Hip']
        offset = hip - joints['Pelvis']
        horizontal = float(np.sum((hip[[0, 2]] - ankle[[0, 2]]) ** 2))
        if horizontal >= reach ** 2:
            raise ValueError(f'Unreachable derived {side} foot at {sf}')
        half = math.sqrt(reach ** 2 - horizontal)
        low = max(low, ankle[1] - offset[1] - half)
        high = min(high, ankle[1] - offset[1] + half)
    if low > high:
        raise ValueError(f'No common Inward foot/pelvis interval at {sf}')
    delta = UP * (float(np.clip(joints['Pelvis'][1], low, high)) - joints['Pelvis'][1])
    for key in joints:
        joints[key] += delta
    for side, (ankle, frame) in feet.items():
        hip = joints[side + 'Hip']
        start, end = original[side + 'Hip'], original[side + 'Ankle']
        line = end - start
        pole = original[side + 'Knee'] - start
        pole -= line * np.dot(pole, line) / np.dot(line, line)
        joints[side + 'Knee'] = limb(hip, ankle, .44, .43, pole)
        joints[side + 'Ankle'] = ankle
        joints[side + 'Toe'] = ankle + frame[:, 2] * math.sqrt(.14 ** 2 - .07 ** 2) - frame[:, 1] * .07
    row['joints'] = {k: joints[k].tolist() for k in JOINTS}
    row['soleUp'] = {side: frame[:, 1].tolist() for side, (_, frame) in feet.items()}
    row['board'] = dict(origin=(origin - UP * carrier).tolist(),
                        quaternion=matrix_quat(rotation).tolist())
    balance(row, s, sf)
    return row


def bake(body_raw, heel_raw, s):
    if sha(body_raw) != s['sourceBodySha256']:
        raise ValueError('Pinned shared Heel body changed')
    if sha(heel_raw) != s['sourceControlsSha256']:
        raise ValueError('Pinned shared Heel constraints changed')
    if sha(Path(heel.__file__).read_bytes()) != s['heelHelperSha256']:
        raise ValueError('Pinned Heel geometry/motion helper changed')
    controls, b = json.loads(body_raw), json.loads(heel_raw)
    for key in b['pitchKeys']:
        if key[0] == b['tailStrikeFrame']:
            key[1] = heel.strike_pitch(b)
    s['_heel'] = b
    s['_catchRotation'] = turn_y(180) @ heel.turn_z(-360)
    s['_worldCatch'] = {}
    for side, z, angle in [('Left', .24, 80), ('Right', -.24, 85)]:
        ankle, frame = deck_shoe(b, np.zeros(3), np.eye(3), -.04, z, angle, .003)
        s['_worldCatch'][side] = (ankle, frame[:, 2])
    first, last, fps = b['firstSourceFrame'], b['lastSourceFrame'], b['sourceFps']
    phases = [(first, 'load', ['LeftFoot', 'RightFoot']),
              (b['popFrame'], 'pop', ['LeftFoot', 'RightFoot']),
              (b['frontSoleReleaseFrame'], 'pop', ['RightFoot']),
              (b['rearSoleReleaseFrame'], 'air', []),
              (s['pressEnd'], 'air', []),
              (b['catchFrame'], 'catch', ['LeftFoot', 'RightFoot']),
              (b['landFrame'], 'land', ['LeftFoot', 'RightFoot']),
              (b['recoveryFrame'], 'recovery', ['LeftFoot', 'RightFoot'])]
    times = np.unique(np.round(np.r_[np.arange(first, last + 1e-7, 1 / 6),
                             [p[0] for p in phases], b['heelFlickFrame'],
                             s['catchStart'], s['rotationEnd']], 9))
    frames = [sample(controls, s, float(sf)) for sf in times]
    duration = (last - first) / fps
    metadata = dict(b['metadata'], id=s['id'], movementKind='inward-heelflip',
                    label='Inward Heelflip — authored shared-Heel + BS180 derivation [UNREVIEWED]',
                    reviewed=False, authored=True, capturedBody=True,
                    canonicalStance='regular', sourceFilmedStance=controls['sourceFilmedStance'],
                    sourceFps=fps, firstSourceFrame=first, lastSourceFrame=last,
                    sourceBodySha256=s['sourceBodySha256'],
                    sourcePoseSha256=controls['sourcePoseSha256'],
                    sharedSourceId='heelflip-source-keyed-v1',
                    referenceFootageMovementKind='heelflip', filmedInwardTake=False,
                    combinedWinding=dict(nativeFlip=-360, nativeShove=-180,
                                         canonicalYaw=180, bodySpin=0),
                    provenance=s['provenance'], sourceScan=s['sourceScan'],
                    authoredBalance=s['authoredBalance'],
                    authoredPressPelvisOffset=s['pressPelvisOffsetKeys'],
                    catchTransform='Anatomical world targets and toe headings transformed into reversed deck by final R-transpose',
                    sourceRootMotion='Reference carrier excluded; actual physics carrier added once by playback')
    clip = dict(format='skate-capture-authored-inward/1', metadata=metadata,
                stance='regular', fps=fps * 6, duration=duration, frames=frames,
                events=contact_events([dict(t=(sf-first)/fps, phase=p, contacts=c)
                                       for sf, p, c in phases], duration))
    return clip, encode_pack([clip])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--body', type=Path, default=HEEL / 'captured-body.json')
    parser.add_argument('--heel-controls', type=Path, default=HEEL / 'heel-constraints.json')
    parser.add_argument('--controls', type=Path, default=ASSET / 'constraints.json')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.resolve() in {HEEL.resolve(), ASSET.resolve()}:
        raise ValueError('Keep generated packs separate from committed sources')
    s = json.loads(args.controls.read_text())
    clip, packed = bake(args.body.read_bytes(), args.heel_controls.read_bytes(), s)
    args.output.mkdir(parents=True, exist_ok=True)
    payloads = [(s['id'] + '.json', (json.dumps(clip, indent=2) + '\n').encode()),
                ('skate_captures.skmc', packed)]
    temporary = []
    try:
        for name, data in payloads:
            target = args.output / name
            temp = target.with_suffix(target.suffix + '.tmp')
            temp.write_bytes(data)
            temporary.append((temp, target))
        for temp, target in temporary:
            temp.replace(target)
    finally:
        for temp, _ in temporary:
            if temp.exists():
                temp.unlink()
    print('INWARD_DERIVED_SOURCE_READY', len(clip['frames']), sha(packed))


if __name__ == '__main__':
    main()
