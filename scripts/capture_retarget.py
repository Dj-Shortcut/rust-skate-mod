"""Recipe-driven fixed-chain retarget of actual MediaPipe world/image33 samples."""
import math
import numpy as np
from capture_math import (UP, JOINTS, DIRS, Curve, RotationCurve, finite, unit,
                          qmatrix, matrix_quat, slerp, contact_events)

PAIRS = [(11,12),(13,14),(15,16),(17,18),(19,20),(21,22),(23,24),
         (25,26),(27,28),(29,30),(31,32),(7,8),(1,4),(2,5),(3,6),(9,10)]
REQUIRED = [0,7,8,11,12,13,14,15,16,23,24,25,26,27,28,29,30,31,32]

class CaptureError(ValueError):
    def __init__(self, message, details=None):
        super().__init__(message); self.details = details or {}

def camera_curve(spec, duration):
    if spec.get('mode') not in ('static', 'dynamic') or not spec.get('provenance'):
        raise ValueError('Camera mode and calibration provenance required')
    def check(m):
        m = finite(m, (3,3))
        if np.max(np.abs(m@m.T-np.eye(3))) > 1e-4 or abs(np.linalg.det(m)+1) > 1e-4:
            raise ValueError('Camera basis rows[right,up,forward] must be orthonormal with determinant-1')
        return m
    if spec['mode'] == 'static':
        m = check(spec['basis']); return lambda t:m
    keys = spec['basisKeys']; reflection = np.diag([-1.,1.,1.])
    rotations = [dict(t=k['t'], value=matrix_quat(check(k['value'])@reflection)) for k in keys]
    curve = RotationCurve(dict(kind='quaternion', keys=rotations), duration)
    return lambda t:qmatrix(curve(t))@reflection

