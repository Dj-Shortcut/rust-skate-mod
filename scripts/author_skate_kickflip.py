#!/usr/bin/env python3
"""Bake original, image-led coupled rider/board keys into bounded SKMCAP01.

No detector, source pixels, player trajectory or old presentation stack is used.
The caller adds the physical support carrier once. Requires NumPy.
"""
import argparse
import json
import math
from pathlib import Path
import numpy as np
from capture_math import (JOINTS, DIRS, UP, contact_events, encode_pack, euler_quat,
                          matrix_quat, qmatrix, slerp, unit)

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_KEYS = ROOT / 'assets/authored/animation/kickflip-keyed/keys.json'


def curve(times, values, t):
    """Componentwise monotone cubic Hermite: no overshooting a keyed channel."""
    x = np.asarray(times, dtype=float)
    y = np.asarray(values, dtype=float)
    i = max(0, min(len(x) - 2, int(np.searchsorted(x, t, side='right')) - 1))
    h = np.diff(x)
    sec = np.diff(y, axis=0) / h[:, None]
    tangent = np.zeros_like(y)
    tangent[0], tangent[-1] = sec[0], sec[-1]
    for n in range(1, len(x) - 1):
        same = sec[n - 1] * sec[n] > 0
        w1, w2 = 2 * h[n] + h[n - 1], h[n] + 2 * h[n - 1]
        tangent[n, same] = (w1 + w2) / (w1 / sec[n - 1, same] + w2 / sec[n, same])
    u = float(np.clip((t - x[i]) / h[i], 0, 1))
    return ((2*u**3 - 3*u*u + 1)*y[i] + (u**3 - 2*u*u + u)*h[i]*tangent[i]
            + (-2*u**3 + 3*u*u)*y[i+1] + (u**3 - u*u)*h[i]*tangent[i+1])


def limb(start, end, first, second, pole):
    delta = end - start
    distance = float(np.linalg.norm(delta))
    if not abs(first-second)+1e-6 < distance < first+second-1e-6:
        raise ValueError('Unreachable authored limb: %.6f' % distance)
    axis = delta / distance
    bend = unit(pole - axis * np.dot(pole, axis))
    along = (first*first - second*second + distance*distance) / (2*distance)
    return start + axis*along + bend*math.sqrt(max(0., first*first - along*along))


def foot_basis(yaw, inversion=0., dorsiflexion=0.):
    forward = np.array([math.sin(math.radians(yaw)), 0., math.cos(math.radians(yaw))])
    up = qmatrix(euler_quat([0, 0, 0])) @ UP
    # Moderate inversion followed by toes-up rotation around the foot's across axis.
    a = math.radians(inversion)
    up = up*math.cos(a) + np.cross(forward, up)*math.sin(a)
    across = unit(np.cross(up, forward))
    a = math.radians(-dorsiflexion)
    turn = lambda v: v*math.cos(a) + np.cross(across, v)*math.sin(a) + across*np.dot(across, v)*(1-math.cos(a))
    return np.column_stack((unit(np.cross(turn(up), turn(forward))), unit(turn(up)), unit(turn(forward))))


def authored_keys(spec):
    result = []
    # A body-only breakdown key must not reshape an already-authored board curve.
    board_keys = [k for k in spec['keys'] if k.get('boardKey', True)]
    board_times = [k['t'] if k['sourceFrame'] is None else (k['sourceFrame']-spec['firstSourceFrame'])/spec['sourceFps'] for k in board_keys]
    board_angles = [[-k['boardPitch'], 0., k['boardRoll']] for k in board_keys]
    board_origins = []
    for k in board_keys:
        pitch = qmatrix(euler_quat([-k['boardPitch'], 0, 0]))
        pivot = np.array([0., 0., -.245])
        board_origins.append(pivot + pitch @ (np.array([0., .092, 0.])-pivot) + UP*k['boardLift'])
    upper = np.array(spec['flickPatch']['shoeLocal'])
    rim = np.array(spec['flickPatch']['deckLocal'])
    for key in spec['keys']:
        t = key['t'] if key['sourceFrame'] is None else (key['sourceFrame']-spec['firstSourceFrame'])/spec['sourceFps']
        angles = curve(board_times, board_angles, t)
        board_q = euler_quat(angles)
        origin = curve(board_times, board_origins, t)
        feet = {}
        for side in ('Left', 'Right'):
            f = key['feet'][side]
            basis = foot_basis(*f['angles'])
            if f['space'] == 'deck':
                basis = qmatrix(board_q) @ basis
                ankle = origin + qmatrix(board_q) @ np.array(f['position'])
            elif f['space'] == 'upper-patch':
                ankle = origin + qmatrix(board_q) @ rim - basis @ upper
            else:
                ankle = np.array(f['position'], dtype=float)
            feet[side] = dict(ankle=ankle, rotation=matrix_quat(basis))
        result.append(dict(sourceFrame=key['sourceFrame'], t=key['t'] if key['sourceFrame'] is None else (key['sourceFrame']-spec['firstSourceFrame'])/spec['sourceFps'],
                           control=key, boardOrigin=origin, boardAngles=angles, feet=feet))
    # The final soft rollaway pose is explicitly held; it is not extra measured footage.
    hold = dict(result[-1]); hold['t'] = spec['duration']; hold['sourceFrame'] = None
    result.append(hold)
    return result


