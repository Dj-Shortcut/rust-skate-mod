#!/usr/bin/env python3
"""Bake the original Tre-specific BATB body/scoop/combined winding controls.

The footage and detector jobs stay private. Published inputs are compact own
reconstruction controls; original source packs remain Reviewed=false.
"""
import argparse,hashlib,json,math
from pathlib import Path
import numpy as np
from capture_math import JOINTS,DIRS,UP,unit,qmatrix,euler_quat,matrix_quat,slerp,contact_events,encode_pack
from author_skate_kickflip import curve,limb
ROOT=Path(__file__).resolve().parents[1];ASSET=ROOT/'assets/authored/animation/treflip-keyed'

def smooth(x):
    x=float(np.clip(x,0,1));return x*x*x*(10+x*(-15+6*x))

def basis(forward,up):
    up=unit(up);forward=unit(forward-up*np.dot(forward,up));return np.column_stack((unit(np.cross(up,forward)),up,forward))

def body_at(c,t):
    a=np.asarray(c['rows']);i=max(0,min(len(a)-2,int(np.searchsorted(a[:,0],t,side='right'))-1));u=float(np.clip((t-a[i,0])/(a[i+1,0]-a[i,0]),0,1));row=a[i]*(1-u)+a[i+1]*u;p=1;j={};d={};soles={}
    for name in c['jointOrder']:j[name]=row[p:p+3];p+=3
    for name in c['directionOrder']:d[name]=unit(row[p:p+3]);p+=3
    for side in('Left','Right'):soles[side]=unit(row[p:p+3]);p+=3
    raw={k:v.copy()for k,v in j.items()};pel=raw['Pelvis'];torso=unit(raw['Neck']-pel);hips=unit(raw['RightHip']-raw['LeftHip']);shoulders=unit(raw['RightShoulder']-raw['LeftShoulder']);shoulders=unit(shoulders-torso*np.dot(shoulders,torso));j.update(Pelvis=pel,Chest=pel+torso*(.52/1.12),Neck=pel+torso*.52);j['Head']=j['Neck']+unit(raw['Head']-raw['Neck'])*.12
    for side,sign in(('Left',-1),('Right',1)):
        hip=pel+hips*(sign*.09);shoulder=pel+torso*.44+shoulders*(sign*.18);j[side+'Hip']=hip;j[side+'Shoulder']=shoulder;j[side+'Elbow']=shoulder+unit(raw[side+'Elbow']-raw[side+'Shoulder'])*.29;j[side+'Hand']=j[side+'Elbow']+unit(raw[side+'Hand']-raw[side+'Elbow'])*.27;j[side+'Knee']=hip+unit(raw[side+'Knee']-raw[side+'Hip'])*.44;j[side+'Ankle']=j[side+'Knee']+unit(raw[side+'Ankle']-raw[side+'Knee'])*.43;j[side+'Toe']=j[side+'Ankle']+unit(raw[side+'Toe']-raw[side+'Ankle'])*.14
    return j,d,soles

def qmul(a,b):
    x,y,z,w=a;X,Y,Z,W=b;return np.array([w*X+x*W+y*Z-z*Y,w*Y-x*Z+y*W+z*X,w*Z+x*Y-y*X+z*W,w*W-x*X-y*Y-z*Z])

def source_q(s,sf):
    k=s['boardQuaternionKeys'];t=[x[0]for x in k];i=max(0,min(len(k)-2,int(np.searchsorted(t,sf,side='right'))-1));u=float(np.clip((sf-t[i])/(t[i+1]-t[i]),0,1));return slerp(k[i][1:],k[i+1][1:],u)

def strike_pitch(s):
    tail=np.array(s['support']['tail']);wheel=np.array(s['support']['wheel']);lo,hi=30.,60.
    for _ in range(45):
        m=(lo+hi)/2;R=qmatrix(euler_quat([-m,0,0]));gap=(tail@R.T)[:,1].min()-(wheel@R.T)[:,1].min()+s['reference']['wheelClearance']
        if gap>0:lo=m
        else:hi=m
    return(lo+hi)/2

