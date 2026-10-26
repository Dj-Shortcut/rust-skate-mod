#!/usr/bin/env python3
# SPDX-License-Identifier: CC0-1.0
"""Original CC0 modal/noise skateboard synthesis; no recordings or external assets.

Run in the supplied local venv: python generate_skate_sounds.py --output skate_sounds
All metrics are measured again from written 44.1kHz/mono/16-bit PCM, not inferred
from normalization gains. Failed hard checks exit 1 and remain in manifest.json.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.metadata
import json
import math
from pathlib import Path
import sys
import wave

import numpy as np
import pyloudnorm as pyln
from scipy import signal

RATE = 44100
PEAK_DB = -1.05  # margin below the requested -1 dBFS after integer quantization
PEAK = 10 ** (PEAK_DB / 20)
SEED = 20261026
METER = pyln.Meter(RATE, block_size=.4)
UPPER = 32767
# Targets are integrated LUFS, not peak dBFS. The quiet additive layers are explicit exceptions.
STEMS = {
    'skate_rolling_smooth': ('loop', 3.0, -20., 'Smooth concrete: urethane wheel/bearing noise, irregular axle micro-ticks.'),
    'skate_rolling_rough': ('loop', 3.0, -20., 'Rough asphalt: granular high texture, low wheel bumps and tiny stones.'),
    'skate_rolling_wood': ('loop', 3.0, -20., 'Wood platform: hollow deck/wheel body and irregular paired seam ticks.'),
    'skate_grit': ('loop', 3.0, -28., 'Quiet additive fine-stone layer; intentionally 8 LU below the main loops.'),
    'skate_whirr': ('loop', 3.0, -32., 'Very quiet additive free-bearing whirr; intentionally 12 LU below the main loops.'),
    'skate_grind': ('loop', 3.0, -20., 'Sustained fine-grain steel truck friction, weak metallic modes.'),
    'skate_deckslide': ('loop', 3.0, -20., 'Waxed wood scrape: lower mid texture and small dry wood grains.'),
    'skate_push': ('one_shot', .62, -22., 'Shoe sole scuff and foot contact; 10 LU below ollie.'),
    'skate_ollie': ('one_shot', .68, -12., 'Dry tail wood crack, grip scrape and short deck modes; no separate flip sound.'),
    'skate_landing': ('one_shot', .85, -12., 'Four-wheel flex thud, wood body and bolt rattle.'),
    'skate_drop': ('one_shot', 1.15, -9., 'Heavier four-wheel landing; 3 LU above ordinary landing.'),
    'skate_seam': ('one_shot', .48, -23., 'Two axle wheel clicks across a surface seam.'),
    'skate_grind_in': ('one_shot', .72, -16., 'Truck engagement: dry steel clank into a short friction burst.'),
    'skate_grind_out': ('one_shot', .58, -19., 'Truck release: short weak metal ring and scrape ending.'),
    'skate_slide_in': ('one_shot', .72, -17., 'Deck engagement: wood impact and wax scrape onset.'),
    'skate_scrape': ('one_shot', .85, -19., 'Tail stop: rising dry deck scrape with a short final scuff.'),
    'skate_slam': ('one_shot', 1.25, -13., 'Clothing/shoe/hand impacts on ground; no voice or human recording.'),
    'skate_bail': ('one_shot', 1.80, -17., 'Escaping board: wheel roll-away, diminishing wood/bolt clatter.'),
}


def db(value: float) -> float:
    return float(20 * np.log10(max(abs(float(value)), 1e-15)))


def lowpass(x, cutoff):
    return signal.sosfilt(signal.butter(3, cutoff, btype='lowpass', fs=RATE, output='sos'), x)


def noise(rng, count, low=20., high=10000., periodic=False):
    """Band-shaped random excitation. FFT loop texture is periodic before endpoint fades."""
    x = rng.standard_normal(count)
    if periodic:
        frequencies = np.fft.rfftfreq(count, 1 / RATE)
        shape = (1 - np.exp(-(frequencies / low) ** 4)) * np.exp(-(frequencies / high) ** 4)
        x = np.fft.irfft(np.fft.rfft(x) * shape, n=count)
    else:
        x = signal.sosfilt(signal.butter(2, [low, high], btype='bandpass', fs=RATE, output='sos'), x)
    return x / max(np.std(x), 1e-12)


def modal(rng, seconds, material='wood', strength=1., pitch=1.):
    t = np.arange(round(seconds * RATE)) / RATE
    if material == 'steel':
        modes, tau = [630, 1238, 2090, 3492, 5320, 8170], [.115, .087, .064, .041, .026, .015]
        amplitudes = [.35, .26, .19, .12, .07, .035]
    elif material == 'rubber':
        modes, tau = [67, 113, 181, 391], [.064, .047, .031, .018]
        amplitudes = [.48, .31, .14, .06]
    elif material == 'cloth':
        modes, tau = [48, 83, 141], [.095, .070, .035]
        amplitudes = [.5, .3, .12]
    else:
        modes, tau = [143, 251, 413, 723, 1147, 1893, 3029], [.145, .114, .086, .061, .038, .023, .013]
        amplitudes = [.31, .24, .18, .13, .08, .045, .025]
    result = np.zeros(len(t))
    for frequency, damping, amplitude in zip(modes, tau, amplitudes):
        f = frequency * pitch * rng.uniform(.96, 1.04)
        phase = rng.uniform(-.15, .15)
        result += amplitude * np.sin(2 * np.pi * f * t + phase) * np.exp(-t / (damping * rng.uniform(.85, 1.15)))
    result *= np.minimum(1, t / .0008)
    tail = min(round(.012 * RATE), len(result) // 5)
    result[-tail:] *= np.sin(np.linspace(np.pi / 2, 0, tail)) ** 2
    return result * strength


def add(destination, fragment, seconds, gain=1., wrap=False):
    at = round(seconds * RATE)
    if wrap:
        indices = (np.arange(len(fragment)) + at) % len(destination)
        np.add.at(destination, indices, fragment * gain)
    else:
        end = min(len(destination), at + len(fragment))
        if at < len(destination) and end > at:
            destination[at:end] += fragment[:end - at] * gain


def hit(rng, seconds=.32, material='wood', weight=1., brightness=1.):
    n = round(seconds * RATE)
    t = np.arange(n) / RATE
    y = modal(rng, seconds, material, weight)
    if material == 'steel':
        y += .55 * weight * noise(rng, n, 900, 10000) * np.exp(-t / .009)
    elif material == 'rubber':
        y += .22 * weight * noise(rng, n, 80, 1800) * np.exp(-t / .016)
    elif material == 'cloth':
        y += .43 * weight * noise(rng, n, 90, 2300) * np.exp(-t / .045)
    else:
        y += .75 * weight * noise(rng, n, 450, min(13000, 7500 * brightness)) * np.exp(-t / .008)
        y += .30 * weight * noise(rng, n, 90, 1700) * np.exp(-t / .036)
    y *= np.minimum(1, t / .0006)
    tail = min(round(.010 * RATE), len(y) // 5)
    y[-tail:] *= np.sin(np.linspace(np.pi / 2, 0, tail)) ** 2
    return y


def friction(rng, seconds, low, high, attack=.012, release=.06):
    n = round(seconds * RATE)
    t = np.arange(n) / RATE
    envelope = np.minimum(1, t / attack) * np.minimum(1, np.maximum(0, seconds - t) / release)
    coarse = lowpass(rng.standard_normal(n), 28)
    coarse /= max(np.std(coarse), 1e-10)
    envelope *= np.clip(.75 + .16 * coarse, .25, 1.3)
    return noise(rng, n, low, high) * envelope


def loop(stem, rng, seconds):
    n = round(seconds * RATE)
    t = np.arange(n) / RATE
    # Slow nonidentical modulation plus irregular micro-events; no metronomic event train.
    modulation = .82 + .10 * np.sin(2 * np.pi * t / seconds + rng.uniform(0, 6))
    modulation += .05 * np.sin(2 * np.pi * 3 * t / seconds + rng.uniform(0, 6))
    if stem == 'skate_whirr':
        y = .23 * noise(rng, n, 470, 4200, True)
        for f, amplitude in [(610, .22), (970, .13), (1430, .065)]:
            # Integer loop harmonic, with slow circular speed wobble.
            frequency = round(f * seconds) / seconds
            y += amplitude * np.sin(2 * np.pi * frequency * t + .09 * np.sin(2 * np.pi * t / seconds))
        return y * modulation
    if stem == 'skate_grit':
        y = .18 * noise(rng, n, 1400, 10500, True)
        density, material, amplitude = 48, 'wood', .017
    elif stem == 'skate_grind':
        y = .39 * noise(rng, n, 1700, 8800, True) + .17 * noise(rng, n, 190, 1600, True)
        for f in [418, 1190, 2430]:
            y += .025 * np.sin(2 * np.pi * round(f * seconds) / seconds * t)
        density, material, amplitude = 70, 'steel', .022
    elif stem == 'skate_deckslide':
        y = .42 * noise(rng, n, 180, 4100, True) + .06 * noise(rng, n, 40, 200, True)
        density, material, amplitude = 37, 'wood', .026
    elif stem == 'skate_rolling_rough':
        y = .39 * noise(rng, n, 430, 7700, True) + .23 * noise(rng, n, 42, 270, True)
        density, material, amplitude = 19, 'rubber', .13
    elif stem == 'skate_rolling_wood':
        y = .25 * noise(rng, n, 350, 4600, True) + .30 * noise(rng, n, 70, 350, True)
        density, material, amplitude = 12, 'wood', .085
        at = .09
        while at < seconds:
            add(y, hit(rng, .16, 'wood', .11, .7), at, wrap=True)
            add(y, hit(rng, .10, 'rubber', .08), at + rng.uniform(.017, .030), wrap=True)
            at += rng.uniform(.21, .41)
    else:
        y = .31 * noise(rng, n, 380, 5600, True) + .15 * noise(rng, n, 52, 235, True)
        density, material, amplitude = 10, 'rubber', .08
    y *= modulation
    at = rng.uniform(0, 1 / density)
    while at < seconds:
        add(y, hit(rng, rng.uniform(.035, .095), material, amplitude * rng.uniform(.35, 1.4)), at, wrap=True)
        at += rng.exponential(1 / density)
    return y


def one_shot(stem, rng, seconds):
    y = np.zeros(round(seconds * RATE))
    onset = rng.uniform(.0007, .0025)
    if stem == 'skate_push':
        add(y, friction(rng, .34, 190, 5100, .008, .09), onset, .55)
        add(y, hit(rng, .27, 'rubber', .45), onset)
        add(y, friction(rng, .12, 900, 6100), .18, .20)
    elif stem == 'skate_ollie':
        add(y, hit(rng, .41, 'wood', 1.1), onset)
        add(y, friction(rng, .13, 950, 7200, .004, .08), onset + .014, .40)
        add(y, hit(rng, .20, 'rubber', .24), onset + .037)
    elif stem in ('skate_landing', 'skate_drop'):
        heavy = stem == 'skate_drop'
        for offset, gain in [(0, .85), (.007, .65), (.019, .72), (.034, .58)]:
            add(y, hit(rng, .43 if heavy else .31, 'rubber', gain), onset + offset * rng.uniform(.8, 1.25))
        add(y, modal(rng, .68 if heavy else .48, 'wood', .95 if heavy else .65, .78 if heavy else 1.), onset)
        add(y, friction(rng, .26 if heavy else .15, 110, 3400, .001, .1), onset, .46 if heavy else .28)
        for offset in rng.uniform(.014, .16, 10 if heavy else 7):
            add(y, hit(rng, .14, 'steel', .032), onset + offset)
    elif stem == 'skate_seam':
        add(y, hit(rng, .16, 'rubber', .8), onset)
        add(y, hit(rng, .14, 'wood', .32), onset)
        add(y, hit(rng, .16, 'rubber', .66), onset + rng.uniform(.026, .045))
    elif stem == 'skate_grind_in':
        add(y, hit(rng, .38, 'steel', .87), onset)
        add(y, friction(rng, .27, 1100, 8800, .001, .09), onset, .48)
        add(y, modal(rng, .34, 'wood', .22), onset)
    elif stem == 'skate_grind_out':
        add(y, hit(rng, .30, 'steel', .46), onset)
        add(y, friction(rng, .18, 1700, 7600, .002, .10), onset, .34)
        add(y, hit(rng, .19, 'rubber', .18), onset + .025)
    elif stem == 'skate_slide_in':
        add(y, hit(rng, .38, 'wood', .88, .75), onset)
        add(y, friction(rng, .31, 170, 4100, .002, .14), onset, .62)
    elif stem == 'skate_scrape':
        add(y, friction(rng, .53, 200, 6500, .004, .15), onset, .58)
        add(y, hit(rng, .16, 'wood', .16), onset)
        for at in rng.uniform(.04, .43, 11):
            add(y, hit(rng, .09, 'wood', rng.uniform(.035, .09)), at)
    elif stem == 'skate_slam':
        add(y, hit(rng, .56, 'cloth', 1.05), onset)
        add(y, friction(rng, .35, 140, 4700, .002, .19), onset, .38)
        add(y, hit(rng, .35, 'rubber', .72), onset + .043)
        add(y, hit(rng, .28, 'cloth', .49), onset + .087)
        add(y, friction(rng, .23, 300, 3400), .15, .21)
    elif stem == 'skate_bail':
        add(y, hit(rng, .27, 'wood', .52), onset)
        add(y, hit(rng, .26, 'rubber', .45), onset + .008)
        add(y, friction(rng, 1.40, 350, 5800, .002, .36), onset, .24)
        for at, gain in [(0.04, .55), (.16, .38), (.31, .27), (.51, .18), (.80, .11), (1.10, .055)]:
            add(y, hit(rng, .28, 'wood', gain), at + rng.uniform(0, .02))
            add(y, hit(rng, .18, 'steel', gain * .07), at)
        y *= np.exp(-np.arange(len(y)) / (RATE * 1.1))
    else:
        raise ValueError(stem)
    return y


def zero_dc_float(y):
    # Remove DC within the actual amplitude envelope, preserving silent natural tails.
    # A whole-file sinusoidal correction would synthesize a new low-frequency tail.
    window = signal.fftconvolve(np.abs(y), np.ones(441) / 441, mode='same')
    window[0] = window[-1] = 0
    return y - window * (np.mean(y) / max(np.mean(window), 1e-15))


def finish(raw, target, is_loop):
    raw = np.asarray(raw, dtype=np.float64)
    if is_loop:
        raw -= np.mean(raw)
    else:
        raw = signal.sosfilt(signal.butter(2, 25, btype='highpass', fs=RATE, output='sos'), raw)
    # Soft saturation models compressed contact material and controls transient crest.
    # Increase drive only when the actual target LUFS would violate the peak ceiling.
    scale = max(np.percentile(np.abs(raw), 98), 1e-10)
    start_fade = round((.003 if is_loop else .001) * RATE)
    end_fade = round((.003 if is_loop else .020) * RATE)
    envelope = np.ones(len(raw))
    envelope[:start_fade] = np.sin(np.linspace(0, np.pi / 2, start_fade)) ** 2
    envelope[-end_fade:] = np.sin(np.linspace(np.pi / 2, 0, end_fade)) ** 2
    raw[0] = raw[-1] = 0
    drive = .55
    for iteration in range(30):
        y = np.tanh(raw / scale * drive)
        if not is_loop:
            y = signal.sosfilt(signal.butter(2, 25, btype='highpass', fs=RATE, output='sos'), y)
        y *= envelope
        y = zero_dc_float(y)
        loudness = float(METER.integrated_loudness(y))
        if not np.isfinite(loudness):
            raise RuntimeError('Non-finite loudness in synthesis')
        y *= 10 ** ((target - loudness) / 20)
        if np.max(np.abs(y)) <= PEAK:
            pcm = np.rint(y * 32768).astype(np.int32)
            # Exact integer zero DC; distribute one-LSB corrections inside the waveform.
            total = int(np.sum(pcm, dtype=np.int64))
            candidates = np.flatnonzero((np.abs(pcm) > 3) & (np.abs(pcm) < 29000))
            if abs(total) > len(candidates):
                raise RuntimeError('DC correction exceeds one LSB per eligible sample')
            if total:
                pcm[candidates[:abs(total)]] -= 1 if total > 0 else -1
            pcm[0] = pcm[-1] = 0
            if np.max(np.abs(pcm)) >= 32768 or np.sum(pcm, dtype=np.int64) != 0:
                raise RuntimeError('Invalid PCM peak/DC')
            return pcm.astype('<i2'), drive
        drive *= 1.38
    raise RuntimeError(f'Unable to reach target {target} LUFS within the peak ceiling')


def write_wave(path, pcm):
    with wave.open(str(path), 'wb') as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(pcm.tobytes())


def read_wave(path):
    with wave.open(str(path), 'rb') as wav:
        params = dict(channels=wav.getnchannels(), sample_width_bytes=wav.getsampwidth(), sample_rate=wav.getframerate(), frames=wav.getnframes(), compression=wav.getcomptype())
        pcm = np.frombuffer(wav.readframes(wav.getnframes()), dtype='<i2').copy()
    return pcm, params


def measurements(path, kind, target):
    pcm, params = read_wave(path)
    y = pcm.astype(np.float64) / 32768
    active = np.flatnonzero(np.abs(y) >= 10 ** (-60 / 20))
    metrics = {
        **params, 'seconds': len(y) / RATE,
        'integrated_lufs': float(METER.integrated_loudness(y)), 'target_lufs': target,
        'peak_dbfs': db(np.max(np.abs(y))),
        'dc_integer_sum': int(np.sum(pcm, dtype=np.int64)),
        'dc_linear': float(np.mean(y)),
        'clipped_samples': int(np.count_nonzero(np.abs(pcm.astype(np.int32)) >= 32767)),
        'onset_ms_at_minus60_dbfs': (float(active[0]) / RATE * 1000 if len(active) else None),
        'last_10ms_rms_dbfs': db(np.sqrt(np.mean(y[-441:] ** 2))),
        'rms_dbfs': db(np.sqrt(np.mean(y ** 2))),
        'first_sample': int(pcm[0]), 'last_sample': int(pcm[-1]),
        'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
    }
    if kind == 'loop':
        pitched = np.interp(np.arange(0, len(y) - 1, 1.4), np.arange(len(y)), y)
        metrics.update({
            'loop_start_sample': 0, 'loop_end_sample_exclusive': len(y),
            'seam_delta_integer_samples': int(pcm[0]) - int(pcm[-1]),
            'seam_delta_dbfs': db(y[0] - y[-1]),
            'ten_repeat_max_join_delta_dbfs': db(max(abs(y[0] - y[-1]) for _ in range(9))),
            'seam_neighbor_max_step_dbfs': db(max(abs(y[1] - y[0]), abs(y[-1] - y[-2]))),
            'pitch_plus40_percent_join_delta_dbfs': db(pitched[0] - pitched[-1]),
            'pitch_plus40_percent_peak_dbfs': db(np.max(np.abs(pitched))),
            'micro_event_source': 'seeded irregular excitation; circular event tails',
        })
    failures = []
    if params != {'channels': 1, 'sample_width_bytes': 2, 'sample_rate': RATE, 'frames': len(y), 'compression': 'NONE'}:
        failures.append('format')
    if metrics['peak_dbfs'] > -1 or metrics['clipped_samples']:
        failures.append('peak/clipping')
    if metrics['dc_integer_sum'] != 0:
        failures.append('DC')
    if abs(metrics['integrated_lufs'] - target) > .1:
        failures.append('target LUFS')
    if metrics['first_sample'] != 0 or metrics['last_sample'] != 0:
        failures.append('endpoint fade')
    if kind == 'one_shot' and (metrics['onset_ms_at_minus60_dbfs'] is None or metrics['onset_ms_at_minus60_dbfs'] > 20):
        failures.append('onset')
    if kind == 'one_shot' and metrics['last_10ms_rms_dbfs'] > -60:
        failures.append('natural end tail')
    if kind == 'loop' and not (2 <= metrics['seconds'] <= 4 and metrics['seam_delta_dbfs'] <= -60 and metrics['ten_repeat_max_join_delta_dbfs'] <= -60 and metrics['pitch_plus40_percent_join_delta_dbfs'] <= -60 and metrics['seam_neighbor_max_step_dbfs'] <= -60):
        failures.append('loop seam/duration/pitch')
    metrics['hard_check_failures'] = failures
    return metrics


def make_demo(output, clips):
    y = np.zeros(RATE * 30)
    # A dry 30-second level-preserving sequence, with crossfaded surface changes.
    def event(stem, variation, at, gain=1.):
        add(y, clips[f'{stem}_{variation:02d}'].astype(np.float64) / 32768, at, gain)
    def bed(stem, begin, end, gain=1.):
        length = round((end - begin) * RATE)
        layer = np.empty(length)
        written = 0; variant = 1
        while written < length:
            source = clips[f'{stem}_{variant:02d}'].astype(np.float64) / 32768
            count = min(len(source), length - written)
            layer[written:written + count] = source[:count]
            written += count; variant = variant % 3 + 1
        fade = min(round(.12 * RATE), length // 2)
        layer[:fade] *= np.linspace(0, 1, fade)
        layer[-fade:] *= np.linspace(1, 0, fade)
        add(y, layer, begin, gain)
    bed('skate_rolling_smooth', 0, 7.5, .65)
    for at in [.25, 1.35, 2.4]: event('skate_push', 1 + int(at) % 3, at)
    event('skate_ollie', 1, 3.5); event('skate_landing', 1, 4.15)
    event('skate_seam', 1, 6.2)
    bed('skate_rolling_rough', 7.35, 12.5, .65); bed('skate_grit', 7.35, 12.5)
    event('skate_ollie', 2, 8.8); event('skate_drop', 1, 9.5)
    event('skate_grind_in', 1, 12.6); bed('skate_grind', 12.63, 16.35, .70); event('skate_grind_out', 1, 16.3)
    bed('skate_rolling_wood', 16.45, 22.25, .65)
    event('skate_slide_in', 1, 18.4); bed('skate_deckslide', 18.48, 20.5, .75)
    event('skate_scrape', 1, 21.3)
    bed('skate_rolling_smooth', 22.3, 27.3, .65); bed('skate_whirr', 22.3, 27.3)
    event('skate_ollie', 3, 24.0); event('skate_landing', 3, 24.7)
    event('skate_slam', 1, 27.1); event('skate_bail', 1, 27.8)
    # A single documented master attenuation preserves all relative clip levels.
    attenuation = min(1., PEAK / max(np.max(np.abs(y)), 1e-12))
    y *= attenuation
    fade = round(.03 * RATE)
    y[:fade] *= np.linspace(0, 1, fade); y[-fade:] *= np.linspace(1, 0, fade)
    y = zero_dc_float(y)
    pcm = np.rint(y * 32768).astype(np.int32)
    total = int(np.sum(pcm, dtype=np.int64)); candidates = np.flatnonzero((np.abs(pcm) > 3) & (np.abs(pcm) < 29000))
    if total: pcm[candidates[:abs(total)]] -= 1 if total > 0 else -1
    pcm[0] = pcm[-1] = 0
    path = output.parent / 'demo.wav'
    write_wave(path, pcm.astype('<i2'))
    metrics = measurements(path, 'demo', float(METER.integrated_loudness(pcm / 32768)))
    metrics['master_attenuation_db'] = db(attenuation)
    metrics['timeline'] = '0-7.5s smooth/push/ollie/landing;7.35-12.5s rough/grit/ollie/drop;12.6-16.35s grind;16.45-22.25s wood/slide/scrape;22.3-27.3s smooth/whirr/ollie/landing;27.1-30s slam/bail'
    (output.parent / 'Demo.metrics.json').write_text(json.dumps(metrics, indent=2) + '\n')
    return metrics



def verify_pack(output):
    """Read the delivered WAV files afresh; refuse corrupt or extra/missing assets."""
    output = output.resolve()
    manifest = json.loads((output / 'manifest.json').read_text())
    expected = {f'{stem}_{variant:02d}.wav' for stem in STEMS for variant in range(1, 4)}
    actual = {path.name for path in output.glob('*.wav')}
    failures = []
    if actual != expected:
        failures.append('asset set: missing=' + str(sorted(expected - actual)) + ',extra=' + str(sorted(actual - expected)))
    if not manifest.get('passed') or manifest.get('license') != 'CC0-1.0':
        failures.append('manifest status/license')
    previous = {item['file']: item for item in manifest.get('files', [])}
    results = []
    for stem, (kind, seconds, target, _) in STEMS.items():
        for variant in range(1, 4):
            name = f'{stem}_{variant:02d}.wav'
            if name not in actual:
                continue
            item = measurements(output / name, kind, target)
            if abs(item['seconds'] - seconds) > 1 / RATE:
                item['hard_check_failures'].append('duration')
            if name not in previous or item['sha256'] != previous[name]['sha256']:
                item['hard_check_failures'].append('SHA readback')
            results.append({'file': name, **item})
            for failure in item['hard_check_failures']:
                failures.append(name + ': ' + failure)
    demo_path = output.parent / 'demo.wav'
    if not demo_path.is_file():
        failures.append('missing 30s demo')
    else:
        demo = measurements(demo_path, 'demo', manifest['demo']['integrated_lufs'])
        if abs(demo['seconds'] - 30) > 1 / RATE or demo['sha256'] != manifest['demo']['sha256'] or demo['hard_check_failures']:
            failures.append('demo duration/hash/format/peak/DC')
    report = {'passed': not failures, 'assets_checked': len(results), 'hard_check_failures': failures, 'measurements': results}
    (output.parent / 'verification.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'passed': not failures, 'assets_checked': len(results), 'hard_check_failures': failures}, indent=2))
    return 0 if not failures else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', default='skate_sounds', type=Path)
    parser.add_argument('--verify-only', action='store_true', help='Read and measure the existing delivered files; do not regenerate them.')
    args = parser.parse_args()
    output = args.output.resolve()
    if args.verify_only:
        return verify_pack(output)
    output.mkdir(parents=True, exist_ok=True)
    (output / 'manifest.json').write_text(json.dumps({'passed': False, 'in_progress': True}) + '\n')
    manifest = {'schema_version': 1, 'license': 'CC0-1.0', 'origin': 'Original authored procedural synthesis. No recordings, game audio, commercial packs or external sound sources.', 'generator': 'generate_skate_sounds.py', 'base_seed': SEED, 'sample_rate': RATE, 'channels': 1, 'encoding': 'PCM_16', 'medium_speed_metres_per_second': 4, 'loudness_measurement': 'pyloudnorm Meter: BS.1770 K weighting, 400ms blocks, -70 LUFS absolute gate, -10 LU relative gate, post-PCM samples', 'dependencies': {name: importlib.metadata.version(name) for name in ['numpy', 'scipy', 'pyloudnorm']}, 'files': []}
    clips = {}; failures = []
    for stem_index, (stem, (kind, seconds, target, description)) in enumerate(STEMS.items()):
        for variation in range(1, 4):
            seed = SEED + stem_index * 1009 + variation * 7919
            rng = np.random.default_rng(seed)
            raw = loop(stem, rng, seconds) if kind == 'loop' else one_shot(stem, rng, seconds)
            pcm, drive = finish(raw, target, kind == 'loop')
            name = f'{stem}_{variation:02d}'
            path = output / (name + '.wav'); write_wave(path, pcm); clips[name] = pcm
            metrics = measurements(path, kind, target)
            manifest['files'].append({'file': path.name, 'stem': stem, 'variation': variation, 'type': kind, 'seed': seed, 'source': 'own synthesis', 'description': description, 'soft_saturation_drive': drive, **metrics})
            if metrics['hard_check_failures']: failures.append(path.name + ': ' + ', '.join(metrics['hard_check_failures']))
            print(f"{path.name}: {metrics['integrated_lufs']:.3f} LUFS, peak {metrics['peak_dbfs']:.3f} dBFS, DCsum {metrics['dc_integer_sum']}, {'FAIL' if metrics['hard_check_failures'] else 'PASS'}", flush=True)
    # Prove content variation; byte-identical variants are refused.
    for stem in STEMS:
        hashes = {item['sha256'] for item in manifest['files'] if item['stem'] == stem}
        if len(hashes) != 3: failures.append(stem + ': duplicate variation')
    if len(manifest['files']) != 54 or len(list(output.glob('*.wav'))) != 54: failures.append('expected exactly 54 asset WAVs')
    demo = make_demo(output, clips)
    if demo['hard_check_failures'] or abs(demo['seconds'] - 30) > 1 / RATE: failures.append('30-second demo hard check')
    manifest['demo'] = {'file': '../demo.wav', **demo}
    manifest['hard_check_failures'] = failures
    manifest['passed'] = not failures
    manifest['generator_sha256'] = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
    (output / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    license_text = '''CC0 1.0 Universal\n\nTo the extent possible under law, the author dedicates all copyright and related\nrights in these original synthesized skateboard sounds, the demonstration and\ngenerate_skate_sounds.py to the public domain under CC0 1.0 Universal.\n\nNo attribution is required. The sounds may be copied, modified and used commercially.\nNo game recordings, commercial packs or external sound recordings were used.\nNo warranty is provided. This dedication does not change the licenses of Python,\nNumPy, SciPy or pyloudnorm, which are tools and are not embedded in the WAV data.\n\nFull CC0 terms: https://creativecommons.org/publicdomain/zero/1.0/legalcode\nSPDX-License-Identifier: CC0-1.0\n'''
    (output / 'LICENSE.txt').write_text(license_text)
    print(json.dumps({'assets': len(manifest['files']), 'passed': not failures, 'failures': failures}, indent=2))
    return 0 if not failures else 1


if __name__ == '__main__':
    sys.exit(main())
