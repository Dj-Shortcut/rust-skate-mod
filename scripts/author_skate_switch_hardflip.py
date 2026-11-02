#!/usr/bin/env python3
"""Bake Rodriguez's own Switch Hardflip body, instep flick and complete turn.

Only original rig/curved-deck and interpolation mathematics is shared. The
normal Hardflip's source timings, motion controls and client tuck are unused.
"""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from author_skate_kickflip import curve, limb
from author_skate_kickflip_hybrid import body_at
from author_skate_treflip import basis, deck_shoe, free_foot_clearance, smooth
from capture_math import (DIRS, JOINTS, UP, contact_events, encode_pack,
                          euler_quat, matrix_quat, qmatrix, slerp, unit)
from capture_retarget import transport_leg_pole

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT/'assets/authored/animation/switch-hardflip-keyed'
GEOMETRY = ROOT/'assets/authored/animation/treflip-keyed'
digest = lambda raw: hashlib.sha256(raw).hexdigest()


def keyed(keys, sf):
    return curve([r[0] for r in keys], [r[1:] for r in keys], sf)


def blend_frame(a, b, weight):
    return qmatrix(slerp(matrix_quat(a), matrix_quat(b), weight))


def board_at(s, sf):
    p = s['phases']
    u = float(np.clip((sf-p['pop'])/(p['land']-p['pop']), 0, 1))
    height = .5*s['reference']['gravity']*s['reference']['flightSeconds']**2*u*(1-u)
    yaw, pitch, roll = [float(keyed(s[name+'Keys'], sf)[0]) for name in ('yaw','pitch','roll')]
    # Compose unwrapped full winding before conversion, never shortest-path
    # interpolation between identical endpoints. The rider does not turn180.
    rotation = (qmatrix(euler_quat([0, yaw, 0])) @
                qmatrix(euler_quat([-pitch, 0, 0])) @
                qmatrix(euler_quat([0, 0, roll])))
    pressure = 1-smooth((sf-p['rearRelease'])/(p['pressEnd']-p['rearRelease']))
    floor = -float((np.asarray(s['_geometry']['support']['board'])@rotation.T)[:,1].min())
    free = .092+height
    need = floor+s['reference']['wheelClearance']*(1-pressure)
    safe = .5*(free+need+math.sqrt((free-need)**2+.004**2))
    world_y = floor*pressure+safe*(1-pressure)
    if sf <= p['pop'] or sf >= p['land']:
        world_y = .092
    return np.array([0., world_y-height, 0.]), rotation, height


def attached(s, sf, side, catch=False):
    origin, rotation, _ = board_at(s, sf)
    target = s['catchFeet' if catch else 'setupFeet'][side]
    x, z, yaw = target['x'], target['z'], target['yaw']
    if catch:
        # Keep anatomical/world-side feet and toe headings when the deck swaps
        # ends. Left remains world noseward, on deck-local negativeZ after180.
        final = qmatrix(euler_quat([0, -180, 0]))
        local = final.T@np.array([x, 0., z])
        heading = final.T@np.array([math.sin(math.radians(yaw)),0.,math.cos(math.radians(yaw))])
        x, z = local[0], local[2]
        yaw = math.degrees(math.atan2(heading[0],heading[2]))
    return deck_shoe(s['_geometry'], origin, rotation, x, z, yaw)


def tail_ball(s, sf):
    origin, rotation, _ = board_at(s, sf)
    t = s['setupFeet']['Right']
    ankle, rest = deck_shoe(s['_geometry'], np.zeros(3), np.eye(3), t['x'],t['z'],t['yaw'])
    ball = np.asarray(s['tailBall']['shoeLocal'])
    point = ankle+rest@ball
    lift = float(keyed(s['tailBall']['heelLiftKeys'],sf)[0])
    # The curved tail rest already has a kick-normal tilt. Unwind that rest
    # toward genuine world-up as the ball presses, before adding the small
    # observed deck follow; retaining the old kick-normal in world space
    # would oppose the snap and falsely invert the sole at its release.
    upright = basis(unit(rest[:,2]*[1,0,1]),UP)
    pressure = smooth((sf-s['phases']['pop'])/(s['phases']['strike']-s['phases']['pop']))
    base = blend_frame(rest,upright,pressure)
    frame = blend_frame(base,rotation@rest,s['tailBall']['boardFollowShare'])@qmatrix(euler_quat([lift,0,0]))
    return origin+rotation@point-frame@ball, frame


