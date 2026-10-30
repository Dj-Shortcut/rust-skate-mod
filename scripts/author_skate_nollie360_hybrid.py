#!/usr/bin/env python3
"""Bake the striped-rider Nollie360: own nose pop, rear instep and compact body.

Only fixed-chain, curved-shoe and pack mathematics are shared. The old standing
root/mean-ankle recipe is not reused. Hidden depth/contact remains reconstruction.
"""
import argparse,hashlib,json,math
from pathlib import Path
import numpy as np
from author_skate_kickflip import curve,foot_basis,limb
from author_skate_kickflip_hybrid import deck_shoe,smooth
from capture_math import JOINTS,DIRS,UP,unit,matrix_quat,qmatrix,slerp,encode_pack,contact_events
ROOT=Path(__file__).resolve().parents[1];ASSET=ROOT/'assets/authored/animation/nollie360-keyed'
def scalar(keys,f):return float(curve([k[0]for k in keys],[[k[1]]for k in keys],f)[0])
def vector(keys,f):return curve([k[0]for k in keys],[k[1:]for k in keys],f)
def turn(axis,degrees):
 a=math.radians(degrees);c,s=math.cos(a),math.sin(a)
 if axis=='x':return np.array([[1,0,0],[0,c,-s],[0,s,c]])
 if axis=='y':return np.array([[c,0,s],[0,1,0],[-s,0,c]])
 return np.array([[c,-s,0],[s,c,0],[0,0,1]])
def orientation(a,b,w):return qmatrix(slerp(matrix_quat(a),matrix_quat(b),w))
def fraction(s,f):return float(np.clip((f-s['popFrame'])/(s['landFrame']-s['popFrame']),0,1))
def reference_height(s,f,flight=None):return .5*s['gravity']*(flight or s['referenceFlight'])**2*fraction(s,f)*(1-fraction(s,f))
def board(s,f):
 r=turn('y',scalar(s['yawKeys'],f))@turn('x',-scalar(s['pitchKeys'],f))@turn('z',scalar(s['rollKeys'],f));h=reference_height(s,f);u=fraction(s,f)
 floor=-float((np.array(s['hybrid']['support']['board'])@r.T)[:,1].min());press=1-smooth((f-s['frontSoleReleaseFrame'])/s['pressFadeFrames'])
 y=floor*press+(.092+h+s['visualLift']*math.sin(math.pi*u)**2)*(1-press)
 if f<s['popFrame']or f>=s['landFrame']:y=.092
 # One coupled envelope, sized for the real minimum charged flight. This is
 # source reconstruction, not a new physics arc or an independent body lift.
 minimum=floor+h-reference_height(s,f,s['minimumFlight'])+.003*(1-press)
 correction=max(0,minimum-y)*(1-press)
 if correction>.12:raise ValueError('Nollie360 coupled model-floor reconstruction exceeds12cm at '+str(f))
 return np.array([0.,y+correction,0.]),r,h,correction

def attached(s,f,side):
 o,r,_,_=board(s,f);e=s['hybrid']['loadEntryFeet'][side];u=smooth((f-s['popFrame'])/2)
 x,z=e['ankle'][0],e['ankle'][2]
 if side=='Left':x+=.026*u # Front foot scoops the actual nose toward toeside.
 else:z=z*(1-u)-.25*u # Rear instep moves from the known charge middle to tail.
 target,fr=deck_shoe(s,o,r,x,z,65 if side=='Left'else 85,.003)
 up=unit(e['up']);forward=unit(np.array(e['toe'])-np.array(e['ankle'])+up*.07)
 seedframe=r@np.column_stack([unit(np.cross(up,forward)),up,forward]);seed=o+r@(np.array(e['ankle'])-UP*.092)
 weight=smooth((f-s['popFrame'])/1.3)
 return seed*(1-weight)+target*weight,orientation(seedframe,fr,weight)

def instep(s,f):
 o,r,_,_=board(s,f);shape=s['hybrid']['popContact'];p=np.array(s['instepDeckFeature']);kick=max(0,abs(p[2])-shape['kickStart'])
 p[1]=shape['kickRise']*(kick/shape['kickSpan'])**2+shape['concave']*(p[0]/shape['deckHalfWidth'])**2+.002
 normal=unit(np.array([-2*shape['concave']*p[0]/shape['deckHalfWidth']**2,1.,-2*shape['kickRise']*kick*np.sign(p[2])/shape['kickSpan']**2]))
 ns=unit(s['instepSurfaceNormal']);fs=unit(np.array([0.,0.,1.])-ns*ns[2]);nt=-normal;ft=unit(np.array(s['instepToeTangent'])-nt*np.dot(nt,s['instepToeTangent']))
 source=np.column_stack([fs,np.cross(ns,fs),ns]);target=np.column_stack([ft,np.cross(nt,ft),nt]);fr=r@target@source.T
 return o+r@p-fr@np.array(s['instepFeature']),fr

