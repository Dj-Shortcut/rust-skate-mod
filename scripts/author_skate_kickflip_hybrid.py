#!/usr/bin/env python3
"""Bake the existing captured skeleton plus bounded authored board/contact edits.

The product inputs are compact retarget controls, not video/detector binaries.
One explicit source-carrier translation was removed at import; runtime adds
its physical upright carrier once. No legacy runtime correction stack is run.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import numpy as np
from author_skate_kickflip import authored_keys, curve, foot_basis, limb
from capture_math import JOINTS, DIRS, UP, contact_events, encode_pack, euler_quat, matrix_quat, qmatrix, slerp, unit

ROOT=Path(__file__).resolve().parents[1]
ASSET=ROOT/'assets/authored/animation/kickflip-keyed'


def smooth(x):
    x=float(np.clip(x,0,1));return x*x*x*(10+x*(-15+6*x))


def window(frame,points):
    a,b,c,d=points
    return smooth((frame-a)/(b-a))*(1-smooth((frame-c)/(d-c)))


def basis(forward,up):
    up=unit(up);forward=unit(forward-up*np.dot(forward,up))
    return np.column_stack((unit(np.cross(up,forward)),up,forward))


def body_at(controls,t):
    rows=np.asarray(controls['rows'],dtype=float);i=max(0,min(len(rows)-2,int(np.searchsorted(rows[:,0],t,side='right'))-1))
    u=float(np.clip((t-rows[i,0])/(rows[i+1,0]-rows[i,0]),0,1));row=rows[i]*(1-u)+rows[i+1]*u;p=1
    joints={}
    for name in controls['jointOrder']:joints[name]=row[p:p+3];p+=3
    dirs={}
    for name in controls['directionOrder']:dirs[name]=unit(row[p:p+3]);p+=3
    soles={}
    for side in ('Left','Right'):soles[side]=unit(row[p:p+3]);p+=3
    # Canonical resampling retains captured segment directions and gesture;
    # it does not create manually posed torso/arms/head keys.
    raw={k:v.copy()for k,v in joints.items()};pel=raw['Pelvis'];torso=unit(raw['Neck']-pel)
    hips=unit(raw['RightHip']-raw['LeftHip']);shoulders=unit(raw['RightShoulder']-raw['LeftShoulder']);shoulders=unit(shoulders-torso*np.dot(shoulders,torso))
    joints.update(Pelvis=pel,Chest=pel+torso*(.52/1.12),Neck=pel+torso*.52)
    joints['Head']=joints['Neck']+unit(raw['Head']-raw['Neck'])*.12
    for side,sign in (('Left',-1),('Right',1)):
        hip=pel+hips*(sign*.09);shoulder=pel+torso*.44+shoulders*(sign*.18)
        joints[side+'Hip']=hip;joints[side+'Shoulder']=shoulder
        joints[side+'Elbow']=shoulder+unit(raw[side+'Elbow']-raw[side+'Shoulder'])*.29
        joints[side+'Hand']=joints[side+'Elbow']+unit(raw[side+'Hand']-raw[side+'Elbow'])*.27
        joints[side+'Knee']=hip+unit(raw[side+'Knee']-raw[side+'Hip'])*.44
        joints[side+'Ankle']=joints[side+'Knee']+unit(raw[side+'Ankle']-raw[side+'Knee'])*.43
        joints[side+'Toe']=joints[side+'Ankle']+unit(raw[side+'Toe']-raw[side+'Ankle'])*.14
    return joints,dirs,soles


def sample(controls,spec,keys,t):
    joints,dirs,soles=body_at(controls,t);frame=254+t*spec['sourceFps'];h=spec['hybrid'];times=[k['t']for k in keys]
    board_keys=[k for k in keys if k['control'].get('boardKey',True)];bt=[k['t']for k in board_keys]
    angles=curve(bt,[k['boardAngles']for k in board_keys],t);origin=curve(bt,[k['boardOrigin']for k in board_keys],t);rotation=euler_quat(angles);br=qmatrix(rotation)
    original={k:v.copy()for k,v in joints.items()}
    # Preserve captured gesture, with the already-reviewed smooth landing height.
    land_weight=smooth((frame-h['softLandingStart'])/(h['softLandingFull']-h['softLandingStart']))
    sy=curve([q[0]for q in h['softHipKeys']],[[q[1]]for q in h['softHipKeys']],frame)[0]
    delta=UP*((sy-joints['Pelvis'][1])*land_weight)
    for name in joints:joints[name]=joints[name]+delta
    i=max(0,min(len(keys)-2,int(np.searchsorted(times,t,side='right'))-1));u=float(np.clip((t-times[i])/(times[i+1]-times[i]),0,1))
    late=window(frame,h['lateClearanceWindow'])*h['lateClearance'];ground=smooth((frame-271)/2)
    for side in ('Left','Right'):
        up=soles[side];ff=unit(original[side+'Toe']-original[side+'Ankle']+up*.07);foot=basis(ff,up)
        ankle=original[side+'Ankle']+delta+UP*late
        if side=='Left':
            weight=window(frame,h['frontWindow']);target=curve(times,[k['feet'][side]['ankle']for k in keys],t);target_r=qmatrix(slerp(keys[i]['feet'][side]['rotation'],keys[i+1]['feet'][side]['rotation'],u))
        else:
            weight=window(frame,h['rearWindow']);target=original[side+'Ankle']+np.array(h['rearAnkleOffset']);target_r=basis(ff,UP)
        ankle=ankle*(1-weight)+target*weight;foot=qmatrix(slerp(matrix_quat(foot),matrix_quat(target_r),weight))
        if side=='Right':
            # An attached tail-pop sole must agree with the NEW board, rather
            # than retaining the old capture's unrelated pitch/vertical arc.
            flat_rear=np.array([0.,.106,-.24])
            if frame<=259:
                ankle=origin+br@flat_rear;foot=br@foot_basis(88)
            elif frame<260:
                pop=next(k for k in keys if k['sourceFrame']==259)
                pop_r=qmatrix(euler_quat(pop['boardAngles']))
                release_ankle=pop['boardOrigin']+pop_r@flat_rear
                release_r=pop_r@foot_basis(88)
                released=smooth(frame-259)
                ankle=release_ankle*(1-released)+ankle*released
                foot=qmatrix(slerp(matrix_quat(release_r),matrix_quat(foot),released))
        # Only the completed Catch pins the feet. Earlier late-Air lift remains free.
        flat=np.array([0.,.106,.24 if side=='Left'else-.24]);ground_ankle=origin+br@flat
        gf=ff-UP*np.dot(ff,UP);ground_r=br@basis(gf,UP)
        ankle=ankle*(1-ground)+ground_ankle*ground;foot=qmatrix(slerp(matrix_quat(foot),matrix_quat(ground_r),ground))
        hip=joints[side+'Hip'];source_axis=unit(original[side+'Ankle']-original[side+'Hip']);bend=original[side+'Knee']-original[side+'Hip'];bend=bend-source_axis*np.dot(bend,source_axis)
        # Minimal source-plane transport, never a generic anatomical knee cap.
        target_axis=unit(ankle-hip);cross=np.cross(source_axis,target_axis);dot=float(np.clip(np.dot(source_axis,target_axis),-1,1));ss=float(np.linalg.norm(cross))
        if ss>1e-8:
            axis=cross/ss;bend=bend*dot+np.cross(axis,bend)*ss+axis*np.dot(axis,bend)*(1-dot)
        elif dot<0:raise ValueError('Ambiguous opposite captured knee plane')
        joints[side+'Knee']=limb(hip,ankle,.44,.43,unit(bend));joints[side+'Ankle']=ankle;soles[side]=foot[:,1]
        joints[side+'Toe']=ankle+foot[:,2]*math.sqrt(.14*.14-.07*.07)-foot[:,1]*.07
    event=next(e for e in reversed(spec['events'])if e['t']<=t+1e-9)
    return dict(t=t,sourceFrame=min(frame,296),sourceCacheTime=min(frame,296)/spec['sourceFps'],sourceVideoTime=136+min(frame,296)/spec['sourceFps'],capturedBody=True,authoredLocalContacts=True,
        joints={k:joints[k].tolist()for k in JOINTS},dirs={k:dirs[k].tolist()for k in DIRS},soleUp={k:soles[k].tolist()for k in soles},board=dict(origin=origin.tolist(),quaternion=rotation.tolist()),phase=event['phase'],contacts=sum({'LeftFoot':1,'RightFoot':2}.get(x,0)for x in event['contacts']),authoredBoardEuler=angles.tolist())


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--body',type=Path,default=ASSET/'captured-body.json');p.add_argument('--constraints',type=Path,default=ASSET/'hybrid-constraints.json');p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    raw=a.body.read_bytes();controls=json.loads(raw);spec=json.loads(a.constraints.read_text());assert hashlib.sha256(raw).hexdigest()==spec['hybrid']['sourceBodySha256']
    keys=authored_keys(spec);frames=[sample(controls,spec,keys,float(t))for t in np.linspace(0,spec['duration'],spec['sampleCount'])]
    meta=dict(spec['metadata'],id='kickflip-source-keyed-v1',movementKind='kickflip',reviewed=False,authored=True,capturedBody=True,canonicalStance='regular',sourceFilmedStance='uncertain; retargeted canonical mapping',sourceFps=spec['sourceFps'],firstSourceFrame=254,lastSourceFrame=296,capturedBodyControlSha256=hashlib.sha256(raw).hexdigest(),capturedBodySourcePoseSha256=controls['sourcePoseSha256'],sourceRootMotion=dict(spec['sourceRootMotion'],extraction=controls['normalization']),provenance='Captured retarget skeleton retained; authored source-board keys, short local foot/contact repairs and smooth landing height. No legacy runtime correction stack; not fully authored body motion.')
    clip=dict(format='skate-capture-hybrid/1',metadata=meta,stance='regular',fps=(spec['sampleCount']-1)/spec['duration'],duration=spec['duration'],events=contact_events(spec['events'],spec['duration']),frames=frames)
    packed=encode_pack([clip]);a.output.mkdir(exist_ok=True,parents=True);(a.output/'kickflip-source-keyed-v1.json').write_text(json.dumps(clip,indent=2)+'\n');(a.output/'skate_captures.skmc').write_bytes(packed);print('HYBRID_CAPTURE_BODY_READY',len(frames),len(packed),hashlib.sha256(packed).hexdigest(),flush=True)

if __name__=='__main__':main()