def instep(s, sf):
    origin, rotation, _ = board_at(s,sf)
    shape = s['_geometry']['shoe']
    feature = s['instepGuide']
    point = np.asarray(feature['deckPoint'],dtype=float).copy()
    x,z = point[0],point[2]
    kick = max(0.,abs(z)-shape['kickStart'])
    point[1] = shape['kickRise']*(kick/shape['kickSpan'])**2+shape['concave']*(x/shape['deckHalfWidth'])**2+feature['gap']
    normal = unit(np.array([-2*shape['concave']*x/shape['deckHalfWidth']**2,1.,-2*shape['kickRise']*kick/shape['kickSpan']**2*np.sign(z)]))
    source_normal = unit(feature['shoeNormal'])
    source_forward = unit(np.array([0.,0.,1.])-source_normal*source_normal[2])
    target_normal = -rotation@normal
    yaw = math.radians(feature['toeYaw'])
    # Both contact normal AND toe tangent follow the same source board frame.
    raw_forward = rotation@np.array([math.sin(yaw),0.,math.cos(yaw)])
    target_forward = unit(raw_forward-target_normal*np.dot(target_normal,raw_forward))
    source = np.column_stack((source_forward,np.cross(source_normal,source_forward),source_normal))
    target = np.column_stack((target_forward,np.cross(target_normal,target_forward),target_normal))
    foot_frame = target@source.T
    return origin+rotation@point-foot_frame@np.asarray(feature['shoePoint']),foot_frame


def foot(s, sf, side):
    p = s['phases']
    if side == 'Left':
        if sf <= p['frontRelease']:
            return attached(s,sf,side)
        if sf <= p['instepRelease']:
            a,ar = attached(s,sf,side); b,br = instep(s,sf)
            w = smooth((sf-p['frontRelease'])/(p['instepRelease']-p['frontRelease']))
            return a*(1-w)+b*w,blend_frame(ar,br,w)
        release,held = p['instepRelease'],lambda f:instep(s,f)
    else:
        release,held = p['rearRelease'],lambda f:tail_ball(s,f)
        if sf <= release:
            return held(sf)
    origin,rotation,height = board_at(s,sf)
    start,start_frame = held(release)
    # A free ankle follows this take's WORLD pickup, independently from the
    # steep deck/floor envelope. Remove reference carrier exactly once.
    start += UP*board_at(s,release)[2]
    target = keyed(s['frontFreeWorldKeys' if side=='Left' else 'rearFreeWorldKeys'],sf)
    w = smooth((sf-release)/s['freeFootBlendFrames'][side])
    ankle = start*(1-w)+target*w-UP*height
    yaw = math.radians(s['catchFeet'][side]['yaw'])
    free_frame = basis(np.array([math.sin(yaw),0.,math.cos(yaw)]),UP)
    frame = blend_frame(start_frame,free_frame,w)
    caught_at = p['firstCatch'] if side=='Left' else p['bothCatch']
    caught = smooth((sf-(caught_at-s['catchBlendFrames']))/s['catchBlendFrames'])
    if caught:
        goal,goal_frame = attached(s,sf,side,catch=True)
        ankle = ankle*(1-caught)+goal*caught
        frame = blend_frame(frame,goal_frame,caught)
    if caught < 1:
        for _ in range(2):
            ankle[1] += free_foot_clearance(s['_geometry'],origin,rotation,ankle,frame)
    return ankle,frame


