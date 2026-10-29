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


def deck_shoe(spec,origin,rotation,x,z,yaw,gap):
    """Fit the authored flat sole to the actual curved deck, in its live frame."""
    shape=spec['hybrid']['popContact'];half=shape['deckHalfWidth'];start=shape['kickStart'];span=shape['kickSpan']
    height=lambda px,pz:shape['kickRise']*(max(0.,abs(pz)-start)/span)**2+shape['concave']*(px/half)**2
    kick=max(0.,abs(z)-start);normal=unit(np.array([-2*shape['concave']*x/(half*half),1.,-2*shape['kickRise']*kick/(span*span)*np.sign(z)]))
    radians=math.radians(yaw);foot=basis(np.array([math.sin(radians),0.,math.cos(radians)]),normal)
    sole=[np.array([side*width,shape['soleLocalY'],along])for along,width in shape['soleRows']for side in(-1,1)]
    points=[foot@p+np.array([x,0.,z])for p in sole]
    # Sample the convex sole boundary where it crosses the board outline too;
    # overhanging toes cannot be mistaken for support on an infinite plane.
    outline=[]
    for a in np.linspace(0,math.pi,19):outline.append(np.array([half*math.cos(a),shape['capBase']+shape['capLength']*math.sin(a)]))
    outline.append(np.array([-half,0.]))
    for a in np.linspace(math.pi,2*math.pi,19):outline.append(np.array([half*math.cos(a),-shape['capBase']+shape['capLength']*math.sin(a)]))
    outline.append(np.array([half,0.]))
    polygon=[points[i][[0,2]]for i in range(0,len(points),2)]+[points[i][[0,2]]for i in range(len(points)-1,0,-2)]
    area=sum(p[0]*polygon[(i+1)%len(polygon)][1]-p[1]*polygon[(i+1)%len(polygon)][0]for i,p in enumerate(polygon))
    if area<0:polygon.reverse()
    cross=lambda a,b:float(a[0]*b[1]-a[1]*b[0])
    for a,b in zip(outline,outline[1:]+outline[:1]):
        if not polygon:break
        clipped=[]
        for p,q in zip(polygon,polygon[1:]+polygon[:1]):
            dp=cross(b-a,p-a);dq=cross(b-a,q-a)
            if dp>=-1e-10:clipped.append(p)
            if (dp>=0)!=(dq>=0):clipped.append(p+(q-p)*dp/(dp-dq))
        polygon=clipped
    if polygon:
        level=max(height(p[0],p[1])-(shape['soleLocalY']-normal[0]*(p[0]-x)-normal[2]*(p[1]-z))/normal[1]for p in polygon)+gap
    else:level=-shape['soleLocalY']+gap
    return origin+rotation@np.array([x,level,z]),rotation@foot


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


