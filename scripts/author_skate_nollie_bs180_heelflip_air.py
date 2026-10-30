#!/usr/bin/env python3
"""Reconstruct one source-led free-Air window in the original Nollie BS180 Heel."""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from author_skate_kickflip import limb
from author_skate_switch_heelflip_air import smooth, upper_rotation
from capture_math import encode_pack
from capture_retarget import transport_leg_pole

ROOT = Path(__file__).resolve().parents[1]
CONTROLS = ROOT / 'assets/authored/animation/nollie-bs180-heelflip-air/constraints.json'
UPPER = ('Chest', 'Neck', 'Head', 'LeftShoulder', 'RightShoulder',
         'LeftElbow', 'RightElbow', 'LeftHand', 'RightHand')


def unit(vector):
    length = np.linalg.norm(vector)
    if length < 1e-8:
        raise ValueError('Source reconstruction has an ambiguous direction')
    return vector / length


def reconstruct(source, controls):
    metadata = source['metadata']
    kind = 'nollie-backside-180-heelflip'
    if metadata['id'] != kind or metadata['movementKind'] != kind or metadata['reviewed']:
        raise ValueError('Use the original unreviewed Nollie BS180 Heelflip')
    if hashlib.sha256(encode_pack([source])).hexdigest() != controls['originalPackSha256']:
        raise ValueError('The original source capture differs from its bound SHA256')
    bounds = controls['sourceFrames']
    if len(bounds) != 4 or not all(a < b for a, b in zip(bounds, bounds[1:])):
        raise ValueError('Expected ordered start/full/release/end source frames')
    if bounds[0] < 3505 or bounds[-1] >= 3516:
        raise ValueError('Keep the established Pop and Catch outside the free-Air window')
    result = copy.deepcopy(source)
    rig = metadata['rig']
    world_up = np.array([0., 1., 0.])
    changed = []
    for before, row in zip(source['frames'], result['frames']):
        frame = row['sourceFrame']
        weight = smooth((frame - bounds[0]) / (bounds[1] - bounds[0])) * (
            1 - smooth((frame - bounds[2]) / (bounds[3] - bounds[2])))
        if weight == 0:
            continue
        if before['contacts'] != 0:
            raise ValueError('The Air reconstruction cannot move an observed contact')
        joints = {k: np.asarray(v, dtype=float) for k, v in before['joints'].items()}
        origin = np.asarray(before['board']['origin'], dtype=float)
        shift = world_up * (origin[1] + controls['pelvisAboveBoardM'] - joints['Pelvis'][1]) * weight
        posed = {k: v + shift for k, v in joints.items()}
        facing = np.asarray(before['dirs']['ChestForward'], dtype=float)
        facing[1] = 0
        facing = unit(facing)
        lean = math.radians(controls['torsoForwardLeanDegrees'])
        desired_up = facing * math.sin(lean) + world_up * math.cos(lean)
        old_up = np.asarray(before['dirs']['ChestUp'], dtype=float)
        rotation = upper_rotation(old_up, old_up * (1 - weight) + desired_up * weight)
        for name in UPPER:
            posed[name] = posed['Pelvis'] + rotation @ (posed[name] - posed['Pelvis'])
        for name in ('ChestForward', 'ChestUp', 'HeadForward'):
            row['dirs'][name] = (rotation @ np.asarray(before['dirs'][name])).tolist()
        # Keep the source's far-arm direction, but shorten its authored reach
        # and bend the elbow toward the visible raised source gesture.
        shoulder, elbow, hand = (posed['Left' + name] for name in ('Shoulder', 'Elbow', 'Hand'))
        guide = controls['leftArm']
        target_hand = shoulder + (hand - shoulder) * guide['reachShare'] + world_up * guide['wristLiftM']
        wrist = hand * (1 - weight) + target_hand * weight
        source_pole = unit(transport_leg_pole(shoulder, elbow, hand, shoulder, wrist))
        pole = source_pole * (1 - weight) + world_up * weight
        posed['LeftElbow'] = limb(shoulder, wrist, rig['upperArm'], rig['forearm'], pole)
        posed['LeftHand'] = wrist
        for side in ('Left', 'Right'):
            ankle = joints[side + 'Ankle'].copy()
            ankle[1] += (origin[1] + controls['feet'][side]['heightAboveBoardM'] - ankle[1]) * weight
            hip = posed[side + 'Hip']
            pole = transport_leg_pole(joints[side + 'Hip'], joints[side + 'Knee'], joints[side + 'Ankle'], hip, ankle)
            posed[side + 'Knee'] = limb(hip, ankle, rig['thigh'], rig['shin'], pole)
            posed[side + 'Ankle'] = ankle
            posed[side + 'Toe'] = ankle + joints[side + 'Toe'] - joints[side + 'Ankle']
        row['joints'] = {k: v.tolist() for k, v in posed.items()}
        changed.append(frame)
    if not changed:
        raise ValueError('The input has no samples inside the declared Air window')
    result['metadata']['reviewed'] = False
    for side in ('Left', 'Right'):
        result['metadata']['freeFootPaths'][side]['provenance'] += (
            ' Source3505–3515.5 uses a source-led coherent pelvis/both-free-foot '
            'collection. Its torso fold and bent far canonical-left elbow are '
            'authored reconstruction; board, body yaw and contact clocks are retained.')
    result['metadata']['authoredNollieBs180Air'] = dict(
        controls, converterSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        sourceFramesChanged=changed)
    assert result['events'] == source['events']
    for before, after in zip(source['frames'], result['frames']):
        assert all(before[k] == after[k] for k in ('t', 'sourceFrame', 'board', 'contacts', 'soleUp'))
        assert all(before['dirs'][k] == after['dirs'][k] for k in ('PelvisForward', 'PelvisUp'))
        if after['sourceFrame'] <= bounds[0] or after['sourceFrame'] >= bounds[-1]:
            assert before == after
    return result, encode_pack([result])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, required=True)
    parser.add_argument('--controls', type=Path, default=CONTROLS)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.resolve() == args.input.parent.resolve():
        raise ValueError('Keep reconstructed output separate from the original')
    result, pack = reconstruct(json.loads(args.input.read_text()), json.loads(args.controls.read_text()))
    args.output.mkdir(parents=True, exist_ok=True)
    for name, data in [('nollie-backside-180-heelflip.json', (json.dumps(result, indent=2) + '\n').encode()),
                       ('skate_captures.skmc', pack)]:
        target = args.output / name
        temporary = target.with_suffix(target.suffix + '.tmp')
        temporary.write_bytes(data)
        temporary.replace(target)
    print('Source-led Nollie BS180 Heel Air written; reviewed=false; SHA256=' + hashlib.sha256(pack).hexdigest())


if __name__ == '__main__':
    main()
