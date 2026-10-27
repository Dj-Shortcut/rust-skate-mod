"""Capture math and SKMCAP01 transport. No Unity, media, or network dependency."""
import math
import struct
import unicodedata
import numpy as np

JOINTS = ('Pelvis Chest Neck Head LeftHip RightHip LeftKnee RightKnee '
          'LeftAnkle RightAnkle LeftToe RightToe LeftShoulder RightShoulder '
          'LeftElbow RightElbow LeftHand RightHand').split()
DIRS = 'PelvisForward PelvisUp ChestForward ChestUp HeadForward'.split()
UP = np.array([0., 1., 0.])
PHASES = dict(unknown=0, load=1, pop=2, air=3, catch=4, land=5, recovery=6)
CONTACTS = dict(LeftFoot=1, RightFoot=2, LeftHand=4, RightHand=8)
STRIDE = 83

def finite(value, shape=None):
    a = np.asarray(value, dtype=float)
    if (shape is not None and a.shape != shape) or not np.isfinite(a).all():
        raise ValueError('Invalid finite numeric shape: expected ' + str(shape))
    return a

def unit(v, fallback=UP):
    v = finite(v, (3,)); length = np.linalg.norm(v)
    if length < 1e-9:
        v = finite(fallback, (3,)); length = np.linalg.norm(v)
    if length < 1e-9: raise ValueError('Degenerate direction')
    return v / length

def quaternion(q):
    q = finite(q, (4,)); n = np.linalg.norm(q)
    if abs(n - 1) > 1e-5: raise ValueError('Quaternion must be unit XYZW')
    return q / n

def qmul(a, b):
    av, bv = np.asarray(a[:3]), np.asarray(b[:3])
    return np.r_[a[3]*bv + b[3]*av + np.cross(av, bv), a[3]*b[3] - np.dot(av, bv)]

def euler_quat(degrees):
    angles = np.radians(finite(degrees, (3,))) / 2
    parts = [np.r_[np.eye(3)[i]*math.sin(a), math.cos(a)] for i, a in enumerate(angles)]
    return quaternion(qmul(parts[2], qmul(parts[1], parts[0])))

def qangle(a, b):
    return math.degrees(2 * math.acos(float(np.clip(abs(np.dot(a, b)), -1, 1))))

def slerp(a, b, weight):
    a, b = quaternion(a), quaternion(b); d = float(np.dot(a, b))
    if d < 0: b = -b; d = -d
    if d > .9995:
        value=(1-weight)*a+weight*b
        return value/np.linalg.norm(value)
    theta = math.acos(min(1., d))
    return (math.sin((1-weight)*theta)*a + math.sin(weight*theta)*b) / math.sin(theta)