def sample(c,s,sf):
    p = s['phases']
    j,d,soles = body_at(c,(sf-s['firstSourceFrame'])/s['sourceFps'])
    recovery = smooth((sf-s['recoveryWindow'][0])/(s['recoveryWindow'][1]-s['recoveryWindow'][0]))
    if recovery:
        terminal,td,_ = body_at(c,(s['recoverySourceFrame']-s['firstSourceFrame'])/s['sourceFps'])
        for k in j:j[k]=j[k]*(1-recovery)+terminal[k]*recovery
        for k in d:d[k]=unit(d[k]*(1-recovery)+td[k]*recovery)
    origin,rotation,height = board_at(s,sf)
    x,z = keyed(s['pelvisXZKeys'],sf)
    pelvis = np.array([x,float(keyed(s['pelvisWorldYKeys'],sf)[0])-height,z])
    shift = pelvis-j['Pelvis']
    for k in j:j[k]+=shift
    # The source wrist extrema clip/blur at the pop. Reconstruct only forearm
    # depth, with bent forward balance in this rider's anatomical torso axes.
    hip_side = unit(j['RightHip']-j['LeftHip'])
    facing = unit(np.cross(hip_side,UP))
    axes = np.column_stack((facing,UP,hip_side))
    prior = s['croppedForearms'];a,b,c0,d0=prior['window']
    weight = prior['strength']*smooth((sf-a)/(b-a))*(1-smooth((sf-c0)/(d0-c0)))
    for side in ('Left','Right'):
        if weight:
            current = unit(j[side+'Hand']-j[side+'Elbow'])
            goal = unit(axes@np.asarray(prior[side]))
            j[side+'Hand']=j[side+'Elbow']+unit(current*(1-weight)+goal*weight)*.27
    original={k:v.copy()for k,v in j.items()}
    feet={side:foot(s,sf,side)for side in ('Left','Right')}
    lo,hi=-float('inf'),float('inf')
    reach=.87*s['maximumReachShare']
    for side,(ankle,_)in feet.items():
        hip=j[side+'Hip'];offset=hip-j['Pelvis'];horizontal=float(np.sum((hip[[0,2]]-ankle[[0,2]])**2))
        if horizontal>=reach*reach:raise ValueError('Own Switch Hardflip horizontal leg reach '+str(sf))
        half=math.sqrt(reach*reach-horizontal);lo=max(lo,ankle[1]-offset[1]-half);hi=min(hi,ankle[1]-offset[1]+half)
    if lo>hi:raise ValueError('Own Switch Hardflip has no fixed-foot root interval '+str(sf))
    shift=UP*(float(np.clip(j['Pelvis'][1],lo,hi))-j['Pelvis'][1])
    for k in j:j[k]+=shift
    depth=float(keyed(s['kneePlaneBlendKeys'],sf)[0])
    for side,(ankle,frame)in feet.items():
        hip=j[side+'Hip'];pole=transport_leg_pole(original[side+'Hip'],original[side+'Knee'],original[side+'Ankle'],hip,ankle)
        toe=unit(frame[:,2]*[1,0,1]);pole=unit(unit(pole)*(1-depth)+toe*depth)
        j[side+'Knee']=limb(hip,ankle,.44,.43,pole);j[side+'Ankle']=ankle
        j[side+'Toe']=ankle+frame[:,2]*math.sqrt(.14**2-.07**2)-frame[:,1]*.07;soles[side]=frame[:,1]
    gaze=unit(d['HeadForward']);head_up=unit(UP-gaze*np.dot(UP,gaze));j['Head']=j['Neck']+head_up*.12
    for side in ('Left','Right'):
        j[side+'Elbow']=j[side+'Shoulder']+unit(j[side+'Elbow']-j[side+'Shoulder'])*.29
        j[side+'Hand']=j[side+'Elbow']+unit(j[side+'Hand']-j[side+'Elbow'])*.27
    return dict(t=(sf-s['firstSourceFrame'])/s['sourceFps'],sourceFrame=sf,sourceCacheTime=sf/s['sourceFps'],sourceVideoTime=sf/s['sourceFps'],joints={k:j[k].tolist()for k in JOINTS},dirs={k:d[k].tolist()for k in DIRS},soleUp={k:soles[k].tolist()for k in soles},board=dict(origin=origin.tolist(),quaternion=matrix_quat(rotation).tolist()))


