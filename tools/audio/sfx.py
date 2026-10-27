"""sfxr-style retro sound effects: oscillator + pitch slide + envelope + filter + bitcrush.
usage: sfx.py [OUT_DIR]  (defaults to Assets/Resources/Audio/Sfx)"""
import sys
from pathlib import Path
import numpy as np, wave
from scipy.signal import butter, sosfilt, resample_poly

SR = 44100
rng = np.random.default_rng(1990)


def osc(kind, freq, duty=.5):
    ph = np.cumsum(freq) / SR % 1.0
    if kind == 'square':
        d = duty if np.isscalar(duty) else duty
        return np.where(ph < d, 1.0, -1.0)
    if kind == 'saw':
        return 2 * ph - 1
    if kind == 'tri':
        return 4 * np.abs(ph - .5) - 1
    if kind == 'sine':
        return np.sin(2 * np.pi * ph)
    if kind == 'noise':
        # NES-style noise: random value held for one period of `freq`.
        step = np.floor(np.cumsum(freq) / SR).astype(int)
        vals = rng.uniform(-1, 1, step.max() + 2)
        return vals[step]
    raise ValueError(kind)


def env(n, attack, sustain, decay, punch=0.0):
    a, s, d = (int(x * SR) for x in (attack, sustain, decay))
    e = np.concatenate([np.linspace(0, 1, max(a, 1)), np.ones(s) * (1 + punch), np.linspace(1 + punch, 0, max(d, 1)) ** 1.6])
    return np.pad(e, (0, max(0, n - len(e))))[:n] / (1 + punch)


def gen(wave_kind, f0, f1, length, attack=.002, sustain=.02, decay=None, punch=.3, curve=2.0,
        vib=(0, 0), duty=.5, duty_slide=0.0, arp=None, lp=None, hp=None, crush=(11025, 8)):
    n = int(length * SR)
    t = np.arange(n) / SR
    x = (t / length) ** (1 / curve)
    freq = f0 + (f1 - f0) * x
    if vib[0]:
        freq = freq * (1 + vib[1] * np.sin(2 * np.pi * vib[0] * t))
    if arp:
        mult, at = arp
        freq = np.where(t >= at, freq * mult, freq)
    d = np.clip(duty + duty_slide * t / length, .05, .95)
    s = osc(wave_kind, np.maximum(freq, 1), d)
    s *= env(n, attack, sustain, decay if decay is not None else length - attack - sustain, punch)
    if lp:
        s = sosfilt(butter(2, lp, 'low', fs=SR, output='sos'), s)
    if hp:
        s = sosfilt(butter(2, hp, 'high', fs=SR, output='sos'), s)
    rate, bits = crush
    held = s[(np.floor(np.arange(n) / (SR / rate)) * (SR / rate)).astype(int)]
    q = 2 ** (bits - 1)
    return np.round(held * q) / q


def layer(*parts):
    n = max(len(p) for _, p in parts)
    out = np.zeros(n)
    for g, p in parts:
        out[:len(p)] += g * p
    return out


