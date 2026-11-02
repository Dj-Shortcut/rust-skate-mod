#!/usr/bin/env python3
"""Reconstruct one source-led free-Air window; keep the original pop and catch.

The supplied capture remains private/unreviewed. Only the compact authored
controls and this converter are product source; no video or game files are read.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path

import numpy as np
from author_skate_kickflip import limb
from capture_math import encode_pack
from capture_retarget import transport_leg_pole

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONTROLS = ROOT / 'assets/authored/animation/fakie-kickflip-air/constraints.json'


def smooth(value):
    value = min(1.0, max(0.0, float(value)))
    return value ** 3 * (10 + value * (-15 + 6 * value))


def weight(frame, bounds):
    start, full, release, end = bounds
    return smooth((frame - start) / (full - start)) * (1 - smooth((frame - release) / (end - release)))


def reconstruct(source, controls):
    if source['metadata']['id'] != 'fakie-kickflip' or source['metadata']['movementKind'] != 'fakie-kickflip':
        raise ValueError('This reconstruction only accepts the exact original Fakie Kickflip')
    original = encode_pack([source])
    if hashlib.sha256(original).hexdigest() != controls['originalPackSha256']:
        raise ValueError('The source differs from the reviewed original Fakie input')
    if source['metadata']['reviewed']:
        raise ValueError('Supply the original unreviewed source, never an evaluation pack')
    bounds = controls['sourceFrames']
    if not len(bounds) == 4 or not all(a < b for a, b in zip(bounds, bounds[1:])):
        raise ValueError('Air window must have ordered start/full/release/end source frames')
    if bounds[0] < 2720.375 or bounds[-1] > 2729.125:
        raise ValueError('The established pop/instep and Catch must remain outside the repair')
    result = copy.deepcopy(source)
    rig = source['metadata']['rig']
    changed = []
    for before, row in zip(source['frames'], result['frames']):
        blend = weight(row['sourceFrame'], bounds)
        if blend == 0:
            continue
        if row['contacts'] & 3:
            raise ValueError('Only the actually free-foot Air interval may be reconstructed')
        joints = {name: np.asarray(value, dtype=float) for name, value in before['joints'].items()}
        board = np.asarray(before['board']['origin'], dtype=float)
        target_height = board[1] + controls['pelvisAboveBoardM']
        shift = np.array([0.0, (target_height - joints['Pelvis'][1]) * blend, 0.0])
        # Translate the whole upper body with its pelvis, preserving the actual
        # captured torso/arms/head. Both feet have their own coherent Air target.
        posed = {name: point + shift for name, point in joints.items()}
        for side in ('Left', 'Right'):
            target = board + np.asarray(controls['feet'][side]['positionRelativeToBoardM'], dtype=float)
            ankle = joints[side + 'Ankle'] * (1 - blend) + target * blend
            hip = posed[side + 'Hip']
            pole = transport_leg_pole(joints[side + 'Hip'], joints[side + 'Knee'], joints[side + 'Ankle'], hip, ankle)
            posed[side + 'Knee'] = limb(hip, ankle, rig['thigh'], rig['shin'], pole)
            posed[side + 'Ankle'] = ankle
            posed[side + 'Toe'] = ankle + joints[side + 'Toe'] - joints[side + 'Ankle']
        row['joints'] = {name: value.tolist() for name, value in posed.items()}
        changed.append(row['sourceFrame'])
    if not changed:
        raise ValueError('No original source samples intersect the authored Air window')
    for side in ('Left', 'Right'):
        result['metadata']['freeFootPaths'][side]['provenance'] += (
            ' The source2720.375–2729.125 Air segment is replaced by the separately '
            'declared coherent authored pelvis/free-ankle reconstruction; source '
            'depth and confidence remain unchanged.'
        )
    result['metadata']['reviewed'] = False
    result['metadata']['authoredAirReconstruction'] = dict(
        controls,
        originalSourcePackSha256=hashlib.sha256(original).hexdigest(),
        converterSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        sourceFramesChanged=changed,
    )
    # The established strict writer validates endpoints and interpolated
    # midpoints. A refusal leaves the previous output intact.
    pack = encode_pack([result])
    return result, pack


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, required=True)
    parser.add_argument('--controls', type=Path, default=DEFAULT_CONTROLS)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.resolve() == args.input.parent.resolve():
        raise ValueError('Keep authored candidates separate from the original source')
    result, pack = reconstruct(json.loads(args.input.read_text()), json.loads(args.controls.read_text()))
    args.output.mkdir(parents=True, exist_ok=True)
    for name, data in [('fakie-kickflip.json', (json.dumps(result, indent=2) + '\n').encode()), ('skate_captures.skmc', pack)]:
        target = args.output / name
        temporary = target.with_suffix(target.suffix + '.tmp')
        temporary.write_bytes(data)
        temporary.replace(target)
    print('Fakie free-Air source written; original reviewed=false; SHA256=' + hashlib.sha256(pack).hexdigest())


if __name__ == '__main__':
    main()