def caught(s,f,side):
 o,r,_,_=board(s,f);return deck_shoe(s,o,r,-.04,.24 if side=='Left'else-.24,80 if side=='Left'else 85,.003)

def foot(s,f,side):
 release=s['frontSoleReleaseFrame']if side=='Left'else s['rearInstepReleaseFrame'];sole=s['rearSoleReleaseFrame']
 if side=='Left'and f<=release:return attached(s,f,side)
 if side=='Right'and f<=sole:return attached(s,f,side)
 if side=='Right'and f<=release:
  a,ar=attached(s,f,side);b,br=instep(s,f);w=smooth((f-sole)/(release-sole));return a*(1-w)+b*w,orientation(ar,br,w)
 o,r,h,correction=board(s,f);target=vector(s['freeWorldFeet'][side],f)+UP*correction
 flat=foot_basis(scalar(s['airYaw'][side],f),0,scalar(s['airDorsi'][side],f))
 # Leave from this take's actual last contact; do not hold the flick foot
 # against the rising tail and then drop it onto unrelated free-height keys.
 start,sr=attached(s,release,side)if side=='Left'else instep(s,release)
 start+=UP*(correction-board(s,release)[3])
 w=smooth((f-release)/s['releaseBlendFrames']);ankle=start*(1-w)+target*w;fr=orientation(sr,flat,w)
 catch=s['frontCatchFrame']if side=='Left'else s['rearCatchFrame'];a,ar=caught(s,f,side);cw=smooth((f-(catch-s['catchApproachFrames']))/s['catchApproachFrames'])
 return ankle*(1-cw)+a*cw,orientation(fr,ar,cw)