def board_at(s,sf):
    pop=s['pop'];strike=s['reference']['tailStrike'];a,b=s['reference']['hold'];pitch=strike_pitch(s);f=float(np.clip((sf-pop)/(s['land']-pop),0,1));H=.5*s['reference']['gravity']*s['reference']['flightSeconds']**2*f*(1-f)
    if sf<=pop:q=np.array([0.,0.,0.,1.])
    elif sf<=b:
        turn=smooth((sf-pop)/(b-pop));angle=pitch*smooth((sf-pop)/(a-pop));q=qmul(euler_quat([0,s['authored']['pressYawDegrees']*turn,0]),euler_quat([-angle,0,0]))
    elif sf<s['pressEnd']:
        press=qmul(euler_quat([0,s['authored']['pressYawDegrees'],0]),euler_quat([-pitch,0,0]));q=slerp(press,source_q(s,sf),smooth((sf-b)/(s['pressEnd']-b)))
    else:q=source_q(s,sf)
    R=qmatrix(q);floor=-float((np.array(s['support']['board'])@R.T)[:,1].min())
    if sf<=pop:y=.092
    elif sf<=b:y=floor-H
    else:
        release=smooth((sf-b)/(s['pressEnd']-b));margin=s['authored']['referenceAirFloorMargin']*release*(1-smooth((sf-418)/(s['land']-418)));base=.092+.04*(1-smooth((sf-408)/10));y=max(base,floor-H+margin)
    return np.array([0.,y,0.]),q,R,H

def deck_shoe(s,origin,R,x,z,yaw):
    a=s['shoe'];half=a['deckHalfWidth'];height=lambda X,Z:a['kickRise']*(max(0.,abs(Z)-a['kickStart'])/a['kickSpan'])**2+a['concave']*(X/half)**2;normal=unit(np.array([-2*a['concave']*x/half**2,1.,-2*a['kickRise']*max(0.,abs(z)-a['kickStart'])/a['kickSpan']**2*np.sign(z)]));ang=math.radians(yaw);F=basis(np.array([math.sin(ang),0,math.cos(ang)]),normal);need=[]
    for along,width in a['soleRows']:
        for sign in(-1,1):
            p=F@np.array([width*sign,a['soleLocalY'],along]);X,Z=x+p[0],z+p[2]
            cap=max(0.,abs(Z)-a['capBase'])/a['capLength']
            if cap<=1 and abs(X)<=half*math.sqrt(max(0.,1-cap*cap))+1e-8:need.append(height(X,Z)-p[1])
    y=max(need,default=-a['soleLocalY'])+a['soleGap'];return origin+R@np.array([x,y,z]),R@F

def entry_foot(s,side,origin,R,u,x,z,yaw):
    e=s['entryFeet'][side];p=np.array(e['ankle'])-[0,.092,0];F=basis(np.array(e['forward']),np.array(e['soleUp']));start_yaw=math.degrees(math.atan2(F[0,2],F[2,2]));fit,_=deck_shoe(s,np.zeros(3),np.eye(3),p[0],p[2],start_yaw);a,r=deck_shoe(s,origin,R,p[0]*(1-u)+x*u,p[2]*(1-u)+z*u,start_yaw*(1-u)+yaw*u);a+=R@(p-fit)*(1-u);r=R@qmatrix(slerp(matrix_quat(F),matrix_quat(R.T@r),u));return a,r

def instep(s,R):
    a=s['shoe'];x,_,z=s['flickPatch']['deckLocal'];N=unit(np.array([-2*a['concave']*x/a['deckHalfWidth']**2,1.,-2*a['kickRise']*max(0.,abs(z)-a['kickStart'])/a['kickSpan']**2]));n=-R@N;t=np.array(s['entryFeet']['Left']['forward']);t=unit(t-n*np.dot(n,t));local=unit(np.array(a['instepSurfaceNormalLocal']));f=unit(np.array([0.,0.,1.])-local*local[2]);return np.column_stack((n,t,unit(np.cross(n,t))))@np.column_stack((local,f,unit(np.cross(local,f)))).T

