#!/usr/bin/env python3
"""Bake the own Rodriguez Nollie Hardflip, with Left nose pop / Right instep.

Only geometry/interpolation/fixed-chain math is shared with existing bakers.
Body directions, timing, nose support and foot paths belong to this take.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from author_skate_kickflip import curve, limb
from author_skate_hardflip_hybrid import turn
from author_skate_kickflip_hybrid import basis, body_at, deck_shoe, smooth
from author_skate_treflip import free_foot_clearance
from capture_math import JOINTS, DIRS, UP, contact_events, encode_pack, euler_quat, matrix_quat, qmatrix, slerp, unit

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'assets/authored/animation/nollie-hardflip-keyed'


def value(rows, frame):
    return curve([r[0] for r in rows], [r[1:] for r in rows], frame)


def reference_height(spec, frame):
    ref = spec['sourceRootMotion']
    u = float(np.clip((frame - ref['popFrame']) / (ref['landFrame'] - ref['popFrame']), 0, 1))
    return .5 * ref['gravity'] * ref['referenceFlight'] ** 2 * u * (1 - u)


def board_at(spec, frame):
    """Own full local roll plus FS halfturn; body heading remains unspun."""
    yaw = float(value(spec['boardYaw'], frame)[0])
    pitch = float(value(spec['boardPitch'], frame)[0])
    rotation = turn('y', yaw) @ turn('x', -pitch) @ turn('z', float(value(spec['boardRoll'], frame)[0]))
    ref = reference_height(spec, frame)
    press = 1 - smooth((frame - spec['noseReleaseFrame']) / (spec['pressEndFrame'] - spec['noseReleaseFrame']))
    hull = np.asarray(spec['support']['board'] + spec['support']['wheel'])
    floor = -float(np.min((hull @ rotation.T)[:, 1]))
    world_y = max(floor, floor * press + (.092 + ref) * (1 - press))
    if frame <= spec['popFrame'] or frame >= spec['landFrame']:
        world_y = .092
    return np.array([0., world_y - ref, 0.]), rotation


def instep_frame(spec, frame):
    """Rear RIGHT outer-upper patch, never the sole; own tail-side path."""
    origin, rotation = board_at(spec, frame)
    patch = spec['rearInstep']
    point = value(patch['deckPath'], frame).copy()
    shape = spec['shoe']
    z = point[2]
    slope = 2 * shape['kickRise'] * (abs(z) - shape['kickStart']) / shape['kickSpan'] ** 2 if abs(z) > shape['kickStart'] else 0.
    slope *= 1 if z >= 0 else -1
    normal = unit(np.array([-2 * shape['concave'] * point[0] / shape['deckHalfWidth'] ** 2, 1., -slope]))
    point[1] = shape['concave'] * (point[0] / shape['deckHalfWidth']) ** 2
    if abs(z) > shape['kickStart']:
        point[1] += shape['kickRise'] * ((abs(z) - shape['kickStart']) / shape['kickSpan']) ** 2
    point[1] += spec['soleGap']
    source_normal = unit(patch['shoeNormal'])
    source_forward = unit(np.array([0., 0., 1.]) - source_normal * source_normal[2])
    target_normal = -normal
    tangent = np.asarray(patch['toeTangent'])
    target_forward = unit(tangent - target_normal * np.dot(tangent, target_normal))
    source = np.column_stack([source_forward, np.cross(source_normal, source_forward), source_normal])
    target = np.column_stack([target_forward, np.cross(target_normal, target_forward), target_normal])
    foot = rotation @ target @ source.T
    return origin + rotation @ point - foot @ np.asarray(patch['shoePoint']), foot


def foot_frame(spec, frame, side):
    origin, rotation = board_at(spec, frame)
    c = spec['feet'][side]
    x, z, yaw = c['load']
    ankle, foot = deck_shoe(spec, origin, rotation, x, z, yaw, spec['soleGap'])
    if side == 'Right':
        patch = spec['rearInstep']
        guided, guide_frame = instep_frame(spec, frame)
        guide = smooth((frame - patch['soleReleaseFrame']) / (patch['fullGuideFrame'] - patch['soleReleaseFrame']))
        ankle = ankle * (1 - guide) + guided * guide
        foot = qmatrix(slerp(matrix_quat(foot), matrix_quat(guide_frame), guide))
        release = patch['releaseFrame']
    else:
        release = spec['noseReleaseFrame']
    weight = smooth((frame - release) / (spec['pressEndFrame'] - release))
    free = np.array([0., spec['sourceRootMotion']['deckOriginGroundDatum'], 0.]) + value(c['free'], frame)
    free_frame = basis(unit(value(c['freeForward'], frame)), UP)
    ankle = ankle * (1 - weight) + free * weight
    foot = qmatrix(slerp(matrix_quat(foot), matrix_quat(free_frame), weight))
    caught = spec['firstCatchFrame'] if side == 'Left' else spec['bothCatchFrame']
    catch = smooth((frame - (caught - spec['catchBlendFrames'])) / spec['catchBlendFrames'])
    x, z, yaw = c['catch']
    target, target_frame = deck_shoe(spec, origin, rotation, x, z, yaw, spec['soleGap'])
    ankle = ankle * (1 - catch) + target * catch
    foot = qmatrix(slerp(matrix_quat(foot), matrix_quat(target_frame), catch))
    if weight > 0 and catch < 1:
        ankle[1] += free_foot_clearance(spec, origin, rotation, ankle, foot)
    return ankle, foot


def sample(body, spec, frame):
    t = (frame - spec['firstSourceFrame']) / spec['sourceFps']
    captured, directions, _ = body_at(body, t)
    origin, rotation = board_at(spec, frame)
    feet = {side: foot_frame(spec, frame, side) for side in ('Left', 'Right')}
    pelvis = value(spec['pelvisCarrierLocal'], frame).copy()
    hip_axis = unit((captured['LeftHip'] - captured['RightHip']) * np.array([1., 0., 1.]))
    facing = unit(np.cross(UP, hip_axis))
    share = spec['reachShare']['press'] + (spec['reachShare']['free'] - spec['reachShare']['press']) * smooth((frame - spec['noseReleaseFrame']) / (spec['pressEndFrame'] - spec['noseReleaseFrame']))
    limits = []
    for side, sign in (('Left', 1), ('Right', -1)):
        ankle = feet[side][0]
        hip = pelvis + hip_axis * (sign * .09)
        horizontal = float(np.linalg.norm((hip - ankle) * np.array([1., 0., 1.])))
        if horizontal >= .87 * share:
            raise ValueError('Own Nollie Hardflip feet exceed fixed-chain horizontal reach')
        limits.append(ankle[1] + math.sqrt((.87 * share) ** 2 - horizontal ** 2))
    pelvis[1] = min(pelvis[1], min(limits))
    source_up = unit(captured['Neck'] - captured['Pelvis'])
    lean = unit(facing * .65 + unit(source_up * np.array([1., 0., 1.]), facing) * .35)
    fold = math.radians(float(value(spec['torsoFold'], frame)[0]))
    up = UP * math.cos(fold) + lean * math.sin(fold)
    across = unit(captured['LeftShoulder'] - captured['RightShoulder'])
    across = unit(across - up * np.dot(across, up))
    chest_forward = unit(np.cross(up, across))
    gaze = unit(np.asarray(directions['HeadForward']) * .7 + chest_forward * .3)
    gaze[1] = float(np.clip(gaze[1], -.65, -.08))
    gaze = unit(gaze)
    head_up = unit(UP - gaze * np.dot(UP, gaze))
    j = dict(Pelvis=pelvis, Chest=pelvis + up * (.52 / 1.12), Neck=pelvis + up * .52)
    j['Head'] = j['Neck'] + head_up * .12
    local = lambda v: unit(facing * v[0] + UP * v[1] + hip_axis * v[2])
    soles = {}
    for side, sign in (('Left', 1), ('Right', -1)):
        ankle, foot = feet[side]
        hip = pelvis + hip_axis * (sign * .09)
        j[side + 'Hip'] = hip
        j[side + 'Knee'] = limb(hip, ankle, .44, .43, unit(facing + hip_axis * (sign * .15)))
        j[side + 'Ankle'] = ankle
        j[side + 'Toe'] = ankle + foot[:, 2] * math.sqrt(.14 ** 2 - .07 ** 2) - foot[:, 1] * .07
        soles[side] = foot[:, 1]
        shoulder = pelvis + up * .44 + across * (sign * .18)
        upper = unit(captured[side + 'Elbow'] - captured[side + 'Shoulder'])
        fore = unit(captured[side + 'Hand'] - captured[side + 'Elbow'])
        correction = spec['armReconstruction'][side]
        w = float(value(correction['weights'], frame)[0])
        upper = unit(upper * (1 - w) + local(value(correction['upper'], frame)) * w)
        fore = unit(fore * (1 - w) + local(value(correction['fore'], frame)) * w)
        j[side + 'Shoulder'] = shoulder
        j[side + 'Elbow'] = shoulder + upper * .29
        j[side + 'Hand'] = j[side + 'Elbow'] + fore * .27
    dirs = dict(PelvisForward=facing, PelvisUp=UP, ChestForward=chest_forward, ChestUp=up, HeadForward=gaze)
    event = next(e for e in reversed(spec['events']) if e['frame'] <= frame + 1e-8)
    return dict(t=t, sourceFrame=frame, sourceCacheTime=frame / spec['sourceFps'],
                joints={k: j[k].tolist() for k in JOINTS}, dirs={k: dirs[k].tolist() for k in DIRS},
                soleUp={k: v.tolist() for k, v in soles.items()}, board=dict(origin=origin.tolist(), quaternion=matrix_quat(rotation).tolist()),
                phase={'load': 1, 'pop': 2, 'air': 3, 'catch': 4, 'land': 5, 'recovery': 6}[event['phase']],
                contacts=sum({'LeftFoot': 1, 'RightFoot': 2}[k] for k in event['contacts']))


def bake(body_bytes, spec):
    if hashlib.sha256(body_bytes).hexdigest() != spec['sourceBodySha256']:
        raise ValueError('Own Nollie Hardflip body checksum differs')
    signature = dict(pop='Nollie', switch=False, tailLeads=False, flip=360, shove=180, spin=0, body=0)
    if spec['id'] != 'nollie-hardflip-source-keyed-v1' or spec['movementKind'] != 'nollie-hardflip' or spec['signature'] != signature:
        raise ValueError('Wrong own Nollie Hardflip identity/signature')
    ref = spec['geometryReference']
    geometry = json.loads((ROOT / ref['path']).read_text())
    part = dict(shoe=geometry['shoe'], support=geometry['support'], shoeInstep=geometry['flickPatch']['shoeLocal'])
    if hashlib.sha256(json.dumps(part, sort_keys=True, separators=(',', ':')).encode()).hexdigest() != ref['geometryDataSha256']:
        raise ValueError('Original board/shoe geometry differs')
    surface_bytes = (ROOT / spec['surfaceReference']['path']).read_bytes()
    if hashlib.sha256(surface_bytes).hexdigest() != spec['surfaceReference']['sha256']:
        raise ValueError('Original shaped board surface differs')
    surface = json.loads(surface_bytes)
    spec = dict(spec, support=part['support'], hybrid=dict(popContact=part['shoe']), shoe=part['shoe'],
                authored=dict(minimumAirSoleClearance=spec['soleGap']),
                _surfacePoints=np.asarray(surface['points']), _surfaceTriangles=np.asarray(surface['triangles']))
    if not spec['popFrame'] < spec['noseReleaseFrame'] < spec['pressEndFrame'] < spec['firstCatchFrame'] < spec['bothCatchFrame'] < spec['landFrame']:
        raise ValueError('Expected distinct own nose release, free Air, catch and landing')
    body = json.loads(body_bytes)
    duration = (spec['lastSourceFrame'] - spec['firstSourceFrame']) / spec['sourceFps']
    count = int(math.ceil(duration * spec['sampleHz']))
    times = sorted(set([i / spec['sampleHz'] for i in range(count)] + [duration] + [(e['frame'] - spec['firstSourceFrame']) / spec['sourceFps'] for e in spec['events']] + [(spec['noseStrikeFrame'] - spec['firstSourceFrame']) / spec['sourceFps']]))
    frames = [sample(body, spec, spec['firstSourceFrame'] + t * spec['sourceFps']) for t in times]
    if min(qmatrix(r['board']['quaternion'])[1, 1] for r in frames) > -.5:
        raise ValueError('Own Nollie Hardflip must actually invert the full board')
    if spec['boardRoll'][0][1] != 0 or spec['boardRoll'][-1][1] != 360 or spec['boardYaw'][-1][1] != -180:
        raise ValueError('Own full winding must be +360 roll and -180 board-only yaw')
    events = [dict(t=(e['frame'] - spec['firstSourceFrame']) / spec['sourceFps'], phase=e['phase'], contacts=e['contacts']) for e in spec['events']]
    metadata = dict(id=spec['id'], movementKind=spec['movementKind'], label='Nollie Hardflip — own Rodriguez nose pop / rear instep / full flip [UNREVIEWED]',
                    reviewed=False, authored=True, independentSource=True, canonicalStance='regular',
                    sourceFilmedStance=body['filmedStance'], sourceUrl=body['sourceUrl'], sourceAuthor=body['sourceAuthor'],
                    sourceVideoSha256=body['sourceVideoSha256'], sourcePoseSha256=body['sourcePoseSha256'], sourceBodySha256=spec['sourceBodySha256'],
                    sourceFps=spec['sourceFps'], firstSourceFrame=spec['firstSourceFrame'], lastSourceFrame=spec['lastSourceFrame'],
                    sourceRootMotion=spec['sourceRootMotion'], boardOnlyYaw=True, combinedWinding=dict(nativeFlip=360, nativeShove=180, nativeBodySpin=0, canonicalBoardYaw=-180, canonicalBoardRoll=360),
                    provenance=spec['provenance'], uncertainty=spec['uncertainty'], authoredGapFrames=body['gapFrames'],
                    lowConfidenceModelFrames=body['lowConfidenceModelFrames'], sourceTiming=body['timing'], authoredBoardAndContacts=True, footRoles=dict(pop='Left nose sole', flick='Right rear outer instep'),
                    instepContact=spec['rearInstep'],
                    toeContract='Physical toe endpoints include one sole-up toeDrop; native sampler restores flat targets before writer drop.')
    clip = dict(format='skate-own-source-combined-flip-keyed/1', metadata=metadata, stance='regular', fps=spec['sampleHz'], duration=duration,
                events=contact_events(events, duration), frames=frames)
    return clip, encode_pack([clip])


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--body', type=Path, default=ASSET / 'captured-body.json')
    p.add_argument('--controls', type=Path, default=ASSET / 'constraints.json')
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    if a.output.resolve() in a.body.resolve().parents:
        raise ValueError('Keep output outside its immutable source ancestors')
    clip, packed = bake(a.body.read_bytes(), json.loads(a.controls.read_text()))
    payloads = [(clip['metadata']['id'] + '.json', (json.dumps(clip, indent=2) + '\n').encode()), ('skate_captures.skmc', packed)]
    a.output.mkdir(parents=True, exist_ok=True)
    temporary = []
    try:
        for name, data in payloads:
            tmp = a.output / (name + '.tmp')
            tmp.write_bytes(data)
            temporary.append((tmp, a.output / name))
        for tmp, target in temporary:
            tmp.replace(target)
    finally:
        for tmp, _ in temporary:
            if tmp.exists():
                tmp.unlink()
    print('OWN_NOLLIE_HARDFLIP', len(clip['frames']), len(clip['events']), hashlib.sha256(packed).hexdigest(), 'reviewed=false fullFlip=360 boardYaw=-180 bodySpin=0')


if __name__ == '__main__':
    main()