def sample(spec, keys, t):
    times = [k['t'] for k in keys]
    body_keys = [k for k in keys if k['control'].get('bodyKey', True)]
    body_times = [k['t'] for k in body_keys]
    value = lambda name: curve(body_times, [k['control'][name] for k in body_keys], t)
    pelvis = value('pelvis')
    lean = float(curve(body_times, [[k['control']['leanDegrees']] for k in body_keys], t)[0])
    lean_direction = unit(np.array([.65,0.,.75]))
    torso = UP*math.cos(math.radians(lean)) + lean_direction*math.sin(math.radians(lean))
    hip_axis = np.array([0.,0.,-1.])
    shoulder_axis = unit(hip_axis - torso*np.dot(hip_axis, torso))
    pelvis_up = unit(torso*.45 + UP*.55)
    pelvis_up = unit(pelvis_up - hip_axis*np.dot(pelvis_up, hip_axis))
    j = dict(Pelvis=pelvis, Chest=pelvis+torso*(.52/1.12), Neck=pelvis+torso*.52)
    gaze = unit(value('headForward'))
    # Keyed gaze and head axis form one anatomical frame; the neck is not extended.
    head_up = unit(UP - gaze*np.dot(UP, gaze))
    j['Head'] = j['Neck'] + head_up*.12
    soles = {}
    i = max(0, min(len(keys)-2, int(np.searchsorted(times,t,side='right'))-1))
    u = float(np.clip((t-times[i])/(times[i+1]-times[i]),0,1))
    for n, side in enumerate(('Left','Right')):
        ankle = curve(times, [k['feet'][side]['ankle'] for k in keys], t)
        foot = qmatrix(slerp(keys[i]['feet'][side]['rotation'], keys[i+1]['feet'][side]['rotation'], u))
        hip = pelvis + hip_axis*(.09 if n else -.09)
        pole = unit(np.array([1.,0.,.15 if n==0 else -.15]))
        j[side+'Hip'] = hip
        j[side+'Knee'] = limb(hip, ankle, .44, .43, pole)
        j[side+'Ankle'] = ankle
        soles[side] = foot[:,1]
        j[side+'Toe'] = ankle + foot[:,2]*math.sqrt(.14*.14-.07*.07) - foot[:,1]*.07
        shoulder = pelvis + torso*.44 + shoulder_axis*(.18 if n else -.18)
        upper = unit(curve(body_times, [k['control']['arms'][side][0] for k in body_keys], t))
        fore = unit(curve(body_times, [k['control']['arms'][side][1] for k in body_keys], t))
        j[side+'Shoulder'] = shoulder
        j[side+'Elbow'] = shoulder + upper*.29
        j[side+'Hand'] = j[side+'Elbow'] + fore*.27
    board_keys = [k for k in keys if k['control'].get('boardKey', True)]
    board_times = [k['t'] for k in board_keys]
    angles = curve(board_times,[k['boardAngles'] for k in board_keys],t)
    origin = curve(board_times,[k['boardOrigin'] for k in board_keys],t)
    q = euler_quat(angles)
    dirs = dict(zip(DIRS,(unit(np.cross(hip_axis,pelvis_up)),pelvis_up,
                         unit(np.cross(shoulder_axis,torso)),torso,gaze)))
    event = next(e for e in reversed(spec['events']) if e['t'] <= t+1e-9)
    source_frame = min(278., spec['firstSourceFrame']+t*spec['sourceFps'])
    return dict(t=t, sourceFrame=source_frame, sourceCacheTime=source_frame/spec['sourceFps'],
                sourceVideoTime=136+source_frame/spec['sourceFps'], authoredRecovery=t>(278-spec['firstSourceFrame'])/spec['sourceFps']+1e-9,
                joints={k:j[k].tolist() for k in JOINTS}, dirs={k:dirs[k].tolist() for k in DIRS},
                soleUp={k:soles[k].tolist() for k in soles}, board=dict(origin=origin.tolist(),quaternion=q.tolist()),
                phase=event['phase'],contacts=sum({'LeftFoot':1,'RightFoot':2}.get(x,0) for x in event['contacts']),
                authoredBoardEuler=angles.tolist())


def bake(spec):
    keys = authored_keys(spec)
    frames = [sample(spec,keys,float(t)) for t in np.linspace(0,spec['duration'],spec['sampleCount'])]
    metadata = dict(spec['metadata'], reviewed=False, id='kickflip-source-keyed-v1', movementKind='kickflip',
                    sourceFps=spec['sourceFps'], firstSourceFrame=spec['firstSourceFrame'],
                    sourceRootMotion=spec['sourceRootMotion'], authored=True, authoredStance='regular', canonicalStance='regular', sourceFilmedStance='uncertain; authored anatomical mirror mapping',
                    toeContract='Physical toe = ankle + flatForward*sqrt(.14^2-.07^2) - soleUp*.07, exactly once.',
                    sampleLimit='Original source-keyed authored interpolation, not measured or detector mocap.')
    clip = dict(format='skate-authored-coupled-keys/1',metadata=metadata,stance='regular',fps=(spec['sampleCount']-1)/spec['duration'],
                duration=spec['duration'],events=contact_events(spec['events'],spec['duration']),frames=frames)
    return clip, [dict(sample(spec,keys,k['t']), sourceFrame=k['sourceFrame']) for k in keys[:-1]]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--keys',type=Path,default=DEFAULT_KEYS)
    parser.add_argument('--output',type=Path,required=True,help='Private output folder; includes original authored JSON and bounded pack.')
    args = parser.parse_args()
    spec = json.loads(args.keys.read_text())
    clip,keyframes = bake(spec)
    packed = encode_pack([clip])
    args.output.mkdir(parents=True,exist_ok=True)
    (args.output/'kickflip-source-keyed-v1.json').write_text(json.dumps(clip,indent=2)+'\n')
    (args.output/'keys-evaluated.private.json').write_text(json.dumps(keyframes,indent=2)+'\n')
    (args.output/'skate_captures.skmc').write_bytes(packed)
    print('AUTHORED_KEYED_CLIP',len(clip['frames']),len(packed),'reviewed=false')

if __name__ == '__main__':
    main()