def confidence_track(raw, first, last, options, actor_expected=False):
    indexed = {}
    for f in raw['frames']:
        i = f['frame_index']
        if i in indexed: raise ValueError('Duplicate source frame index')
        if first <= i <= last: indexed[i] = f
    if len(indexed) != last-first+1: raise ValueError('Source frame window is incomplete')
    n = last-first+1; world = np.full((n,33,3), np.nan); good = np.zeros((n,33), bool); retained = np.zeros((n,33), bool)
    reasons = {}; visibility = float(options.get('visibility', .5)); presence = float(options.get('presence', .5))
    if not 0 <= visibility <= 1 or not 0 <= presence <= 1: raise ValueError('Confidence thresholds outside0..1')
    occluded=options.get('occludedPolicy','interpolate');retain_presence=float(options.get('retainModelPresence',.8));margin=float(options.get('roiEdgeMargin',.01))
    if occluded not in ('interpolate','retain-model'):raise ValueError('Unknown occludedPolicy')
    if not .8<=retain_presence<=1 or not 0<=margin<=.2:raise ValueError('Invalid retain-model presence/ROI threshold')
    rejected=options.get('rejectedModelFrames',{})
    if not isinstance(rejected,dict):raise ValueError('rejectedModelFrames must map raw landmark indices to source frames')
    if rejected and (not isinstance(options.get('rejectedModelProvenance'),str) or not options['rejectedModelProvenance'].strip()):
        raise ValueError('Explicit model-point rejection requires rejectedModelProvenance')
    rejected_sets={}
    for name,frames in rejected.items():
        if name not in {str(j) for j in REQUIRED} or not isinstance(frames,list) or not frames or any(type(i) is not int or not first<=i<=last for i in frames):
            raise ValueError('Invalid rejected model landmark or source frames')
        if frames!=sorted(set(frames)):raise ValueError('Rejected model source frames must be unique and sorted')
        rejected_sets[int(name)]=set(frames)
    for row, i in enumerate(range(first,last+1)):
        f = indexed[i]
        if f['pose_count'] not in (0,1): raise CaptureError('Multiple people require explicit subject extraction', dict(frame=i))
        if f['pose_count'] == 0:
            actor=raw.get('source',{}).get('actorHint')
            if actor_expected or (isinstance(actor,dict) and actor.get('hipAndHeightSourceKeys')):
                raise CaptureError('Missing or ambiguous declared actor; identity gaps cannot be interpolated',dict(frame=i,targetSelection=f.get('target_selection')))
            for j in REQUIRED: reasons.setdefault(str(j), []).append(dict(frame=i, reason='absent_pose'))
            continue
        groups = [f.get('world_landmarks'), f.get('image_landmarks')]
        if any(not isinstance(group,list) or len(group)!=1 or not isinstance(group[0],list)
               or len(group[0])!=33 or any(not isinstance(point,dict) for point in group[0]) for group in groups):
            raise CaptureError('Expected one complete33-point world/image landmark array',dict(frame=i))
        w, im = groups[0][0], groups[1][0]
        roi=f.get('roi')
        if roi is not None and (not isinstance(roi,list) or len(roi)!=4 or any(type(v) is not int for v in roi)
                                or min(roi[:2])<0 or min(roi[2:])<1):
            raise CaptureError('Invalid retained ROI array',dict(frame=i))
        for j in range(33):
            try: xyz = finite([w[j][a] for a in ('x','y','z')], (3,))
            except (KeyError,ValueError): xyz = np.full(3,np.nan)
            why = ['review_rejected_model'] if i in rejected_sets.get(j,()) else []
            for label, item in [('image',im[j]),('world',w[j])]:
                p, v = item.get('presence'), item.get('visibility')
                if p is None or not math.isfinite(p) or not 0<=p<=1 or p < presence: why.append(label+'_presence')
                if v is None or not math.isfinite(v) or not 0<=v<=1 or v < visibility: why.append(label+'_visibility')
            try:
                image_xyz = finite([im[j][a] for a in ('x','y','z')], (3,));image_xy=image_xyz[:2]
                if not np.all((image_xy>=0)&(image_xy<=1)): why.append('cropped')
            except (KeyError,ValueError): why.append('invalid_image')
            if not np.isfinite(xyz).all(): why.append('invalid_world')
            if im[j].get('roi_cropped'): why.append('roi_edge_or_crop')
            if roi is not None:
                px, py = im[j].get('x', -1)*f['width'], im[j].get('y', -1)*f['height']
                if not (roi[0]+roi[2]*margin <= px <= roi[0]+roi[2]*(1-margin) and roi[1]+roi[3]*margin <= py <= roi[1]+roi[3]*(1-margin)):
                    why.append('roi_edge_or_crop')
            world[row,j] = xyz; good[row,j] = not why
            confidences=[item.get(key) for item in (im[j],w[j]) for key in ('presence','visibility')]
            finite_confidence=all(v is not None and math.isfinite(v) and 0<=v<=1 for v in confidences)
            high_presence=finite_confidence and min(im[j]['presence'],w[j]['presence'])>=retain_presence
            # This retains an ORIGINAL MODEL ESTIMATE, never a trusted observation
            # or an interpolation anchor. Crops/presence/missing identity are excluded.
            retained[row,j]=bool(occluded=='retain-model' and why and high_presence and set(why)<= {'image_visibility','world_visibility'})
            if why and j in REQUIRED: reasons.setdefault(str(j),[]).append(dict(frame=i, reason=','.join(why)))
    gap_limit = options.get('maxGapFrames', 12); edge = options.get('edgePolicy', 'fail'); corrections = {}
    if type(gap_limit) is not int or not 0 <= gap_limit <= 240: raise ValueError('maxGapFrames outside0..240')
    if edge not in ('fail','nearest-visible'): raise ValueError('Unknown confidence edgePolicy')
    for j in REQUIRED:
        valid = np.flatnonzero(good[:,j]); bad = np.flatnonzero(~good[:,j]&~retained[:,j])
        if not len(valid) and len(bad): raise CaptureError('No trusted observations for landmark'+str(j), dict(confidenceFlags=reasons))
        if len(bad):
            runs = np.split(bad, np.flatnonzero(np.diff(bad)>1)+1)
            for run in runs:
                left=valid[valid<run[0]];right=valid[valid>run[-1]];at_edge=not len(left) or not len(right)
                # Retained low-visibility estimates do not shorten a gap between
                # trusted anchors or authorize a distant future-air-pose fill.
                span=int(right[0]-left[-1]-1) if not at_edge else int(right[0]-run[0]) if len(right) else int(run[-1]-left[-1])
                if len(run)>gap_limit or span>gap_limit or (at_edge and edge=='fail'):
                    raise CaptureError('Unrecoverable confidence gap for landmark'+str(j),dict(landmark=j,sourceFrames=[int(first+x) for x in run],edge=at_edge,confidenceFlags=reasons))
            for k in range(3): world[bad,j,k] = np.interp(bad,valid,world[valid,j,k])
            corrections[str(j)] = [int(first+x) for x in bad]
    estimates={str(j):[int(first+x) for x in np.flatnonzero(retained[:,j])] for j in REQUIRED if retained[:,j].any()}
    return world, dict(requiredLandmarks=REQUIRED,thresholds=dict(visibility=visibility,presence=presence,confidencePolicy='Both image and world must satisfy both thresholds (equivalent to their minimum)',crop='normalized x/y inside0..1'),
                       flaggedObservations=reasons,interpolatedFrames=corrections,edgePolicy=edge,maxGapFrames=gap_limit,
                       occludedPolicy=occluded,retainModelPresenceMinimum=retain_presence,estimatedModelFrames=estimates,
                       estimatedModelFrameCounts={k:len(v) for k,v in estimates.items()},trustedFrameCounts={str(j):int(good[:,j].sum()) for j in REQUIRED},
                       estimatedModelStatus='UNREVIEWED original model estimates; visibility failed, trusted=FALSE, not used as interpolation anchors',
                       reviewRejectedModelFrames=rejected,reviewRejectionProvenance=options.get('rejectedModelProvenance'),
                       correctionMeaning='Interpolated/edge-held observations are reconstruction, not additional measured capture')