def bake(body_raw,controls):
    s=copy.deepcopy(controls)
    if digest(body_raw)!=s['sourceBodySha256']:raise ValueError('Own Rodriguez body controls differ')
    c=json.loads(body_raw)
    if (c['firstSourceFrame'],c['lastSourceFrame'],c['filmedStance'],c['sourceReflectionAppliedOnce'],c['reviewed'])!=(7804,7932,'regular',False,False):raise ValueError('Own Switch Hardflip source/anatomy binding differs')
    raw=(GEOMETRY/'constraints.json').read_bytes()
    if digest(raw)!=s['geometrySha256']:raise ValueError('Original board/shoe geometry differs')
    g=json.loads(raw);surface=(GEOMETRY/'own-board-surface.json').read_bytes()
    if digest(surface)!=g['sourceSurfaceSha256']:raise ValueError('Original shaped deck surface differs')
    shape=json.loads(surface);g['_surfacePoints'],g['_surfaceTriangles']=np.asarray(shape['points']),np.asarray(shape['triangles']);g['authored']['minimumAirSoleClearance']=s['minimumAirSoleClearance'];s['_geometry']=g
    p=s['phases'];ordered=[s['firstSourceFrame']]+[p[k]for k in ['pop','frontRelease','instepRelease','strike','rearRelease','pressEnd','firstCatch','bothCatch','land','recovery']]+[s['lastSourceFrame']]
    if not all(a<b for a,b in zip(ordered,ordered[1:])):raise ValueError('Own source phases must strictly increase')
    for name in ('yawKeys','pitchKeys','rollKeys','pelvisXZKeys','pelvisWorldYKeys','frontFreeWorldKeys','rearFreeWorldKeys','kneePlaneBlendKeys'):
        if not all(a[0]<b[0]for a,b in zip(s[name],s[name][1:])):raise ValueError('Own keys must increase: '+name)
    samples=np.unique(np.round(np.r_[np.arange(s['firstSourceFrame'],s['lastSourceFrame']+1e-7,1/6),ordered],9));frames=[sample(c,s,float(sf))for sf in samples]
    events=[(s['firstSourceFrame'],'load',['LeftFoot','RightFoot']),(p['pop'],'pop',['LeftFoot','RightFoot']),(p['frontRelease'],'pop',['RightFoot']),(p['rearRelease'],'pop',[]),(p['pressEnd'],'air',[]),(p['firstCatch'],'catch',['LeftFoot']),(p['bothCatch'],'catch',['LeftFoot','RightFoot']),(p['land'],'land',['LeftFoot','RightFoot']),(p['recovery'],'recovery',['LeftFoot','RightFoot'])]
    meta=dict(id=s['id'],movementKind=s['movementKind'],label='Switch Hardflip — own Rodriguez source [UNREVIEWED]',reviewed=False,authored=True,capturedBody=True,canonicalStance='regular',filmedStance='regular',sourceReflectionAppliedOnce=False,nativeSignature=s['nativeSignature'],firstSourceFrame=s['firstSourceFrame'],lastSourceFrame=s['lastSourceFrame'],sourceFps=s['sourceFps'],sourceUrl=s['sourceUrl'],sourceAuthor='The Berrics',sourceVideoSha256=s['sourceVideoSha256'],sourceBodyControlSha256=s['sourceBodySha256'],sourcePoseSha256=c['sourcePoseSha256'],sourceTiming=c['sourceTiming'],sourceVideoOffsetSeconds=s['firstSourceFrame']/s['sourceFps'],sourceCacheOriginalOffsetSeconds=0,referenceGroundPress=s['reference'],combinedWinding=dict(nativeFlip=360,nativeShove=180,bodySpin=0,canonicalRoll=360,canonicalYaw=-180),sourceRootMotion='Own hip-centered capture; independent world pickup/root exclude reference carrier once; actual native carrier owns jump/travel.',contactLimit='Own source-led Right tail pressure and Left upper-outer instep flick, not a sole flick. Shoe contact/depth/3D board rotation are authored estimates.',provenance=s['provenance'],uncertainty=s['uncertainty'],sourcePhaseLimit=s['sourcePhaseLimit'],reconstruction=dict(pelvisWorldYKeys=s['pelvisWorldYKeys'],kneePlaneBlendKeys=s['kneePlaneBlendKeys'],croppedForearms=s['croppedForearms'],recoveryWindow=s['recoveryWindow']),rig=dict(thigh=.44,shin=.43,upperArm=.29,forearm=.27,spine=.52,shoulderRise=.44,hipHalfWidth=.09,shoulderHalfWidth=.18,head=.12,foot=.14,toeDrop=.07))
    duration=(s['lastSourceFrame']-s['firstSourceFrame'])/s['sourceFps'];clip=dict(format='skate-source-keyed-switch-hardflip/1',metadata=meta,stance='regular',fps=s['sourceFps']*6,duration=duration,events=contact_events([dict(t=(sf-s['firstSourceFrame'])/s['sourceFps'],phase=ph,contacts=cs)for sf,ph,cs in events],duration),frames=frames)
    return clip,encode_pack([clip])


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--body',type=Path,default=ASSET/'captured-body.json');parser.add_argument('--controls',type=Path,default=ASSET/'constraints.json');parser.add_argument('--output',type=Path,required=True);a=parser.parse_args()
    if a.body.resolve().parent==a.output.resolve():raise ValueError('Preserve authored source inputs')
    clip,packed=bake(a.body.read_bytes(),json.loads(a.controls.read_text()));a.output.mkdir(parents=True,exist_ok=True);temporary=[]
    try:
        for name,data in [('switch-hardflip-source-keyed-v1.json',(json.dumps(clip,indent=2)+'\n').encode()),('skate_captures.skmc',packed)]:
            target=a.output/name;tmp=target.with_suffix(target.suffix+'.tmp');tmp.write_bytes(data);temporary.append((tmp,target))
        for tmp,target in temporary:tmp.replace(target)
    finally:
        for tmp,_ in temporary:
            if tmp.exists():tmp.unlink()
    print('OWN_SWITCH_HARDFLIP_READY',len(clip['frames']),digest(packed))


if __name__=='__main__':main()
