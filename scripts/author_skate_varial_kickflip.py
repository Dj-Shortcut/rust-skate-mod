#!/usr/bin/env python3
"""Author a BS180/kickflip derivation from an explicitly shared Tre body.

Requires the pinned, unreviewed Tre reconstruction, not a filmed Varial take.
"""
import argparse,hashlib,json,math
from pathlib import Path
import numpy as np
import author_skate_treflip as tre
from author_skate_kickflip import limb
from capture_math import JOINTS,DIRS,UP,unit,qmatrix,euler_quat,matrix_quat,slerp,contact_events,encode_pack
ROOT=Path(__file__).resolve().parents[1]
CONTROLS=ROOT/'assets/authored/animation/varial-kickflip-derived/constraints.json'
TRE=ROOT/'assets/authored/animation/treflip-keyed'
sha=lambda data:hashlib.sha256(data).hexdigest()


def source_at(source,sf):
    rows=source['frames'];times=source['_times'];t=(sf-source['metadata']['firstSourceFrame'])/source['metadata']['sourceFps'];i=max(0,min(len(rows)-2,int(np.searchsorted(times,t,side='right'))-1));u=float(np.clip((t-times[i])/(times[i+1]-times[i]),0,1))
    blend=lambda a,b:np.asarray(a)*(1-u)+np.asarray(b)*u
    j={k:blend(rows[i]['joints'][k],rows[i+1]['joints'][k])for k in JOINTS};d={k:unit(blend(rows[i]['dirs'][k],rows[i+1]['dirs'][k]))for k in DIRS};soles={k:unit(blend(rows[i]['soleUp'][k],rows[i+1]['soleUp'][k]))for k in ['Left','Right']}
    return j,d,soles


def board_at(s,sf):
    b=s['_base'];a,hold=b['reference']['hold'];pitch=tre.strike_pitch(b);pop=s['pop'];f=float(np.clip((sf-pop)/(s['land']-pop),0,1));H=.5*b['reference']['gravity']*b['reference']['flightSeconds']**2*f*(1-f)
    if sf<=pop:q=np.array([0.,0.,0.,1.])
    elif sf<=hold:
        yaw=s['pressYawDegrees']*tre.smooth((sf-pop)/(hold-pop));tilt=pitch*tre.smooth((sf-pop)/(a-pop));q=tre.qmul(euler_quat([0,yaw,0]),euler_quat([-tilt,0,0]))
    else:
        yaw=s['pressYawDegrees']+(180-s['pressYawDegrees'])*tre.smooth((sf-hold)/(s['rotationEnd']-hold));roll=360*tre.smooth((sf-s['upperRelease'])/(s['rotationEnd']-s['upperRelease']));tilt=pitch*(1-tre.smooth((sf-hold)/(s['pressEnd']+1-hold)));q=tre.qmul(euler_quat([0,yaw,0]),euler_quat([-tilt,0,roll]))
    R=qmatrix(q);floor=-float((np.asarray(b['support']['board'])@R.T)[:,1].min())
    if sf<=pop:y=.092
    elif sf<=hold:y=floor-H
    else:
        free=tre.smooth((sf-hold)/(s['pressEnd']-hold));margin=b['authored']['referenceAirFloorMargin']*free*(1-tre.smooth((sf-s['rotationEnd'])/(s['land']-s['rotationEnd'])));y=max(.092,floor-H+margin)
    return np.array([0.,y,0.]),q,R,H


def catch_frame(s,side,origin,R):
    # Preserve the anatomical world-side target, then transform ALL coordinates
    # and heading into the reversed deck. Keeping Tre +/-Z pins swaps the feet.
    point,forward=s['_catchWorld'][side];local=s['_catchRotation'].T@point;direction=s['_catchRotation'].T@forward;yaw=math.degrees(math.atan2(direction[0],direction[2]))
    return tre.deck_shoe(s['_base'],origin,R,float(local[0]),float(local[2]),yaw)


def free_frame(source,s,sf,side):
    if sf>s['catchStart']:
        a,F=free_frame(source,s,s['catchStart'],side);b,_=free_frame(source,s,s['catchStart']-.001,side)
        return a+(a-b)*((sf-s['catchStart'])/.001),F
    j,d,soles=source_at(source,sf);ankle=j[side+'Ankle'].copy();forward=unit(j[side+'Toe']-ankle+soles[side]*.07)
    if side=='Right':
        depth=tre.smooth((sf-s['rearRelease'])/4)*(1-tre.smooth((sf-414)/6));ankle-=np.array([-.934,0.,-.356])*s['rearScoopDepthReduction']*depth
    return ankle,tre.basis(forward,soles[side])


