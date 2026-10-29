#!/usr/bin/env python3
"""Bake the black-shirt source Hardflip, with its own shove, body and contacts.

Fixed-chain/curved-deck mathematics is shared; no Kickflip or Heelflip motion
keys are reused. Hidden depth, instep contact and actor gaps stay authored.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import numpy as np
from author_skate_kickflip import curve, foot_basis, limb
from author_skate_kickflip_hybrid import basis, deck_shoe, smooth
from capture_math import JOINTS, DIRS, UP, unit, matrix_quat, qmatrix, slerp, encode_pack, contact_events

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'assets/authored/animation/hardflip-keyed'


def scalar(keys, frame):
    return float(curve([k[0] for k in keys], [[k[1]] for k in keys], frame)[0])


def vector(keys, frame):
    return curve([k[0] for k in keys], [k[1:] for k in keys], frame)


def turn(axis, degrees):
    a = math.radians(degrees); c, s = math.cos(a), math.sin(a)
    if axis == 'x': return np.array([[1,0,0],[0,c,-s],[0,s,c]])
    if axis == 'y': return np.array([[c,0,s],[0,1,0],[-s,0,c]])
    return np.array([[c,-s,0],[s,c,0],[0,0,1]])


def blend_frame(a, b, weight):
    return qmatrix(slerp(matrix_quat(a), matrix_quat(b), weight))


def reference_height(spec, frame):
    u = float(np.clip((frame-spec['popFrame'])/(spec['landFrame']-spec['popFrame']),0,1))
    return .5*spec['gravity']*spec['referenceFlight']**2*u*(1-u)


def board(spec, frame):
    rotation = turn('y', scalar(spec['yawKeys'],frame)) @ turn('x',-scalar(spec['pitchKeys'],frame)) @ turn('z',scalar(spec['rollKeys'],frame))
    h = reference_height(spec,frame)
    u = float(np.clip((frame-spec['popFrame'])/(spec['landFrame']-spec['popFrame']),0,1))
    release = 1-smooth((frame-spec['rearSoleReleaseFrame'])/2)
    floor = -float((np.array(spec['hybrid']['support']['board'])@rotation.T)[:,1].min())
    y = floor*release + (.092+h+spec['visualLift']*math.sin(math.pi*u)**2)*(1-release)
    if frame < spec['popFrame'] or frame >= spec['landFrame']: y=.092
    return np.array([0.,y,0.]),rotation,h


def attached(spec, frame, side):
    o,r,_=board(spec,frame);entry=spec['hybrid']['loadEntryFeet'][side]
    u=smooth((frame-spec['popFrame'])/(spec['rearSoleReleaseFrame']-spec['popFrame']))
    x,z=entry['ankle'][0],entry['ankle'][2]
    if side=='Left': x=x*(1-u)-.045*u;z=z*(1-u)+.31*u
    else: x+=.035*u # Authored frontside tail pocket; exact hidden depth uncertain.
    return deck_shoe(spec,o,r,x,z,65+15*u if side=='Left' else 85,.003)


def instep(spec, frame):
    o,r,_=board(spec,frame);shape=spec['hybrid']['popContact'];p=np.array(spec['instepDeckFeature'])
    normal=unit(np.array([-2*shape['concave']*p[0]/shape['deckHalfWidth']**2,1.,-2*shape['kickRise']*(p[2]-shape['kickStart'])/shape['kickSpan']**2]))
    source_normal=unit(spec['instepSurfaceNormal']);source_forward=unit(np.array([0.,0.,1.])-source_normal*source_normal[2])
    target_normal=-normal;target_forward=unit(np.array([.9,0.,.35])-target_normal*np.dot(target_normal,[.9,0.,.35]))
    source=np.column_stack([source_forward,np.cross(source_normal,source_forward),source_normal])
    target=np.column_stack([target_forward,np.cross(target_normal,target_forward),target_normal])
    foot=r@target@source.T
    return o+r@p-foot@np.array(spec['instepFeature']),foot


def catch_foot(spec, frame, side):
    o,r,_=board(spec,frame)
    # Board has actually turned180; body and anatomical front foot do not turn.
    return deck_shoe(spec,o,r,.04,-.24 if side=='Left' else .24,-100 if side=='Left' else -95,.003)


def foot(spec, frame, side):
    release=spec['instepReleaseFrame'] if side=='Left' else spec['rearSoleReleaseFrame']
    if side=='Left' and frame<=spec['frontSoleReleaseFrame']:return attached(spec,frame,side)
    if side=='Right' and frame<=release:return attached(spec,frame,side)
    if side=='Left' and frame<=release:
        a,ar=attached(spec,frame,side);b,br=instep(spec,frame)
        w=smooth((frame-spec['frontSoleReleaseFrame'])/(release-spec['frontSoleReleaseFrame']))
        return a*(1-w)+b*w,blend_frame(ar,br,w)
    o,r,h=board(spec,frame);local=vector(spec['frontAirKeys'] if side=='Left' else spec['rearAirKeys'],frame)
    target=o+local;flat=foot_basis(80 if side=='Left' else 85,0,scalar(spec['frontAirDorsiKeys'] if side=='Left' else spec['rearAirDorsiKeys'],frame))
    start,sr=instep(spec,release) if side=='Left' else attached(spec,release,side)
    start+=UP*(h-reference_height(spec,release))
    w=smooth((frame-release)/2.4);ankle=start*(1-w)+target*w;fr=blend_frame(sr,flat,w)
    landing,lr=catch_foot(spec,frame,side);caught=smooth((frame-(spec['catchFrame']-3))/3)
    return ankle*(1-caught)+landing*caught,blend_frame(fr,lr,caught)


def body_height(spec, frame, origin):
    a,b=spec['landingBodyWindow'];keys=spec['pelvisHeightKeys']
    if not a<frame<b:return origin[1]+scalar(keys,frame)
    def endpoint(f):
        u=float(np.clip((f-spec['popFrame'])/(spec['landFrame']-spec['popFrame']),0,1));span=spec['landFrame']-spec['popFrame']
        height=.092+reference_height(spec,f)+spec['visualLift']*math.sin(math.pi*u)**2+scalar(keys,f)
        slope=float(curve([k[0]for k in keys],[[k[1]]for k in keys],f,derivative=True)[0])
        if spec['popFrame']<f<spec['landFrame']:slope+=(.5*spec['gravity']*spec['referenceFlight']**2*(1-2*u)+spec['visualLift']*math.pi*math.sin(2*math.pi*u))/span
        return height,slope*(b-a)
    first,va=endpoint(a);last,vb=endpoint(b);u=(frame-a)/(b-a);delta=last-first
    return first+va*u+(10*delta-6*va-4*vb)*u**3+(-15*delta+8*va+7*vb)*u**4+(6*delta-3*va-3*vb)*u**5


def sample(controls,spec,frame):
    o,r,h=board(spec,frame);la,lf=foot(spec,frame,'Left');ra,rf=foot(spec,frame,'Right')
    rows=controls['frames'];times=[a['frame']for a in rows]
    rel={k:curve(times,[a['relative'][k]for a in rows],frame)for k in rows[0]['relative']}
    dirs={k:unit(curve(times,[a['dirs'][k]for a in rows],frame))for k in DIRS}
    pelvis=np.array([-.04,body_height(spec,frame,o),0.])
    # Preserve strict measured reach, never change feet to hide an overlong leg.
    maximum=[]
    for ankle,z in [(la,.09),(ra,-.09)]:
        horizontal=(pelvis[0]-ankle[0])**2+(z-ankle[2])**2
        if horizontal>=(.87*.995)**2:raise ValueError('Hardflip horizontal leg reach '+str(frame))
        maximum.append(ankle[1]+math.sqrt((.87*.995)**2-horizontal))
    pelvis[1]=min(pelvis[1],min(maximum));j={'Pelvis':pelvis}
    for side,ankle,fr,z in [('Left',la,lf,.09),('Right',ra,rf,-.09)]:
        hip=pelvis+np.array([0,0,z]);j[side+'Hip']=hip
        j[side+'Knee']=limb(hip,ankle,.44,.43,unit(vector(spec['kneePlaneKeys'][side],frame)))
        j[side+'Ankle']=ankle;j[side+'Toe']=ankle+fr[:,2]*math.sqrt(.14**2-.07**2)-fr[:,1]*.07
    old_up=unit(dirs['ChestUp']);old_forward=unit(np.array([1.,0.,0.])-old_up*old_up[0]);old_across=unit(np.cross(old_up,old_forward))
    angle=math.radians(scalar(spec['torsoLeanKeys'],frame));up=np.array([0.,math.cos(angle),math.sin(angle)])
    forward=np.array([1.,0.,0.]);across=unit(np.cross(up,forward))
    upper=np.column_stack([forward,up,across])@np.column_stack([old_forward,old_up,old_across]).T
    j['Chest']=pelvis+up*(.52/1.12);j['Neck']=pelvis+up*.52
    gaze=unit(upper@dirs['HeadForward']);head_up=unit(UP-gaze*np.dot(UP,gaze));j['Head']=j['Neck']+head_up*.12
    for side,sign in [('Left',-1),('Right',1)]:
        shoulder=pelvis+up*.44+across*(sign*.18)
        j[side+'Shoulder']=shoulder;j[side+'Elbow']=shoulder+unit(upper@(rel[side+'Elbow']-rel[side+'Shoulder']))*.29
        j[side+'Hand']=j[side+'Elbow']+unit(upper@(rel[side+'Hand']-rel[side+'Elbow']))*.27
    directions=dict(PelvisForward=forward,PelvisUp=UP,ChestForward=forward,ChestUp=up,HeadForward=gaze)
    for key in j:j[key]=j[key]-UP*h
    contacts=3 if frame<spec['frontSoleReleaseFrame'] else 2 if frame<spec['rearSoleReleaseFrame'] else 0
    phase='load' if frame<spec['popFrame'] else 'pop' if frame<spec['airFrame'] else 'air'
    if frame>=spec['catchFrame']:contacts=3;phase='catch'
    if frame>=spec['landFrame']:phase='land'
    if frame>=spec['recoveryFrame']:phase='recovery'
    return dict(t=(frame-spec['firstSourceFrame'])/spec['sourceFps'],sourceFrame=frame,sourceCacheTime=frame/spec['sourceFps'],joints={k:j[k].tolist()for k in JOINTS},dirs={k:directions[k].tolist()for k in DIRS},soleUp=dict(Left=lf[:,1].tolist(),Right=rf[:,1].tolist()),board=dict(origin=(o-UP*h).tolist(),quaternion=matrix_quat(r).tolist()),phase=phase,contacts=contacts)


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--body',type=Path,default=ASSET/'captured-body.json');p.add_argument('--constraints',type=Path,default=ASSET/'hardflip-constraints.json');p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    raw=a.body.read_bytes();controls=json.loads(raw);spec=json.loads(a.constraints.read_text())
    if hashlib.sha256(raw).hexdigest()!=spec['sourceBodySha256']:raise ValueError('Hardflip source body hash mismatch')
    tail=np.array(spec['hybrid']['support']['tail']);wheel=np.array(spec['hybrid']['support']['wheel']);lo,hi=30.,60.
    for _ in range(40):
        mid=(lo+hi)/2;rotation=turn('x',-mid);gap=(tail@rotation.T)[:,1].min()-(wheel@rotation.T)[:,1].min()+.005
        if gap>0:lo=mid
        else:hi=mid
    for key in spec['pitchKeys']:
        if key[0]==spec['tailStrikeFrame']:key[1]=(lo+hi)/2
    first,end,fps=spec['firstSourceFrame'],spec['lastSourceFrame'],spec['sourceFps'];duration=(end-first)/fps
    moments=[spec[k]for k in ['popFrame','tailStrikeFrame','frontSoleReleaseFrame','rearSoleReleaseFrame','instepReleaseFrame','airFrame','catchFrame','landFrame','recoveryFrame']]
    times=np.unique(np.r_[np.linspace(first,end,round(duration*60)+1),moments,spec['rearSoleReleaseFrame']+2])
    frames=[sample(controls,spec,float(f))for f in times]
    events=[dict(t=(f-first)/fps,phase=phase,contacts=contact)for f,phase,contact in [(first,'load',['LeftFoot','RightFoot']),(spec['popFrame'],'pop',['LeftFoot','RightFoot']),(spec['frontSoleReleaseFrame'],'pop',['RightFoot']),(spec['rearSoleReleaseFrame'],'air',[]),(spec['rearSoleReleaseFrame']+2,'air',[]),(spec['catchFrame'],'catch',['LeftFoot','RightFoot']),(spec['landFrame'],'land',['LeftFoot','RightFoot']),(spec['recoveryFrame'],'recovery',['LeftFoot','RightFoot'])]]
    meta=dict(spec['metadata'],id='hardflip-source-keyed-v1',label='Hardflip — own source body and frontside instep flip [UNREVIEWED]',movementKind='hardflip',reviewed=False,authored=True,canonicalStance='regular',sourceFilmedStance=controls['sourceFilmedStance'],sourceFps=fps,firstSourceFrame=first,lastSourceFrame=end,sourceBodySha256=spec['sourceBodySha256'],sourcePoseSha256=controls['sourcePoseSha256'],provenance='Own black-shirt Hardflip source retarget upper directions, authored lower-chain/contact reconstruction, non-spinning body heading, full+360roll/−180yaw board and source-specific compact pickup. Not measured3D mocap.')
    clip=dict(format='skate-capture-hybrid/1',metadata=meta,stance='regular',fps=60,duration=duration,events=contact_events(events,duration),frames=frames)
    a.output.mkdir(parents=True,exist_ok=True);(a.output/'hardflip-source-keyed-v1.json').write_text(json.dumps(clip,indent=2)+'\n');pack=encode_pack([clip]);(a.output/'skate_captures.skmc').write_bytes(pack)
    print('HARDFLIP_READY',len(frames),len(pack),hashlib.sha256(pack).hexdigest())


if __name__=='__main__':main()
