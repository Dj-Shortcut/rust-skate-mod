#!/usr/bin/env python3
"""Reconstruct the Switch Heelflip's free Air from its own source take.

The original capture is a private, SHA-bound input. The original pop, board,
events, catch and recovery remain unchanged. No video, network or game input
is required by this baker.
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
DEFAULT_CONTROLS = ROOT / 'assets/authored/animation/switch-heelflip-air/constraints.json'


def smooth(value):
    value = float(np.clip(value, 0, 1))
    return value ** 3 * (10 + value * (-15 + 6 * value))


def reconstruct(source, controls):
    metadata = source['metadata']
    if metadata['id'] != 'switch-heelflip' or metadata['movementKind'] != 'switch-heelflip':
        raise ValueError('Only the exact original Switch Heelflip is supported')
    original = encode_pack([source])
    if hashlib.sha256(original).hexdigest() != controls['originalPackSha256']:
        raise ValueError('The original Switch Heelflip capture differs from the bound source')
    if metadata['reviewed']:
        raise ValueError('Use the original unreviewed input, not an evaluation pack')
    bounds = controls['sourceFrames']
    if len(bounds) != 4 or not all(a < b for a, b in zip(bounds, bounds[1:])):
        raise ValueError('Expected ordered start/full/release/end source frames')
    if bounds[0] < 2235 or bounds[-1] >= 2245:
        raise ValueError('Keep the established pop/release and Catch outside this Air window')
    result = copy.deepcopy(source)
    rig = metadata['rig']
    changed = []
    for before, row in zip(source['frames'], result['frames']):
        frame = row['sourceFrame']
        blend = smooth((frame - bounds[0]) / (bounds[1] - bounds[0])) * (
            1 - smooth((frame - bounds[2]) / (bounds[3] - bounds[2])))
        if blend == 0:
            continue
        if before['contacts'] != 0:
            raise ValueError('The authored Air window cannot move an observed contact')
        joints = {key: np.asarray(point, dtype=float) for key, point in before['joints'].items()}
        origin = np.asarray(before['board']['origin'], dtype=float)
        shift = np.array([0., (origin[1] + controls['pelvisAboveBoardM'] - joints['Pelvis'][1]) * blend, 0.])
        # These are upright carrier coordinates. Do not rotate the rider root
        # with the flipping deck or add a second physical flight trajectory.
        posed = {key: point + shift for key, point in joints.items()}
        for side in ('Left', 'Right'):
            guide = controls['feet'][side]
            target = joints[side + 'Ankle'].copy()
            target[1] = origin[1] + guide['heightAboveBoardM']
            # The take's wide heel-side gesture remains source-specific. Its
            # tangential path is retained rather than replaced by Kickflip feet.
            ankle = joints[side + 'Ankle'] * (1 - blend) + target * blend
            hip = posed[side + 'Hip']
            pole = transport_leg_pole(joints[side + 'Hip'], joints[side + 'Knee'],
                                      joints[side + 'Ankle'], hip, ankle)
            posed[side + 'Knee'] = limb(hip, ankle, rig['thigh'], rig['shin'], pole)
            posed[side + 'Ankle'] = ankle
            posed[side + 'Toe'] = ankle + joints[side + 'Toe'] - joints[side + 'Ankle']
        row['joints'] = {key: point.tolist() for key, point in posed.items()}
        changed.append(frame)
    if not changed:
        raise ValueError('The input has no samples inside the declared Air window')
    for side in ('Left', 'Right'):
        result['metadata']['freeFootPaths'][side]['provenance'] += (
            ' Source2235–2244.75 now uses the separately declared source-led '
            'pelvis/both-free-ankle Air reconstruction; tangential foot paths '
            'and source bend planes are retained. Hidden depth remains authored.')
    result['metadata']['reviewed'] = False
    result['metadata']['authoredAirReconstruction'] = dict(
        controls, converterSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        sourceFramesChanged=changed)
    assert result['events'] == source['events']
    assert all(after['board'] == before['board'] for before, after in zip(source['frames'], result['frames']))
    assert all(before == after for before, after in zip(source['frames'], result['frames'])
               if after['sourceFrame'] <= bounds[0] or after['sourceFrame'] >= bounds[-1])
    # The existing strict writer also checks interpolated midpoints. Failure
    # occurs before any previous reader-valid output can be replaced.
    return result, encode_pack([result])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, required=True)
    parser.add_argument('--controls', type=Path, default=DEFAULT_CONTROLS)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.resolve() == args.input.parent.resolve():
        raise ValueError('Keep reconstructed output separate from the original capture')
    result, pack = reconstruct(json.loads(args.input.read_text()), json.loads(args.controls.read_text()))
    args.output.mkdir(parents=True, exist_ok=True)
    for name, data in [('switch-heelflip.json', (json.dumps(result, indent=2) + '\n').encode()),
                       ('skate_captures.skmc', pack)]:
        target = args.output / name
        temporary = target.with_suffix(target.suffix + '.tmp')
        temporary.write_bytes(data)
        temporary.replace(target)
    print('Switch Heelflip source-led Air written; reviewed=false; SHA256=' + hashlib.sha256(pack).hexdigest())


if __name__ == '__main__':
    main()
