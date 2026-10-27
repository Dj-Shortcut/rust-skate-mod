#!/usr/bin/env python3
"""Batch local MediaPipe captures into private JSON/reports and SKMCAP01 packs.

No source footage/model is bundled. No URL or command from a manifest is run.
Raw-JSON retarget needs NumPy; local inference additionally needs MediaPipe and
OpenCV plus an explicitly supplied local PoseLandmarker .task model.
"""
import argparse
import hashlib
import gzip
import importlib.metadata
import inspect
import json
import math
import os
from pathlib import Path
import re
import sys
import tempfile

import numpy as np
from capture_math import encode_pack
from capture_retarget import retarget, window_timing

MAX_JSON = 512*1024*1024
ROOT = Path(__file__).resolve().parent
ID = re.compile(r'[a-z0-9][a-z0-9_-]{0,63}\Z')
RESERVED = {'con','prn','aux','nul'} | {p+str(i) for p in ('com','lpt') for i in range(1,10)}
ACTOR_BOUNDS=dict(minimumBodyHeightPx=140.,maximumHintDistancePx=220.,maximumPreviousHipDistancePx=110.,minimumBestVsSecondCostMarginPx=25.,minimumMeanHipPresence=.7,maximumFeetHintErrorPx=70.)

def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda:f.read(4*1024*1024),b''):h.update(chunk)
    return h.hexdigest()

def read_json(path):
    if path.stat().st_size>MAX_JSON:raise ValueError('JSON input exceeds512MiB')
    def invalid(value):raise ValueError('Nonfinite JSON number: '+value)
    if path.suffix=='.gz':
        with gzip.open(path,'rb') as f:data=f.read(MAX_JSON+1)
        if len(data)>MAX_JSON:raise ValueError('Decompressed JSON exceeds512MiB')
        return json.loads(data.decode('utf-8'),parse_constant=invalid)
    return json.loads(path.read_text(encoding='utf-8'),parse_constant=invalid)

def json_bytes(value):
    return (json.dumps(value,indent=2,ensure_ascii=False,allow_nan=False)+'\n').encode('utf-8')

def failure_details(value):
    """Keep failed numeric diagnostics valid JSON; never repair capture values."""
    if isinstance(value,dict):return {str(k):failure_details(v) for k,v in value.items()}
    if isinstance(value,(list,tuple)):return [failure_details(v) for v in value]
    if isinstance(value,np.ndarray):return failure_details(value.tolist())
    if isinstance(value,np.generic):return failure_details(value.item())
    if isinstance(value,float) and not math.isfinite(value):return str(value)
    return value

def atomic(path,data):
    path.parent.mkdir(parents=True,exist_ok=True)
    fd,name=tempfile.mkstemp(prefix='.'+path.name+'-',dir=path.parent)
    try:
        with os.fdopen(fd,'wb') as f:f.write(data);f.flush();os.fsync(f.fileno())
        os.replace(name,path)
    finally:
        if os.path.exists(name):os.unlink(name)

def local_path(value,base):
    if not isinstance(value,str) or not value or '://' in value or '\x00' in value:
        raise ValueError('Source/model must be a local file path')
    p=Path(value);p=p if p.is_absolute() else base/p
    p=p.resolve(strict=True)
    if not p.is_file():raise ValueError('Input is not a file')
    return p

def code_key():
    return hashlib.sha256(b''.join((ROOT/name).read_bytes() for name in
        ('capture_pipeline.py','capture_retarget.py','capture_math.py'))).hexdigest()

def validate_actor_hint(hint,first,last):
    if hint is None:return None
    if not isinstance(hint,dict) or 'hipAndHeightSourceKeys' not in hint:
        raise ValueError('actorHint requires explicit hipAndHeightSourceKeys; descriptive text cannot select a person')
    for name,width in [('hipAndHeightSourceKeys',4),('footSourceYKeys',2)]:
        if name not in hint:continue
        keys=np.asarray(hint[name],dtype=float)
        if keys.ndim!=2 or keys.shape[1]!=width or len(keys)<2 or not np.isfinite(keys).all() or np.any(np.diff(keys[:,0])<=0):
            raise ValueError('Invalid actorHint '+name)
        if np.any(keys[:,0]!=np.floor(keys[:,0])) or keys[0,0]>first or keys[-1,0]<last:
            raise ValueError('Actor hint keys must cover original source frame window')
        if name=='hipAndHeightSourceKeys' and np.any(keys[:,3]<=0):raise ValueError('Actor hint body height must be positive')
    return hint