def ik(hip, ankle, a, b, pole, limit, corrections, t, side):
    delta = ankle-hip; d = float(np.linalg.norm(delta)); low, high = abs(a-b)+.001, a+b-.001
    distance = float(np.clip(d,low,high)); correction = abs(distance-d)
    if correction > 1e-8:
        if correction > limit+1e-8: raise CaptureError('Unreachable '+side+' leg',dict(t=t,reachM=d,allowedM=[low,high],requiredCorrectionM=correction,maxReachCorrectionM=limit))
        corrections.append(dict(t=t,side=side,metres=correction)); ankle = hip+unit(delta)*distance
    axis = unit(ankle-hip); d = distance
    bend = pole-axis*np.dot(pole,axis); bend = unit(bend, np.cross(axis,[0,0,1]) if abs(axis[2])<.9 else np.cross(axis,[1,0,0]))
    along = (a*a-b*b+d*d)/(2*d)
    knee = hip+axis*along+bend*math.sqrt(max(0.,a*a-along*along))
    return knee, ankle

def interpolate_direction(a, b, weight):
    """Interpolate fixed-length limb direction, not variable model bone length."""
    a,b=finite(a,(3,)),finite(b,(3,))
    if min(np.linalg.norm(a),np.linalg.norm(b))<1e-8:
        raise CaptureError('Degenerate source arm direction')
    a,b=unit(a),unit(b);dot=float(np.clip(np.dot(a,b),-1,1))
    if dot<-.9995:
        raise CaptureError('Ambiguous opposing source arm directions; review the source observations')
    if dot>.9995:return unit(a*(1-weight)+b*weight)
    angle=math.acos(dot)
    return (a*math.sin((1-weight)*angle)+b*math.sin(weight*angle))/math.sin(angle)

