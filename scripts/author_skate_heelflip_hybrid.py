#!/usr/bin/env python3
"""Reproducible source-led Heelflip, with an actual heel exit and coupled envelope.

The source skeleton is this family's own cached retarget, not Kickflip motion.
Only generic fixed-chain, circular-deck and SKMCAP math is shared.
"""
import argparse,hashlib,json,math
from pathlib import Path
import numpy as np
from author_skate_kickflip import curve,foot_basis,limb
from author_skate_kickflip_hybrid import deck_shoe,basis,smooth
from capture_math import JOINTS,DIRS,UP,unit,matrix_quat,qmatrix,slerp,encode_pack,contact_events
ROOT=Path(__file__).resolve().parents[1]
ASSET=ROOT/'assets/authored/animation/heelflip-keyed'


def scalar(keys,frame):return float(curve([k[0]for k in keys],[[k[1]]for k in keys],frame)[0])
def vector(keys,frame):return curve([k[0]for k in keys],[k[1:]for k in keys],frame)
def turn_x(degrees):
 a=math.radians(degrees);c,s=math.cos(a),math.sin(a);return np.array([[1,0,0],[0,c,-s],[0,s,c]])
def turn_z(degrees):
 a=math.radians(degrees);c,s=math.cos(a),math.sin(a);return np.array([[c,-s,0],[s,c,0],[0,0,1]])
def orientation(a,b,w):return qmatrix(slerp(matrix_quat(a),matrix_quat(b),w))
def strike_pitch(spec):
 tail=np.array(spec['hybrid']['support']['tail']);wheel=np.array(spec['hybrid']['support']['wheel']);lo,hi=30.,60.
 for _ in range(40):
  mid=(lo+hi)/2;r=turn_x(-mid);gap=(tail@r.T)[:,1].min()-(wheel@r.T)[:,1].min()+.005
  if gap>0:lo=mid
  else:hi=mid
 return(lo+hi)/2

def reference_height(spec,sf):
 f=float(np.clip((sf-spec['popFrame'])/(spec['landFrame']-spec['popFrame']),0,1))
 return .5*spec['gravity']*spec['referenceFlight']**2*f*(1-f)

def board(spec,sf):
 pitch=scalar(spec['pitchKeys'],sf);roll=scalar(spec['rollKeys'],sf)
 # Pitch then a rotation around the pitched longitudinal deck axis.
 r=turn_x(-pitch)@turn_z(roll);h=reference_height(spec,sf)
 f=float(np.clip((sf-spec['popFrame'])/(spec['landFrame']-spec['popFrame']),0,1));lift=spec['visualLift']*math.sin(math.pi*f)**2
 press=1-smooth((sf-spec['rearSoleReleaseFrame'])/2)
 floor=-float((np.array(spec['hybrid']['support']['board'])@r.T)[:,1].min())
 y=floor*press+(.092+h+lift)*(1-press)
 if sf<spec['popFrame'] or sf>=spec['landFrame']:y=.092
 return np.array([0.,y,0.]),r,h

def attached(spec,sf,side):
 o,r,_=board(spec,sf);pop=spec['popFrame'];u=smooth((sf-pop)/2.5)
 if side=='Left':
  entry=spec['hybrid']['loadEntryFeet']['Left'];x=entry['ankle'][0]*(1-u)+.035*u;z=entry['ankle'][2]*(1-u)+.275*u;yaw=65+15*u
 else:
  entry=spec['hybrid']['loadEntryFeet']['Right'];x=entry['ankle'][0];z=entry['ankle'][2];yaw=85
 return deck_shoe(spec,o,r,x,z,yaw,.003)

def heel_contact(spec,sf):
 o,r,_=board(spec,sf);fr=r@foot_basis(75,-8,12)
 return o+r@np.array(spec['heelDeckFeature'])-fr@np.array(spec['heelFeature']),fr

