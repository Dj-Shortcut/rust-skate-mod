#!/usr/bin/env python3
"""Bake the own BATB13 black1091 Switch FS Pop Shove-it body and scoop."""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from author_skate_bs_pop_shove import board_at
from author_skate_kickflip import curve, limb
from author_skate_kickflip_hybrid import body_at
from author_skate_treflip import basis, deck_shoe, free_foot_clearance, smooth
from capture_math import (JOINTS, DIRS, UP, contact_events, encode_pack,
                          euler_quat, matrix_quat, qmatrix, slerp, unit)
from capture_retarget import transport_leg_pole

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT/'assets/authored/animation/switch-fs-pop-shove-keyed'
GEOMETRY = ROOT/'assets/authored/animation/treflip-keyed'
digest = lambda data: hashlib.sha256(data).hexdigest()


def keyed(keys, sf):
    return curve([r[0] for r in keys], [r[1:] for r in keys], sf)


def attached(s, sf, side, catch=False):
    origin, rotation = board_at(s, sf)
    target = s['catchFeet' if catch else 'setupFeet'][side]
    if catch:
        # Board turns; anatomical/world-side feet and headings do not turn180.
        final = qmatrix(euler_quat([0, -180, 0]))
        local = final.T@np.array([target['x'], 0., target['z']])
        heading = final.T@np.array([math.sin(math.radians(target['yaw'])), 0., math.cos(math.radians(target['yaw']))])
        yaw = math.degrees(math.atan2(heading[0], heading[2]))
        return deck_shoe(s['_geometry'], origin, rotation, local[0], local[2], yaw)
    return deck_shoe(s['_geometry'], origin, rotation, target['x'], target['z'], target['yaw'])


def rear_ball(s, sf):
    origin, rotation = board_at(s, sf)
    target = s['setupFeet']['Right']
    rest, rest_frame = deck_shoe(s['_geometry'], np.zeros(3), np.eye(3), target['x'], target['z'], target['yaw'])
    ball = np.asarray(s['rearBall']['shoeLocal'])
    point = rest+rest_frame@ball
    lift = float(keyed(s['rearBall']['heelLiftKeys'], sf)[0])
    frame = rotation@rest_frame@qmatrix(euler_quat([lift, 0, 0]))
    return origin+rotation@point-frame@ball, frame


def foot(s, sf, side):
    p = s['phases']
    release = p['frontRelease'] if side == 'Left' else p['rearRelease']
    held = (lambda t: attached(s, t, side)) if side == 'Left' else (lambda t: rear_ball(s, t))
    if sf <= release:
        return held(sf)
    start, start_frame = held(release)
    target = keyed(s['frontFreeKeys' if side == 'Left' else 'rearFreeKeys'], sf)
    origin, rotation = board_at(s, sf)
    target = target+origin
    yaw = math.radians(s['catchFeet'][side]['yaw'])
    free_frame = basis(np.array([math.sin(yaw), 0., math.cos(yaw)]), UP)
    released = smooth((sf-release)/s['freeFootBlendFrames'])
    ankle = start*(1-released)+target*released
    frame = qmatrix(slerp(matrix_quat(start_frame), matrix_quat(free_frame), released))
    caught_at = p['firstCatch'] if side == 'Left' else p['bothCatch']
    caught = smooth((sf-(caught_at-s['catchBlendFrames']))/s['catchBlendFrames'])
    if caught:
        goal, goal_frame = attached(s, sf, side, catch=True)
        ankle = ankle*(1-caught)+goal*caught
        frame = qmatrix(slerp(matrix_quat(frame), matrix_quat(goal_frame), caught))
    if caught < 1:
        # Existing shaped-deck/shoe guard; no remote-nose projection or guard relaxation.
        for _ in range(2):
            ankle[1] += free_foot_clearance(s['_geometry'], origin, rotation, ankle, frame)
    return ankle, frame


