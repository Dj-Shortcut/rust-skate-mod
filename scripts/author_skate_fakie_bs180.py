#!/usr/bin/env python3
"""Bake one own Fakie BS180 take in a source-board-heading-relative frame.

Native NoseYaw owns the actual whole-rider/board turn. Full source scene yaw
is removed from every point, direction, sole and board, retaining the own
relative windup, compact pop, catch and recovery without adding a second180.
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

ROOT=Path(__file__).resolve().parents[1]
ASSET=ROOT/'assets/authored/animation/fakie-bs180-keyed'


def value(rows, frame):
    return curve([r[0]for r in rows],[r[1:]for r in rows],frame)


def reference_height(spec, frame):
    ref=spec['sourceRootMotion']
    u=float(np.clip((frame-ref['popFrame'])/(ref['landFrame']-ref['popFrame']),0,1))
    return .5*ref['gravity']*ref['referenceFlight']**2*u*(1-u)


def instep_frame(spec, origin, rotation, frame):
    """Outer-upper original shoe patch guides grip; it never flicks off edge."""
    h=spec['instep'];shape=spec['hybrid']['popContact'];x,z=value(h['deckPath'],frame)
    y=shape['kickRise']*(max(0.,abs(z)-shape['kickStart'])/shape['kickSpan'])**2+shape['concave']*(x/shape['deckHalfWidth'])**2
    normal=unit(np.array([-2*shape['concave']*x/shape['deckHalfWidth']**2,1.,-2*shape['kickRise']*max(0.,abs(z)-shape['kickStart'])/shape['kickSpan']**2]))
    outward=-rotation@normal
    yaw=math.radians(h['yaw']);toe=rotation@np.array([math.sin(yaw),0.,math.cos(yaw)])
    toe=unit(toe-outward*np.dot(toe,outward))
    local_normal=unit(np.asarray(spec['_shoeNormal']));local_toe=unit(np.array([0.,0.,1.])-local_normal*local_normal[2])
    local=np.column_stack((local_normal,local_toe,unit(np.cross(local_normal,local_toe))))
    world=np.column_stack((outward,toe,unit(np.cross(outward,toe))))
    foot=world@local.T
    return origin+rotation@np.array([x,y,z])-foot@np.asarray(spec['_shoeInstep']),foot


def foot_frame(spec, origin, rotation, frame, side):
    c=spec['feet'][side];x,z,yaw=c['load']
    if side=='Left':z=z*(1-smooth((frame-spec['popFrame'])/2.5))+.27*smooth((frame-spec['popFrame'])/2.5)
    ankle,foot=deck_shoe(spec,origin,rotation,x,z,yaw,.005)
    if side=='Left':
        h=spec['instep'];guide,guide_r=instep_frame(spec,origin,rotation,frame)
        blend=smooth((frame-h['start'])/(h['full']-h['start']))
        ankle=ankle*(1-blend)+guide*blend
        foot=qmatrix(slerp(matrix_quat(foot),matrix_quat(guide_r),blend))
        start,end=h['end'],h['releaseEnd']
    else:start,end=spec['rearReleaseFrame'],spec['pressEndFrame']
    free=origin+rotation@value(c['free'],frame)
    free_r=rotation@basis(unit(np.array([1.,0.,.25 if side=='Left'else .04])),UP)
    release=smooth((frame-start)/(end-start))
    ankle=ankle*(1-release)+free*release
    foot=qmatrix(slerp(matrix_quat(foot),matrix_quat(free_r),release))
    x,z,yaw=c['catch'];attached,attached_r=deck_shoe(spec,origin,rotation,x,z,yaw,.005)
    catch=smooth((frame-(spec['catchFrame']-2))/(2.))
    ankle=ankle*(1-catch)+attached*catch
    foot=qmatrix(slerp(matrix_quat(foot),matrix_quat(attached_r),catch))
    return ankle,foot


def sample(body, spec, frame):
    t=(frame-spec['firstSourceFrame'])/spec['sourceFps']
    captured,directions,_=body_at(body,t)
    yaw=float(value(spec['sourceBoardYaw'],frame)[0]);turn=qmatrix(euler_quat([0.,yaw,0.]))
    pitch=float(value(spec['earlyPitchKeys']if frame<spec['earlyMechanicsEndFrame']else spec['boardPitch'],frame)[0]);rotation=turn@qmatrix(euler_quat([-pitch,0.,0.]))
    origin=np.array([0.,value(spec['boardHeight'],frame)[0],0.])
    if frame<=spec['pressEndFrame']:
        hull=np.asarray(spec['support']['board']+spec['support']['wheel'])
        origin[1]=max(origin[1],-float(np.min((hull@rotation.T)[:,1])))
    feet={side:foot_frame(spec,origin,rotation,frame,side)for side in ['Left','Right']}
    pelvis=origin+turn@value(spec['pelvisRelativeToBoard'],frame)
    extension=spec['popExtension'];start,full,held,finish=extension['window']
    pelvis+=UP*extension['metres']*smooth((frame-start)/(full-start))*(1-smooth((frame-held)/(finish-held)))
    hip_axis=unit((captured['LeftHip']-captured['RightHip'])*np.array([1.,0.,1.]))
    facing=unit(np.cross(UP,hip_axis));maxima=[]
    share=spec['reachShare']['press']+(spec['reachShare']['free']-spec['reachShare']['press'])*smooth((frame-spec['rearReleaseFrame'])/(spec['pressEndFrame']-spec['rearReleaseFrame']))
    for side,sign in [('Left',1),('Right',-1)]:
        ankle=feet[side][0];hip=pelvis+hip_axis*(sign*.09);distance=float(np.linalg.norm((hip-ankle)*np.array([1.,0.,1.])))
        if distance>=.87*share:raise ValueError('Own Fakie BS180 source feet exceed the authored fixed-chain reach')
        maxima.append(ankle[1]+math.sqrt((.87*share)**2-distance**2))
    pelvis[1]=min(pelvis[1],min(maxima))
    old_torso=unit(captured['Neck']-captured['Pelvis']);lean_dir=unit(facing*.65+unit(old_torso* np.array([1.,0.,1.]),facing)*.35)
    fold=math.radians(float(value(spec['torsoFold'],frame)[0]));up=UP*math.cos(fold)+lean_dir*math.sin(fold)
    across=unit(captured['LeftShoulder']-captured['RightShoulder']);across=unit(across-up*np.dot(across,up))
    j=dict(Pelvis=pelvis,Chest=pelvis+up*(.52/1.12),Neck=pelvis+up*.52)
    chest_forward=unit(np.cross(up,across));recovery=smooth((frame-spec['lastFilmedSourceFrame'])/(spec['lastSourceFrame']-spec['lastFilmedSourceFrame']));chest_forward=unit(chest_forward*(1-recovery)+facing*recovery);gaze=unit(np.asarray(directions['HeadForward'])*.7+chest_forward*.3)
    gaze=unit(gaze*(1-recovery)+unit(facing-UP*.14)*recovery);gaze[1]=float(np.clip(gaze[1],-.65,-.08));gaze=unit(gaze);head_up=unit(UP-gaze*np.dot(UP,gaze));j['Head']=j['Neck']+head_up*.12
    local_vector=lambda v:unit(facing*v[0]+UP*v[1]+hip_axis*v[2])
    soles={}
    for side,sign in [('Left',1),('Right',-1)]:
        ankle,foot=feet[side];hip=pelvis+hip_axis*(sign*.09)
        pole=unit(facing+hip_axis*(sign*.1))
        if side=='Left':
            guide=spec['frontKneeGuide'];start,full,held,finish=guide['window'];weight=smooth((frame-start)/(full-start))*(1-smooth((frame-held)/(finish-held)))
            target=facing*guide['pole'][0]+UP*guide['pole'][1]+hip_axis*guide['pole'][2]
            if weight>0:pole=unit(pole*(1-weight)+target*weight)
        j[side+'Hip']=hip;j[side+'Knee']=limb(hip,ankle,.44,.43,pole)
        j[side+'Ankle']=ankle;j[side+'Toe']=ankle+foot[:,2]*math.sqrt(.14**2-.07**2)-foot[:,1]*.07;soles[side]=foot[:,1]
        shoulder=pelvis+up*.44+across*(sign*.18)
        upper=unit(captured[side+'Elbow']-captured[side+'Shoulder']);fore=unit(captured[side+'Hand']-captured[side+'Elbow'])
        c=spec['armReconstruction'];w=float(value(c['weights'],frame)[0]);a=c[side]
        upper=unit(upper*(1-w)+local_vector(value(a['upper'],frame))*w);fore=unit(fore*(1-w)+local_vector(value(a['fore'],frame))*w)
        # The original raised-wrist reconstruction reads as an overhead cheer.
        # Author only this Guide/Catch balance window in anatomical body axes;
        # shoulders and every non-arm point retain their accepted source paths.
        balance=spec['guideBalance'];start,full,held,finish=balance['window']
        weight=smooth((frame-start)/(full-start))*(1-smooth((frame-held)/(finish-held)))
        if weight>0:
            upper=unit(upper*(1-weight)+local_vector(balance[side]['upper'])*weight)
            fore=unit(fore*(1-weight)+local_vector(balance[side]['fore'])*weight)
        j[side+'Shoulder']=shoulder;j[side+'Elbow']=shoulder+upper*.29;j[side+'Hand']=j[side+'Elbow']+fore*.27
    dirs=dict(PelvisForward=facing,PelvisUp=UP,ChestForward=chest_forward,ChestUp=up,HeadForward=gaze)
    # Complete scene-heading removal. This includes physical toes, all limb
    # points, every anatomical direction, both sole frames AND board orientation.
    # Native NoseYaw already owns the actual spin and the landed Switch heading.
    inverse=turn.T;reference=reference_height(spec,frame)
    j={k:inverse@j[k]-UP*reference for k in JOINTS}
    dirs={k:unit(inverse@dirs[k])for k in DIRS};soles={k:unit(inverse@v)for k,v in soles.items()}
    normalized_board=inverse@rotation
    event=next(e for e in reversed(spec['events'])if e['frame']<=frame+1e-8)
    return dict(t=t,sourceFrame=min(frame,spec['lastFilmedSourceFrame']),authoredTimelineFrame=frame,authoredRecovery=frame>spec['lastFilmedSourceFrame'],sourceCacheTime=.033+min(frame,spec['lastFilmedSourceFrame'])/spec['sourceFps'],joints={k:j[k].tolist()for k in JOINTS},dirs={k:dirs[k].tolist()for k in DIRS},soleUp={k:v.tolist()for k,v in soles.items()},board=dict(origin=(inverse@origin-UP*reference).tolist(),quaternion=matrix_quat(normalized_board).tolist()),phase={'load':1,'pop':2,'air':3,'catch':4,'land':5,'recovery':6}[event['phase']],contacts=sum({'LeftFoot':1,'RightFoot':2}[k]for k in event['contacts']),removedSourceYawDegrees=yaw)


def bake(body_bytes, spec):
    if hashlib.sha256(body_bytes).hexdigest()!=spec['sourceBodySha256']:raise ValueError('Own Fakie BS180 body checksum differs')
    if spec['id']!='fakie-bs180-source-keyed-v1' or spec['movementKind']!='fakie-bs180' or spec['signature']!=dict(pop='Ollie',switch=True,tailLeads=True,flip=0,shove=0,spin=-180,body=0):raise ValueError('Wrong own Fakie BS180 identity/signature')
    ref=spec['geometryReference'];geometry=json.loads((ROOT/ref['path']).read_text());part=dict(shoe=geometry['shoe'],support=geometry['support'],shoeInstep=geometry['flickPatch']['shoeLocal'])
    if hashlib.sha256(json.dumps(part,sort_keys=True,separators=(',',':')).encode()).hexdigest()!=ref['geometryDataSha256']:raise ValueError('Original board/shoe geometry differs')
    spec=dict(spec,support=part['support'],hybrid=dict(popContact=part['shoe']),_shoeInstep=part['shoeInstep'],_shoeNormal=part['shoe']['instepSurfaceNormalLocal'])
    body=json.loads(body_bytes);duration=(spec['lastSourceFrame']-spec['firstSourceFrame'])/spec['sourceFps'];count=int(math.ceil(duration*spec['sampleHz']))
    times=sorted(set([i/spec['sampleHz']for i in range(count)]+[duration]+[(e['frame']-spec['firstSourceFrame'])/spec['sourceFps']for e in spec['events']]+[(spec['tailStrikeFrame']-spec['firstSourceFrame'])/spec['sourceFps']]))
    frames=[sample(body,spec,spec['firstSourceFrame']+t*spec['sourceFps'])for t in times]
    events=[dict(t=(e['frame']-spec['firstSourceFrame'])/spec['sourceFps'],phase=e['phase'],contacts=e['contacts'])for e in spec['events']]
    metadata=dict(id=spec['id'],movementKind=spec['movementKind'],label='Fakie BS180 — own plaid Halfcab source, native single-turn authority [UNREVIEWED]',reviewed=False,authored=True,independentSource=True,canonicalStance='regular',sourceFilmedStance=body['filmedStance'],sourceUrl=body['sourceUrl'],sourceAuthor=body['sourceAuthor'],sourceVideoSha256=body['sourceVideoSha256'],sourcePoseSha256=body['sourcePoseSha256'],sourceBodySha256=spec['sourceBodySha256'],sourceFps=spec['sourceFps'],firstSourceFrame=spec['firstSourceFrame'],lastSourceFrame=spec['lastFilmedSourceFrame'],authoredTimelineLastFrame=spec['lastSourceFrame'],authoredRecoveryAfterCut=True,sourceRootMotion=spec['sourceRootMotion'],nativeYawAuthority=True,removedSourceBoardYawKeys=spec['sourceBoardYaw'],provenance=spec['provenance'],uncertainty=spec['uncertainty'],authoredGapFrames=body['gapFrames'],lowConfidenceModelFrames=body['lowConfidenceModelFrames'],sourceTiming=body['timing'],authoredBoardAndContacts=True,toeContract='Physical toe endpoints include one sole-up toeDrop; the native sampler restores flat targets before writer drop.')
    clip=dict(format='skate-own-source-native-yaw-keyed/1',metadata=metadata,stance='regular',fps=spec['sampleHz'],duration=duration,events=contact_events(events,duration),frames=frames)
    return clip,encode_pack([clip])


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--body',type=Path,default=ASSET/'captured-body.json');p.add_argument('--controls',type=Path,default=ASSET/'constraints.json');p.add_argument('--output',type=Path,required=True);a=p.parse_args();clip,packed=bake(a.body.read_bytes(),json.loads(a.controls.read_text()));a.output.mkdir(parents=True,exist_ok=True);temporary=[]
    try:
        for name,data in [(clip['metadata']['id']+'.json',(json.dumps(clip,indent=2)+'\n').encode()),('skate_captures.skmc',packed)]:
            tmp=a.output/(name+'.tmp');tmp.write_bytes(data);temporary.append((tmp,a.output/name))
        for tmp,target in temporary:tmp.replace(target)
    finally:
        for tmp,_ in temporary:
            if tmp.exists():tmp.unlink()
    print('OWN_FAKIE_BS180',len(clip['frames']),len(clip['events']),hashlib.sha256(packed).hexdigest(),'reviewed=false nativeYaw=once')


if __name__=='__main__':main()