def upper_reconstruction(joints,dirs,frame,h):
    """Short labelled upper-body reconstruction; lower-body contacts stay fixed."""
    weight=window(frame,h['upperWindow'])
    if not weight:return
    rows=h['upperKeys'];times=[r['frame']for r in rows]
    vector=lambda name:unit(curve(times,[r[name]for r in rows],frame))
    old_up=unit(joints['Neck']-joints['Pelvis']);target_up=vector('torsoUp')
    new_up=unit(old_up*(1-weight)+target_up*weight)
    axis=np.cross(old_up,new_up);s=float(np.linalg.norm(axis));c=float(np.clip(np.dot(old_up,new_up),-1,1))
    rotation=np.eye(3)
    if s>1e-9:
        axis/=s;x,y,z=axis;cross=np.array([[0,-z,y],[z,0,-x],[-y,x,0]])
        rotation=np.eye(3)+cross*s+(cross@cross)*(1-c)
    anchor=joints['Pelvis']
    for name in ('Chest','Neck','Head','LeftShoulder','RightShoulder','LeftElbow','RightElbow','LeftHand','RightHand'):
        joints[name]=anchor+rotation@(joints[name]-anchor)
    dirs['ChestUp']=new_up
    dirs['ChestForward']=unit(rotation@dirs['ChestForward'])
    for side in ('Left','Right'):
        shoulder=joints[side+'Shoulder'];elbow=joints[side+'Elbow'];hand=joints[side+'Hand']
        upper=unit(unit(elbow-shoulder)*(1-weight)+vector(side+'Upper')*weight)
        fore=unit(unit(hand-elbow)*(1-weight)+vector(side+'Fore')*weight)
        joints[side+'Elbow']=shoulder+upper*.29
        joints[side+'Hand']=joints[side+'Elbow']+fore*.27
    old_head=basis(rotation@dirs['HeadForward'],unit(joints['Head']-joints['Neck']))
    gaze=vector('headForward')
    target_head=basis(gaze,unit(UP-gaze*np.dot(UP,gaze)))
    head=qmatrix(slerp(matrix_quat(old_head),matrix_quat(target_head),weight))
    joints['Head']=joints['Neck']+head[:,1]*.12
    dirs['HeadForward']=head[:,2]


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
    late=window(frame,h['lateClearanceWindow'])*h['lateClearance']
    ground=smooth((frame-h['groundPinStart'])/(h['groundPinFull']-h['groundPinStart']))
    for side in ('Left','Right'):
        up=soles[side];ff=unit(original[side+'Toe']-original[side+'Ankle']+up*.07);foot=basis(ff,up)
        ankle=original[side+'Ankle']+delta+UP*late
        if side=='Left':
            weight=window(frame,h['frontWindow']);target=curve(times,[k['feet'][side]['ankle']for k in keys],t);target_r=qmatrix(slerp(keys[i]['feet'][side]['rotation'],keys[i+1]['feet'][side]['rotation'],u))
        else:
            weight=window(frame,h['rearWindow']);target=original[side+'Ankle']+np.array(h['rearAnkleOffset']);target_r=basis(ff,UP)
            # Relocate the released rear foot beside the board without adding
            # an arbitrary vertical tuck. Retain the captured leg's extension.
            source_hip=original[side+'Hip'];distance=np.linalg.norm(original[side+'Ankle']-source_hip)
            horizontal=(target[0]-source_hip[0])**2+(target[2]-source_hip[2])**2
            if horizontal>=distance*distance:
                raise ValueError('Rear-foot clearance exceeds captured leg reach')
            target[1]=source_hip[1]-math.sqrt(distance*distance-horizontal)
        ankle=ankle*(1-weight)+target*weight;foot=qmatrix(slerp(matrix_quat(foot),matrix_quat(target_r),weight))
        if side=='Right':
            # An attached tail-pop sole must agree with the NEW board, rather
            # than retaining the old capture's unrelated pitch/vertical arc.
            if frame<=259:
                ankle,foot=deck_shoe(spec,origin,br,0.,-.24,88,h['popContact']['soleGap'])
            elif frame<264.5:
                slide=smooth(frame-259)
                attached,attached_r=deck_shoe(spec,origin,br,.24*slide,-.24-.03*slide,88,h['popContact']['soleGap']+.02*slide)
                rows=h['popContact']['rearFreeKeys'];free=curve([r[0]for r in rows],[r[1:]for r in rows],frame)
                released=smooth(frame-260)
                ankle=attached*(1-released)+free*released
                foot=qmatrix(slerp(matrix_quat(attached_r),matrix_quat(basis(ff,UP)),released))
        elif frame<=262:
            slide=smooth(frame-259)
            attached,attached_r=deck_shoe(spec,origin,br,-.055+.01*slide,.22+.055*slide,45+11*slide,h['popContact']['soleGap'])
            upper_r=foot_basis(*h['popContact']['instepAngles']);upper=origin+br@np.array(spec['flickPatch']['deckLocal'])-upper_r@np.array(spec['flickPatch']['shoeLocal'])
            if frame<=260:ankle,foot=attached,attached_r
            elif frame<=261:
                roll=smooth(frame-260)
                ankle=attached*(1-roll)+upper*roll+br[:,1]*h['popContact']['rollClearance']*(64*(frame-260)**3*(261-frame)**3)
                foot=qmatrix(slerp(matrix_quat(attached_r),matrix_quat(upper_r),roll))
            else:
                released=smooth(frame-261)
                ankle=upper*(1-released)+np.array(h['popContact']['frontFree262'])*released
                foot=upper_r
        # One coherent free-Air path: absolute tuck target replaces the late
        # clearance contribution as its weight rises; no second additive lift.
        tuck=window(frame,h['freeAirWindow'])
        if tuck:
            air_keys=h['freeAirTargetKeys'][side]
            air_target=curve([k[0]for k in air_keys],[k[1:]for k in air_keys],frame)
            ankle=ankle*(1-tuck)+air_target*tuck
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
    upper_reconstruction(joints,dirs,frame,h)
    event=next(e for e in reversed(spec['events'])if e['t']<=t+1e-9)
    return dict(t=t,sourceFrame=min(frame,296),sourceCacheTime=min(frame,296)/spec['sourceFps'],sourceVideoTime=136+min(frame,296)/spec['sourceFps'],capturedBody=True,authoredLocalContacts=True,
        joints={k:joints[k].tolist()for k in JOINTS},dirs={k:dirs[k].tolist()for k in DIRS},soleUp={k:soles[k].tolist()for k in soles},board=dict(origin=origin.tolist(),quaternion=rotation.tolist()),phase=event['phase'],contacts=sum({'LeftFoot':1,'RightFoot':2}.get(x,0)for x in event['contacts']),authoredBoardEuler=angles.tolist())


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--body',type=Path,default=ASSET/'captured-body.json');p.add_argument('--constraints',type=Path,default=ASSET/'hybrid-constraints.json');p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    raw=a.body.read_bytes();controls=json.loads(raw);spec=json.loads(a.constraints.read_text());assert hashlib.sha256(raw).hexdigest()==spec['hybrid']['sourceBodySha256']
    keys=authored_keys(spec);frames=[sample(controls,spec,keys,float(t))for t in np.linspace(0,spec['duration'],spec['sampleCount'])]
    meta=dict(spec['metadata'],id='kickflip-source-keyed-v1',movementKind='kickflip',reviewed=False,authored=True,capturedBody=True,canonicalStance='regular',sourceFilmedStance='uncertain; retargeted canonical mapping',sourceFps=spec['sourceFps'],firstSourceFrame=254,lastSourceFrame=296,capturedBodyControlSha256=hashlib.sha256(raw).hexdigest(),capturedBodySourcePoseSha256=controls['sourcePoseSha256'],sourceRootMotion=dict(spec['sourceRootMotion'],extraction=controls['normalization']),provenance='Captured retarget skeleton is the basis; short labelled source-led upper reconstruction, authored board/foot contacts and smooth landing height. No legacy runtime correction stack; neither fully captured nor fully hand-authored body motion.')
    clip=dict(format='skate-capture-hybrid/1',metadata=meta,stance='regular',fps=(spec['sampleCount']-1)/spec['duration'],duration=spec['duration'],events=contact_events(spec['events'],spec['duration']),frames=frames)
    packed=encode_pack([clip]);a.output.mkdir(exist_ok=True,parents=True);(a.output/'kickflip-source-keyed-v1.json').write_text(json.dumps(clip,indent=2)+'\n');(a.output/'skate_captures.skmc').write_bytes(packed);print('HYBRID_CAPTURE_BODY_READY',len(frames),len(packed),hashlib.sha256(packed).hexdigest(),flush=True)

if __name__=='__main__':main()
