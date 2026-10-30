#!/usr/bin/env python3
"""Bake one explicitly derived Double, retaining the ordinary Kick prefix.

The native continuation owner must select this clip only after a real held
extension and drive its live winding. This source data never manufactures input.
"""
import argparse
import copy
import hashlib
import json
import math
import os
from pathlib import Path
import subprocess
import sys
import tempfile

import numpy as np
from author_skate_kickflip import authored_keys, limb
from author_skate_kickflip_hybrid import balance_forward, basis, sample, smooth, window
from capture_math import DIRS, JOINTS, UP, encode_pack, euler_quat, matrix_quat, qmatrix, slerp, unit

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / 'assets/authored/animation/kickflip-keyed'
DEFAULT = ROOT / 'assets/authored/animation/double-kickflip-derived/constraints.json'
UPPER = ('Chest', 'Neck', 'Head', 'LeftShoulder', 'RightShoulder',
         'LeftElbow', 'RightElbow', 'LeftHand', 'RightHand')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def rotate_between(first, second):
    axis = np.cross(first, second)
    sine = float(np.linalg.norm(axis))
    cosine = float(np.clip(np.dot(first, second), -1, 1))
    if sine < 1e-10:
        if cosine < 0:
            raise ValueError('Ambiguous opposite upper-body frame')
        return np.eye(3)
    x, y, z = axis / sine
    cross = np.array([[0., -z, y], [z, 0., -x], [-y, x, 0.]])
    return np.eye(3) + cross * sine + cross @ cross * (1 - cosine)


def double_upper(row, control, source_frame):
    weight = window(source_frame, control['upperFoldWindow'])
    if not weight:
        return
    joints = {k: np.array(v) for k, v in row['joints'].items()}
    old_up = unit(joints['Neck'] - joints['Pelvis'])
    old_forward = np.array(row['dirs']['ChestForward'])
    old_forward = unit(old_forward - old_up * np.dot(old_forward, old_up))
    angle = math.radians(control['additionalTorsoFoldDegrees']) * weight
    new_up = unit(old_up * math.cos(angle) + old_forward * math.sin(angle))
    rotation = rotate_between(old_up, new_up)
    anchor = joints['Pelvis']
    for name in UPPER:
        joints[name] = anchor + rotation @ (joints[name] - anchor)
    row['dirs']['ChestUp'] = (rotation @ np.array(row['dirs']['ChestUp'])).tolist()
    row['dirs']['ChestForward'] = (rotation @ np.array(row['dirs']['ChestForward'])).tolist()
    # Reconstruct the visible downward gaze without levering the head back
    # from the more folded torso. The existing neck segment stays fixed-length.
    gaze = unit(np.array(row['board']['origin']) - joints['Head'])
    wanted_up = unit(UP - gaze * np.dot(UP, gaze))
    axis = np.cross(new_up, wanted_up)
    turn = math.atan2(float(np.linalg.norm(axis)), float(np.dot(new_up, wanted_up)))
    limit = math.radians(control['maximumNeckTurnDegrees'])
    if turn > limit:
        wanted_up = rotate_between(new_up, wanted_up)
        wanted_up = qmatrix(slerp([0., 0., 0., 1.], matrix_quat(wanted_up), limit / turn)) @ new_up
    old_head_up = unit(joints['Head'] - joints['Neck'])
    head_up = qmatrix(slerp([0., 0., 0., 1.],
                         matrix_quat(rotate_between(old_head_up, wanted_up)), weight)) @ old_head_up
    joints['Head'] = joints['Neck'] + head_up * .12
    old_gaze = rotation @ np.array(row['dirs']['HeadForward'])
    gaze = unit(old_gaze * (1 - weight) + gaze * weight)
    row['dirs']['HeadForward'] = unit(gaze - head_up * np.dot(gaze, head_up)).tolist()
    row['joints'] = {k: joints[k].tolist() for k in JOINTS}


