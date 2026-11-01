#!/usr/bin/env python3
"""Bake the own Fakie Ollie source with original board/shoe contact reconstruction.

Reuse already-main Ollie geometry/sampling mathematics only. The own source,
body controls, pressure clock, contacts and reference are independently pinned.
"""
import argparse
import copy
import json
from pathlib import Path
import numpy as np
import author_skate_ollie as core
from author_skate_kickflip import limb
from capture_math import contact_events, encode_pack, unit

ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / 'assets/authored/animation/fakie-ollie-keyed'
GEOMETRY = ROOT / 'assets/authored/animation/treflip-keyed'


def sample(controls, spec, source_frame):
    frame = core.sample(controls, spec, source_frame)
    # Monocular leg depth can reverse a knee even when image confidence is high.
    # Reconstruct coherent toe-side planes while retaining exact fixed shoe
    # paths, fixed lengths, source torso/arms and the single native carrier.
    pole = unit(np.asarray(spec['kneePlane']['forward']))
    for side in ('Left', 'Right'):
        hip = np.asarray(frame['joints'][side+'Hip'])
        ankle = np.asarray(frame['joints'][side+'Ankle'])
        frame['joints'][side+'Knee'] = limb(hip, ankle, .44, .43, pole).tolist()
    return frame


def bake(body_raw, controls):
    s = copy.deepcopy(controls)
    if core.digest(body_raw) != s['sourceBodySha256']:
        raise ValueError('Own Fakie Ollie body binding differs')
    c = json.loads(body_raw)
    if (c['firstSourceFrame'], c['lastSourceFrame'], c['filmedStance'], c['canonicalStance'], c['reviewed']) != (274, 347, 'regular', 'regular', False):
        raise ValueError('Use only the own unreviewed Fakie Ollie source')
    if c['sourceVideoSha256'] != s['sourceVideoSha256']:
        raise ValueError('Own bounded video binding differs')
    if core.digest((ROOT/'scripts/author_skate_ollie.py').read_bytes()) != s['baseSamplerSha256']:
        raise ValueError('Already-main Ollie contact sampler differs')
    geometry_raw = (GEOMETRY/'constraints.json').read_bytes()
    if core.digest(geometry_raw) != s['geometrySha256']:
        raise ValueError('Original board/shoe geometry differs')
    geometry = json.loads(geometry_raw)
    surface_raw = (GEOMETRY/'own-board-surface.json').read_bytes()
    if core.digest(surface_raw) != geometry['sourceSurfaceSha256']:
        raise ValueError('Original shaped board surface differs')
    surface = json.loads(surface_raw)
    geometry['_surfacePoints'] = np.asarray(surface['points'])
    geometry['_surfaceTriangles'] = np.asarray(surface['triangles'])
    geometry['authored']['minimumAirSoleClearance'] = s['minimumAirSoleClearance']
    s['_geometry'] = geometry
    s['_strikePitch'] = core.tail_strike_pitch(s)
    ordered = [s[k] for k in ('firstSourceFrame', 'pop', 'frontRelease', 'tailStrike', 'tailRelease', 'pressEnd', 'catch', 'land', 'recovery', 'lastSourceFrame')]
    if not all(a < b for a, b in zip(ordered, ordered[1:])):
        raise ValueError('Expected own tail-sole release then distinct later free-Air before catch')
    for field in ('pitchKeys', 'pelvisKeys', 'frontFreeKeys', 'rearFreeKeys', 'gazeKeys'):
        keys = s[field]
        if not all(a[0] < b[0] for a, b in zip(keys, keys[1:])):
            raise ValueError('Own source control clock must strictly increase: '+field)
    phases = [(s['firstSourceFrame'], 'load', ['LeftFoot', 'RightFoot']),
              (s['pop'], 'pop', ['LeftFoot', 'RightFoot']),
              (s['frontRelease'], 'pop', ['RightFoot']),
              (s['tailRelease'], 'pop', []), (s['pressEnd'], 'air', []),
              (s['catch'], 'catch', ['LeftFoot', 'RightFoot']),
              (s['land'], 'land', ['LeftFoot', 'RightFoot']),
              (s['recovery'], 'recovery', ['LeftFoot', 'RightFoot'])]
    times = np.unique(np.round(np.r_[np.arange(s['firstSourceFrame'], s['lastSourceFrame']+1e-7, 1/6), ordered,
                      s['guide']['start'], s['guide']['full'], s['guide']['release'], s['guide']['end']], 9))
    frames = [sample(c, s, float(sf)) for sf in times]
    metadata = dict(id=s['id'], movementKind=s['movementKind'], reviewed=False,
        label='Fakie Ollie — own P4C source, authored contact/depth [UNREVIEWED]',
        authored=True, capturedBody=True, canonicalStance='regular', filmedStance=c['filmedStance'],
        sourceReflectionAppliedOnce=c['sourceReflectionAppliedOnce'], nativeSignature=s['nativeSignature'],
        firstSourceFrame=s['firstSourceFrame'], lastSourceFrame=s['lastSourceFrame'], sourceFps=s['sourceFps'],
        sourceUrl=s['sourceUrl'], sourceAuthor=s['sourceAuthor'],
        sourceVideoOffsetSeconds=s['sourceCacheOriginalOffsetSeconds']+s['firstSourceFrame']/s['sourceFps'],
        sourceCacheOriginalOffsetSeconds=s['sourceCacheOriginalOffsetSeconds'],
        sourceBodyControlSha256=s['sourceBodySha256'], sourceVideoSha256=s['sourceVideoSha256'],
        fullSourceVideoSha256=None, sourceFrameSpace='Bounded transcoded preview decoder indices, NOT original global video frames',
        sourcePoseSha256=c['sourcePoseSha256'], sourceTiming=c['sourceTiming'], sourceRootMotion=s['sourceRootMotion'],
        sourceRawMissingFrames=c['sourceRawMissingFrames'], authoredInterpolationAtMissingFrames=c['authoredInterpolationAtMissingFrames'],
        provenance=s['provenance'], uncertainty=s['uncertainty'], sourcePhaseLimit=s['phaseClockLimit'],
        contactLimit='Own authored canonicalRight tail-sole and Left outer-upper guide; sole/contact bits are authoring candidates, not measured3D.',
        sourceTailStrikeFrame=s['tailStrike'], referenceGroundPress=s['reference'],
        combinedWinding=dict(nativeFlip=0, nativeShove=0, bodySpin=0, canonicalRoll=0, canonicalYaw=0),
        reconstruction=dict(pelvisKeys=s['pelvisKeys'], recoveryWindow=s['recoveryWindow'], guide=s['guide'],
                            gazeKeys=s['gazeKeys'], kneePlane=s['kneePlane'], maximumReachShare=s['maximumReachShare']),
        rig=dict(thigh=.44, shin=.43, upperArm=.29, forearm=.27, spine=.52, shoulderRise=.44, hipHalfWidth=.09, shoulderHalfWidth=.18, head=.12, foot=.14, toeDrop=.07))
    duration = (s['lastSourceFrame']-s['firstSourceFrame'])/s['sourceFps']
    clip = dict(format='skate-capture-source-keyed-fakie-ollie/1', metadata=metadata, stance='regular',
                duration=duration, fps=s['sourceFps']*6, frames=frames,
                events=contact_events([dict(t=(sf-s['firstSourceFrame'])/s['sourceFps'], phase=phase, contacts=contacts)
                                       for sf, phase, contacts in phases], duration))
    return clip, encode_pack([clip])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--controls', type=Path, default=ASSET/'constraints.json')
    args = parser.parse_args()
    clip, packed = bake((ASSET/'captured-body.json').read_bytes(), json.loads(args.controls.read_text()))
    args.output.mkdir(parents=True, exist_ok=True)
    for name, data in [(clip['metadata']['id']+'.json', (json.dumps(clip, indent=2)+'\n').encode()), ('skate_captures.skmc', packed)]:
        target = args.output/name
        temporary = target.with_suffix(target.suffix+'.tmp')
        temporary.write_bytes(data)
        temporary.replace(target)
    print('FAKIE_OLLIE_SOURCE_READY', len(clip['frames']), core.digest(packed))


if __name__ == '__main__':
    main()