def foot(spec,sf,side):
 release=spec['heelFlickFrame']if side=='Left'else spec['rearSoleReleaseFrame']
 if side=='Left'and sf<=spec['frontSoleReleaseFrame']:return attached(spec,sf,side)
 if side=='Right'and sf<=release:return attached(spec,sf,side)
 if side=='Left'and sf<=release:
  a,ar=attached(spec,sf,side);b,br=heel_contact(spec,sf);w=smooth((sf-spec['frontSoleReleaseFrame'])/(release-spec['frontSoleReleaseFrame']))
  return a*(1-w)+b*w,orientation(ar,br,w)
 o,r,h=board(spec,sf);support=np.array(spec['hybrid']['support']['board']);top=float((support@r.T)[:,1].max())
 x,z=vector(spec['frontAirKeys']if side=='Left'else spec['rearAirKeys'],sf)
 extra=scalar(spec['frontAirLiftKeys'],sf)if side=='Left'else 0.
 target=np.array([x,o[1]+top-spec['hybrid']['popContact']['soleLocalY']+.018+extra,z]);flat=foot_basis(80 if side=='Left'else 85)
 start,fr=heel_contact(spec,release)if side=='Left'else attached(spec,release,side)
 start=start+UP*(h-reference_height(spec,release));w=smooth((sf-release)/(3 if side=='Left'else 2.25))
 ankle=start*(1-w)+target*w;fr=orientation(fr,flat,w)
 # Return from the free envelope to actual measured deck soles before Catch.
 ground,gr=deck_shoe(spec,o,r,-.04,.24 if side=='Left'else-.24,80 if side=='Left'else 85,.003)
 caught=smooth((sf-(spec['catchFrame']-2.5))/2.5)
 return ankle*(1-caught)+ground*caught,orientation(fr,gr,caught)

def compact_body_height(spec,sf,origin):
 target=origin[1]+scalar(spec['compactBodyHeightKeys'],sf)
 a,b=spec['landingBodyWindow']
 if not a<sf<b:return target
 pop,land=spec['popFrame'],spec['landFrame'];span=land-pop
 keys=spec['compactBodyHeightKeys'];ts=[k[0]for k in keys];values=[[k[1]]for k in keys]
 def endpoint(frame):
  f=float(np.clip((frame-pop)/span,0,1));lift=spec['visualLift']*math.sin(math.pi*f)**2
  height=.092+lift+scalar(keys,frame)+reference_height(spec,frame)
  derivative=float(curve(ts,values,frame,derivative=True)[0])
  if pop<frame<land:derivative+=(spec['visualLift']*math.pi*math.sin(2*math.pi*f)+.5*spec['gravity']*spec['referenceFlight']**2*(1-2*f))/span
  return height,derivative*(b-a)
 first,va=endpoint(a);last,vb=endpoint(b);u=(sf-a)/(b-a);delta=last-first
 return first+va*u+(10*delta-6*va-4*vb)*u**3+(-15*delta+8*va+7*vb)*u**4+(6*delta-3*va-3*vb)*u**5

def source_upper(controls,sf):
 rows=controls['frames'];ts=[r['frame']for r in rows]
 rel={k:curve(ts,[r['relative'][k]for r in rows],sf)for k in rows[0]['relative']}
 dirs={k:unit(curve(ts,[r['dirs'][k]for r in rows],sf))for k in DIRS}
 return rel,dirs