def sample(c, s, sf):
    p = s['phases']
    joints, dirs, soles = body_at(c, (sf-s['firstSourceFrame'])/s['sourceFps'])
    recovery = smooth((sf-s['recoveryWindow'][0])/(s['recoveryWindow'][1]-s['recoveryWindow'][0]))
    if recovery:
        terminal, terminal_dirs, _ = body_at(c, (s['recoverySourceFrame']-s['firstSourceFrame'])/s['sourceFps'])
        for k in joints:
            joints[k] = joints[k]*(1-recovery)+terminal[k]*recovery
        for k in dirs:
            dirs[k] = unit(dirs[k]*(1-recovery)+terminal_dirs[k]*recovery)
    origin, rotation = board_at(s, sf)
    height = float(keyed(s['pelvisHeightKeys'], sf)[0])
    target = origin+np.array([s['pelvisXZ'][0], height, s['pelvisXZ'][1]])
    shift = target-joints['Pelvis']
    for k in joints:
        joints[k] += shift
    # Own cropped/occluded arm depth is reconstructed in anatomical torso axes.
    # These are explicit authored directions, not another take's body motion.
    hip_side = unit(joints['RightHip']-joints['LeftHip'])
    facing = unit(np.cross(hip_side, UP))
    axes = np.column_stack((facing, UP, hip_side))
    arm = s['croppedArmBalance']
    a, b, c0, d = arm['window']
    weight = smooth((sf-a)/(b-a))*(1-smooth((sf-c0)/(d-c0)))
    hidden = s['hiddenArm']
    suffix = smooth((sf-hidden['start'])/(hidden['full']-hidden['start']))
    for prior, blend in ((arm, weight), (hidden, suffix)):
        side = prior['side']
        for name, previous, length in [('Elbow', 'Shoulder', .29), ('Hand', 'Elbow', .27)]:
            direction = unit(joints[side+name]-joints[side+previous])
            goal = unit(axes@np.asarray(prior['upper' if name == 'Elbow' else 'fore']))
            joints[side+name] = joints[side+previous]+unit(direction*(1-blend)+goal*blend)*length
    original = {k:v.copy() for k,v in joints.items()}
    feet = {side:foot(s, sf, side) for side in ('Left', 'Right')}
    lo, hi = -float('inf'), float('inf')
    reach = .87*s['maximumReachShare']
    for side, (ankle, _) in feet.items():
        hip = joints[side+'Hip']
        offset = hip-joints['Pelvis']
        horizontal = float(np.sum((hip[[0,2]]-ankle[[0,2]])**2))
        if horizontal >= reach*reach:
            raise ValueError('Own Switch shove exceeds horizontal fixed-leg reach at '+str(sf))
        half = math.sqrt(reach*reach-horizontal)
        lo, hi = max(lo, ankle[1]-offset[1]-half), min(hi, ankle[1]-offset[1]+half)
    if lo > hi:
        raise ValueError('Own Switch shove has no common fixed-foot root interval at '+str(sf))
    shift = UP*(float(np.clip(joints['Pelvis'][1],lo,hi))-joints['Pelvis'][1])
    for k in joints:
        joints[k] += shift
    depth_weight = float(keyed(s['kneePlaneBlendKeys'], sf)[0])
    for side, (ankle, frame) in feet.items():
        hip = joints[side+'Hip']
        pole = transport_leg_pole(original[side+'Hip'], original[side+'Knee'], original[side+'Ankle'], hip, ankle)
        toe = unit(frame[:,2]*[1,0,1])
        pole = unit(unit(pole)*(1-depth_weight)+toe*depth_weight)
        joints[side+'Knee'] = limb(hip,ankle,.44,.43,pole)
        joints[side+'Ankle'] = ankle
        joints[side+'Toe'] = ankle+frame[:,2]*math.sqrt(.14**2-.07**2)-frame[:,1]*.07
        soles[side] = frame[:,1]
    gaze = unit(dirs['HeadForward'])
    head_up = unit(UP-gaze*np.dot(UP,gaze))
    joints['Head'] = joints['Neck']+head_up*.12
    for side in ('Left','Right'):
        joints[side+'Elbow'] = joints[side+'Shoulder']+unit(joints[side+'Elbow']-joints[side+'Shoulder'])*.29
        joints[side+'Hand'] = joints[side+'Elbow']+unit(joints[side+'Hand']-joints[side+'Elbow'])*.27
    return dict(t=(sf-s['firstSourceFrame'])/s['sourceFps'],sourceFrame=sf,sourceCacheTime=sf/s['sourceFps'],
        sourceVideoTime=s['sourceCacheOriginalOffsetSeconds']+sf/s['sourceFps'],
        joints={k:joints[k].tolist()for k in JOINTS},dirs={k:dirs[k].tolist()for k in DIRS},
        soleUp={k:soles[k].tolist()for k in soles},board=dict(origin=origin.tolist(),quaternion=matrix_quat(rotation).tolist()))