def retarget(raw, job, provenance):
    window=job['window']; first,last=window['firstFrame'],window['lastFrame']; fps=float(window['fps'])
    if type(first) is not int or type(last) is not int or first<0 or last<=first or not 1<=fps<=240: raise ValueError('Invalid source frame window/fps')
    duration=(last-first)/fps
    if not 0<duration<=10: raise ValueError('Clip duration outside(0,10]')
    source_fps=raw.get('source',{}).get('fps')
    if source_fps is not None and (not math.isfinite(source_fps) or abs(source_fps-fps)>max(.0001,fps*.000001)):
        raise ValueError('Recipe fps differs from actual source fps; do not round fractional frame rates')
    cuts=set(job['source'].get('cutFrames',[]))|set(raw.get('source',{}).get('cutFrames',[]))
    cuts.update(f['frame_index'] for f in raw['frames'] if f.get('hard_cut') or f.get('scene_cut'))
    if any(type(k) is not int for k in cuts):raise ValueError('cutFrames must contain original integer frame indices')
    internal_cuts=sorted(k for k in cuts if first<k<=last)
    if internal_cuts:raise CaptureError('Source window crosses a declared/detected cut',dict(sourceFrames=internal_cuts))
    opts=job.get('retarget',{});actor=job['source'].get('actorHint')
    arm_interpolation=opts.get('armInterpolation','linear')
    if arm_interpolation not in ('linear','spherical'):raise ValueError('Unknown armInterpolation')
    head_anchor=job.get('body',{}).get('headAnchor','nose')
    if head_anchor not in ('nose','ears'):raise ValueError('Unknown headAnchor')
    head_provenance=job.get('body',{}).get('headAnchorProvenance')
    if head_anchor=='ears' and (not isinstance(head_provenance,str) or not head_provenance.strip()):
        raise ValueError('Ear-mid head anchor requires headAnchorProvenance')
    world,confidence=confidence_track(raw,first,last,opts,isinstance(actor,dict) and bool(actor.get('hipAndHeightSourceKeys')))
    basis=camera_curve(job['camera'],duration)
    for i in range(len(world)): world[i]=world[i]@basis(i/fps).T
    stance=job['stance']; key_space=job.get('keySpace','canonical')
    if stance not in ('regular','goofy') or key_space not in ('canonical','captured'): raise ValueError('Explicit stance/keySpace required')
    goofy=stance=='goofy'; reflect_keys=goofy and key_space=='captured'
    if goofy:
        world[:,:,0]*=-1
        for l,r in PAIRS: world[:,[l,r]]=world[:,[r,l]]
    passes=opts.get('smoothingPasses',0)
    if type(passes) is not int or not 0<=passes<=4: raise ValueError('smoothingPasses outside0..4')
    for _ in range(passes):
        padded=np.pad(world,((1,1),(0,0),(0,0)),mode='edge');world=(padded[:-2]+2*padded[1:-1]+padded[2:])/4
    rig=dict(thigh=.44,shin=.43,upperArm=.29,forearm=.27,spine=.52,shoulderRise=.44,hipHalfWidth=.09,shoulderHalfWidth=.18,head=.12,foot=.14,toeDrop=.07)
    for k,v in job.get('rig',{}).items():
        if k not in rig or not math.isfinite(v) or abs(v-rig[k])>1e-10:
            raise ValueError('SKMCAP01 uses the canonical reference rig; custom dimension not supported: '+k)
    if rig['toeDrop']>=rig['foot']:raise ValueError('Toe drop exceeds foot length')
    observed=[np.linalg.norm(world[:,h]-world[:,k],axis=1)+np.linalg.norm(world[:,k]-world[:,a],axis=1) for h,k,a in ((23,25,27),(24,26,28))]
    scale=float(job.get('body',{}).get('metricScale',(rig['thigh']+rig['shin'])/np.median(observed)))
    if not math.isfinite(scale) or not .01<scale<100:raise ValueError('Invalid body scale')
    world*=scale
    pelvis_curve=Curve(job['body']['pelvisKeys'],duration);board_spec=job['board']
    if board_spec.get('provenance') not in ('observed','estimated','authored'):raise ValueError('Board provenance must be observed/estimated/authored')
    position=Curve(board_spec['positionKeys'],duration);rotation=RotationCurve(board_spec['orientation'],duration)
    hz=float(job.get('sampleHz',120)); intervals=int(math.ceil(duration*hz-1e-8))
    if not 1<=hz<=240 or not 1<=intervals<=2400:raise ValueError('sampleHz/sample count outside contract')
    times=[i/hz for i in range(intervals)]+[duration];rot_quality=rotation.check_density(times)
    events=contact_events(job['events'],duration,reflect_keys)
    if not events or events[0]['t']!=0:raise ValueError('Explicit initial contact event at t=0 required')
    feet=job.get('feet',{});foot_curves={}
    for side in ('Left','Right'):
        source_side=('Right' if side=='Left' else 'Left') if reflect_keys else side
        config=feet.get(source_side,{})
        foot_curves[side]={key:Curve(config[key],duration) for key in ('positionKeys','soleUpKeys','airSoleUpKeys','airPositionKeys') if key in config}
        if 'airPositionKeys' in config and (not isinstance(config.get('airPositionProvenance'),str) or not config['airPositionProvenance'].strip()):
            raise ValueError('Explicit free-foot paths require airPositionProvenance: '+source_side)
        for key in ('soleUpKeys','airSoleUpKeys'):
            if key not in foot_curves[side]:continue
            values=foot_curves[side][key].values
            if np.any(np.linalg.norm(values,axis=1)<1e-8):raise ValueError('Explicit sole orientation keys must be nonzero')
            if any(np.dot(unit(a),unit(b))<math.cos(math.radians(150)) for a,b in zip(values,values[1:])):
                raise ValueError('Sole orientation keys need intermediate directions for rotations above150 degrees')
    floor_policy=board_spec.get('floorPolicy','fail');floor=float(board_spec.get('floorY',0))
    dimensions=finite(board_spec.get('dimensions',[.21,.025,.8]),(3,))
    if np.any(dimensions<=0) or floor_policy not in ('fail','raise') or not math.isfinite(floor):raise ValueError('Invalid board floor policy/dimensions')
    corners=np.array([[x,y,z] for x in (-dimensions[0]/2,dimensions[0]/2) for y in (-dimensions[1],0) for z in (-dimensions[2]/2,dimensions[2]/2)])
    allow_inverted=board_spec.get('allowInvertedFootContact',False)
    if type(allow_inverted) is not bool:raise ValueError('allowInvertedFootContact must be boolean')
    if allow_inverted and any(not all(k in foot_curves[s] for k in ('positionKeys','soleUpKeys')) for s in ('Left','Right')):
        raise ValueError('Inverted contact exception requires explicit positionKeys and soleUpKeys for BOTH feet')
    inverted_contacts=[]
    frames=[];reaches=[];floor_corrections=[];event_index=0;last_q=None;maximum_chain_error=0.;minimum_floor=float('inf')
    limit=float(opts.get('maxReachCorrectionM',0))
    if not 0<=limit<=.5:raise ValueError('maxReachCorrectionM outside0..0.5')
    for t in times:
        source_pos=min(last-first,t*fps);lo=int(source_pos);source_hi=min(len(world)-1,lo+1);w=world[lo]*(1-(source_pos-lo))+world[source_hi]*(source_pos-lo)
        for a,b in ((23,24),(11,12),(11,13),(13,15),(12,14),(14,16)):
            if np.linalg.norm(w[a]-w[b])<1e-8:raise CaptureError('Degenerate captured body direction',dict(t=t,landmarks=[a,b]))
        if np.linalg.norm((w[11]+w[12]-w[23]-w[24])*.5)<1e-8:
            raise CaptureError('Degenerate captured torso',dict(t=t))
        while event_index+1<len(events) and events[event_index+1]['t']<=t+1e-9:event_index+=1
        mask=events[event_index]['contacts'];origin=position(t);q=rotation(t);pelvis=pelvis_curve(t)
        if reflect_keys:origin[0]*=-1;pelvis[0]*=-1;q=q*np.array([1,-1,-1,1])
        if last_q is not None and np.dot(last_q,q)<0:q=-q
        last_q=q;R=qmatrix(q);up=R@UP
        floor_min=float(np.min((corners@R.T+origin)[:,1]));minimum_floor=min(minimum_floor,floor_min)
        if floor_min<floor-1e-7:
            if floor_policy=='fail':raise CaptureError('Full board eight-corner floor envelope penetrates ground',dict(t=t,minCornerY=floor_min,floorY=floor))
            raise_y=floor-floor_min;origin[1]+=raise_y;floor_corrections.append(dict(t=t,raiseM=raise_y))
        if mask&3==3 and np.dot(up,UP)<-.1:
            if not allow_inverted:raise CaptureError('Both feet attached to an inverted board; declare detach/catch events or explicit dark-catch exception',dict(t=t))
            inverted_contacts.append(t)
        hip_axis=unit(w[24]-w[23],[0,0,-1]);torso=unit((w[11]+w[12]-w[23]-w[24])*.5)
        shoulder_axis=unit(w[12]-w[11]);shoulder_axis=unit(shoulder_axis-torso*np.dot(shoulder_axis,torso),hip_axis)
        hips=[pelvis-hip_axis*rig['hipHalfWidth'],pelvis+hip_axis*rig['hipHalfWidth']]
        anatomy=unit(np.cross(hip_axis,UP),[1,0,0]);hip_mid=(w[23]+w[24])*.5
        j=dict(Pelvis=pelvis,Chest=pelvis+torso*(rig['spine']/1.12),Neck=pelvis+torso*rig['spine'])
        head_point=(w[7]+w[8])*.5 if head_anchor=='ears' else w[0]
        head_offset=head_point-(w[11]+w[12])*.5
        if head_anchor=='ears' and np.linalg.norm(head_offset)<1e-9:
            raise CaptureError('Degenerate ear-mid head anchor')
        j['Head']=j['Neck']+unit(head_offset)*rig['head'];soles={}
        for index,side in enumerate(('Left','Right')):
            ai,ki,hi,heel,toe=(27,25,23,29,31) if index==0 else (28,26,24,30,32);curves=foot_curves[side];contact=bool(mask&(1<<index))
            if contact:
                local=curves['positionKeys'](t) if 'positionKeys' in curves else np.array([.035,.095,.22 if index==0 else -.22])
                if reflect_keys:local[0]*=-1
                local_sole=curves['soleUpKeys'](t) if 'soleUpKeys' in curves else UP.copy()
                if reflect_keys:local_sole[0]*=-1
                ankle=origin+R@local;sole=R@unit(local_sole)
            else:
                # Optional reconstructed free-foot paths use the same absolute
                # authoring frame as pelvisKeys. They can meet a source-observed
                # catch without changing raw model points or faking contact.
                if 'airPositionKeys' in curves:
                    ankle=curves['airPositionKeys'](t)
                    if reflect_keys:ankle[0]*=-1
                else:ankle=pelvis+w[ai]-hip_mid
                sole=unit(curves['airSoleUpKeys'](t) if 'airSoleUpKeys' in curves else UP)
            if reflect_keys and not contact:sole[0]*=-1
            heading=unit(w[toe]-w[heel],anatomy)
            fallback=np.cross(sole,[0,0,1]) if abs(sole[2])<.9 else np.cross(sole,[1,0,0])
            heading=unit(heading-sole*np.dot(heading,sole),fallback)
            knee,ankle=ik(hips[index],ankle,rig['thigh'],rig['shin'],w[ki]-w[hi],limit,reaches,t,side)
            j[side+'Hip']=hips[index];j[side+'Knee']=knee;j[side+'Ankle']=ankle
            j[side+'Toe']=ankle+heading*math.sqrt(rig['foot']**2-rig['toeDrop']**2)-sole*rig['toeDrop'];soles[side]=unit(sole)
            si,ei,wi=(11,13,15) if index==0 else (12,14,16)
            shoulder=pelvis+torso*rig['shoulderRise']+shoulder_axis*(rig['shoulderHalfWidth'] if index else -rig['shoulderHalfWidth'])
            if arm_interpolation=='spherical':
                weight=source_pos-lo
                upper=interpolate_direction(world[lo,ei]-world[lo,si],world[source_hi,ei]-world[source_hi,si],weight)
                fore=interpolate_direction(world[lo,wi]-world[lo,ei],world[source_hi,wi]-world[source_hi,ei],weight)
            else:upper,fore=unit(w[ei]-w[si]),unit(w[wi]-w[ei])
            j[side+'Shoulder']=shoulder;j[side+'Elbow']=shoulder+upper*rig['upperArm'];j[side+'Hand']=j[side+'Elbow']+fore*rig['forearm']
            for a,b,length in [('Hip','Knee',rig['thigh']),('Knee','Ankle',rig['shin']),('Ankle','Toe',rig['foot']),('Shoulder','Elbow',rig['upperArm']),('Elbow','Hand',rig['forearm'])]:
                maximum_chain_error=max(maximum_chain_error,abs(float(np.linalg.norm(j[side+a]-j[side+b]))-length))
        pelvis_up=unit(torso*.45+UP*.55);pelvis_up=unit(pelvis_up-hip_axis*np.dot(pelvis_up,hip_axis),UP)
        directions=dict(zip(DIRS,(unit(np.cross(hip_axis,pelvis_up)),pelvis_up,unit(np.cross(shoulder_axis,torso)),torso,unit(w[0]-(w[7]+w[8])*.5,anatomy))))
        frames.append(dict(t=t,sourceFrame=first+t*fps,board=dict(origin=origin.tolist(),quaternion=q.tolist()),joints={k:finite(j[k],(3,)).tolist() for k in JOINTS},dirs={k:unit(directions[k]).tolist() for k in DIRS},soleUp={k:v.tolist() for k,v in soles.items()},phase=events[event_index]['phase'],contacts=mask))
    if maximum_chain_error>1e-6:raise CaptureError('Retarget chain length error',dict(maxChainErrorM=maximum_chain_error))
    metadata=dict(id=job['id'],label=job['label'],sourceUrl=job['source']['url'],sourceAuthor=job['source']['author'],movementKind=job['movementKind'],reviewed=job['reviewed'],rights=job['source'].get('rights','No publication/relicensing permission asserted'),canonicalStance='regular',capturedStance=stance,keySpace=key_space,coordinateFrame='Y up, +Z nose, +X right; right-handed quaternion XYZW; origin=deck top',toeContract='Physical toe endpoints include soleUp*toeDrop once; native sampler reconstructs flat toe targets before writer drop',sourceFps=fps,firstSourceFrame=first,lastSourceFrame=last,provenance=provenance,boardProvenance=board_spec['provenance'],boardLimit='Observed keys mean recipe observations/estimates, not calibrated measured3D board mocap',footOrientationLimit='Soles default to board-up in contact and world-up in air unless per-foot keys supplied. Captured heel/toe heading is projected onto that sole; degenerate projections use a perpendicular basis. Not measured shoe orientation.',rootLimit='Pelvis/board trajectory supplied explicitly by recipe; MediaPipe world33 is hip-relative; no player-root/physics authority',sampleLimit='Dense retarget interpolation, not additional measured source frames',camera=job['camera'],rig=rig,metricScale=scale)
    metadata['armInterpolation']=arm_interpolation
    if head_anchor=='ears':
        metadata['headAnchor']=head_anchor
        metadata['headAnchorProvenance']=head_provenance
    metadata['freeFootPaths']={side:dict(coordinates='Absolute authoring frame, same as pelvisKeys; reflected with captured keySpace',provenance=feet[('Right' if side=='Left' else 'Left') if reflect_keys else side]['airPositionProvenance']) for side in ('Left','Right') if 'airPositionKeys' in foot_curves[side]}
    selection_frames=[dict(frame=f['frame_index'],selection=f['target_selection']) for f in raw['frames'] if first<=f['frame_index']<=last and f.get('target_selection') is not None]
    quality=dict(status='validated transport/kinematics; visual/native acceptance remains open',reviewed=job['reviewed'],confidence=confidence,actorSelection=selection_frames,cutAudit=dict(declaredOrDetectedFrames=sorted(cuts),startsAtDeclaredCut=first in cuts,limit='No automatic visual cut detector; absent markers are not proof of a continuous shot'),boardRotation=rot_quality,boardFloor=dict(policy=floor_policy,minUncorrectedCornerY=minimum_floor,corrections=floor_corrections),invertedFootContactException=dict(explicitlyAllowed=allow_inverted,sampleTimes=inverted_contacts),reachCorrections=reaches,maxChainErrorM=maximum_chain_error,feetDefaultOffsetsEstimated=[s for s in ('Left','Right') if not foot_curves[s].get('positionKeys')],bodyYaw='Preserved in declared camera basis; no per-frame inferred hip-facing normalization',sourceFrames=last-first+1,outputSamples=len(frames))
    return dict(format='skate-capture-clip/1',fps=hz,duration=duration,stride=83,jointOrder=JOINTS,directionOrder=DIRS,metadata=metadata,events=events,frames=frames),quality