def actor_hint_at(keys,frame):
    values=np.asarray(keys,dtype=float)
    return np.array([np.interp(frame,values[:,0],values[:,i]) for i in range(1,values.shape[1])])

def select_actor(images,width,height,frame,hint,previous):
    """Source-pixel hint + bounded previous accepted track; never index0 fallback."""
    manual=actor_hint_at(hint['hipAndHeightSourceKeys'],frame)
    foot=float(actor_hint_at(hint['footSourceYKeys'],frame)[0]) if 'footSourceYKeys' in hint else None
    candidates=[]
    for index,points in enumerate(images):
        try:
            xy=np.array([[p['x']*width,p['y']*height] for p in points],dtype=float)
            if xy.shape!=(33,2) or not np.isfinite(xy).all():raise ValueError('Invalid image landmarks')
            hip=xy[[23,24]].mean(axis=0);body_height=float(np.ptp(xy[:,1]));distance=float(np.linalg.norm(hip-manual[:2]))
            previous_distance=distance if previous is None else float(np.linalg.norm(hip-previous))
            presence_values=[points[k].get('presence') for k in (23,24)]
            presence=float(np.mean(presence_values)) if all(v is not None and math.isfinite(v) for v in presence_values) else 0.
            cost=(distance if previous is None else .75*previous_distance+.25*distance)+.15*abs(body_height-manual[2])
            feet_y=float(xy[[27,28],1].mean());foot_error=abs(feet_y-foot) if foot is not None else None
            failures=[]
            if body_height<ACTOR_BOUNDS['minimumBodyHeightPx']:failures.append('bodyHeight')
            if distance>ACTOR_BOUNDS['maximumHintDistancePx']:failures.append('hintDistance')
            if previous is not None and previous_distance>ACTOR_BOUNDS['maximumPreviousHipDistancePx']:failures.append('previousHipDistance')
            if presence<ACTOR_BOUNDS['minimumMeanHipPresence']:failures.append('hipPresence')
            if foot_error is not None and foot_error>ACTOR_BOUNDS['maximumFeetHintErrorPx']:failures.append('feetHintDistance')
            candidates.append(dict(index=index,hipSourcePx=hip.tolist(),heightPx=body_height,hintDistancePx=distance,previousHipDistancePx=previous_distance,meanHipPresence=presence,cost=cost,feetSourceY=feet_y,manualFeetYHint=foot,feetHintErrorPx=foot_error,eligible=not failures,rejectedBy=failures))
        except (ValueError,TypeError,KeyError,IndexError):
            candidates.append(dict(index=index,eligible=False,cost=None,rejectedBy=['invalidCandidateLandmarks']))
    eligible=sorted((q for q in candidates if q['eligible']),key=lambda q:q['cost']);best=eligible[0] if eligible else None
    margin=None if len(eligible)<2 else eligible[1]['cost']-eligible[0]['cost']
    selected=None if best is None or (margin is not None and margin<ACTOR_BOUNDS['minimumBestVsSecondCostMarginPx']) else best['index']
    reason='acceptedTargetTrack' if selected is not None else ('ambiguousCandidates' if eligible else 'noEligibleTarget')
    summary=dict(selectedCandidateIndex=selected,candidateCount=len(candidates),manualHintHipAndHeightSourcePx=manual.tolist(),bestVsSecondCostMarginPx=margin,selectedCost=best['cost'] if selected is not None else None,reason=reason,candidates=candidates)
    return selected,summary,np.array(best['hipSourcePx']) if selected is not None else previous

