#!/usr/bin/env python3
"""Author a labelled Fakie Inward Heel from reviewed shared mechanics.

The body/entry reference is the reviewed Fakie Kickflip-derived Heelflip.
Heel contact and a board-only backside half-shove are authored; no independent
filmed Fakie Inward take, source detection or calibrated mocap is claimed.
"""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path

import numpy as np
import author_skate_fakie_heelflip as fakie
from author_skate_inward_heelflip import turn_y
from author_skate_heelflip_hybrid import orientation
from author_skate_kickflip import limb
from author_skate_kickflip_hybrid import deck_shoe, smooth
from capture_math import JOINTS, UP, contact_events, encode_pack, matrix_quat

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'assets/authored/animation/fakie-inward-heelflip-keyed'
sha = lambda data: hashlib.sha256(data).hexdigest()


def scalar(rows, frame):
    return float(fakie.value(rows, frame)[0])


def board(spec, frame):
    origin, rotation = fakie.board_at(spec['_fakie'], frame)
    return origin, turn_y(scalar(spec['boardYaw'], frame)) @ rotation


def pressure_foot(spec, frame, side):
    base = spec['_fakie']
    origin, rotation = fakie.board_at(base, frame)
    ankle, foot = fakie.shoe(base, origin, rotation, frame, side)
    scoop = turn_y(scalar(spec['boardYaw'], frame))
    return origin + scoop @ (ankle - origin), scoop @ foot


def catch_foot(spec, frame, side):
    # A reversed deck exchanges its nose/tail, not anatomical feet. Transform
    # the fixed world-side ankle target AND toe heading into that deck.
    origin, rotation = board(spec, frame)
    point, heading = spec['_worldCatch'][side]
    local = spec['_catchRotation'].T @ point
    forward = spec['_catchRotation'].T @ heading
    yaw = math.degrees(math.atan2(forward[0], forward[2]))
    return deck_shoe(spec['_fakie'], origin, rotation,
                     float(local[0]), float(local[2]), yaw, .005)


def foot(spec, frame, side):
    base = spec['_fakie']
    release = base['heelContact']['end'] if side == 'Left' else base['rearReleaseFrame']
    if frame <= release:
        return pressure_foot(spec, frame, side)
    # Free anatomical feet do not orbit with the turning board. The old Heel
    # catch is deliberately excluded until the new reversed-deck catch begins.
    held_frame = min(frame, spec['catchStartFrame'])
    held_origin, held_rotation = fakie.board_at(base, held_frame)
    origin, _ = fakie.board_at(base, frame)
    ankle, rotation = fakie.shoe(base, held_origin, held_rotation, held_frame, side)
    ankle += origin - held_origin
    ankle += UP * scalar(spec['freeAnkleOffsetKeys'][side], frame)
    source_origin, source_rotation = fakie.board_at(base, release)
    source_ankle, _ = fakie.shoe(base, source_origin, source_rotation, release, side)
    pressed, pressed_rotation = pressure_foot(spec, release, side)
    released = smooth((frame - release) / (spec['pressEndFrame'] - release))
    ankle += (pressed - source_ankle) * (1 - released)
    rotation = orientation(pressed_rotation, rotation, released)
    caught = smooth((frame - spec['catchStartFrame']) /
                    (spec['catchFrame'] - spec['catchStartFrame']))
    if caught:
        target, target_rotation = catch_foot(spec, frame, side)
        ankle = ankle * (1 - caught) + target * caught
        rotation = orientation(rotation, target_rotation, caught)
    return ankle, rotation


def balance(row, spec, frame):
    config = spec['authoredBalance']
    start, full, hold, end = config['window']
    amount = smooth((frame - start) / (full - start)) * (1 - smooth((frame - hold) / (end - hold)))
    if not amount:
        return
    from capture_math import unit
    for side in ['Left', 'Right']:
        joints = row['joints']
        shoulder = np.asarray(joints[side + 'Shoulder'])
        elbow = np.asarray(joints[side + 'Elbow'])
        hand = np.asarray(joints[side + 'Hand'])
        upper = unit(unit(elbow - shoulder) * (1 - amount) + unit(config[side]['upper']) * amount)
        fore = unit(unit(hand - elbow) * (1 - amount) + unit(config[side]['fore']) * amount)
        elbow = shoulder + upper * .29
        joints[side + 'Elbow'] = elbow.tolist()
        joints[side + 'Hand'] = (elbow + fore * .27).tolist()


