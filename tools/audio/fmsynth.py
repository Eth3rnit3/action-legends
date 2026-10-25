"""Tiny YM2151-flavoured FM synth + PCM-style drums + arcade effects (numpy/scipy)."""
import numpy as np, wave
from scipy.signal import lfilter, butter, sosfilt, resample_poly

SR = 44100
RNG = np.random.default_rng(1792)


def hz(n): return 440.0 * 2 ** ((n - 69) / 12)


# --- FM voices --------------------------------------------------------------
PATCHES = {
    # 3-op chain M2 -> M1 -> C. idx: modulation index at attack, settles to idx*sus_idx.
    'brass':  dict(r1=1, idx=3.0, sus_idx=.55, idec=.1, r2=1, idx2=1.1, a=.012, d=.22, s=.72, rel=.09, vib=.007, vibd=.22, bright=1.0),
    'lead2':  dict(r1=2, idx=2.2, sus_idx=.5, idec=.08, r2=1, idx2=.4, a=.004, d=.18, s=.6, rel=.07, vib=.006, vibd=.18, bright=1.0),
    'musette': dict(r1=2, idx=1.6, sus_idx=.8, idec=.3, r2=3, idx2=.35, a=.015, d=.1, s=.85, rel=.06, vib=.004, vibd=.1, bright=.8, detune=9),
    'slap':   dict(r1=1, idx=4.2, sus_idx=.18, idec=.028, r2=1, idx2=.9, a=.002, d=.16, s=.45, rel=.02, vib=0, vibd=1, bright=1.0),
    'epiano': dict(r1=1, idx=1.3, sus_idx=.25, idec=.04, r2=1, idx2=.3, a=.001, d=.12, s=.15, rel=.05, vib=0, vibd=1, bright=1.0),
    'stab':   dict(r1=1, idx=2.6, sus_idx=.3, idec=.06, r2=1, idx2=.6, a=.004, d=.09, s=.25, rel=.05, vib=0, vibd=1, bright=1.0),
    # Darker, growling lead for the boss: modulator an octave down, slow brightness decay.
    'growl':  dict(r1=.5, idx=4.0, sus_idx=.6, idec=.25, r2=1, idx2=1.4, a=.006, d=.2, s=.8, rel=.08, vib=.009, vibd=.15, bright=1.0, detune=7),
    'pad':    dict(r1=1, idx=1.1, sus_idx=1, idec=1, r2=2, idx2=.2, a=.12, d=.3, s=.8, rel=.25, vib=.003, vibd=.3, bright=.6, detune=6),
}


def _env(t, gate, a, d, s, rel):
    e = np.where(t < a, t / a, np.where(t < a + d, 1 - (1 - s) * (t - a) / d, s))
    at_gate = np.interp(gate, t, e) if gate < t[-1] else e[-1]
    return np.where(t < gate, e, at_gate * np.clip(1 - (t - gate) / rel, 0, 1))


def fm(note, dur, patch, vel=1.0):
    p = PATCHES[patch] if isinstance(patch, str) else patch
    n = int((dur + p['rel']) * SR)
    t = np.arange(n) / SR
    out = np.zeros(n)
    for cents in ([0] if not p.get('detune') else [-p['detune'], p['detune']]):
        f = hz(note) * 2 ** (cents / 1200)
        vib = 1 + p['vib'] * np.sin(2 * np.pi * 5.6 * t) * np.clip((t - p['vibd']) / .15, 0, 1)
        ph = 2 * np.pi * np.cumsum(f * vib) / SR
        index = p['idx'] * vel * (p['sus_idx'] + (1 - p['sus_idx']) * np.exp(-t / p['idec']))
        m2 = p['idx2'] * np.sin(ph * p['r2'])
        out += np.sin(ph + index * p['bright'] * np.sin(ph * p['r1'] + m2))
    out /= 1 if not p.get('detune') else 2
    return out * _env(t, dur, p['a'], p['d'], p['s'], p['rel']) * vel


# --- PCM-style drums (crushed like an OKI ADPCM chip) -------------------------
def _crush(x, rate=15625, bits=8):
    step = SR / rate
    held = x[(np.floor(np.arange(len(x)) / step) * step).astype(int)]
    q = 2 ** (bits - 1)
    return np.round(held * q) / q


def _bp(x, lo, hi):
    return sosfilt(butter(2, [lo, hi], 'band', fs=SR, output='sos'), x)


def _hp(x, f):
    return sosfilt(butter(2, f, 'high', fs=SR, output='sos'), x)


