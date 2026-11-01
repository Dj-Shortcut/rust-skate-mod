#!/usr/bin/env python3
"""Bake own-source Switch Kickflip controls, never a mirrored normal capture.

The selected body directions come from Paul Rodriguez's independent landed
switch take. Pelvis, invisible limb depth, shoe contacts and board motion are
explicitly authored. The failed generic source-root recipe is not promoted.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import numpy as np
from author_skate_kickflip import curve, limb
from author_skate_kickflip_hybrid import basis, body_at, deck_shoe, smooth
from capture_math import JOINTS, DIRS, UP, contact_events, encode_pack, euler_quat, matrix_quat, qmatrix, slerp, unit

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'assets/authored/animation/switch-kickflip-keyed'


def value(rows, frame):
    return curve([r[0] for r in rows], [r[1:] for r in rows], frame)


def carrier(spec, frame):
    ref = spec['sourceRootMotion']
    u = float(np.clip((frame-ref['popFrame'])/(ref['landFrame']-ref['popFrame']), 0, 1))
    return .5*ref['gravity']*ref['referenceFlight']**2*u*(1-u)


def instep(spec, board_origin, board_rotation):
    """Align the own shoe's side/upper patch to the curved heel-side nose."""
    patch = spec['instep']; shape = spec['hybrid']['popContact']
    x, _, z = patch['deckLocal']
    normal = unit(np.array([-2*shape['concave']*x/shape['deckHalfWidth']**2, 1.,
                            -2*shape['kickRise']*max(0., abs(z)-shape['kickStart'])/shape['kickSpan']**2]))
    outward = -board_rotation@normal
    toe = board_rotation@unit(np.array([1., 0., .8]))
    toe = unit(toe-outward*np.dot(toe, outward))
    local_normal = unit(np.array(patch['surfaceNormalLocal']))
    local_toe = unit(np.array([0., 0., 1.])-local_normal*local_normal[2])
    local = np.column_stack((local_normal, local_toe, unit(np.cross(local_normal, local_toe))))
    world = np.column_stack((outward, toe, unit(np.cross(outward, toe))))
    foot = world@local.T
    return board_origin+board_rotation@np.array(patch['deckLocal'])-foot@np.array(patch['shoeLocal']), foot