def extract(video,model,job):
    # Imports are deliberately lazy: using retained raw JSON never loads a model.
    import cv2
    import mediapipe as mp
    from mediapipe.tasks import python
    from mediapipe.tasks.python import vision
    first,last,_,_,frame_times=window_timing(job['window']);roi=job['source'].get('roi')
    actor=validate_actor_hint(job['source'].get('actorHint'),first,last);num_poses=6 if actor else 2
    if roi is not None and (not isinstance(roi,list) or len(roi)!=4 or any(type(x) is not int for x in roi) or min(roi[:2])<0 or min(roi[2:])<1):
        raise ValueError('source.roi must be [x,y,width,height] original pixel integers')
    margin=float(job.get('retarget',{}).get('roiEdgeMargin',.01))
    if not 0<=margin<=.2:raise ValueError('roiEdgeMargin outside0..0.2')
    cap=cv2.VideoCapture(str(video));fps=cap.get(cv2.CAP_PROP_FPS)
    if not math.isfinite(fps) or abs(fps-job['window']['fps'])>max(.0001,fps*.000001):raise ValueError('Actual decoded video fps differs from recipe; retain fractional fps')
    options=vision.PoseLandmarkerOptions(base_options=python.BaseOptions(model_asset_path=str(model),delegate=python.BaseOptions.Delegate.CPU),
        running_mode=vision.RunningMode.VIDEO,num_poses=num_poses,min_pose_detection_confidence=.5,min_pose_presence_confidence=.5,min_tracking_confidence=.5,output_segmentation_masks=False)
    rows=[];previous=-1;previous_hip=None
    def point(p):
        return dict(x=float(p.x),y=float(p.y),z=float(p.z),presence=None if p.presence is None else float(p.presence),visibility=None if p.visibility is None else float(p.visibility))
    try:
        with vision.PoseLandmarker.create_from_options(options) as task:
            for i in range(last+1):
                if not cap.grab():raise ValueError('Video ends before requested source window')
                if i<first:continue
                ok,bgr=cap.retrieve()
                if not ok:raise ValueError('Source decode failed')
                height,width=bgr.shape[:2];x,y,w,h=roi if roi is not None else [0,0,width,height]
                if x+w>width or y+h>height:raise ValueError('ROI extends outside original frame')
                source_time=i/fps if frame_times is None else float(frame_times[i-first])
                crop=bgr[y:y+h,x:x+w];timestamp=max(previous+1,round(i*1000/fps if frame_times is None else source_time*1000));previous=timestamp
                image=mp.Image(image_format=mp.ImageFormat.SRGB,data=np.ascontiguousarray(cv2.cvtColor(crop,cv2.COLOR_BGR2RGB)))
                result=task.detect_for_video(image,timestamp);images=[]
                for pose in result.pose_landmarks:
                    points=[]
                    for landmark in pose:
                        q=point(landmark);q['roi_x']=q['x'];q['roi_y']=q['y'];q['roi_z']=q['z'];q['roi_cropped']=not(margin<=q['x']<=1-margin and margin<=q['y']<=1-margin)
                        q['x']=(x+q['x']*w)/width;q['y']=(y+q['y']*h)/height;q['z']*=w/width;points.append(q)
                    images.append(points)
                worlds=[[point(q) for q in pose] for pose in result.pose_world_landmarks]
                selection=None
                if actor:
                    selected,selection,previous_hip=select_actor(images,width,height,i,actor,previous_hip)
                    images=[images[selected]] if selected is not None else [];worlds=[worlds[selected]] if selected is not None else []
                rows.append(dict(frame_index=i,source_time_seconds=source_time,inference_timestamp_ms=timestamp,width=width,height=height,roi=roi,
                    pose_count=len(images),image_landmarks=images,world_landmarks=worlds,target_selection=selection))
    finally:cap.release()
    raw=dict(scope='Actual MediaPipe VIDEO outputs. No body retarget/root trajectory. Multiple detections are retained and later rejected, never silently subject-selected.',
        source=dict(sha256=digest(video),fps=fps,roi=roi,actorHint=actor,selection=dict(method='Explicit source hip/height hint + bounded accepted track; missing/ambiguous=0, no actor fallback',bounds=ACTOR_BOUNDS,cost='First:hintDistance+.15*heightError; thereafter:.75*previousDistance+.25*hintDistance+.15*heightError') if actor else 'Exactly one detection required; no subject selection without numeric hint'),
        model=dict(sha256=digest(model),origin='Caller-supplied local PoseLandmarker task; no download performed'),
        environment=dict(mediapipe=importlib.metadata.version('mediapipe'),numpy=np.__version__,numPoses=num_poses,delegate='CPU'),frames=rows)
    if frame_times is not None:
        raw['source']['sourceTiming']=dict(mode='variable-frame-times',origin='Window first source frame is t=0',
            frameTimes=frame_times.tolist(),frameTimeProvenance=job['window']['frameTimeProvenance'])
    return raw

