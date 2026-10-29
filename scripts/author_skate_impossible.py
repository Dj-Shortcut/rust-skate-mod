#!/usr/bin/env python3
"""Bake an original source-led rear-foot Impossible reconstruction (NumPy only).

The captured directions remain a gesture basis. Board winding, rear-hook depth,
free-foot tuck and physical-carrier normalization are explicitly authored.
"""
import argparse, hashlib, json, math
from pathlib import Path
import numpy as np
from author_skate_kickflip import curve, limb, foot_basis
from capture_math import JOINTS, DIRS, UP, encode_pack, contact_events, euler_quat, matrix_quat, qmatrix, slerp, unit
ROOT=Path(__file__).resolve().parents[1]
ASSET=ROOT/'assets/authored/animation/impossible-keyed'


def smooth(x):
 x=float(np.clip(x,0,1));return x*x*x*(10+x*(-15+6*x))

def value(keys,sf):return float(curve([k[0]for k in keys],[[k[1]]for k in keys],sf)[0])
def basis(f,u):
 u=unit(u);f=unit(f-u*np.dot(f,u));return np.column_stack((unit(np.cross(u,f)),u,f))
def blend_frame(a,b,u):return qmatrix(slerp(matrix_quat(a),matrix_quat(b),smooth(u)))
def upper(c,sf,pelvis):
 rows=c['rows'];i=max(0,min(len(rows)-2,int(sf)-c['firstSourceFrame']));a,b=rows[i:i+2];u=float(np.clip(sf-a['frame'],0,1))
 raw={k:np.asarray(a['joints'][k])*(1-u)+np.asarray(b['joints'][k])*u for k in JOINTS};dirs={k:unit(np.asarray(a['dirs'][k])*(1-u)+np.asarray(b['dirs'][k])*u)for k in DIRS}
 torso=unit(raw['Neck']-raw['Pelvis']);hipaxis=unit(raw['RightHip']-raw['LeftHip']);shoulders=unit(raw['RightShoulder']-raw['LeftShoulder']);shoulders=unit(shoulders-torso*np.dot(shoulders,torso))
 j=dict(Pelvis=pelvis,Chest=pelvis+torso*(.52/1.12),Neck=pelvis+torso*.52);j['Head']=j['Neck']+unit(raw['Head']-raw['Neck'])*.12
 for side,sign in [('Left',-1),('Right',1)]:
  j[side+'Hip']=pelvis+hipaxis*(sign*.09);j[side+'Shoulder']=pelvis+torso*.44+shoulders*(sign*.18)
  j[side+'Elbow']=j[side+'Shoulder']+unit(raw[side+'Elbow']-raw[side+'Shoulder'])*.29
  j[side+'Hand']=j[side+'Elbow']+unit(raw[side+'Hand']-raw[side+'Elbow'])*.27
 # Exact input-owned Load at Pop, then the actual source upper gesture.
 n=c['neutralCharge'];w=smooth((sf-477)/2.5)
 for k in ('Chest','Neck','Head','LeftShoulder','RightShoulder','LeftElbow','RightElbow','LeftHand','RightHand'):
  j[k]=pelvis+(np.array(n['joints'][k])-np.array(n['joints']['Pelvis']))*(1-w)+(j[k]-pelvis)*w
 hipaxis=unit((np.array(n['joints']['RightHip'])-np.array(n['joints']['LeftHip']))*(1-w)+hipaxis*w)
 for side,sign in [('Left',-1),('Right',1)]:j[side+'Hip']=pelvis+hipaxis*(sign*.09)
 for k in DIRS:dirs[k]=unit(np.array(n['dirs'][k])*(1-w)+dirs[k]*w)
 # Reconstruct the fixed chain lengths after direction interpolation.
 torso=unit(j['Neck']-pelvis);j['Chest']=pelvis+torso*(.52/1.12);j['Neck']=pelvis+torso*.52;j['Head']=j['Neck']+unit(j['Head']-j['Neck'])*.12
 shoulderaxis=unit(j['RightShoulder']-j['LeftShoulder']);shoulderaxis=unit(shoulderaxis-torso*np.dot(shoulderaxis,torso))
 for side,sign in [('Left',-1),('Right',1)]:
  arm=unit(j[side+'Elbow']-j[side+'Shoulder']);fore=unit(j[side+'Hand']-j[side+'Elbow']);j[side+'Shoulder']=pelvis+torso*.44+shoulderaxis*(sign*.18);j[side+'Elbow']=j[side+'Shoulder']+arm*.29;j[side+'Hand']=j[side+'Elbow']+fore*.27
 return j,dirs