def save(path, s):
    s = s / max(1e-6, np.abs(s).max()) * .9
    fade = int(.003 * SR)
    s[-fade:] *= np.linspace(1, 0, fade)
    s = np.clip(resample_poly(s, 1, 2), -1, 1)  # 44.1 kHz render, 22.05 kHz like the rest of the game
    with wave.open(str(path), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR // 2)
        w.writeframes((s * 32767).astype('<i2').tobytes())
    print(f'{path.name:28s} {len(s) / (SR // 2):.2f}s')


SOUNDS = {
    # Machine-gun shots: short, spammed constantly, so kept dry and not too bright.
    'tir-A-mitraille': lambda: layer((1, gen('noise', 2600, 500, .09, punch=.6, lp=6000, crush=(16000, 8))),
                                     (.6, gen('square', 420, 110, .07, duty=.3))),
    'tir-B-laser-snes': lambda: gen('square', 1200, 180, .12, duty=.25, duty_slide=.4, punch=.4),
    'tir-C-blaster-cps1': lambda: layer((.9, gen('noise', 4000, 900, .11, punch=.5, crush=(22050, 8))),
                                        (.7, gen('saw', 300, 70, .1, lp=3000))),
    'tir-D-pistolet-sec': lambda: layer((1, gen('noise', 6000, 1200, .06, sustain=.005, punch=1, crush=(11025, 6))),
                                        (.5, gen('tri', 180, 60, .06))),
    # Bullet impact on enemy.
    'impact-A-metal': lambda: layer((.8, gen('square', 1800, 900, .08, duty=.15, vib=(60, .15))),
                                    (.6, gen('noise', 8000, 3000, .05, hp=2000))),
    'impact-B-tchak': lambda: layer((1, gen('noise', 1500, 300, .1, punch=.8, lp=4000)), (.5, gen('square', 260, 90, .08))),
    'impact-C-ping': lambda: gen('square', 2400, 2400, .12, duty=.5, arp=(1.5, .03), punch=.2),
    # Explosions / special attack.
    'explosion-A-grosse': lambda: layer((1, gen('noise', 900, 60, .9, sustain=.05, punch=.8, curve=1.4, crush=(11025, 8))),
                                        (.5, gen('tri', 90, 30, .7))),
    'explosion-B-courte': lambda: gen('noise', 1400, 120, .45, sustain=.02, punch=1, curve=1.6, crush=(8000, 7)),
    'explosion-C-bombe-neo': lambda: layer((1, gen('noise', 600, 40, 1.3, sustain=.1, punch=.5, curve=1.2, lp=5000, crush=(11025, 8))),
                                           (.7, gen('sine', 70, 25, 1.0)),
                                           (.4, gen('noise', 5000, 1000, .15, hp=3000))),
    # Extras the game does not use yet.
    'bonus-ramasse': lambda: np.concatenate([gen('square', f, f, .06, duty=.5, punch=.2, sustain=.03) for f in (988, 1319, 1976)]),
    'bonus-coin': lambda: np.concatenate([gen('square', 988, 988, .07, punch=.1), gen('square', 1319, 1319, .35, duty=.5, punch=.1)]),
    'joueur-touche': lambda: gen('square', 600, 90, .3, duty=.5, vib=(30, .25), punch=.5),
    'boss-explose': lambda: np.concatenate([layer((1, gen('noise', 700 - 80 * k, 60, .35, punch=.8, crush=(11025, 8)))) for k in range(4)]
                                          + [gen('noise', 500, 20, 1.6, sustain=.2, curve=1.1, crush=(8000, 8))]),
    'menu-select': lambda: gen('square', 1760, 1760, .07, duty=.25, arp=(2, .035)),
    'menu-start': lambda: np.concatenate([gen('square', f, f * 1.01, .09, duty=.5, sustain=.05) for f in (523, 659, 784, 1047)]),
    'roquette-depart': lambda: layer((1, gen('noise', 300, 3000, .5, attack=.05, curve=.8, lp=7000)), (.4, gen('saw', 80, 200, .5))),
    # Close combat (lot 1a): kept last so the noise of the sounds above stays the same.
    'couteau': lambda: layer((1, gen('noise', 2500, 7000, .13, attack=.01, sustain=.01, punch=.4, curve=.7, hp=1800, crush=(22050, 8))),
                             (.45, gen('square', 3100, 2600, .1, duty=.12, vib=(45, .04), punch=.2, crush=(22050, 8)))),
    'grenade-lancer': lambda: np.concatenate([gen('square', 2600, 2200, .025, duty=.3, punch=.5),
                                              layer((1, gen('noise', 500, 1900, .26, attack=.04, curve=.9, lp=3500, crush=(16000, 8))),
                                                    (.3, gen('tri', 260, 420, .22)))]),
}

if __name__ == '__main__':
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[2] / 'Assets/Resources/Audio/Sfx'
    out.mkdir(parents=True, exist_ok=True)
    for name, fn in SOUNDS.items():
        save(out / f'{name}.wav', fn())