def sample(c,s,sf):
    t=(sf-s['firstSourceFrame'])/s['sourceFps'];j,d,sole=body_at(c,t);o,q,R,H=board_at(s,sf);original={k:v.copy()for k,v in j.items()};feet={side:basis(unit(j[side+'Toe']-j[side+'Ankle']+sole[side]*.07),sole[side])for side in('Left','Right')};ankles={side:j[side+'Ankle'].copy()for side in feet};pop=s['pop'];release=s['pressRelease'];end=s['pressEnd']
    if sf<end:
        u=smooth((sf-pop)/(release-pop));rear,rr=entry_foot(s,'Right',o,R,u,s['authored']['pressRearX'],s['authored']['pressRearZ'],85);front,fr=entry_foot(s,'Left',o,R,u,-.045,.275,56)
        roll=smooth((sf-(pop+.25))/(release+.5-pop-.25));contact_R=R if sf<=release+.5 else board_at(s,release+.5)[2];upper_r=instep(s,contact_R);upper=o+R@np.array(s['flickPatch']['deckLocal'])-upper_r@np.array(s['flickPatch']['shoeLocal']);front=front*(1-roll)+upper*roll;fr=qmatrix(slerp(matrix_quat(fr),matrix_quat(upper_r),roll))
        free=smooth((sf-release)/(end-release));front=front*(1-free)+original['LeftAnkle']*free;rear=rear*(1-free)+original['RightAnkle']*free;rr=qmatrix(slerp(matrix_quat(rr),matrix_quat(feet['Right']),free));fr=qmatrix(slerp(matrix_quat(fr),matrix_quat(feet['Left']),free));ankles.update(Left=front,Right=rear);feet.update(Left=fr,Right=rr)
        pressure=smooth((sf-(pop-.5))/.5)*(1-smooth((sf-release-.25)/(end-release-.25)));reach=curve([pop,release,release+.25,end],[[.88],[.97],[.97],[.86]],sf)[0]*.87;hip=rear+np.array([-.04,math.sqrt(reach*reach-.04**2-.06**2),.06]);shift=(hip-(j['RightHip']-j['Pelvis'])-j['Pelvis'])*pressure
        for name in j:j[name]+=shift
    air=smooth((sf-release)/(end-release));caught=smooth((sf-s['authored']['catchStart'])/(s['catch']-s['authored']['catchStart']));upper=float((np.array(s['support']['board'])@R.T)[:,1].max());height=o[1]+upper-s['shoe']['soleLocalY']+s['authored']['minimumAirSoleClearance']
    if air:
        # Tre-specific scissor: the small front flick returns first; the scooping
        # rear leg stays compact above it. Neither trajectory is a Kickflip relabel.
        collect=air*(1-caught);targets={'Left':np.array([-.04,height,.26]),'Right':np.array([-.02,height+.10,-.26])}
        for side in feet:
            ankles[side]=ankles[side]*(1-collect)+targets[side]*collect;flat=basis(np.array(s['entryFeet'][side]['forward']),UP);feet[side]=qmatrix(slerp(matrix_quat(feet[side]),matrix_quat(flat),collect))
        keys=s['authored']['airPelvisKeys'];target=curve([k[0]for k in keys],[[k[1]]for k in keys],sf)[0];delta=UP*((target-j['Pelvis'][1])*air)
        if sf< s['catch']:
            mid=(ankles['Left']+ankles['Right'])/2;delta+=np.array([-mid[0],0.,-mid[2]])*air*.5
        else:delta+=np.array([-.10-j['Pelvis'][0],0.,-j['Pelvis'][2]])
        for name in j:j[name]+=delta
    if caught:
        for side,z,yaw in(('Left',.24,65),('Right',-.24,85)):
            target,target_r=deck_shoe(s,o,R,-.04,z,yaw);ankles[side]=ankles[side]*(1-caught)+target*caught;feet[side]=qmatrix(slerp(matrix_quat(feet[side]),matrix_quat(target_r),caught))
    # The hidden face is reconstructed toward the board; the monocular
    # HeadForward tilts upward and does not describe the filmed downward look.
    look=smooth((sf-402)/4)*(1-smooth((sf-424)/8))
    if look:
        az=unit(d['HeadForward']*np.array([1.,0.,1.]));gaze=unit(az-UP*.70);headup=unit(UP-gaze*np.dot(UP,gaze));headup=unit(unit(j['Head']-j['Neck'])*(1-look)+headup*look);j['Head']=j['Neck']+headup*.12;d['HeadForward']=unit(d['HeadForward']*(1-look)+gaze*look)
    for side in feet:
        hip=j[side+'Hip'];a=ankles[side];F=feet[side];pole=unit(np.array([1.,0.,.1])*.85+F[:,2]*.15);j[side+'Knee']=limb(hip,a,.44,.43,pole);j[side+'Ankle']=a;sole[side]=F[:,1];j[side+'Toe']=a+F[:,2]*math.sqrt(.14**2-.07**2)-F[:,1]*.07
    phase='Load';contacts=3
    for e in events(s):
        if e['sourceFrame']<=sf+1e-9:phase=e['phase'].title();contacts=sum({'LeftFoot':1,'RightFoot':2}[v]for v in e['contacts'])
    return dict(t=t,sourceFrame=sf,sourceCacheTime=sf/s['sourceFps'],sourceVideoTime=240+sf/s['sourceFps'],joints={k:j[k].tolist()for k in JOINTS},dirs={k:d[k].tolist()for k in DIRS},soleUp={k:sole[k].tolist()for k in sole},board=dict(origin=o.tolist(),quaternion=q.tolist()),phase=phase,contacts=contacts)

