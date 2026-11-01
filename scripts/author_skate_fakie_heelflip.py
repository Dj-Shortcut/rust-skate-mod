#!/usr/bin/env python3
"""Bake an explicitly authored Fakie Heelflip from the shared Fakie body.

The reference is a Fakie Kickflip, not a filmed Fakie Heelflip. The heel guide,
negative board roll, lower body, contact timing and recovery are authored.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from author_skate_heelflip_hybrid import source_upper, turn_x, turn_z
from author_skate_kickflip import curve, foot_basis, limb
from author_skate_kickflip_hybrid import basis, deck_shoe, smooth
from capture_math import JOINTS, DIRS, UP, contact_events, encode_pack, matrix_quat, qmatrix, slerp, unit

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'assets/authored/animation/fakie-heelflip-keyed'


def value(rows, frame):
    return curve([r[0] for r in rows], [r[1:] for r in rows], frame)


def reference_height(spec, frame):
    ref = spec['sourceRootMotion']
    f = float(np.clip((frame-ref['popFrame'])/(ref['landFrame']-ref['popFrame']), 0, 1))
    return .5*ref['gravity']*ref['referenceFlight']**2*f*(1-f)


def board_at(spec, frame):
    pitch, roll = float(value(spec['boardPitch'], frame)[0]), float(value(spec['boardRoll'], frame)[0])
    rotation = turn_x(-pitch)@turn_z(roll)
    origin = np.array([0., value(spec['boardHeight'], frame)[0], 0.])
    if frame <= spec['pressEndFrame']:
        hull = np.asarray(spec['support']['board']+spec['support']['wheel'])
        origin[1] = max(origin[1], -float(np.min((hull@rotation.T)[:, 1])))
    return origin, rotation


def heel_frame(spec, origin, rotation):
    """Place the heel patch, rather than the sole or side instep, at the edge."""
    h = spec['heelContact']
    foot = rotation@foot_basis(h['yaw'], h['pitch'], h['roll'])
    ankle = origin+rotation@np.asarray(h['deckLocal'])-foot@np.asarray(h['shoeLocal'])
    return ankle, foot


def shoe(spec, origin, rotation, frame, side):
    config = spec['feet'][side]
    x, z, yaw = config['load']
    if side == 'Left':
        slide = smooth((frame-spec['tailStrikeFrame']+2.0)/2.1)
        z = z*(1-slide)+.275*slide
    ankle, foot = deck_shoe(spec, origin, rotation, x, z, yaw, .005)
    if side == 'Left':
        h = spec['heelContact']
        guide_a, guide_r = heel_frame(spec, origin, rotation)
        blend = smooth((frame-h['start'])/(h['full']-h['start']))
        ankle = ankle*(1-blend)+guide_a*blend
        foot = qmatrix(slerp(matrix_quat(foot), matrix_quat(guide_r), blend))
        release_start, release_end = h['end'], spec['pressEndFrame']+1.0
    else:
        release_start, release_end = spec['rearReleaseFrame'], spec['pressEndFrame']+1.0
    free = origin+value(config['free'], frame)
    free_r = basis(unit(np.array([1., 0., .25 if side == 'Left' else .02])), UP)
    release = smooth((frame-release_start)/(release_end-release_start))
    ankle = ankle*(1-release)+free*release
    foot = qmatrix(slerp(matrix_quat(foot), matrix_quat(free_r), release))
    x, z, yaw = config['catch']
    attached, attached_r = deck_shoe(spec, origin, rotation, x, z, yaw, .005)
    catch = smooth((frame-2727.5)/(2730-2727.5))
    ankle = ankle*(1-catch)+attached*catch
    foot = qmatrix(slerp(matrix_quat(foot), matrix_quat(attached_r), catch))
    return ankle, foot


def sample(body, spec, frame):
    captured, directions = source_upper(body, frame)
    origin, rotation = board_at(spec, frame)
    feet = {side:shoe(spec, origin, rotation, frame, side) for side in ['Left','Right']}
    pelvis = origin+value(spec['pelvisRelativeToBoard'], frame)
    # The same anatomical frame is used in backwards Fakie travel. Travel is
    # supplied by the controller; turning this frame by travel would swap feet.
    forward = np.array([1., 0., 0.])
    across = np.array([0., 0., -1.])
    hip_offsets = dict(Left=-across*.09, Right=across*.09)
    # Leave anatomical knee flex while the runtime replaces the reference
    # carrier rise during the ground-held press; a nearly locked source knee
    # cannot absorb the ordinary charged-flight timing difference.
    reach_share = spec['reachShare']['press']+(spec['reachShare']['free']-spec['reachShare']['press'])*smooth((frame-spec['rearReleaseFrame'])/(spec['pressEndFrame']-spec['rearReleaseFrame']))
    maximum = []
    for side in ['Left','Right']:
        ankle = feet[side][0]; hip = pelvis+hip_offsets[side]
        horizontal = float(np.linalg.norm((hip-ankle)*np.array([1.,0.,1.])))
        if horizontal >= .87*.995:
            raise ValueError('Authored Fakie heel horizontal leg reach exceeds fixed chain')
        maximum.append(ankle[1]+math.sqrt((.87*reach_share)**2-horizontal**2))
    pelvis[1] = min(pelvis[1], min(maximum))
    torso = unit(np.asarray(captured['Neck'])-np.asarray(captured['Pelvis']))
    # Retain the shared source extension and fold, then return naturally toward
    # upright riding. No noisy ankle or hidden-knee source points drive support.
    lean = min(math.radians(55), math.acos(float(np.clip(torso[1], -.99, 1))))
    direction = unit(torso-UP*torso[1], forward)
    torso = UP*math.cos(lean)+direction*math.sin(lean)
    recovery = smooth((frame-2741)/(2762-2741))
    goal_up = unit(np.array([math.sin(math.radians(12)), math.cos(math.radians(12)), 0.]))
    torso = unit(torso*(1-recovery)+goal_up*recovery)
    shoulders = unit(across-torso*np.dot(across, torso))
    j = dict(Pelvis=pelvis, Chest=pelvis+torso*(.52/1.12), Neck=pelvis+torso*.52)
    gaze = unit(np.asarray(directions['HeadForward'])*.65+np.array([.85,-.4,-.25])*.35)
    gaze[1] = float(np.clip(gaze[1], -.7, .25)); gaze=unit(gaze)
    head_up = unit(UP-gaze*np.dot(UP,gaze))
    j['Head'] = j['Neck']+head_up*.12
    goal, _ = source_upper(body, 2755)
    arm_return = smooth((frame-2730)/(2745-2730))
    soles = {}
    for side, sign in [('Left',-1),('Right',1)]:
        ankle, foot = feet[side]; hip=pelvis+hip_offsets[side]
        j[side+'Hip']=hip
        pole=unit(forward+np.array([0.,0.,.10 if side=='Left' else -.10]))
        j[side+'Knee']=limb(hip,ankle,.44,.43,pole)
        j[side+'Ankle']=ankle
        j[side+'Toe']=ankle+foot[:,2]*math.sqrt(.14**2-.07**2)-foot[:,1]*.07
        soles[side]=foot[:,1]
        shoulder=pelvis+torso*.44+shoulders*(sign*.18)
        upper=unit(np.asarray(captured[side+'Elbow'])-np.asarray(captured[side+'Shoulder']))
        fore=unit(np.asarray(captured[side+'Hand'])-np.asarray(captured[side+'Elbow']))
        goal_upper=unit(np.asarray(goal[side+'Elbow'])-np.asarray(goal[side+'Shoulder']))
        goal_fore=unit(np.asarray(goal[side+'Hand'])-np.asarray(goal[side+'Elbow']))
        upper=unit(upper*(1-arm_return)+goal_upper*arm_return)
        fore=unit(fore*(1-arm_return)+goal_fore*arm_return)
        j[side+'Shoulder']=shoulder;j[side+'Elbow']=shoulder+upper*.29;j[side+'Hand']=j[side+'Elbow']+fore*.27
    reference=reference_height(spec,frame)
    for name in j:j[name]=j[name]-UP*reference
    dirs=dict(zip(DIRS,[forward,UP,unit(np.cross(shoulders,torso)),torso,gaze]))
    event=next(e for e in reversed(spec['events'])if e['frame']<=frame+1e-8)
    phase={'load':1,'pop':2,'air':3,'catch':4,'land':5,'recovery':6}[event['phase']]
    return dict(t=(frame-spec['firstSourceFrame'])/spec['sourceFps'],sourceFrame=frame,
                joints={k:j[k].tolist()for k in JOINTS},dirs={k:dirs[k].tolist()for k in DIRS},
                soleUp={k:soles[k].tolist()for k in soles},board=dict(origin=(origin-UP*reference).tolist(),quaternion=matrix_quat(rotation).tolist()),
                phase=phase,contacts=sum({'LeftFoot':1,'RightFoot':2}[k]for k in event['contacts']))


def bake(body_bytes, spec):
    if hashlib.sha256(body_bytes).hexdigest()!=spec['sourceBodySha256']:
        raise ValueError('Shared Fakie body differs from the pinned source')
    if spec['id']!='fakie-heelflip-source-keyed-v1' or spec['movementKind']!='fakie-heelflip':
        raise ValueError('Wrong authored Fakie Heelflip identity')
    if spec['signature']!=dict(pop='Ollie',switch=True,tailLeads=True,flip=-360,shove=0,spin=0):
        raise ValueError('Wrong Fakie signature')
    support_ref=spec['supportReference']; support_path=ROOT/support_ref['path']
    support_bytes=support_path.read_bytes()
    if hashlib.sha256(support_bytes).hexdigest()!=support_ref['sha256']:
        raise ValueError('Original board support geometry differs from the pinned source')
    spec=dict(spec,support=json.loads(support_bytes)['hybrid']['support'])
    body=json.loads(body_bytes)
    if body['independentFakieHeelflipSource'] is not False or body['baseMovementKind']!='fakie-kickflip':
        raise ValueError('The derivation must remain explicitly shared-body authored motion')
    duration=(spec['lastSourceFrame']-spec['firstSourceFrame'])/spec['sourceFps']
    count=int(math.ceil(duration*spec['sampleHz']))
    times=sorted(set([i/spec['sampleHz']for i in range(count)]+[duration]+[(e['frame']-spec['firstSourceFrame'])/spec['sourceFps']for e in spec['events']]+[(spec['tailStrikeFrame']-spec['firstSourceFrame'])/spec['sourceFps']]))
    frames=[sample(body,spec,spec['firstSourceFrame']+t*spec['sourceFps'])for t in times]
    events=[dict(t=(e['frame']-spec['firstSourceFrame'])/spec['sourceFps'],phase=e['phase'],contacts=e['contacts'])for e in spec['events']]
    metadata=dict(id=spec['id'],label='Fakie Heelflip — authored shared Fakie body / heel derivation [UNREVIEWED]',movementKind=spec['movementKind'],reviewed=False,authored=True,independentSource=False,canonicalStance='regular',sourceFilmedStance=body['sourceFilmedStance'],sourceUrl=body['sourceUrl'],sourceAuthor=body['sourceAuthor'],sourcePoseSha256=body['sourcePoseSha256'],sourceVideoSha256=body['sourceVideoSha256'],sourceFps=spec['sourceFps'],firstSourceFrame=spec['firstSourceFrame'],lastSourceFrame=spec['lastSourceFrame'],sourceRootMotion=spec['sourceRootMotion'],sourceBodySha256=spec['sourceBodySha256'],baseSourcePackSha256=body['baseSourcePackSha256'],baseMovementKind=body['baseMovementKind'],provenance=spec['provenance'],uncertainty=spec['uncertainty'],authoredBoardAndContacts=True,toeContract='Physical toe endpoints contain one sole-up toeDrop; the native sampler restores the flat target before the writer drop.')
    clip=dict(format='skate-authored-shared-source-keyed/1',metadata=metadata,stance='regular',fps=spec['sampleHz'],duration=duration,events=contact_events(events,duration),frames=frames)
    return clip,encode_pack([clip])


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--body',type=Path,default=ASSET/'shared-fakie-body.json');p.add_argument('--controls',type=Path,default=ASSET/'constraints.json');p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    clip,packed=bake(a.body.read_bytes(),json.loads(a.controls.read_text()))
    a.output.mkdir(parents=True,exist_ok=True);temporary=[]
    try:
        for name,data in [(clip['metadata']['id']+'.json',(json.dumps(clip,indent=2)+'\n').encode()),('skate_captures.skmc',packed)]:
            tmp=a.output/(name+'.tmp');tmp.write_bytes(data);temporary.append((tmp,a.output/name))
        for tmp,target in temporary:tmp.replace(target)
    finally:
        for tmp,_ in temporary:
            if tmp.exists():tmp.unlink()
    print('AUTHORED_FAKIE_HEEL',len(clip['frames']),len(clip['events']),hashlib.sha256(packed).hexdigest(),'reviewed=false')


if __name__=='__main__':main()