def sample(c,s,sf):
 f=float(np.clip((sf-s['pop'])/(s['land']-s['pop']),0,1));href=.5*s['gravity']*s['referenceFlight']**2*f*(1-f)
 pitch=value(s['pitchKeys'],sf);q=euler_quat([-pitch,0,0]);r=qmatrix(q)
 neutral=c['neutralCharge'];nj={k:np.array(v)for k,v in neutral['joints'].items()};nb={side:basis(nj[side+'Toe']-nj[side+'Ankle']+np.array(neutral['soleUp'][side])*.07,neutral['soleUp'][side])for side in ('Left','Right')}
 # A real solid-board support hull fixes the tail strike, without moving physics.
 profile=np.array(s['support']['profileYZ']);hull=np.column_stack((np.zeros(len(profile)),profile));support=-float(np.min((hull@r.T)[:,1]))
 board=np.array([0.,.092,0.]);rear=nj['RightAnkle'].copy();front=nj['LeftAnkle'].copy();ru=nb['Right'];lu=nb['Left']
 if sf>s['pop']:
  # During pressure, both soles begin from the exact native shaped-deck Load.
  on_ground=np.array([0.,support,0.]);rear_press=on_ground+r@(nj['RightAnkle']-UP*.092)
  path=s['rearBodyRelativeXZ'];xz=curve([k[0]for k in path],[k[1:]for k in path],sf);collect=smooth((sf-478.25)/2.75)*(1-smooth((sf-487)/3));rest_x=-.045;rest_z=-.323+smooth((sf-485)/8)*.103
  rear_hook=np.array([rest_x*(1-collect)+xz[0]*collect,value(s['rearWorldY'],sf),rest_z*(1-collect)+xz[1]*collect]);hook_weight=smooth((sf-478.25)/1.75)
  rear=rear_press*(1-hook_weight)+rear_hook*hook_weight
  target_ru=foot_basis(85,0,-value(s['rearToeDown'],sf));ru=blend_frame(r@nb['Right'],target_ru,(sf-478.25)/1.75)
  front=np.array([-.04-.055*smooth((sf-478)/3)*(1-smooth((sf-487)/6)),value(s['frontWorldY'],sf),.04267+.20733*smooth((sf-478)/3)-.03*smooth((sf-487)/6)])
  target_lu=foot_basis(65,0,12*smooth((sf-478)/4)*(1-smooth((sf-487)/6)));lu=blend_frame(nb['Left'],target_lu,(sf-477)/2)
  # The supported tail sole becomes a forefoot hook. Contact is one continuous
  # surface feature, not an ankle pivot and not a free flip with rear-foot pins.
  shape=s['shape'];h=lambda x,z:shape['kickRise']*(max(0,abs(z)-shape['kickStart'])/shape['kickSpan'])**2+shape['concave']*(x/shape['deckHalfWidth'])**2
  deck0=np.array([0.,h(0,-.32),-.32]);feature0=nb['Right'].T@(UP*.092+deck0-nj['RightAnkle'])
  wrap=smooth((sf-479)/3)*(1-smooth((sf-487)/3))
  feature=feature0*(1-wrap)+np.array(s['hookShoeLocal'])*wrap
  dz=-.32-.025*wrap;width=shape['deckHalfWidth']*math.sqrt(max(0,1-((abs(dz)-.288)/.112)**2));dx=-(width+.002)*wrap
  outward=unit((ru@np.array([0.,-1.,0.]))*(1-wrap)+(ru@np.array(s['hookSurfaceNormalLocal']))*wrap)
  underside=smooth((float(np.dot(outward,r[:,1]))+.1)/.2)*wrap
  deck=np.array([dx,h(dx,dz)-s['deckThickness']*underside,dz])
  hooked=rear+ru@feature-r@deck
  # Pressure ends at the existing .005m-tail-contact plateau; thereafter the
  # board follows the rear feature throughout its full vertical winding.
  board=on_ground*(1-hook_weight)+hooked*hook_weight
  airclear=.025*smooth((sf-479)/2)*(1-smooth((sf-490)/3));needed=airclear-(board[1]-support)
  if needed>0:
   board+=UP*needed;rear+=UP*needed
 # Return to actual flat two-foot riding; source gesture continues through514.
 recovery=smooth((sf-490)/3)
 flat=np.array([0.,.092+href,0.]);board=board*(1-recovery)+flat*recovery
 if sf>=490:
  for side in ('Left','Right'):
   target=np.array([-.055,.187+href,.22 if side=='Left' else -.22]);target_basis=foot_basis(65 if side=='Left' else 85)
   if side=='Left':front=front*(1-recovery)+target*recovery;lu=blend_frame(lu,target_basis,recovery)
   else:rear=rear*(1-recovery)+target*recovery;ru=blend_frame(ru,target_basis,recovery)
 pelvis=np.array([-.055,value(s['pelvisLocalY'],sf),-.001]);j,dirs=upper(c,sf,pelvis)
 # Native carrier contributes exactly one physical arc; subtract the reference
 # arc from authored absolute foot/board controls, never from source directions.
 front-=UP*href;rear-=UP*href;board-=UP*href
 for side,a,foot in [('Left',front,lu),('Right',rear,ru)]:
  j[side+'Ankle']=a;j[side+'Toe']=a+foot[:,2]*math.sqrt(.14*.14-.07*.07)-foot[:,1]*.07
  # A stable anatomical plane avoids the old captured recovery pole flips.
  j[side+'Knee']=limb(j[side+'Hip'],a,.44,.43,unit(np.array([1.,0.,.10 if side=='Left' else -.10])))
 event='Load';contacts=['LeftFoot','RightFoot']
 if sf>=s['pop']:event='Pop';contacts=['RightFoot']
 if sf>=480:event='Air'
 if sf>=s['catch']:event='Catch';contacts=['LeftFoot','RightFoot']
 if sf>=s['land']:event='Land'
 if sf>=s['recovery']:event='Recovery'
 return dict(t=(sf-c['firstSourceFrame'])/c['sourceFps'],sourceFrame=sf,joints={k:j[k].tolist()for k in JOINTS},dirs={k:dirs[k].tolist()for k in DIRS},soleUp=dict(Left=lu[:,1].tolist(),Right=ru[:,1].tolist()),board=dict(origin=board.tolist(),quaternion=q.tolist()),phase=event,contacts=sum(dict(LeftFoot=1,RightFoot=2)[x]for x in contacts),authoredBoardPitch=pitch,rearHook=True)

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',type=Path,required=True);a=p.parse_args();c=json.loads((ASSET/'captured-body.json').read_text());s=json.loads((ASSET/'keys.json').read_text());first=c['firstSourceFrame'];duration=(s['end']-first)/c['sourceFps']
 times=np.unique(np.r_[np.linspace(first,s['end'],85),[477,477.75,478.25,480,481,482,483,484,485,486,487,488,489,490,493,499]])
 frames=[sample(c,s,float(sf))for sf in times]
 events=[dict(t=(sf-first)/c['sourceFps'],phase=phase.lower(),contacts=contacts)for sf,phase,contacts in [(472,'Load',['LeftFoot','RightFoot']),(477,'Pop',['RightFoot']),(480,'Air',['RightFoot']),(490,'Catch',['LeftFoot','RightFoot']),(493,'Land',['LeftFoot','RightFoot']),(499,'Recovery',['LeftFoot','RightFoot'])]]
 meta=dict(id=s['id'],label='Normal Impossible - rear-foot vertical wrap',movementKind='impossible',reviewed=False,sourceUrl=c['sourceUrl'],sourceAuthor=c['sourceAuthor'],canonicalStance='regular',authored=True,capturedBody=True,sourceFps=c['sourceFps'],firstSourceFrame=first,lastSourceFrame=s['end'],sourcePoseSha256=c['sourcePoseSha256'],provenance=s['provenance'],rearPathProvenance=s['rearPathProvenance'],hookSurfaceProvenance=s['hookSurfaceProvenance'],rearHookContract='Canonical right/rear foot owns the tail through Pop and Air; front is free. Rear hook is not a flat sole-support event. Goofy mirrors anatomically.',sourceRootMotion='Source estimated world root and board discarded. Absolute board/foot reference subtracts T=.6/g8 once; native upright carrier adds actual physical root once.')
 clip=dict(format='skate-authored-impossible/1',metadata=meta,stance='regular',fps=(len(frames)-1)/duration,duration=duration,events=contact_events(events,duration),frames=frames)
 packed=encode_pack([clip]);a.output.mkdir(parents=True,exist_ok=True);(a.output/(s['id']+'.json')).write_text(json.dumps(clip,indent=2)+'\n');temp=a.output/'.skate_captures.skmc.tmp';temp.write_bytes(packed);temp.replace(a.output/'skate_captures.skmc');print('IMPOSSIBLE_SOURCE_READY',len(frames),len(packed),hashlib.sha256(packed).hexdigest(),'reviewed=false')
if __name__=='__main__':main()