def job_inputs(job,base,model,expected_model_sha=None):
    source=job['source'];video=local_path(source['video'],base) if source.get('video') else None
    pose=local_path(source['poseJson'],base) if source.get('poseJson') else None
    if pose is None and (video is None or model is None):raise ValueError('Provide source.poseJson or local source.video plus CLI --model')
    hashes=dict(codeSha256=code_key(),effectiveJobSha256=hashlib.sha256(json_bytes(job)).hexdigest(),numpy=np.__version__,python=sys.version.split()[0])
    if video:hashes['sourceSha256']=digest(video)
    if pose:hashes['rawPoseSha256']=digest(pose)
    if not pose:
        import cv2
        model=local_path(str(model),Path.cwd())
        hashes['modelSha256']=digest(model)
        if expected_model_sha and hashes['modelSha256']!=expected_model_sha:raise ValueError('Model checksum mismatch')
        hashes['mediapipeVersion']=importlib.metadata.version('mediapipe')
        hashes['opencvVersion']=cv2.__version__
    if source.get('sha256') and video and source['sha256']!=hashes['sourceSha256']:raise ValueError('Source content SHA differs from declared SHA')
    key=hashlib.sha256(json_bytes(hashes)).hexdigest()
    return video,pose,hashes,key

def retained_inference(video,model,job,out,resume,expected_model_sha):
    import cv2
    model=local_path(str(model),Path.cwd())
    model_sha=digest(model)
    if expected_model_sha and model_sha!=expected_model_sha:raise ValueError('Model checksum mismatch')
    recipe=dict(window=job['window'],roi=job['source'].get('roi'),actorHint=job['source'].get('actorHint'),roiEdgeMargin=job.get('retarget',{}).get('roiEdgeMargin',.01))
    ingredients=dict(sourceSha256=digest(video),modelSha256=model_sha,recipe=recipe,
        extractorCodeSha256=hashlib.sha256((''.join(inspect.getsource(f) for f in [extract,window_timing,validate_actor_hint,actor_hint_at,select_actor])+json.dumps(ACTOR_BOUNDS,sort_keys=True)).encode()).hexdigest(),
        mediapipe=importlib.metadata.version('mediapipe'),opencv=cv2.__version__,numpy=np.__version__)
    key=hashlib.sha256(json_bytes(ingredients)).hexdigest();path=out/'raw-cache'/(key+'.json.gz');receipt=path.with_suffix('.receipt.json')
    if resume and path.is_file() and receipt.is_file() and read_json(receipt).get('sha256')==digest(path):
        return read_json(path),dict(status='retained-raw-cache',key=key,rawSha256=digest(path))
    raw=extract(video,model,job);data=gzip.compress(json_bytes(raw),mtime=0)
    atomic(path,data);atomic(receipt,json_bytes(dict(ingredients=ingredients,sha256=hashlib.sha256(data).hexdigest())))
    return raw,dict(status='actual-model-extraction',key=key,rawSha256=digest(path))

def process(job,base,out,model,resume,expected_model_sha=None):
    stem=job['id'];video,pose,hashes,key=job_inputs(job,base,model,expected_model_sha);cache=out/'cache'/key
    destination=out/'jobs';clip_path=destination/(stem+'.json');quality_path=destination/(stem+'.quality.json')
    if resume and (cache/'receipt.json').is_file():
        receipt=read_json(cache/'receipt.json')
        if receipt['cacheKey']==key and all((cache/name).is_file() and digest(cache/name)==receipt['files'][name] for name in ('clip.json','quality.json')):
            clip=read_json(cache/'clip.json');encode_pack([clip])
            atomic(clip_path,(cache/'clip.json').read_bytes());atomic(quality_path,(cache/'quality.json').read_bytes())
            return clip,dict(id=stem,status='cached',cacheKey=key,clip=str(clip_path.relative_to(out)),quality=str(quality_path.relative_to(out)))
    inference=dict(status='retained-pose-json')
    if pose:raw=read_json(pose)
    else:raw,inference=retained_inference(video,model,job,out,resume,expected_model_sha)
    declared=raw.get('source',{}).get('sha256')
    if not isinstance(declared,str) or not re.fullmatch('[0-9a-f]{64}',declared):raise ValueError('Actual raw provenance source SHA256 required')
    if video and declared!=hashes['sourceSha256']:raise ValueError('Raw pose source SHA differs from actual video')
    if job['source'].get('sha256') and declared!=job['source']['sha256']:raise ValueError('Raw source SHA differs from recipe')
    hashes['sourceSha256']=declared
    provenance=dict(**hashes,sourceContentVerified=video is not None,rawModel=raw.get('model',raw.get('environment',{}).get('model')),
                    actorSelection=raw.get('source',{}).get('selection'),roi=job['source'].get('roi',raw.get('source',{}).get('roi')),actorHint=job['source'].get('actorHint',raw.get('source',{}).get('actorHint')),inference=inference)
    clip,quality=retarget(raw,job,provenance);encode_pack([clip])
    quality.update(cacheKey=key,provenance=provenance,rig=clip['metadata']['rig'])
    clip_bytes,quality_bytes=json_bytes(clip),json_bytes(quality)
    atomic(cache/'clip.json',clip_bytes);atomic(cache/'quality.json',quality_bytes)
    atomic(cache/'receipt.json',json_bytes(dict(cacheKey=key,files={n:hashlib.sha256(b).hexdigest() for n,b in [('clip.json',clip_bytes),('quality.json',quality_bytes)]})))
    atomic(clip_path,clip_bytes);atomic(quality_path,quality_bytes)
    return clip,dict(id=stem,status='completed',cacheKey=key,clip=str(clip_path.relative_to(out)),quality=str(quality_path.relative_to(out)))