def sample(body, spec, frame):
    base = spec['_fakie']
    row = fakie.sample(body, base, frame)
    original = {key: np.asarray(value) for key, value in row['joints'].items()}
    offset = UP * scalar(spec['pelvisOffsetKeys'], frame)
    joints = {key: value + offset for key, value in original.items()}
    carrier = fakie.reference_height(base, frame)
    feet = {side: foot(spec, frame, side) for side in ['Left', 'Right']}
    feet = {side: (ankle - UP * carrier, rotation) for side, (ankle, rotation) in feet.items()}
    # Same fixed .44/.43 leg chains and common pelvis reach interval. No bone
    # shortening, frame reflection or travel-frame swap is used to fit a foot.
    reach = .87 * .995
    low, high = -float('inf'), float('inf')
    for side, (ankle, _) in feet.items():
        hip = joints[side + 'Hip']; hip_offset = hip - joints['Pelvis']
        horizontal = float(np.sum((hip[[0, 2]] - ankle[[0, 2]]) ** 2))
        if horizontal >= reach ** 2:
            raise ValueError(f'Unreachable authored {side} foot at {frame}')
        half = math.sqrt(reach ** 2 - horizontal)
        low = max(low, ankle[1] - hip_offset[1] - half)
        high = min(high, ankle[1] - hip_offset[1] + half)
    if low > high:
        raise ValueError(f'No common Fakie Inward pelvis interval at {frame}')
    adjustment = UP * (float(np.clip(joints['Pelvis'][1], low, high)) - joints['Pelvis'][1])
    for key in joints:
        joints[key] += adjustment
    for side, (ankle, rotation) in feet.items():
        start, end = original[side + 'Hip'], original[side + 'Ankle']
        line = end - start; pole = original[side + 'Knee'] - start
        pole -= line * np.dot(pole, line) / np.dot(line, line)
        joints[side + 'Knee'] = limb(joints[side + 'Hip'], ankle, .44, .43, pole)
        joints[side + 'Ankle'] = ankle
        joints[side + 'Toe'] = ankle + rotation[:, 2] * math.sqrt(.14 ** 2 - .07 ** 2) - rotation[:, 1] * .07
    origin, rotation = board(spec, frame)
    row['joints'] = {key: joints[key].tolist() for key in JOINTS}
    row['soleUp'] = {side: rotation[:, 1].tolist() for side, (_, rotation) in feet.items()}
    row['board'] = dict(origin=(origin - UP * carrier).tolist(), quaternion=matrix_quat(rotation).tolist())
    balance(row, spec, frame)
    return row