def qmatrix(q):
    x, y, z, w = quaternion(q)
    return np.array([[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
                     [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
                     [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]])

def matrix_quat(m):
    m = finite(m, (3, 3)); trace = np.trace(m)
    if trace > 0:
        s = math.sqrt(trace+1)*2
        q = [(m[2,1]-m[1,2])/s, (m[0,2]-m[2,0])/s, (m[1,0]-m[0,1])/s, s/4]
    else:
        i = int(np.argmax(np.diag(m))); j, k = (i+1)%3, (i+2)%3
        s = math.sqrt(max(0., 1+m[i,i]-m[j,j]-m[k,k]))*2
        q = [0., 0., 0., (m[k,j]-m[j,k])/s]
        q[i] = s/4; q[j] = (m[j,i]+m[i,j])/s; q[k] = (m[k,i]+m[i,k])/s
    return quaternion(q)

class Curve:
    def __init__(self, keys, duration, shape=(3,)):
        if not isinstance(keys, list) or len(keys) < 2: raise ValueError('At least two covering keys required')
        self.times = finite([k['t'] for k in keys])
        self.values = np.array([finite(k['value'], shape) for k in keys])
        if np.any(np.diff(self.times) <= 0) or abs(self.times[0]) > 1e-8 or abs(self.times[-1]-duration) > 1e-6:
            raise ValueError('Keys must strictly cover t=0 through duration')

    def __call__(self, t):
        return np.array([np.interp(t, self.times, self.values[:, i]) for i in range(self.values.shape[1])])

class RotationCurve:
    def __init__(self, spec, duration):
        self.kind = spec['kind']
        if self.kind not in ('euler', 'quaternion'): raise ValueError('Orientation kind must be euler or quaternion')
        if self.kind == 'euler' and spec.get('order', 'XYZ') != 'XYZ': raise ValueError('Only explicit XYZ Euler order supported')
        self.curve = Curve(spec['keys'], duration, (3,) if self.kind == 'euler' else (4,))
        if self.kind == 'quaternion':
            self.curve.values = np.array([quaternion(q) for q in self.curve.values])
            if any(qangle(a, b) > 150 for a, b in zip(self.curve.values, self.curve.values[1:])):
                raise ValueError('Quaternion keys exceed150 degrees; add unambiguous intermediate keys')

    def __call__(self, t):
        if self.kind == 'euler': return euler_quat(self.curve(t))
        i = max(0, min(len(self.curve.times)-2, int(np.searchsorted(self.curve.times, t, side='right'))-1))
        a, b = self.curve.times[i:i+2]
        return slerp(self.curve.values[i], self.curve.values[i+1], float(np.clip((t-a)/(b-a), 0, 1)))

    def check_density(self, times):
        paths=[(a,b,[a]+[float(t) for t in self.curve.times if a<t<b]+[b]) for a,b in zip(times,times[1:])]
        if self.kind == 'euler':
            maximum = max(sum(float(np.sum(np.abs(self.curve(v)-self.curve(u)))) for u,v in zip(split,split[1:])) for a,b,split in paths)
            if maximum > 150: raise ValueError('Euler winding exceeds150 degrees per sample; increase sampleHz')
        else: maximum = 0.
        piecewise_angle=max(sum(qangle(self(u),self(v)) for u,v in zip(split,split[1:])) for a,b,split in paths)
        if piecewise_angle>150:raise ValueError('Quaternion path exceeds150 degrees per sample; increase sampleHz')
        quats = [self(t) for t in times]
        angle = max(qangle(a,b) for a,b in zip(quats,quats[1:]))
        if angle > 150: raise ValueError('Board rotation samples are ambiguous (>150 degrees)')
        return dict(maxAdjacentQuaternionDegrees=angle, maxAdjacentEulerTravelDegrees=maximum,
                    maxPiecewiseQuaternionTravelDegrees=piecewise_angle,
                    denseQuaternionArcDegrees=sum(qangle(a,b) for a,b in zip(quats,quats[1:])),
                    declaredEulerTravelDegrees=np.sum(np.abs(np.diff(self.curve.values,axis=0)),axis=0).tolist() if self.kind=='euler' else None,
                    inputKind=self.kind, winding='Continuous unwrapped Euler evaluated densely' if self.kind=='euler' else 'Dense shortest-arc quaternion keys')

def contact_events(events, duration, goofy=False):
    if not isinstance(events, list) or len(events) > 128: raise ValueError('Invalid event count')
    result = []; previous = -1.
    for event in events:
        t = float(event['t']); phase = PHASES[event['phase']]; flags = 0
        if not math.isfinite(t) or t < previous or not 0 <= t <= duration+1e-8: raise ValueError('Invalid sorted event time')
        for name in event['contacts']:
            if goofy: name = ('Right'+name[4:]) if name.startswith('Left') else ('Left'+name[5:])
            flags |= CONTACTS[name]
        result.append(dict(t=min(t,duration), phase=phase, contacts=flags)); previous = t
    return result

def row83(frame):
    row = [frame['t']] + frame['board']['origin'] + frame['board']['quaternion']
    row += [x for k in JOINTS for x in frame['joints'][k]]
    row += [x for k in DIRS for x in frame['dirs'][k]]
    row += frame['soleUp']['Left'] + frame['soleUp']['Right']
    finite(row, (STRIDE,)); quaternion(row[4:8])
    return row

def encode_pack(clips):
    if not 1 <= len(clips) <= 256: raise ValueError('Pack clip count outside1..256')
    out = bytearray(b'SKMCAP01' + struct.pack('<I', len(clips))); total = 0; ids=set()
    for clip in clips:
        meta = clip['metadata']; duration = float(clip['duration']); frames = clip['frames']; total += len(frames)
        if not 0 < duration <= 10 or not 2 <= len(frames) <= 2401 or total > 200000: raise ValueError('Pack duration/sample bounds exceeded')
        if not isinstance(meta.get('id'),str):raise ValueError('Pack id must be text')
        if meta['id'].casefold() in ids:raise ValueError('Duplicate pack id')
        ids.add(meta['id'].casefold())
        for key, limit in [('id',64),('label',192),('sourceUrl',2048),('sourceAuthor',192),('movementKind',64)]:
            if not isinstance(meta[key],str) or not meta[key].strip() or any(unicodedata.category(c)=='Cc' for c in meta[key]):
                raise ValueError('Explicit nonempty text without control characters required: '+key)
            value = meta[key].encode('utf-8')
            if not value or len(value) > limit: raise ValueError('Invalid bounded string: '+key)
            out += struct.pack('<H', len(value)) + value
        if len(out)+13+len(frames)*STRIDE*8+2>32*1024*1024:raise ValueError('Pack exceeds32MiB')
        if type(meta['reviewed']) is not bool: raise ValueError('reviewed must be explicit boolean')
        out += struct.pack('<BdI', int(meta['reviewed']), duration, len(frames))
        previous = -1.; last_q = None
        for frame in frames:
            row = row83(frame)
            if not previous < row[0] <= duration+1e-6 or row[0] < 0: raise ValueError('Invalid pack sample timestamp')
            if last_q is not None and qangle(last_q,row[4:8]) > 150: raise ValueError('Ambiguous pack rotation')
            previous = row[0]; last_q = row[4:8]; out += struct.pack('<83d', *row)
        if abs(frames[0]['t']) > 1e-8 or abs(frames[-1]['t']-duration) > 1e-6: raise ValueError('Pack must cover both endpoints')
        events = clip['events']
        if len(events) > 128: raise ValueError('Too many events')
        out += struct.pack('<H', len(events)); prior = -1.
        for e in events:
            if not prior <= e['t'] <= duration or not 0 <= e['phase'] <= 6 or not 0 <= e['contacts'] <= 15: raise ValueError('Invalid event')
            out += struct.pack('<dBB', e['t'], e['phase'], e['contacts']); prior = e['t']
    if len(out) > 32*1024*1024: raise ValueError('Pack exceeds32MiB')
    return bytes(out)