def events(s):
    return[dict(sourceFrame=s['firstSourceFrame'],phase='load',contacts=['LeftFoot','RightFoot']),dict(sourceFrame=s['pop'],phase='pop',contacts=['RightFoot']),dict(sourceFrame=s['pressRelease'],phase='air',contacts=[]),dict(sourceFrame=s['pressEnd'],phase='air',contacts=[]),dict(sourceFrame=s['catch'],phase='catch',contacts=['LeftFoot','RightFoot']),dict(sourceFrame=s['land'],phase='land',contacts=['LeftFoot','RightFoot']),dict(sourceFrame=430,phase='recovery',contacts=['LeftFoot','RightFoot'])]

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',type=Path,required=True);a=p.parse_args();s=json.loads((ASSET/'constraints.json').read_text());raw=(ASSET/'captured-body.json').read_bytes();assert hashlib.sha256(raw).hexdigest()==s['sourceBodySha256'];c=json.loads(raw);duration=(s['lastSourceFrame']-s['firstSourceFrame'])/s['sourceFps'];times=np.linspace(0,duration,98);critical=[s['pop'],s['pressRelease'],s['pressEnd'],s['catch'],s['land'],*s['reference']['hold']];times=np.unique(np.r_[times,[(sf-s['firstSourceFrame'])/s['sourceFps']for sf in critical]]);frames=[]
    for t in times:
        row=sample(c,s,s['firstSourceFrame']+float(t)*s['sourceFps']);row['t']=float(t);frames.append(row)
    meta=dict(id=s['id'],label=s['label'],movementKind=s['movementKind'],reviewed=False,authored=True,capturedBody=True,sourceUrl=s['sourceUrl'],sourceAuthor=s['sourceAuthor'],canonicalStance='regular',sourceFilmedStance='goofy',sourceFps=s['sourceFps'],firstSourceFrame=s['firstSourceFrame'],lastSourceFrame=s['lastSourceFrame'],sourceRootMotion=s['sourceRootMotion'],provenance=s['provenance'],capturedBodyControlSha256=s['sourceBodySha256'],sourcePoseSha256=c['sourcePoseSha256'],combinedWinding=dict(nativeFlip=360,nativeShove=-360,visualShove=360),referenceGroundPress=s['reference'])
    clip=dict(format='skate-capture-source-tre/1',metadata=meta,stance='regular',fps=60,duration=duration,events=contact_events([dict(t=(e['sourceFrame']-s['firstSourceFrame'])/s['sourceFps'],phase=e['phase'],contacts=e['contacts'])for e in events(s)],duration),frames=frames);packed=encode_pack([clip]);a.output.mkdir(parents=True,exist_ok=True);(a.output/'360-flip-source-keyed-v1.json').write_text(json.dumps(clip,indent=2)+'\n');(a.output/'skate_captures.skmc').write_bytes(packed);print('TRE_SOURCE_READY',len(frames),hashlib.sha256(packed).hexdigest())
if __name__=='__main__':main()