def derive_row(base_row, control, base_spec, support_points):
    row = copy.deepcopy(base_row)
    sf = row['sourceFrame']
    if sf <= control['startsAfterSourceFrame']:
        return row
    start, finish = control['extraWindingWindow']
    winding = smooth((sf - start) / (finish - start))
    angles = np.array(row['authoredBoardEuler'])
    angles[2] += 360 * winding
    row['board']['quaternion'] = euler_quat(angles).tolist()
    row['authoredBoardEuler'] = angles.tolist()
    double_upper(row, control, sf)
    # One board-close free interval accommodates every actual roll phase.
    # Feet remain in the upright carrier; neither the root nor board is lifted.
    release = smooth((sf - control['startsAfterSourceFrame']) / 1.25)
    returning = smooth((sf - control['returnStartSourceFrame']) /
                       (control['catchSourceFrame'] - control['returnStartSourceFrame']))
    free_weight = release * (1 - returning)
    origin = np.array(row['board']['origin'])
    # Pitch/yaw precede the live flip. A radius of the unpitched mesh alone
    # can miss the higher wheel at the last part of the early pitched Air.
    before_roll = support_points @ qmatrix(euler_quat([angles[0], angles[1], 0.])).T
    support_radius = float(np.max(np.linalg.norm(before_roll[:, :2], axis=1)))
    ankle_y = origin[1] + support_radius - base_spec['hybrid']['popContact']['soleLocalY'] + control['freeSoleGap']
    for side in ('Left', 'Right'):
        ankle = np.array(row['joints'][side + 'Ankle'])
        up = np.array(row['soleUp'][side])
        forward = unit(np.array(row['joints'][side + 'Toe']) - ankle + up * .07)
        foot = basis(forward, up)
        x, z = control['freeFeet'][side]
        target = origin + np.array([x, ankle_y - origin[1], z])
        ankle = ankle * (1 - free_weight) + target * free_weight
        entry = base_spec['hybrid']['referenceGroundPress']['loadEntryFeet'][side]
        foot = qmatrix(slerp(matrix_quat(foot),
                            matrix_quat(basis(np.array(entry['forward']), UP)), free_weight))
        hip = np.array(row['joints'][side + 'Hip'])
        pole = unit(balance_forward(base_spec['hybrid'], sf) * .85 + foot[:, 2] * .15)
        row['joints'][side + 'Knee'] = limb(hip, ankle, .44, .43, pole).tolist()
        row['joints'][side + 'Ankle'] = ankle.tolist()
        row['joints'][side + 'Toe'] = (ankle + foot[:, 2] * math.sqrt(.14 ** 2 - .07 ** 2)
                                     - foot[:, 1] * .07).tolist()
        row['soleUp'][side] = foot[:, 1].tolist()
    row['authoredDouble'] = True
    return row


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, help='Optional unreviewed ordinary Kick JSON; otherwise bake tracked controls')
    parser.add_argument('--constraints', type=Path, default=DEFAULT)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    control = json.loads(args.constraints.read_text())
    if control['targetFlipDegrees'] != 720 or not control['reference']['verifiedTwoFlipsAndLanding']:
        raise ValueError('Only the verified-reference authored Double is supported')
    if sha(BASE / 'captured-body.json') != control['baseBodySha256'] or sha(BASE / 'hybrid-constraints.json') != control['baseConstraintsSha256']:
        raise ValueError('Shared body/constraint source changed; review before rebaking Double')
    with tempfile.TemporaryDirectory(prefix='skate-double-base-') as temporary:
        input_path = args.input
        if input_path is None:
            subprocess.run([sys.executable, str(ROOT / 'scripts/author_skate_kickflip_hybrid.py'),
                            '--output', temporary], check=True, stdout=subprocess.PIPE)
            input_path = Path(temporary) / 'kickflip-source-keyed-v1.json'
        base = json.loads(input_path.read_text())
        base_sha = sha(input_path)
    if base['metadata']['id'] != 'kickflip-source-keyed-v1' or base['metadata']['reviewed'] is not False:
        raise ValueError('Expected the original unreviewed shared Kickflip basis')
    spec = json.loads((BASE / 'hybrid-constraints.json').read_text())
    controls = json.loads((BASE / 'captured-body.json').read_text())
    keys = authored_keys(spec)
    prefix_end = next(f['t'] for f in base['frames'] if f['sourceFrame'] > control['prefixThroughSourceFrame'])
    if control['startsAfterSourceFrame'] <= 254 + prefix_end * spec['sourceFps']:
        raise ValueError('The first original row after the entry target must remain untouched')
    points = np.array(spec['hybrid']['referenceGroundPress']['support']['board'])
    extra = [(sf - 254) / spec['sourceFps'] for sf in
             (control['returnStartSourceFrame'], control['catchSourceFrame'], control['landSourceFrame'])]
    times = sorted(set([f['t'] for f in base['frames']] + extra))
    exact = {f['t']: f for f in base['frames']}
    frames = [derive_row(exact[t] if t in exact else sample(controls, spec, keys, t),
                         control, spec, points) for t in times]
    for before in base['frames']:
        if before['t'] <= prefix_end:
            after = next(f for f in frames if f['t'] == before['t'])
            if before != after:
                raise ValueError('Shared prefix differs at an original sample')
    catch = (control['catchSourceFrame'] - 254) / spec['sourceFps']
    return_start = (control['returnStartSourceFrame'] - 254) / spec['sourceFps']
    events = [copy.deepcopy(e) for e in base['events'] if e['phase'] < 4]
    events += [dict(t=return_start, phase=3, contacts=0), dict(t=catch, phase=4, contacts=3)]
    events += [copy.deepcopy(e) for e in base['events'] if e['phase'] >= 5]
    for row in frames:
        event = next(e for e in reversed(events) if e['t'] <= row['t'] + 1e-9)
        row['phase'] = {1: 'load', 2: 'pop', 3: 'air', 4: 'catch', 5: 'land', 6: 'recovery'}[event['phase']]
        row['contacts'] = event['contacts']
    meta = copy.deepcopy(base['metadata'])
    meta.update(id=control['id'], movementKind=control['movementKind'], reviewed=False,
                label='Double Kickflip — shared Kick basis + authored Double [UNREVIEWED]',
                sourceUrl=control['reference']['url'], sourceAuthor='Project reconstruction; The Berrics visual reference',
                derived=True, capturedDouble=False, sourceMovementKind='kickflip',
                sharedBodySourceUrl=base['metadata']['sourceUrl'], sharedBodyInputSha256=base_sha,
                doubleReference=control['reference'], targetFlipDegrees=720,
                authoredDouble=control['provenance'], lateFreeReturnStartSourceFrame=control['returnStartSourceFrame'],
                authoredCatchRecovery='Distinct authored late return/catch; ordinary273 catch is not used. Existing landing/root recovery is retained.',
                authoredFreeAirDepth='Dedicated board-close free interval accepts every roll phase; no root/board lift or measured Double depth claim.',
                authoredUpperReconstruction='After265 the shared upper group adds the verified Double mid-Air fold and downward board gaze; early source prefix unchanged.',
                sourceClockMeaning='254-based shared ordinary Kick control clock. Double reference frames are qualitative, a different take; no same-take or physical edit-time mapping.')
    clip = dict(base, metadata=meta, frames=frames, events=events)
    packed = encode_pack([clip])  # Validate before replacing either prior output.
    args.output.mkdir(parents=True, exist_ok=True)
    json_path = args.output / (control['id'] + '.json')
    pack_path = args.output / 'skate_captures.skmc'
    json_tmp = json_path.with_suffix('.json.tmp')
    pack_tmp = pack_path.with_suffix('.skmc.tmp')
    json_tmp.write_text(json.dumps(clip, indent=2) + '\n')
    pack_tmp.write_bytes(packed)
    os.replace(json_tmp, json_path)
    os.replace(pack_tmp, pack_path)
    print('DOUBLE_KICKFLIP_DERIVED_READY', len(frames), hashlib.sha256(packed).hexdigest())


if __name__ == '__main__':
    main()
