#!/usr/bin/env python3
"""Read-only loudness gate for the18 selected game WAVs; never edits audio.

Dependencies: numpy, scipy, pyloudnorm (tested with0.2.0). The existing synthesis
catalog supplies names/targets only; this command never invokes its generator.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.metadata
import json
import math
from pathlib import Path
import re
import shutil
import subprocess
import sys
import warnings
import wave

import numpy as np
import pyloudnorm as pyln

from generate_skate_sounds import RATE, STEMS

TOLERANCE_LU = .5
METER_AGREEMENT_LU = .1
BLOCK = round(.4 * RATE)
HOP = round(.1 * RATE)
MAX_WAV_BYTES = 2 * 1024 * 1024


def finite_loudness(y):
    with warnings.catch_warnings():
        warnings.simplefilter('ignore', RuntimeWarning)
        value = float(pyln.Meter(RATE, block_size=.4).integrated_loudness(y))
    return value if math.isfinite(value) else None


def complete_block_loudness(y):
    """Exclude partial analysis windows, retaining the actual WAV untouched."""
    if len(y) < BLOCK:
        raise ValueError('Fewer than400ms: no complete loudness block')
    count = 1 + (len(y) - BLOCK) // HOP
    end = BLOCK + (count - 1) * HOP
    # At an exact complete-window end, pyloudnorm's rounded block count cannot
    # add its erroneous partial final window. This is only an analysis view.
    value = finite_loudness(y[:end])
    if value is None:
        raise ValueError('No finite gated loudness: silent/unmeasurable audio')
    return value, count, end


def read_pcm(path, kind):
    if path.stat().st_size > MAX_WAV_BYTES:
        raise ValueError('WAV exceeds bounded2MiB input size')
    with wave.open(str(path), 'rb') as wav:
        if (wav.getnchannels(), wav.getsampwidth(), wav.getframerate(), wav.getcomptype()) != (1, 2, RATE, 'NONE'):
            raise ValueError('Expected mono44100Hz16-bitPCM')
        count = wav.getnframes()
        if count < BLOCK or count > RATE * (4 if kind == 'loop' else 6):
            raise ValueError('Duration outside400ms minimum or game maximum')
        if kind == 'loop' and count < 2 * RATE:
            raise ValueError('Loop shorter than2seconds')
        raw = wav.readframes(count + 1)
        if len(raw) != count * 2:
            raise ValueError('Truncated or inconsistent PCM sample count')
    return np.frombuffer(raw, dtype='<i2')


def ffmpeg_input(executable, path):
    # Only input_i/input_tp are used. The normalizer output is discarded; no
    # source/destination WAV is ever opened for writing by this command.
    result = subprocess.run(
        [executable, '-hide_banner', '-nostdin', '-i', str(path), '-af',
         'loudnorm=I=-12:TP=-1:LRA=11:print_format=json', '-f', 'null', '-'],
        stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True, timeout=30,
        check=False)
    match = re.search(r'(\{\s*"input_i"[\s\S]*?\})', result.stderr)
    if result.returncode or match is None:
        raise ValueError('Independent FFmpeg input measurement failed')
    data = json.loads(match.group(1))
    lufs, peak = float(data['input_i']), float(data['input_tp'])
    if not all(math.isfinite(value) for value in (lufs, peak)):
        raise ValueError('Nonfinite independent FFmpeg result')
    return dict(input_lufs=lufs, input_true_peak_dbfs=peak)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('sounds', type=Path, help='Folder with the18 required *_01.wav files')
    parser.add_argument('--ffmpeg', metavar='EXECUTABLE',
                        help='Also require independent input LUFS/truepeak and meter agreement; otherwise NOT_RUN')
    args = parser.parse_args()
    failures = []
    if not args.sounds.is_dir():
        parser.error('sounds must be an existing directory')
    executable = shutil.which(args.ffmpeg) if args.ffmpeg else None
    if args.ffmpeg and executable is None:
        parser.error('--ffmpeg executable not found; independent gate was requested')
    # Use only the18 game names from the shared catalog. A demo or other WAV
    # in the delivery root is never substituted for a missing game sound.
    expected = {stem + '_01.wav' for stem in STEMS}
    present = {path.name for path in args.sounds.iterdir() if path.is_file()}
    missing = sorted(expected - present)
    if missing:
        failures.append(dict(gate='required_game_filenames', missing=missing))
    rows = []
    for stem, (kind, _, target, _) in STEMS.items():
        name = stem + '_01.wav'
        row = dict(name=name, kind=kind, requested_lufs=target, tolerance_lu=TOLERANCE_LU,
                   failures=[], independent_ffmpeg='NOT_RUN')
        try:
            path = args.sounds / name
            pcm = read_pcm(path, kind)
            y = pcm.astype(np.float64) / 32768
            loudness, count, end = complete_block_loudness(y)
            peak = float(np.max(np.abs(y)))
            raw_loudness = finite_loudness(y)
            row.update(samples=len(pcm), seconds=len(pcm) / RATE,
                       sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                       integrated_lufs=loudness, deviation_lu=loudness-target,
                       complete_blocks=count, analysis_end_sample=end,
                       raw_pyloudnorm_lufs=raw_loudness,
                       raw_pyloudnorm_target_met=raw_loudness is not None and abs(raw_loudness-target) <= TOLERANCE_LU,
                       sample_peak_dbfs=20*math.log10(peak) if peak else None)
            if abs(loudness-target) > TOLERANCE_LU:
                row['failures'].append('complete_block_lufs_target')
            if peak > 10**(-1/20) or np.any(np.abs(pcm.astype(np.int32)) >= 32767):
                row['failures'].append('sample_peak_or_clipping')
            if executable:
                independent = ffmpeg_input(executable, path)
                independent['meter_difference_lu'] = loudness-independent['input_lufs']
                row['independent_ffmpeg'] = independent
                if abs(independent['input_lufs']-target) > TOLERANCE_LU:
                    row['failures'].append('independent_ffmpeg_lufs_target')
                if independent['input_true_peak_dbfs'] > -1:
                    row['failures'].append('independent_ffmpeg_true_peak')
                if abs(independent['meter_difference_lu']) > METER_AGREEMENT_LU:
                    row['failures'].append('independent_meter_disagreement')
        except (OSError, ValueError, wave.Error, EOFError, subprocess.SubprocessError) as error:
            row['failures'].append(type(error).__name__ + ': ' + str(error))
        if row['failures']:
            failures.append(dict(name=name, failures=row['failures']))
        rows.append(row)
    result = dict(
        schema='selected18-loudness-gate/1',
        result='FAIL' if failures else ('PASS_LUFS_AND_INDEPENDENT_FFMPEG' if executable else 'PASS_LUFS_ONLY_INDEPENDENT_NOT_RUN'),
        meter='pyloudnorm K weighting; complete400ms blocks/100ms hops; raw result retained as diagnosis',
        dependencies={name: importlib.metadata.version(name) for name in ('numpy', 'scipy', 'pyloudnorm')},
        sample_rate=RATE, channels=1, pcm_bits=16, expected_files=18,
        independent_ffmpeg_gate='RUN_AND_REQUIRED' if executable else 'NOT_RUN',
        independent_meter_agreement_tolerance_lu=METER_AGREEMENT_LU,
        audio_mutated=False, audio_padded=False, audio_trimmed=False,
        subjective_quality_approved=False, native_game_playback=False,
        additional_wavs_ignored=sorted(name for name in present-expected if name.lower().endswith('.wav')),
        unchecked_gates=['DC', 'onset/tail', 'loop seams', 'truepeak when FFmpeg not requested', 'listening', 'installation/game playback'],
        files=rows, failures=failures)
    print(json.dumps(result, indent=2, allow_nan=False))
    return 1 if failures else 0


if __name__ == '__main__':
    raise SystemExit(main())