def sample(source,s,sf):
    b=s['_base'];j,d,soles=source_at(source,sf);original={k:v.copy()for k,v in j.items()};origin,q,R,H=board_at(s,sf);feet={}
    for side in ['Left','Right']:
        if sf<s['pressEnd']:
            u=tre.smooth((sf-s['pop'])/(s['rearRelease']-s['pop']));z=b['authored']['pressRearZ']if side=='Right'else .275;x=b['authored']['pressRearX']if side=='Right'else -.045;yaw=85 if side=='Right'else 56
            ankle,F=tre.entry_foot(b,side,origin,R,u,x,z,yaw)
            if side=='Left':
                roll=tre.smooth((sf-(s['pop']+.25))/(s['upperRelease']-s['pop']-.25));contactR=R if sf<=s['upperRelease']else board_at(s,s['upperRelease'])[2];upperF=tre.instep(b,contactR);upper=origin+R@np.asarray(b['flickPatch']['deckLocal'])-upperF@np.asarray(b['flickPatch']['shoeLocal']);ankle=ankle*(1-roll)+upper*roll;F=qmatrix(slerp(matrix_quat(F),matrix_quat(upperF),roll))
            release=s['upperRelease']if side=='Left'else s['rearRelease'];weight=tre.smooth((sf-release)/(s['pressEnd']-release));target,targetF=free_frame(source,s,s['pressEnd'],side);ankle=ankle*(1-weight)+target*weight;F=qmatrix(slerp(matrix_quat(F),matrix_quat(targetF),weight))
        else:ankle,F=free_frame(source,s,sf,side)
        caught=tre.smooth((sf-s['catchStart'])/(s['catch']-s['catchStart']))
        if caught:
            goal,goalF=catch_frame(s,side,origin,R);ankle=ankle*(1-caught)+goal*caught;F=qmatrix(slerp(matrix_quat(F),matrix_quat(goalF),caught))
        if sf>s['upperRelease']and sf<s['catch']:ankle[1]+=tre.free_foot_clearance(b,origin,R,ankle,F)
        feet[side]=ankle,F
    lo,hi=-float('inf'),float('inf');reach=.87*s['maximumReachShare']
    for side,(ankle,F)in feet.items():
        offset=j[side+'Hip']-j['Pelvis'];horizontal=float(np.sum((j[side+'Hip'][[0,2]]-ankle[[0,2]])**2))
        if horizontal>=reach*reach:raise ValueError(f'Unreachable derived {side} horizontal target at {sf}')
        half=math.sqrt(reach*reach-horizontal);lo=max(lo,ankle[1]-offset[1]-half);hi=min(hi,ankle[1]-offset[1]+half)
    if lo>hi:raise ValueError(f'No common fixed-foot pelvis interval at {sf}')
    delta=UP*(float(np.clip(j['Pelvis'][1],lo,hi))-j['Pelvis'][1])
    for key in j:j[key]+=delta
    for side,(ankle,F)in feet.items():
        hip=j[side+'Hip'];a=original[side+'Hip'];end=original[side+'Ankle'];v=end-a;pole=original[side+'Knee']-a-v*np.dot(original[side+'Knee']-a,v)/np.dot(v,v);j[side+'Knee']=limb(hip,ankle,.44,.43,pole);j[side+'Ankle']=ankle;j[side+'Toe']=ankle+F[:,2]*math.sqrt(.14**2-.07**2)-F[:,1]*.07;soles[side]=F[:,1]
    return dict(t=(sf-s['firstSourceFrame'])/s['_fps'],sourceFrame=sf,sourceCacheTime=sf/s['_fps'],joints={k:j[k].tolist()for k in JOINTS},dirs={k:d[k].tolist()for k in DIRS},soleUp={k:soles[k].tolist()for k in soles},board=dict(origin=origin.tolist(),quaternion=q.tolist()))