def sample(body, spec, t):
    frame = spec['firstSourceFrame']+t*spec['sourceFps']
    captured, directions, _ = body_at(body, t)
    pitch = float(value(spec['boardPitch'], frame)[0])
    roll = float(value(spec['boardRoll'], frame)[0])
    q = euler_quat([-pitch, 0., roll]); rotation = qmatrix(q)
    origin = np.array([0., value(spec['boardHeight'], frame)[0], 0.])
    if frame <= spec['pressEndFrame']:
        hull = np.asarray(spec['support']['board']+spec['support']['wheel'])
        origin[1] = max(origin[1], -float(np.min((hull@rotation.T)[:, 1])))
    pelvis = value(spec['pelvisKeys'], frame)
    # Heading uses the own visible shoulder/hip side, with estimated depth
    # regularized into one coherent anatomical frame instead of a jumping pole.
    forward = unit(directions['ChestForward']*np.array([1., 0., 1.]))
    if forward[0] < 0: forward = -forward
    forward = unit(np.array([1., 0., 0.])*.75+forward*.25)
    hip_axis = unit(np.cross(UP, forward))
    torso = unit(captured['Neck']-captured['Pelvis'])
    # The raw model's far-side depth is not observed. A modest source-facing
    # lean retains the filmed fold without a camera-axis back arch.
    lean = float(np.clip(np.dot(torso, forward), -.12, .55))
    torso = unit(UP*math.sqrt(max(.01, 1-lean*lean))+forward*lean)
    shoulders = unit(hip_axis-torso*np.dot(hip_axis, torso))
    j = dict(Pelvis=pelvis, Chest=pelvis+torso*(.52/1.12), Neck=pelvis+torso*.52)
    gaze = unit(directions['HeadForward']*.7+forward*.3)
    gaze[1] = float(np.clip(gaze[1], -.45, .25)); gaze = unit(gaze)
    head_up = unit(UP-gaze*np.dot(UP, gaze)); j['Head'] = j['Neck']+head_up*.12
    soles = {}
    for side, sign in [('Left', -1), ('Right', 1)]:
        hip = pelvis+hip_axis*sign*.09
        config = spec['feet'][side]
        x, z, yaw = config['load']
        if side == 'Left':
            slide = smooth((frame-spec['tailStrikeFrame']+1.8)/2.0)
            z = z*(1-slide)+.275*slide
        ankle, foot = deck_shoe(spec, origin, rotation, x, z, yaw, .005)
        if side == 'Left':
            patch = spec['instep']; start, full, end = patch['start'], patch['full'], patch['end']
            if frame > start:
                ia, ir = instep(spec, origin, rotation)
                weight = smooth((frame-start)/(full-start))
                ankle = ankle*(1-weight)+ia*weight
                foot = qmatrix(slerp(matrix_quat(foot), matrix_quat(ir), weight))
            release_start = spec['frontReleaseFrame']; release_end = end
        else:
            release_start = spec['rearReleaseFrame']; release_end = spec['pressEndFrame']
        free = value(config['free'], frame)
        free_r = basis(unit(np.array([1., 0., .25 if side == 'Left' else .05])), UP)
        release = smooth((frame-release_start)/(release_end-release_start))
        ankle = ankle*(1-release)+free*release
        foot = qmatrix(slerp(matrix_quat(foot), matrix_quat(free_r), release))
        catch_frame = 5498 if side == 'Left' else 5500
        catch_start = catch_frame-2
        cx, cz, cyaw = config['catch']
        attached, attached_r = deck_shoe(spec, origin, rotation, cx, cz, cyaw, .005)
        catch = smooth((frame-catch_start)/(catch_frame-catch_start))
        ankle = ankle*(1-catch)+attached*catch
        foot = qmatrix(slerp(matrix_quat(foot), matrix_quat(attached_r), catch))
        j[side+'Hip'] = hip
        # Far knee depth is consciously reconstructed, not a confidence upgrade.
        pole = unit(forward+np.array([0., 0., .12 if side == 'Left' else -.12]))
        j[side+'Knee'] = limb(hip, ankle, .44, .43, pole)
        j[side+'Ankle'] = ankle
        j[side+'Toe'] = ankle+foot[:, 2]*math.sqrt(.14**2-.07**2)-foot[:, 1]*.07
        soles[side] = foot[:, 1]
        shoulder = pelvis+torso*.44+shoulders*sign*.18
        upper = unit(captured[side+'Elbow']-captured[side+'Shoulder'])
        fore = unit(captured[side+'Hand']-captured[side+'Elbow'])
        # Preserve the visible swing. The mostly hidden far left arm uses an
        # explicitly authored compact balance reconstruction through the air.
        if side == 'Left':
            w = smooth((frame-5481)/5)*(1-smooth((frame-5504)/10))
            upper = unit(upper*(1-w)+unit(np.array([-.35, -.25, .9]))*w)
            fore = unit(fore*(1-w)+unit(np.array([.5, -.7, .25]))*w)
        j[side+'Shoulder'] = shoulder
        j[side+'Elbow'] = shoulder+upper*.29
        j[side+'Hand'] = j[side+'Elbow']+fore*.27
    reference = carrier(spec, frame)
    for name in j: j[name] = j[name]-UP*reference
    origin = origin-UP*reference
    pelvis_up = unit(torso*.45+UP*.55); pelvis_up = unit(pelvis_up-hip_axis*np.dot(pelvis_up, hip_axis))
    dirs = dict(zip(DIRS, [unit(np.cross(hip_axis, pelvis_up)), pelvis_up,
                           unit(np.cross(shoulders, torso)), torso, gaze]))
    events = spec['events']; event = next(e for e in reversed(events) if e['frame'] <= frame+1e-8)
    phase = {'load':1, 'pop':2, 'air':3, 'catch':4, 'land':5, 'recovery':6}[event['phase']]
    return dict(t=t, sourceFrame=frame, sourceVideoTime=frame/spec['sourceFps'],
                joints={k:j[k].tolist() for k in JOINTS}, dirs={k:dirs[k].tolist() for k in DIRS},
                soleUp={k:soles[k].tolist() for k in soles}, board=dict(origin=origin.tolist(), quaternion=q.tolist()),
                phase=phase, contacts=sum({'LeftFoot':1,'RightFoot':2}[k] for k in event['contacts']),
                authoredBoardEuler=[-pitch, 0., roll])