def drum(kind):
    if kind == 'kick':
        t = np.arange(int(.28 * SR)) / SR
        f = 52 + 110 * np.exp(-t * 26)
        body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 9)
        click = _bp(RNG.uniform(-1, 1, len(t)), 1500, 5000) * np.exp(-t * 300) * .6
        return _crush(np.tanh((body + click) * 2.2), bits=10)
    if kind == 'snare':
        # Gated-reverb snare: body + noise burst, then a flat noise tail chopped at 140 ms.
        t = np.arange(int(.2 * SR)) / SR
        noise = _bp(RNG.uniform(-1, 1, len(t)), 500, 6000)
        body = (np.sin(2 * np.pi * np.cumsum(180 + 70 * np.exp(-t * 50)) / SR) * np.exp(-t * 22)
                + .5 * np.sin(2 * np.pi * np.cumsum(330 + 40 * np.exp(-t * 50)) / SR) * np.exp(-t * 30))
        gate = np.where(t < .14, .35 + .65 * np.exp(-t * 30), np.exp(-(t - .14) * 120) * .35)
        return _crush(np.tanh((noise * gate * .9 + body * 1.1) * 1.6), bits=10)
    if kind in ('hat', 'ohat'):
        t = np.arange(int((.05 if kind == 'hat' else .25) * SR)) / SR
        metal = sum(np.sign(np.sin(2 * np.pi * f * t)) for f in (317, 421, 553, 709, 811, 1013))
        s = _hp(metal * .15 + RNG.uniform(-1, 1, len(t)), 7000) * np.exp(-t * (70 if kind == 'hat' else 14))
        return _crush(s)
    if kind.startswith('tom'):
        base = {'tom1': 210, 'tom2': 150, 'tom3': 105}[kind]
        t = np.arange(int(.3 * SR)) / SR
        s = np.sin(2 * np.pi * np.cumsum(base * (1 + .8 * np.exp(-t * 25))) / SR) * np.exp(-t * 9)
        return _crush(np.tanh(s * 1.5 + RNG.uniform(-1, 1, len(t)) * np.exp(-t * 200) * .3))
    if kind == 'crash':
        t = np.arange(int(1.6 * SR)) / SR
        metal = sum(np.sin(2 * np.pi * f * t + 3 * np.sin(2 * np.pi * f * 1.41 * t)) for f in (523, 787, 1129, 1597))
        s = _hp(RNG.uniform(-1, 1, len(t)) + metal * .1, 4000) * np.exp(-t * 2.6)
        return _crush(s, bits=8)
    raise ValueError(kind)


DRUMS = {}


def drum_cached(k):
    if k not in DRUMS:
        DRUMS[k] = drum(k)
    return DRUMS[k]


# --- Mixer / effects ------------------------------------------------------------
class Mix:
    def __init__(self, seconds):
        self.n = int(seconds * SR)
        self.buses = {}

    def add(self, bus, sig, start, gain=1.0, pan=0.0):
        b = self.buses.setdefault(bus, np.zeros((self.n, 2)))
        i = int(start * SR)
        if i >= self.n or i + len(sig) <= 0:
            return
        j = min(self.n, i + len(sig))
        seg = sig[: j - i] * gain
        b[i:j, 0] += seg * np.sqrt((1 - pan) / 2)
        b[i:j, 1] += seg * np.sqrt((1 + pan) / 2)

    def bus(self, name):
        return self.buses.get(name, np.zeros((self.n, 2)))


def pingpong(x, delay_s, fb=.35, wet=.3):
    d = int(delay_s * SR)
    out = x.copy()
    l, r = np.zeros(len(x)), np.zeros(len(x))
    src = x.mean(axis=1)
    # Two taps feeding each other: L at d, R at 2d, decaying.
    for k in range(1, 6):
        g = wet * fb ** (k - 1)
        sh = d * k
        if sh >= len(x):
            break
        (l if k % 2 else r)[sh:] += src[:-sh] * g
    out[:, 0] += l
    out[:, 1] += r
    return out


def reverb(x, wet=.12, size=1.0):
    out = np.zeros_like(x)
    for ch in range(2):
        s = x[:, ch]
        acc = np.zeros(len(s))
        for dl, g in ((1557, .84), (1617, .83), (1491, .84), (1422, .83)):
            dl = int(dl * size) + ch * 23
            a = np.zeros(dl + 1); a[0] = 1; a[dl] = -g
            acc += lfilter([1], a, s)
        for dl in (225, 556):
            a = np.zeros(dl + 1); a[0] = 1; a[dl] = -.5
            b = np.zeros(dl + 1); b[0] = -.5; b[dl] = 1
            acc = lfilter(b, a, acc)
        out[:, ch] = acc / 4
    lp = butter(1, 5000, 'low', fs=SR, output='sos')
    return x + sosfilt(lp, out, axis=0) * wet


def master(x, bits=12):
    x = sosfilt(butter(2, 30, 'high', fs=SR, output='sos'), x, axis=0)
    x = sosfilt(butter(2, 11000, 'low', fs=SR, output='sos'), x, axis=0)
    x = np.tanh(x * 1.2)
    x /= max(1e-6, np.abs(x).max())
    q = 2 ** (bits - 1)
    x = np.round(x * .92 * q) / q  # 12-bit DAC grain
    return x


def write_wav(path, x, rate=22050, fade=True):
    if fade:  # loops skip this: the tail is already folded onto the head
        x = x.copy()
        n = int(.004 * SR)
        x[:n] *= np.linspace(0, 1, n)[:, None]
        x[-n:] *= np.linspace(1, 0, n)[:, None]
    # Resample with wrapped edges so a loop stays seamless at the game's 22.05 kHz.
    pad = 2048
    wrapped = np.concatenate([x[-pad:], x, x[:pad]]) if not fade else np.pad(x, ((pad, pad), (0, 0)))
    x = resample_poly(wrapped, rate, SR, axis=0)[pad * rate // SR: -(pad * rate // SR)]
    with wave.open(str(path), 'wb') as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(rate)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype('<i2').tobytes())


CHORD_SHAPES = {'': (0, 4, 7), 'm': (0, 3, 7), '7': (0, 4, 7, 10), 'dim7': (0, 3, 6, 9)}
ROOTS = {'C': 36, 'D': 38, 'Eb': 39, 'E': 40, 'F': 41, 'F#': 42, 'G': 43, 'A': 45, 'Bb': 46, 'B': 47}


def chord(name):
    root = name[:2] if name[:2] in ROOTS else name[0]
    return ROOTS[root], CHORD_SHAPES[name[len(root):]]