def bake(raw,s):
    if sha(raw)!=s['sourceJsonSha256']:raise ValueError('Shared Tre source differs from its pinned reconstruction')
    if sha(Path(tre.__file__).read_bytes())!=s['treHelperSha256']:raise ValueError('Shared geometry helper changed')
    source=json.loads(raw);m=source['metadata']
    if m['id']!=s['sourceId']or m['reviewed']is not False:raise ValueError('An explicitly unreviewed Tre source is required')
    source['_times']=[r['t']for r in source['frames']];s['_fps']=m['sourceFps'];b=json.loads((TRE/'constraints.json').read_text());surface_raw=(TRE/'own-board-surface.json').read_bytes()
    if sha(surface_raw)!=b['sourceSurfaceSha256']:raise ValueError('Own board surface changed')
    surface=json.loads(surface_raw);b['_surfacePoints']=np.asarray(surface['points']);b['_surfaceTriangles']=np.asarray(surface['triangles']);s['_base']=b;s['_catchRotation']=qmatrix(euler_quat([0,180,360]));s['_catchWorld']={}
    for side,z,yaw in [('Left',.24,65),('Right',-.24,85)]:
        ankle,F=tre.deck_shoe(b,np.zeros(3),np.eye(3),-.04,z,yaw);s['_catchWorld'][side]=(ankle,F[:,2])
    phases=[(s['firstSourceFrame'],'load',['LeftFoot','RightFoot']),(s['pop'],'pop',['RightFoot']),(s['rearRelease'],'air',[]),(s['pressEnd'],'air',[]),(s['catch'],'catch',['LeftFoot','RightFoot']),(s['land'],'land',['LeftFoot','RightFoot']),(s['recovery'],'recovery',['LeftFoot','RightFoot'])]
    frames=np.unique(np.round(np.r_[np.arange(s['firstSourceFrame'],s['lastSourceFrame']+1e-7,1/6),[v[0]for v in phases],s['upperRelease'],s['rotationEnd'],s['catchStart']],9));samples=[sample(source,s,float(sf))for sf in frames];duration=(s['lastSourceFrame']-s['firstSourceFrame'])/s['_fps']
    meta=dict(id=s['id'],label='Varial Kickflip — authored shared-Tre derivation [UNREVIEWED]',movementKind=s['movementKind'],reviewed=False,authored=True,capturedBody=True,sourceUrl=m['sourceUrl'],sourceAuthor=m['sourceAuthor'],sourceFps=s['_fps'],firstSourceFrame=s['firstSourceFrame'],lastSourceFrame=s['lastSourceFrame'],canonicalStance='regular',referenceFootageMovementKind='360-flip',filmedVarialTake=False,sharedSourceId=s['sourceId'],sharedSourceJsonSha256=s['sourceJsonSha256'],provenance=s['provenance'],referenceGroundPress=b['reference'],combinedWinding=dict(nativeFlip=360,nativeShove=-180,canonicalYaw=180,bodySpin=0),catchTransform='Anatomical world targets transformed by final deck R-transpose: full XYZ and toe heading, with measured sole/deck fitting',sourceRootMotion='Carrier-local body and board; reference ballistic carrier excluded; actual physical carrier added once by playback')
    clip=dict(format='skate-capture-authored-varial/1',metadata=meta,stance='regular',fps=s['_fps']*6,duration=duration,events=contact_events([dict(t=(sf-s['firstSourceFrame'])/s['_fps'],phase=p,contacts=c)for sf,p,c in phases],duration),frames=samples)
    return clip,encode_pack([clip])


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--input',type=Path,required=True);p.add_argument('--controls',type=Path,default=CONTROLS);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    if a.input.resolve().parent==a.output.resolve():raise ValueError('Preserve the shared source directory')
    clip,packed=bake(a.input.read_bytes(),json.loads(a.controls.read_text()));payloads=[('varial-kickflip-derived-tre-v1.json',(json.dumps(clip,indent=2)+'\n').encode()),('skate_captures.skmc',packed)];a.output.mkdir(parents=True,exist_ok=True);temporary=[]
    try:
        for name,data in payloads:
            target=a.output/name;tmp=target.with_suffix(target.suffix+'.tmp');tmp.write_bytes(data);temporary.append((tmp,target))
        for tmp,target in temporary:tmp.replace(target)
    finally:
        for tmp,target in temporary:
            if tmp.exists():tmp.unlink()
    print('VARIAL_DERIVED_SOURCE_READY',len(clip['frames']),sha(packed))


if __name__=='__main__':main()