def bake(body_bytes, spec):
    if hashlib.sha256(body_bytes).hexdigest() != spec['sourceBodySha256']:
        raise ValueError('Own Switch source body hash differs')
    if spec['id'] != 'switch-kickflip-source-keyed-v1' or spec['movementKind'] != 'switch-kickflip':
        raise ValueError('Wrong own Switch clip identity')
    if not (spec['firstSourceFrame']==5470 and spec['lastSourceFrame']==5534 and
            spec['firstSourceFrame'] < spec['tailStrikeFrame'] < spec['frontReleaseFrame'] < spec['rearReleaseFrame'] < spec['pressEndFrame']):
        raise ValueError('Wrong own source/contact window')
    body = json.loads(body_bytes)
    duration = (spec['lastSourceFrame']-spec['firstSourceFrame'])/spec['sourceFps']
    count = int(math.ceil(duration*spec['sampleHz']))
    times = sorted(set([i/spec['sampleHz'] for i in range(count)]+[duration]+[(e['frame']-spec['firstSourceFrame'])/spec['sourceFps'] for e in spec['events']]+[(spec['tailStrikeFrame']-spec['firstSourceFrame'])/spec['sourceFps']]))
    frames = [sample(body,spec,t) for t in times]
    events = [dict(t=(e['frame']-spec['firstSourceFrame'])/spec['sourceFps'],phase=e['phase'],contacts=e['contacts']) for e in spec['events']]
    metadata = dict(id=spec['id'],label='Switch Kickflip — own Paul source-keyed reconstruction [UNREVIEWED]', movementKind=spec['movementKind'],reviewed=False,authored=True,capturedBody=True,canonicalStance='regular',sourceFilmedStance=body['filmedStance'],sourceUrl=body['sourceUrl'],sourceAuthor=body['sourceAuthor'],sourcePoseSha256=body['sourcePoseSha256'],sourceVideoSha256=body['sourceVideoSha256'],sourceFps=spec['sourceFps'],firstSourceFrame=5470,lastSourceFrame=5534,sourceRootMotion=spec['sourceRootMotion'],provenance=spec['provenance'],uncertainty=spec['uncertainty'],lowVisibilityModelFrames=body['lowVisibilityModelFrames'],toeContract='Physical toe endpoints include one sole-up toeDrop; native sampler restores the flat target before the writer drop.',authoredBoardAndContacts=True,independentSource=True)
    clip=dict(format='skate-own-source-keyed/1',metadata=metadata,stance='regular',fps=spec['sampleHz'],duration=duration,events=contact_events(events,duration),frames=frames)
    return clip,encode_pack([clip])


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--body',type=Path,default=ASSET/'captured-body.json');p.add_argument('--controls',type=Path,default=ASSET/'constraints.json');p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    clip,packed=bake(a.body.read_bytes(),json.loads(a.controls.read_text()))
    a.output.mkdir(parents=True,exist_ok=True)
    (a.output/'switch-kickflip-source-keyed-v1.json').write_text(json.dumps(clip,indent=2)+'\n')
    (a.output/'skate_captures.skmc').write_bytes(packed)
    print('OWN_SWITCH',len(clip['frames']),len(clip['events']),hashlib.sha256(packed).hexdigest(),'reviewed=false')

if __name__=='__main__': main()