def bake(body_raw, spec):
    if spec['id'] != 'fakie-inward-heelflip-authored-v1' or spec['movementKind'] != 'fakie-inward-heelflip':
        raise ValueError('Wrong authored Fakie Inward identity')
    signature = dict(pop='Ollie', switch=True, tailLeads=True, flip=-360, shove=-180, spin=0)
    if spec['signature'] != signature or sha(body_raw) != spec['sourceBodySha256']:
        raise ValueError('Authored Fakie Inward signature/body pin differs')
    for reference in spec['references'].values():
        if sha((ROOT / reference['path']).read_bytes()) != reference['sha256']:
            raise ValueError('A pinned shared source or helper changed')
    base = json.loads((ROOT / spec['references']['fakieHeelControls']['path']).read_text())
    support = base['supportReference']
    support_raw = (ROOT / support['path']).read_bytes()
    if sha(support_raw) != support['sha256']:
        raise ValueError('Pinned original shaped-board support changed')
    base['support'] = json.loads(support_raw)['hybrid']['support']
    body = json.loads(body_raw)
    if body.get('independentFakieInwardSource') is not False or body['baseMovementKind'] != 'fakie-kickflip':
        raise ValueError('Shared-body derivation must stay explicit')
    spec = copy.deepcopy(spec); spec['_fakie'] = base
    spec['_catchRotation'] = turn_y(180) @ fakie.turn_z(-360)
    spec['_worldCatch'] = {}
    for side in ['Left', 'Right']:
        x, z, yaw = base['feet'][side]['catch']
        ankle, rotation = deck_shoe(base, np.zeros(3), np.eye(3), x, z, yaw, .005)
        spec['_worldCatch'][side] = (ankle, rotation[:, 2])
    first, last, fps = base['firstSourceFrame'], base['lastSourceFrame'], base['sourceFps']
    duration = (last - first) / fps
    events = copy.deepcopy(base['events'])
    for event in events:
        if event['frame'] == base['pressEndFrame']:
            event['frame'] = spec['pressEndFrame']
    count = int(math.ceil(duration * spec['sampleHz']))
    times = sorted(set([i / spec['sampleHz'] for i in range(count)] + [duration] +
                       [(event['frame'] - first) / fps for event in events] +
                       [(base['heelContact']['end'] - first) / fps, (base['tailStrikeFrame'] - first) / fps,
                        (spec['catchStartFrame'] - first) / fps]))
    frames = [sample(body, spec, first + time * fps) for time in times]
    metadata = dict(id=spec['id'], movementKind=spec['movementKind'],
        label='Fakie Inward Heelflip — authored shared-Fakie Heel + BS180 derivation [UNREVIEWED]',
        reviewed=False, authored=True, independentSource=False, filmedInwardTake=False,
        canonicalStance='regular', sourceFilmedStance=body['sourceFilmedStance'],
        sourceUrl=body['sourceUrl'], sourceAuthor=body['sourceAuthor'],
        sourcePoseSha256=body['sourcePoseSha256'], sourceVideoSha256=body['sourceVideoSha256'],
        sourceFps=fps, firstSourceFrame=first, lastSourceFrame=last,
        sourceRootMotion=base['sourceRootMotion'], sourceBodySha256=spec['sourceBodySha256'],
        baseMovementKind='fakie-kickflip', sharedSourceId='fakie-heelflip-source-keyed-v1',
        combinedWinding=dict(nativeFlip=-360, nativeShove=-180, canonicalYaw=180, bodySpin=0),
        provenance=spec['provenance'], uncertainty=spec['uncertainty'], sourceScan=spec['sourceScan'],
        authoredBoardAndContacts=True, authoredBalance=spec['authoredBalance'],
        catchTransform='Both anatomical world targets and toe headings transformed by final reversed-deck R-transpose',
        toeContract='Physical toe endpoints contain one sole-up toeDrop; native sampler restores flat target before writer drop.')
    clip = dict(format='skate-authored-shared-source-keyed/1', metadata=metadata,
                stance='regular', fps=spec['sampleHz'], duration=duration, frames=frames,
                events=contact_events([dict(t=(event['frame'] - first) / fps,
                    phase=event['phase'], contacts=event['contacts']) for event in events], duration))
    return clip, encode_pack([clip])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--body', type=Path, default=ASSET / 'captured-body.json')
    parser.add_argument('--controls', type=Path, default=ASSET / 'constraints.json')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    if output == (ROOT / 'assets').resolve() or (ROOT / 'assets').resolve() in output.parents:
        raise ValueError('Generated packs must remain outside committed source folders')
    clip, packed = bake(args.body.read_bytes(), json.loads(args.controls.read_text()))
    args.output.mkdir(parents=True, exist_ok=True); temporary = []
    try:
        for name, data in [(clip['metadata']['id'] + '.json', (json.dumps(clip, indent=2) + '\n').encode()),
                           ('skate_captures.skmc', packed)]:
            target = args.output / name; pending = target.with_suffix(target.suffix + '.tmp')
            pending.write_bytes(data); temporary.append((pending, target))
        for pending, target in temporary:
            pending.replace(target)
    finally:
        for pending, _ in temporary:
            if pending.exists(): pending.unlink()
    print('AUTHORED_FAKIE_INWARD', len(clip['frames']), sha(packed), 'reviewed=false')


if __name__ == '__main__':
    main()
