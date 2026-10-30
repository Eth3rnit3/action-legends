"""Bells of the Antipape (Avignon boss): the warning chime over a marked circle and the bell that crashes on it.
A church bell is a few inharmonic partials (hum, prime, tierce, quint, nominal) that ring down at their own pace.
usage: cloches.py [OUT_DIR]  (defaults to Assets/Resources/Audio/Sfx/avignon)"""
import sys
from pathlib import Path
import numpy as np
from scipy.signal import butter, sosfilt
from sfx import SR, rng, save

# Partials of a tuned bell, as ratios of the prime, with their level and how long they ring (s).
PARTIALS = [(.5, .5, 1.6), (1, 1, 1.1), (1.2, .6, .8), (1.5, .35, .6), (2, .7, .5), (2.51, .25, .3), (3.01, .2, .22)]


def bell(f, length, strike=.004, bright=1.0):
    t = np.arange(int(length * SR)) / SR
    s = np.zeros_like(t)
    for ratio, level, ring in PARTIALS:
        s += level * (bright if ratio > 1.5 else 1) * np.sin(2 * np.pi * f * ratio * t + rng.uniform(0, 6)) * np.exp(-t / (ring * length))
    return s * np.minimum(1, t / strike)


def thud(length):
    n = int(length * SR)
    t = np.arange(n) / SR
    noise = sosfilt(butter(2, 500, 'low', fs=SR, output='sos'), rng.standard_normal(n))
    return noise * np.exp(-t / .07) * 2 + np.sin(2 * np.pi * (70 - 30 * t) * t) * np.exp(-t / .12)


SOUNDS = {
    # Two quick high strokes, like the alarm bell of a belfry: the circle on the ground is a bell to come.
    'cloche-alerte': lambda: np.concatenate([bell(1320, .16, bright=.8), bell(1320, .34, bright=.8)]) * .8,
    # A heavy bell crashing on the paving: thud, low clang, a cracked partial that beats against it.
    'cloche-impact': lambda: thud(.6) * .8 + bell(196, .6, strike=.002) + .35 * bell(203, .6, strike=.002, bright=.5),
    # The crosier shot: a short bright chime sliding up, light enough to be repeated in a sweep.
    'crosse-tir': lambda: bell(880, .18, strike=.002) * np.linspace(.6, 1, int(.18 * SR)),
}

if __name__ == '__main__':
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[2] / 'Assets/Resources/Audio/Sfx/avignon'
    out.mkdir(parents=True, exist_ok=True)
    for name, make in SOUNDS.items():
        save(out / f'{name}.wav', make())