def sample(controls,spec,sf):
 o,r,h=board(spec,sf);la,lf=foot(spec,sf,'Left');ra,rf=foot(spec,sf,'Right');rel,d=source_upper(controls,sf)
 pelvis=np.array([-.04,0.,0.]);hips=[pelvis+np.array([0,0,.09]),pelvis+np.array([0,0,-.09])];ankles=[la,ra]
 knee=scalar(spec['kneeKeys'],sf);distance2=.44**2+.43**2-2*.44*.43*math.cos(math.radians(knee));heights=[];maximum=[]
 for hip,ankle in zip(hips,ankles):
  horizontal=(hip[0]-ankle[0])**2+(hip[2]-ankle[2])**2
  if horizontal>=distance2 or horizontal>=(.87*.995)**2:raise ValueError('Heel authored horizontal leg reach exceeds fixed chain at '+str(sf))
  heights.append(ankle[1]+math.sqrt(distance2-horizontal));maximum.append(ankle[1]+math.sqrt((.87*.995)**2-horizontal))
 pelvis[1]=min(sum(heights)*.5,min(maximum))
 a,b,c,e=spec['compactBodyWindow'];weight=smooth((sf-a)/(b-a))*(1-smooth((sf-c)/(e-c)))
 target=compact_body_height(spec,sf,o)
 pelvis[1]=min(pelvis[1]*(1-weight)+target*weight,min(maximum));j={};j['Pelvis']=pelvis
 for side,ankle,fr,offset in [('Left',la,lf,.09),('Right',ra,rf,-.09)]:
  hip=pelvis+np.array([0,0,offset]);pole=unit(vector(spec['kneePlaneKeys'][side],sf));j[side+'Hip']=hip;j[side+'Knee']=limb(hip,ankle,.44,.43,pole);j[side+'Ankle']=ankle;j[side+'Toe']=ankle+fr[:,2]*math.sqrt(.14**2-.07**2)-fr[:,1]*.07
 # This source's own torso/arm directions, with explicit non-spinning heading.
 old_up=unit(d['ChestUp']);old_forward=unit(np.array([1.,0.,0.])-old_up*old_up[0]);old_across=unit(np.cross(old_up,old_forward))
 lean=math.radians(scalar(spec['torsoLeanKeys'],sf));up=unit(np.array([0.,math.cos(lean),math.sin(lean)]));forward=unit(np.array([1.,0.,0.])-up*up[0]);across=unit(np.cross(up,forward));upper_turn=np.column_stack([forward,up,across])@np.column_stack([old_forward,old_up,old_across]).T
 j['Chest']=pelvis+up*(.52/1.12);j['Neck']=pelvis+up*.52
 gaze=unit(np.array([.8,-.55,.23]));headup=unit(UP-gaze*np.dot(UP,gaze));j['Head']=j['Neck']+headup*.12
 goal=min(controls['frames'],key=lambda row:abs(row['frame']-spec['armRecoveryReferenceFrame']));goal_up=unit(goal['dirs']['ChestUp']);goal_forward=unit(np.array([1.,0.,0.])-goal_up*goal_up[0]);goal_across=unit(np.cross(goal_up,goal_forward));goal_turn=np.column_stack([forward,up,across])@np.column_stack([goal_forward,goal_up,goal_across]).T
 arm_return=smooth((sf-spec['armRecoveryStartFrame'])/(spec['armRecoveryEndFrame']-spec['armRecoveryStartFrame']))
 for side,sign in [('Left',-1),('Right',1)]:
  shoulder=pelvis+up*.44+across*(sign*.18);upper=unit(upper_turn@(rel[side+'Elbow']-rel[side+'Shoulder']));fore=unit(upper_turn@(rel[side+'Hand']-rel[side+'Elbow']));goal_upper=unit(goal_turn@(np.array(goal['relative'][side+'Elbow'])-np.array(goal['relative'][side+'Shoulder'])));goal_fore=unit(goal_turn@(np.array(goal['relative'][side+'Hand'])-np.array(goal['relative'][side+'Elbow'])));upper=unit(upper*(1-arm_return)+goal_upper*arm_return);fore=unit(fore*(1-arm_return)+goal_fore*arm_return);j[side+'Shoulder']=shoulder;j[side+'Elbow']=shoulder+upper*.29;j[side+'Hand']=j[side+'Elbow']+fore*.27
 dirs=dict(PelvisForward=np.array([1.,0.,0.]),PelvisUp=UP,ChestForward=forward,ChestUp=up,HeadForward=gaze)
 # Remove the reference carrier once; runtime adds its real charged flight.
 for k in j:j[k]=j[k]-UP*h
 contacts=3 if sf<spec['frontSoleReleaseFrame']else 2 if sf<spec['rearSoleReleaseFrame']else 0
 phase='load'if sf<spec['popFrame']else'pop'if sf<spec['airFrame']else'air'
 if sf>=spec['catchFrame']:contacts=3;phase='catch'
 if sf>=spec['landFrame']:phase='land'
 if sf>=spec['recoveryFrame']:phase='recovery'
 return dict(t=(sf-spec['firstSourceFrame'])/spec['sourceFps'],sourceFrame=sf,sourceCacheTime=sf/spec['sourceFps'],joints={k:j[k].tolist()for k in JOINTS},dirs={k:dirs[k].tolist()for k in DIRS},soleUp=dict(Left=lf[:,1].tolist(),Right=rf[:,1].tolist()),board=dict(origin=(o-UP*h).tolist(),quaternion=matrix_quat(r).tolist()),phase=phase,contacts=contacts,heelContactAuthored=spec['frontSoleReleaseFrame']<=sf<=spec['heelFlickFrame'])

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--body',type=Path,default=ASSET/'captured-body.json');p.add_argument('--constraints',type=Path,default=ASSET/'heel-constraints.json');p.add_argument('--output',type=Path,required=True);a=p.parse_args();raw=a.body.read_bytes();c=json.loads(raw);s=json.loads(a.constraints.read_text());assert hashlib.sha256(raw).hexdigest()==s['sourceBodySha256'];s['pitchKeys']=[list(k)for k in s['pitchKeys']]
 for k in s['pitchKeys']:
  if k[0]==s['tailStrikeFrame']:k[1]=strike_pitch(s)
 fps=s['sourceFps'];first=s['firstSourceFrame'];end=s['lastSourceFrame'];duration=(end-first)/fps
 times=np.linspace(first,end,round(duration*60)+1);times=np.unique(np.r_[times,[s[k]for k in['popFrame','tailStrikeFrame','frontSoleReleaseFrame','rearSoleReleaseFrame','heelFlickFrame','airFrame','catchFrame','landFrame','recoveryFrame']],s['rearSoleReleaseFrame']+2])
 frames=[sample(c,s,float(sf))for sf in times];events=[dict(t=(sf-first)/fps,phase=phase,contacts=contact)for sf,phase,contact in[(first,'load',['LeftFoot','RightFoot']),(s['popFrame'],'pop',['LeftFoot','RightFoot']),(s['frontSoleReleaseFrame'],'pop',['RightFoot']),(s['rearSoleReleaseFrame'],'air',[]),(s['rearSoleReleaseFrame']+2,'air',[]),(s['catchFrame'],'catch',['LeftFoot','RightFoot']),(s['landFrame'],'land',['LeftFoot','RightFoot']),(s['recoveryFrame'],'recovery',['LeftFoot','RightFoot'])]]
 meta=dict(s['metadata'],label='Heelflip — source-specific heel contact and coupled flight [UNREVIEWED]',id='heelflip-source-keyed-v1',movementKind='heelflip',reviewed=False,authored=True,canonicalStance='regular',sourceFilmedStance=c['sourceFilmedStance'],sourceFps=fps,firstSourceFrame=first,lastSourceFrame=end,sourceBodySha256=s['sourceBodySha256'],sourcePoseSha256=c['sourcePoseSha256'],provenance='This Heelflip source captured torso/arm directions, with authored fixed-chain lower body, noseward torso/gaze reconstruction, non-spinning heading, actual tail support and heel-led toe-side exit; coupled measured-board envelope and gradual recovery. Not measured3D mocap.',contactLimit='Heel guide is authored; no calibrated surface-contact confidence. Existing source actor gaps333/355/356 retained as authored uncertainty.',armException='Visible source lifts arms above head350–354; retained instead of applying a Kickflip arm cap.',armRecoveryPrior=s['armRecoveryPrior'],compactBodyPrior=s['compactBodyPrior'])
 clip=dict(format='skate-capture-hybrid/1',metadata=meta,stance='regular',fps=60,duration=duration,events=contact_events(events,duration),frames=frames);a.output.mkdir(parents=True,exist_ok=True);(a.output/'heelflip-source-keyed-v1.json').write_text(json.dumps(clip,indent=2)+'\n');pack=encode_pack([clip]);(a.output/'skate_captures.skmc').write_bytes(pack);print('HEEL_READY',len(frames),len(pack),hashlib.sha256(pack).hexdigest())
if __name__=='__main__':main()