def sample(controls,s,f):
 o,r,h,correction=board(s,f);la,lf=foot(s,f,'Left');ra,rf=foot(s,f,'Right');rows=controls['frames'];ts=[x['frame']for x in rows]
 rel={k:curve(ts,[x['relative'][k]for x in rows],f)for k in rows[0]['relative']};d={k:unit(curve(ts,[x['dirs'][k]for x in rows],f))for k in DIRS}
 px,pz=vector(s['pelvisXZKeys'],f);p=np.array([px,scalar(s['pelvisWorldYKeys'],f)+correction,pz]);heading=unit(np.array([d['PelvisForward'][0],0.,d['PelvisForward'][2]]));left=-unit(np.cross(UP,heading));hips=[p+left*.09,p-left*.09]
 maximum=[]
 for hip,ankle in zip(hips,[la,ra]):
  horizontal=(hip[0]-ankle[0])**2+(hip[2]-ankle[2])**2
  if horizontal>=(.87*.995)**2:raise ValueError('Nollie360 horizontal leg reach '+str(f))
  maximum.append(ankle[1]+math.sqrt((.87*.995)**2-horizontal))
 p[1]=min(p[1],min(maximum));j={'Pelvis':p}
 for side,ankle,fr,sign in [('Left',la,lf,1),('Right',ra,rf,-1)]:
  hip=p+left*(sign*.09);j[side+'Hip']=hip;j[side+'Knee']=limb(hip,ankle,.44,.43,unit(vector(s['kneePlanes'][side],f)))
  j[side+'Ankle']=ankle;j[side+'Toe']=ankle+fr[:,2]*math.sqrt(.14**2-.07**2)-fr[:,1]*.07
 up=d['ChestUp'];forward=unit(d['ChestForward']-up*np.dot(up,d['ChestForward']));across=unit(np.cross(up,forward));j['Chest']=p+up*(.52/1.12);j['Neck']=p+up*.52
 gaze=d['HeadForward'];headup=unit(UP-gaze*np.dot(UP,gaze));j['Head']=j['Neck']+headup*.12
 for side,sign in [('Left',-1),('Right',1)]:
  shoulder=p+up*.44+across*(sign*.18);j[side+'Shoulder']=shoulder;j[side+'Elbow']=shoulder+unit(rel[side+'Elbow']-rel[side+'Shoulder'])*.29;j[side+'Hand']=j[side+'Elbow']+unit(rel[side+'Hand']-rel[side+'Elbow'])*.27
  if side=='Left':
   # The visible lead wrist stays around shoulder level during this Air.
   # Its old standing-root retarget pulled it down; preserve its own depth
   # and measured reach, rebuilding only this source-led balance height.
   weight=smooth((f-4590)/2)*(1-smooth((f-4605)/2))
   offset=j[side+'Hand']-shoulder;reach=np.linalg.norm(offset);height=scalar(s['leadWristHeightKeys'],f)
   horizontal=unit(np.array([offset[0],0.,offset[2]]));goal=horizontal*math.sqrt(max(0,reach*reach-height*height))+UP*height
   target=shoulder+offset*(1-weight)+goal*weight
   j[side+'Elbow']=limb(shoulder,target,.29,.27,unit(rel[side+'Elbow']-rel[side+'Shoulder']));j[side+'Hand']=target
 # Correct this take's free-body metric placement as one rigid pickup. The
 # independently rotating board must not cut through a free sole. Both feet,
 # hips, torso and arms move together; source knee shapes are unchanged.
 pickup=s['freeRiderPickup'];a,b,c,e=pickup['window']
 amount=pickup['height']*smooth((f-a)/(b-a))*(1-smooth((f-c)/(e-c)))
 for key in j:j[key]+=UP*amount
 directions=dict(PelvisForward=heading,PelvisUp=UP,ChestForward=forward,ChestUp=up,HeadForward=gaze)
 contacts=3 if f<s['rearSoleReleaseFrame']else 1 if f<s['frontSoleReleaseFrame']else 0;phase='load'if f<s['popFrame']else'pop'if f<s['airFrame']else'air'
 if f>=s['frontCatchFrame']:contacts|=1;phase='catch'
 if f>=s['rearCatchFrame']:contacts|=2;phase='catch'
 if f>=s['landFrame']:phase='land'
 if f>=s['recoveryFrame']:phase='recovery'
 return dict(t=(f-s['firstSourceFrame'])/s['sourceFps'],sourceFrame=f,sourceCacheTime=f/s['sourceFps'],joints={k:(j[k]-UP*h).tolist()for k in JOINTS},dirs={k:directions[k].tolist()for k in DIRS},soleUp=dict(Left=lf[:,1].tolist(),Right=rf[:,1].tolist()),board=dict(origin=(o-UP*h).tolist(),quaternion=matrix_quat(r).tolist()),phase=phase,contacts=contacts)

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--body',type=Path,default=ASSET/'captured-body.json');p.add_argument('--constraints',type=Path,default=ASSET/'nollie360-constraints.json');p.add_argument('--output',required=True,type=Path);a=p.parse_args();raw=a.body.read_bytes();controls=json.loads(raw);s=json.loads(a.constraints.read_text())
 if hashlib.sha256(raw).hexdigest()!=s['sourceBodySha256']:raise ValueError('Nollie360 own body hash mismatch')
 a0,b0,c0,e0=s['freeRiderPickup']['window']
 if not s['frontSoleReleaseFrame']<a0<b0<=c0<e0<=s['frontCatchFrame'] or not 0<=s['freeRiderPickup']['height']<=.12:raise ValueError('Invalid free-only Nollie rider pickup')
 first,end,fps=s['firstSourceFrame'],s['lastSourceFrame'],s['sourceFps'];duration=(end-first)/fps
 moments=[s[k]for k in ['popFrame','noseStrikeFrame','frontSoleReleaseFrame','rearSoleReleaseFrame','rearInstepReleaseFrame','airFrame','frontCatchFrame','rearCatchFrame','landFrame','recoveryFrame']]
 times=np.unique(np.r_[np.linspace(first,end,round(duration*60)+1),moments,s['frontSoleReleaseFrame']+s['pressFadeFrames']]);frames=[sample(controls,s,float(f))for f in times]
 events=[dict(t=(f-first)/fps,phase=phase,contacts=contacts)for f,phase,contacts in [(first,'load',['LeftFoot','RightFoot']),(s['popFrame'],'pop',['LeftFoot','RightFoot']),(s['rearSoleReleaseFrame'],'pop',['LeftFoot']),(s['frontSoleReleaseFrame'],'air',[]),(s['frontSoleReleaseFrame']+s['pressFadeFrames'],'air',[]),(s['frontCatchFrame'],'catch',['LeftFoot']),(s['rearCatchFrame'],'catch',['LeftFoot','RightFoot']),(s['landFrame'],'land',['LeftFoot','RightFoot']),(s['recoveryFrame'],'recovery',['LeftFoot','RightFoot'])]]
 meta=dict(s['metadata'],id='nollie-360-flip-source-keyed-v1',label='Nollie360 Flip — striped-rider nose scoop and rear instep [UNREVIEWED]',movementKind='nollie-360-flip',reviewed=False,authored=True,canonicalStance='regular',sourceFps=fps,firstSourceFrame=first,lastSourceFrame=end,sourceBodySha256=s['sourceBodySha256'],sourcePoseSha256=controls['sourcePoseSha256'],provenance='Own Nollie360 upper-body capture basis with visible lead-arm balance reconstruction; source-led nose/front-pop and rear-instep/free-foot/root reconstruction. Complete +360roll/+360yaw. Monocular depth/contact/foot-orientation is authored; not measured 3D capture.')
 clip=dict(format='skate-capture-hybrid/1',metadata=meta,stance='regular',fps=60,duration=duration,events=contact_events(events,duration),frames=frames)
 a.output.mkdir(parents=True,exist_ok=True);(a.output/'nollie-360-flip-source-keyed-v1.json').write_text(json.dumps(clip,indent=2)+'\n');pack=encode_pack([clip]);(a.output/'skate_captures.skmc').write_bytes(pack);print('NOLLIE360_READY',len(frames),len(pack),hashlib.sha256(pack).hexdigest())
if __name__=='__main__':main()