def validate_jobs(manifest):
    if manifest.get('version')!=1 or not isinstance(manifest.get('jobs'),list) or not 1<=len(manifest['jobs'])<=256:raise ValueError('Expected version1 manifest with1..256 jobs')
    ids=[]
    for job in manifest['jobs']:
        stem=job.get('id')
        if not isinstance(stem,str) or not ID.fullmatch(stem) or stem in RESERVED:raise ValueError('Job id must be a safe lowercase filename stem')
        if stem in ids:raise ValueError('Duplicate job id: '+stem)
        ids.append(stem)
    return manifest['jobs']

def batch(args):
    manifest=read_json(args.manifest);jobs=validate_jobs(manifest);out=args.output.resolve();base=args.manifest.resolve().parent
    model=args.model.resolve() if args.model else None
    records=[];clips=[];failed=0
    for job in jobs:
        try:
            if type(job['reviewed']) is not bool:raise ValueError('reviewed must be an explicit boolean')
            clip,record=process(job,base,out,model,args.resume,args.model_sha256);clips.append(clip);records.append(record)
            print(job['id']+': '+record['status'],flush=True)
        except (ValueError,KeyError,TypeError,IndexError,OSError,ImportError,OverflowError,RuntimeError,np.linalg.LinAlgError,MemoryError) as error:
            failed+=1;failure=dict(id=job['id'],status='failed',errorType=type(error).__name__,error=str(error),details=failure_details(getattr(error,'details',{})),previousSuccessfulFilesRetained=True)
            atomic(out/'jobs'/(job['id']+'.failure.json'),json_bytes(failure));records.append(failure);print(job['id']+': failed: '+str(error),file=sys.stderr,flush=True)
    pack=out/'skate_captures.skmc';updated=False
    if clips and (not failed or args.partial_pack):atomic(pack,encode_pack(clips));updated=True
    report=dict(format='skate-capture-batch/1',manifestSha256=digest(args.manifest),codeSha256=code_key(),jobs=records,succeeded=len(clips),failed=failed,
        packUpdated=updated,partialPack=bool(failed and updated),packSha256=digest(pack) if pack.is_file() else None,
        limits='Transport/kinematic checks do not grant visual acceptance, publication rights or normal gameplay integration. No player-root authority.')
    atomic(out/'batch-report.json',json_bytes(report));print(json.dumps(dict(succeeded=len(clips),failed=failed,packUpdated=updated)),flush=True)
    return 1 if failed else 0

def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__);sub=p.add_subparsers(dest='command',required=True)
    b=sub.add_parser('batch',help='Process independent declarative local capture jobs')
    b.add_argument('manifest',type=Path);b.add_argument('--output',type=Path,required=True);b.add_argument('--model',type=Path)
    b.add_argument('--model-sha256');b.add_argument('--resume',action='store_true');b.add_argument('--partial-pack',action='store_true',help='Explicitly write successful clips even when another job fails (exit remains1)')
    a=p.parse_args(argv)
    try:return batch(a)
    except (ValueError,OSError,KeyError,TypeError,RuntimeError,np.linalg.LinAlgError,MemoryError) as error:
        atomic(a.output/'batch-failure.json',json_bytes(dict(status='failed',errorType=type(error).__name__,error=str(error))))
        print('capture pipeline failed: '+str(error),file=sys.stderr);return 2

if __name__=='__main__':raise SystemExit(main())
