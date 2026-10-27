#!/usr/bin/env python3
"""Retarget one real MediaPipe ollie capture; no video/model is bundled.

Python 3 + numpy. Input is the retained per-frame MediaPipe image/world33 JSON.
The generated motion/data are CC BY-SA 3.0, derived from Medvedev's Ollie 1.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import numpy as np

JOINTS = ('Pelvis Chest Neck Head LeftHip RightHip LeftKnee RightKnee '
          'LeftAnkle RightAnkle LeftToe RightToe LeftShoulder RightShoulder '
          'LeftElbow RightElbow LeftHand RightHand').split()
DIRS = 'PelvisForward PelvisUp ChestForward ChestUp HeadForward'.split()
UP = np.array([0., 1., 0.])

def unit(v, fallback=UP):
    n = np.linalg.norm(v)
    return v / n if n > 1e-8 else np.array(fallback, dtype=float)

def interp(keys, frame):
    a = np.array(keys, dtype=float)
    return np.array([np.interp(frame, a[:, 0], a[:, k]) for k in range(1, a.shape[1])])

def smooth(a):
    b = np.pad(a, ((1, 1),) + ((0, 0),) * (a.ndim - 1), mode='edge')
    return (b[:-2] + 2 * b[1:-1] + b[2:]) / 4

def ramp(t, a, b):
    x = min(1., max(0., (t-a)/(b-a)))
    return x*x*(3-2*x)

def turn(v, axis, radians):
    axis = unit(axis)
    return v*math.cos(radians) + np.cross(axis, v)*math.sin(radians) + axis*np.dot(axis, v)*(1-math.cos(radians))

def ik(a, target, l1, l2, pole):
    d = target - a
    length = np.linalg.norm(d)
    length = min(l1+l2-0.008, max(abs(l1-l2)+0.008, length))
    axis = unit(d)
    target = a + axis*length
    bend = unit(pole-axis*np.dot(pole, axis), [0, 0, 1])
    along = (l1*l1-l2*l2+length*length)/(2*length)
    middle = a+axis*along+bend*math.sqrt(max(0., l1*l1-along*along))
    return middle, target

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--pose-json', required=True, type=Path)
    p.add_argument('--calibration', required=True, type=Path)
    p.add_argument('--output', required=True, type=Path)
    p.add_argument('--csharp', required=True, type=Path)
    args = p.parse_args()
    raw = json.loads(args.pose_json.read_text())
    cal = json.loads(args.calibration.read_text())
    first, last = cal['first_frame'], cal['last_frame']
    fs = [f for f in raw['frames'] if first <= f['frame_index'] <= last]
    if len(fs) != last-first+1 or any(f['pose_count'] != 1 for f in fs):
        raise ValueError('Every selected source frame must have one actual detection.')
    source_sha = raw['source']['sha256']
    if cal['source_sha256'] and cal['source_sha256'] != source_sha:
        raise ValueError('Source checksum differs from the selected reference.')
    world = np.array([[[v[k] for k in ('x','y','z')] for v in f['world_landmarks'][0]] for f in fs])
    image = np.array([[[v[k] for k in ('x','y')] for v in f['image_landmarks'][0]] for f in fs])
    # Hip-relative model coordinates: x right, y down, z camera depth.
    # A single fixed basis avoids inventing a new body heading every frame.
    forward = np.mean(world[:7, 28]-world[:7, 27], axis=0)
    forward[1] = 0
    forward = unit(forward)
    up_camera = np.array([0.,-1.,0.])
    # MediaPipe camera-world is right-handed; our Unity-style Y-up,+Z-nose
    # authoring frame is left-handed. This basis must include a reflection.
    # A rotation-only basis reverses torso lean relative to anatomical knees.
    right = -unit(np.cross(up_camera, forward))
    basis = np.stack((right, up_camera, forward), axis=1)
    world = smooth(world@basis)
    image = smooth(image)
    legs = [np.linalg.norm(world[:,h]-world[:,k], axis=1)+np.linalg.norm(world[:,k]-world[:,a], axis=1)
            for h,k,a in ((23,25,27),(24,26,28))]
    metric_scale = .87/float(np.median(legs))
    world *= metric_scale
    # Captured Right is the front foot. Mirror plus limb exchange gives Regular.
    world[:,:,0] *= -1
    pairs = [(11,12),(13,14),(15,16),(17,18),(19,20),(21,22),(23,24),(25,26),(27,28),(29,30),(31,32),(7,8)]
    for left, right_index in pairs:
        world[:,[left,right_index]] = world[:,[right_index,left]]
        image[:,[left,right_index]] = image[:,[right_index,left]]
    frames=[]
    for index,(f,w,im) in enumerate(zip(fs,world,image)):
        fi=f['frame_index']; t=(fi-first)/cal['source_fps']
        nose, tail = interp(cal['board_endpoint_keys'],fi).reshape(2,2)
        px=float(interp(cal['ground_board_pixels'],fi)[0])
        ppm=px/cal['board_length_m']
        ground_y=float(interp(cal['ground_center_y_pixels'],fi)[0])
        rise=max(0.,(ground_y-(nose[1]+tail[1])*.5)/ppm)
        pitch=0.
        if 29 <= fi <= 41:
            dy=(tail[1]+cal['ground_nose_minus_tail_y_pixels']-nose[1])/ppm
            pitch=-math.asin(np.clip(dy/cal['board_length_m'],-.93,.93))
            rise=max(rise, .4*abs(math.sin(pitch))-.07)
        else: rise=0.
        board=np.array([0.,cal['ground_deck_top_m']+rise,0.])
        R=np.array([[1,0,0],[0,math.cos(pitch),-math.sin(pitch)],[0,math.sin(pitch),math.cos(pitch)]])
        bforward=R@np.array([0.,0.,1.]); bup=R@UP
        ankle_mean=(w[27]+w[28])*.5
        pelvis=-ankle_mean
        pelvis[0]=np.clip(pelvis[0],-.14,.14)
        pelvis[2]=np.clip(pelvis[2],-.16,.16)
        pelvis=board+pelvis+UP*.095
        # Retain the captured lean/hip axis, retarget lengths, constrain deck contacts.
        hip_axis=unit(w[24]-w[23], [0,0,-1])
        torso=unit((w[11]+w[12]-w[23]-w[24])*.5)
        torso=unit(np.array([np.clip(torso[0],-.8,.8),max(.35,torso[1]),np.clip(torso[2],-.8,.8)]))
        shoulder_axis=unit(w[12]-w[11], [0,0,-1])
        shoulder_axis=unit(shoulder_axis-torso*np.dot(shoulder_axis,torso))
        hips=[pelvis-hip_axis*.09,pelvis+hip_axis*.09]
        anatomical_forward=unit(np.cross(hip_axis,UP),[1,0,0])
        ankles=[]; poles=[]; soles=[]; headings=[]
        for side, ai, ki, hi, heel, toe in ((0,27,25,23,29,31),(1,28,26,24,30,32)):
            # Actual image ankle projection along the manually observed board endpoints.
            pix=im[ai]*np.array([f['width'],f['height']])
            line=nose-tail
            q=float(np.dot(pix-tail,line)/np.dot(line,line))
            z=float(np.clip((q-.5)*.8, .08,.30) if side==0 else np.clip((q-.5)*.8,-.30,-.08))
            # Single-view heel/toe depth can point a foot backwards. The anatomical
            # forward hemisphere is known from the measured hip line, not the old
            # preview mesh's opposite-facing rest basis.
            heading=unit(w[toe]-w[heel],anatomical_forward)
            heading=unit(heading-bup*np.dot(heading,bup),anatomical_forward)
            expected=unit(anatomical_forward-bup*np.dot(anatomical_forward,bup))
            if np.dot(heading,expected)<0: heading=-heading
            heading=unit(expected*.7+heading*.3)
            # The source front foot has levelled with the deck by frame 36.
            # End the side/instep roll before the apex instead of carrying it
            # through the airborne catch.
            blend=ramp(fi,30,33)*(1-ramp(fi,33,36)) if side==0 else 0.
            sole=unit(turn(bup,heading,math.radians(58)*blend))
            ankles.append(board+R@np.array([.035,.095,z]))
            pole=w[ki]-w[hi]
            axial=unit(ankles[-1]-hips[side])
            bend=pole-axial*np.dot(pole,axial)
            expected_bend=anatomical_forward-axial*np.dot(anatomical_forward,axial)
            if np.dot(bend,expected_bend)<0: bend=-bend
            poles.append(unit(expected_bend)*.65+unit(bend)*.35)
            soles.append(sole);headings.append(heading)
        # Prevent an overextended native leg before solving its measured two-bone chain.
        for _ in range(8):
            excess=max(np.linalg.norm(ankles[s]-hips[s])-.852 for s in (0,1))
            if excess<=0:break
            pelvis[1]-=excess+.001
            hips=[pelvis-hip_axis*.09,pelvis+hip_axis*.09]
        j={'Pelvis':pelvis,'Chest':pelvis+torso*(.52/1.12),'Neck':pelvis+torso*.52}
        j['Head']=j['Neck']+unit(w[0]-(w[11]+w[12])*.5)*.12
        for side,name in enumerate(('Left','Right')):
            knee,ankle=ik(hips[side],ankles[side],.44,.43,poles[side])
            j[name+'Hip']=hips[side];j[name+'Knee']=knee;j[name+'Ankle']=ankle
            j[name+'Toe']=ankle+headings[side]*math.sqrt(.14**2-.07**2)-soles[side]*.07
            si,ei,wi=(11,13,15) if side==0 else (12,14,16)
            shoulder=pelvis+torso*.44+shoulder_axis*(.18 if side==1 else -.18)
            elbow=shoulder+unit(w[ei]-w[si])*.29
            wrist=elbow+unit(w[wi]-w[ei])*.27
            j[name+'Shoulder']=shoulder;j[name+'Elbow']=elbow;j[name+'Hand']=wrist
        p_up=unit(torso*.45+UP*.55)
        p_fwd=unit(np.cross(hip_axis,p_up))
        c_fwd=unit(np.cross(shoulder_axis,torso))
        head_fwd=unit(w[0]-(w[7]+w[8])*.5,c_fwd)
        dirs=dict(zip(DIRS,(p_fwd,p_up,c_fwd,torso,head_fwd)))
        frames.append(dict(t=round(t,9),sourceFrame=fi,joints={k:list(map(float,v)) for k,v in j.items()},
             dirs={k:list(map(float,v)) for k,v in dirs.items()},soleUp={s:list(map(float,v)) for s,v in zip(('Left','Right'),soles)},
             deck=dict(pos=[0.,rise,0.],rotDeg=[-math.degrees(pitch),0.,0.]),
             boardDeckTop=list(map(float,board)),boardPitchRadians=pitch,
             contact=dict(leftFoot='deck',rightFoot='deck',leftHand=None,rightHand=None),
             phase='load' if fi<30 else 'pop/instep' if fi<35 else 'tuck/level' if fi<39 else 'landing' if fi<44 else 'recovery'))
    metadata=dict(sourceUrl=cal['source_url'],sourceAuthor=cal['author'],license=cal['license'],sourceSha256=source_sha,
          rawPoseSha256=hashlib.sha256(args.pose_json.read_bytes()).hexdigest(),
          method='Actual MediaPipe heavy world33 directions, one [1,2,1]/4 temporal filter, fixed-length retarget with observed board projection and authored deck/instep constraints.',
          sourceFps=cal['source_fps'],firstSourceFrame=first,lastSourceFrame=last,metricScale=metric_scale,
          sourceCameraBasis=basis.tolist(),duration=(last-first)/cal['source_fps'],loop=False,
          limits=cal['annotations']+['No captured finger motion. Native game skin/playback and normal-pop phase integration remain unverified.'])
    result=dict(metadata=metadata,fps=cal['source_fps'],frames=frames)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(result,indent=2)+'\n')
    rows=[]
    for f in frames:
        row=[f['t']]+f['boardDeckTop']+[math.degrees(f['boardPitchRadians'])]
        row += [x for k in JOINTS for x in f['joints'][k]]
        row += [x for k in DIRS for x in f['dirs'][k]]
        row += f['soleUp']['Left']+f['soleUp']['Right']
        assert len(row)==80 and all(math.isfinite(x) for x in row)
        rows.append('            { '+', '.join(format(x,'.9g') for x in row)+' }')
    text='''// Generated by scripts/retarget_ollie_video.py. Motion/data: CC BY-SA 3.0.
// Derived from Medvedev, Ollie 1 (2012): https://commons.wikimedia.org/wiki/File:Ollie_1.ogv
// See assets/authored/animation/ollie-video/ for provenance and retarget limitations.
namespace Shortcut.RustMod
{
    internal static class SkateOllieCaptureData
    {
        public const double Duration = '''+format(metadata['duration'],'.9g')+''';
        public static readonly double[,] Samples = {
'''+',\n'.join(rows)+'''
        };
    }
}
'''
    args.csharp.parent.mkdir(parents=True,exist_ok=True);args.csharp.write_text(text)
    print(json.dumps(dict(frames=len(frames),duration=metadata['duration'],riseMax=max(f['deck']['pos'][1] for f in frames),dataSha256=hashlib.sha256(text.encode()).hexdigest())))
if __name__=='__main__':main()