def bake(body_raw, controls):
    s = copy.deepcopy(controls)
    if digest(body_raw) != s['sourceBodySha256']:
        raise ValueError('Own black1091 Switch body controls differ')
    c = json.loads(body_raw)
    if (c['firstSourceFrame'],c['lastSourceFrame'],c['filmedStance'],c['sourceReflectionAppliedOnce'],c['reviewed']) != (738,817,'regular',False,False):
        raise ValueError('Own Switch source/anatomical reflection binding differs')
    raw = (GEOMETRY/'constraints.json').read_bytes()
    if digest(raw) != s['geometrySha256']:
        raise ValueError('Original board/shoe geometry differs')
    g = json.loads(raw)
    surface = (GEOMETRY/'own-board-surface.json').read_bytes()
    if digest(surface) != g['sourceSurfaceSha256']:
        raise ValueError('Original shaped deck surface differs')
    shape = json.loads(surface)
    g['_surfacePoints'],g['_surfaceTriangles'] = np.asarray(shape['points']),np.asarray(shape['triangles'])
    g['authored']['minimumAirSoleClearance'] = s['minimumAirSoleClearance']
    s['_geometry'],s['support'] = g,g['support']
    ordered = [s['firstSourceFrame']]+[s['phases'][k]for k in ['pop','frontRelease','strike','rearRelease','pressEnd','firstCatch','bothCatch','land','recovery']]+[s['lastSourceFrame']]
    if not all(a < b for a,b in zip(ordered,ordered[1:])):
        raise ValueError('Own Switch phase clock must strictly increase')
    for name in ('yawKeys','pitchKeys','pelvisHeightKeys','frontFreeKeys','rearFreeKeys','kneePlaneBlendKeys'):
        keys=s[name]
        if not all(a[0]<b[0]for a,b in zip(keys,keys[1:])):
            raise ValueError('Own Switch keys must strictly increase: '+name)
    frames=[sample(c,s,float(sf))for sf in np.unique(np.round(np.r_[np.arange(s['firstSourceFrame'],s['lastSourceFrame']+1e-7,1/6),ordered],9))]
    if any(qmatrix(f['board']['quaternion'])[1,1] <= 0 for f in frames):
        raise ValueError('A pure shove may not turn the board underneath in a flip')
    p=s['phases'];events=[(s['firstSourceFrame'],'load',['LeftFoot','RightFoot']),(p['pop'],'pop',['LeftFoot','RightFoot']),
        (p['frontRelease'],'pop',['RightFoot']),(p['rearRelease'],'pop',[]),(p['pressEnd'],'air',[]),
        (p['firstCatch'],'catch',['LeftFoot']),(p['bothCatch'],'catch',['LeftFoot','RightFoot']),
        (p['land'],'land',['LeftFoot','RightFoot']),(p['recovery'],'recovery',['LeftFoot','RightFoot'])]
    meta=dict(id=s['id'],movementKind=s['movementKind'],reviewed=False,authored=True,capturedBody=True,
        label='Switch FS Pop Shove-it — own BATB13 black1091 source [UNREVIEWED]',canonicalStance='regular',filmedStance='regular',
        sourceReflectionAppliedOnce=False,nativeSignature=s['nativeSignature'],firstSourceFrame=s['firstSourceFrame'],lastSourceFrame=s['lastSourceFrame'],sourceFps=s['sourceFps'],
        sourceUrl=s['sourceUrl'],sourceAuthor=s['sourceAuthor'],sourceVideoSha256=s['sourceVideoSha256'],sourceBodyControlSha256=s['sourceBodySha256'],sourcePoseSha256=c['sourcePoseSha256'],sourceTiming=c['sourceTiming'],
        sourceVideoOffsetSeconds=s['sourceCacheOriginalOffsetSeconds']+s['firstSourceFrame']/s['sourceFps'],sourceCacheOriginalOffsetSeconds=s['sourceCacheOriginalOffsetSeconds'],
        referenceGroundPress=s['reference'],combinedWinding=dict(nativeFlip=0,nativeShove=180,bodySpin=0,canonicalYaw=-180,canonicalRoll=0),
        sourceRootMotion='Hip-centered own capture; reference carrier excluded once; actual native carrier owns jump and travel.',
        contactLimit='Own source-led tail-ball/sole release, shaped-deck fit and signed180 scoop are authored3D estimates. Front foot lifts; no instep or heel flick.',
        provenance=s['provenance'],uncertainty=s['uncertainty'],sourcePhaseLimit=s['sourcePhaseLimit'],
        reconstruction=dict(pelvisHeightKeys=s['pelvisHeightKeys'],kneePlaneBlendKeys=s['kneePlaneBlendKeys'],hiddenArm=s['hiddenArm'],recoveryWindow=s['recoveryWindow'],croppedArmBalance=s['croppedArmBalance']),
        rig=dict(thigh=.44,shin=.43,upperArm=.29,forearm=.27,spine=.52,shoulderRise=.44,hipHalfWidth=.09,shoulderHalfWidth=.18,head=.12,foot=.14,toeDrop=.07))
    duration=(s['lastSourceFrame']-s['firstSourceFrame'])/s['sourceFps']
    clip=dict(format='skate-source-keyed-switch-fs-shove/1',metadata=meta,stance='regular',fps=s['sourceFps']*6,duration=duration,
        events=contact_events([dict(t=(sf-s['firstSourceFrame'])/s['sourceFps'],phase=phase,contacts=contacts)for sf,phase,contacts in events],duration),frames=frames)
    return clip,encode_pack([clip])


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--controls',type=Path,default=ASSET/'constraints.json');p.add_argument('--body',type=Path,default=ASSET/'captured-body.json');p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    if a.body.resolve().parent == a.output.resolve():
        raise ValueError('Preserve the authored source inputs')
    clip,packed=bake(a.body.read_bytes(),json.loads(a.controls.read_text()));a.output.mkdir(parents=True,exist_ok=True)
    payloads=[('switch-fs-pop-shove-it-source-keyed-v1.json',(json.dumps(clip,indent=2)+'\n').encode()),('skate_captures.skmc',packed)]
    temporary=[]
    try:
        for name,data in payloads:
            target=a.output/name;tmp=target.with_suffix(target.suffix+'.tmp');tmp.write_bytes(data);temporary.append((tmp,target))
        for tmp,target in temporary:tmp.replace(target)
    finally:
        for tmp,target in temporary:
            if tmp.exists():tmp.unlink()
    print('OWN_SWITCH_FS_SHOVE_READY',len(clip['frames']),digest(packed))


if __name__=='__main__':main()
