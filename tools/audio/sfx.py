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
    # Lettered weapons (lot 1b), also last for the same reason.
    'tir-H-lourde': lambda: layer((1, gen('noise', 2000, 380, .075, punch=.8, lp=5200, crush=(16000, 8))),
                                  (.75, gen('square', 260, 70, .07, duty=.35)),
                                  (.35, gen('noise', 7000, 2500, .025, hp=3000))),
    'tir-S-pompe': lambda: np.concatenate([layer((1, gen('noise', 1800, 90, .34, sustain=.01, punch=1, curve=1.5, lp=4500, crush=(11025, 8))),
                                                 (.6, gen('tri', 120, 40, .3))),
                                           np.zeros(int(.05 * SR)),
                                           gen('square', 900, 700, .03, duty=.2, punch=.3),
                                           np.zeros(int(.03 * SR)),
                                           gen('square', 1100, 850, .03, duty=.2, punch=.3)]),
    'tir-F-flammes': lambda: layer((1, gen('noise', 700, 1100, .13, attack=.02, sustain=.05, punch=.2, curve=.8, lp=2600, crush=(11025, 8))),
                                   (.3, gen('noise', 3000, 2000, .1, attack=.02, hp=1500))),
    'tir-L-laser': lambda: layer((.8, gen('square', 1750, 1500, .09, attack=.004, sustain=.05, duty=.18, vib=(55, .05), punch=.1, crush=(22050, 8))),
                                 (.5, gen('saw', 880, 760, .09, attack=.004, sustain=.05, lp=6000, crush=(22050, 8)))),
    # A freed hostage (lot 2a): the rope snaps, then a cheerful rising jingle. Last, like the others.
    'otage-libere': lambda: np.concatenate([gen('noise', 3200, 700, .08, sustain=.01, punch=.9, hp=900, crush=(16000, 8)),
                                            np.zeros(int(.02 * SR)),
                                            gen('square', 784, 784, .07, duty=.25, punch=.2, sustain=.03),
                                            gen('square', 1047, 1047, .07, duty=.25, punch=.2, sustain=.03),
                                            gen('square', 1319, 1319, .07, duty=.25, punch=.2, sustain=.03),
                                            gen('square', 1568, 1568, .22, duty=.25, punch=.2, sustain=.08, vib=(12, .02))]),
    # Staged encounters (lot 2b), last again: an enemy's surprise, a truck braking then driving off, a parachute opening, a door.
    'surprise': lambda: np.concatenate([gen('square', 880, 1320, .05, duty=.25, punch=.4, sustain=.01),
                                        gen('square', 1320, 1980, .09, duty=.25, punch=.4, sustain=.03, vib=(30, .03))]),
    'camion-frein': lambda: np.concatenate([layer((1, gen('saw', 70, 55, .45, attack=.02, sustain=.3, lp=900, crush=(11025, 8))),
                                                  (.45, gen('square', 2300, 1900, .4, attack=.05, sustain=.25, duty=.1, vib=(20, .02), hp=1500))),
                                            gen('noise', 600, 200, .25, sustain=.05, lp=1800, crush=(11025, 8))]),
    'parachute': lambda: layer((1, gen('noise', 1200, 3000, .3, attack=.06, sustain=.05, curve=.7, lp=4000, crush=(11025, 8))),
                               (.5, gen('noise', 300, 150, .35, attack=.02, punch=.6, lp=900))),
    'porte': lambda: np.concatenate([gen('square', 180, 260, .16, duty=.4, vib=(18, .25), attack=.02, lp=1500),
                                     gen('noise', 900, 120, .18, sustain=.01, punch=1, lp=2500, crush=(11025, 7))]),
    # Destructible props (lot 3a), last again: a knock on wood, a clang on metal, then each breaking, glass shattering, hay.
    'bois-coup': lambda: layer((1, gen('noise', 900, 300, .08, sustain=.005, punch=1, lp=2200, crush=(11025, 8))),
                               (.6, gen('tri', 220, 140, .07, punch=.6))),
    'bois-casse': lambda: np.concatenate([layer((1, gen('noise', 1400, 200, .16, sustain=.01, punch=1, lp=3000, crush=(11025, 8))),
                                                (.5, gen('tri', 160, 70, .14, punch=.6))),
                                          gen('noise', 700, 150, .12, sustain=.01, punch=.8, lp=2000, crush=(11025, 7)),
                                          gen('noise', 1100, 250, .2, sustain=.02, punch=.5, lp=2400, crush=(11025, 7))]),
    'metal-coup': lambda: layer((.8, gen('square', 1300, 1150, .14, duty=.2, vib=(70, .06), punch=.5)),
                                (.5, gen('noise', 6000, 2500, .04, hp=2500)), (.4, gen('sine', 640, 600, .16))),
    'metal-casse': lambda: np.concatenate([layer((1, gen('noise', 2200, 300, .3, sustain=.02, punch=1, curve=1.4, crush=(11025, 8))),
                                                 (.6, gen('square', 520, 180, .3, duty=.3, vib=(40, .1)))),
                                           layer((.6, gen('square', 900, 700, .25, duty=.15, vib=(55, .08))), (.4, gen('noise', 3000, 800, .2, hp=1200)))]),
    'verre-casse': lambda: np.concatenate([gen('noise', 9000, 5000, .05, sustain=.005, punch=1, hp=3000, crush=(22050, 8)),
                                           layer((.7, gen('noise', 7000, 3500, .35, sustain=.02, punch=.4, curve=.8, hp=2500, crush=(22050, 8))),
                                                 (.35, gen('square', 3300, 2900, .3, duty=.1, vib=(90, .05), hp=2000)))]),
    'foin-casse': lambda: layer((1, gen('noise', 1800, 900, .35, attack=.02, sustain=.05, punch=.2, curve=.8, lp=3500, hp=400, crush=(11025, 8))),
                                (.4, gen('noise', 400, 150, .25, punch=.8, lp=900))),
    # Drivable vehicles (lot 3b), last again: the engine's putter while it drives, getting in (starter and rev), getting out
    # (a hatch), the alarm and pop of an ejection, the cannon's boom and the turret gun's burst.
    'vehicule-moteur': lambda: np.concatenate([layer((1, gen('saw', 62, 70, .11, attack=.005, sustain=.05, punch=.5, lp=700, crush=(11025, 8))),
                                                      (.4, gen('noise', 300, 200, .1, punch=.6, lp=600))) for _ in range(4)]),
    'vehicule-monte': lambda: np.concatenate([gen('noise', 800, 200, .08, sustain=.01, punch=1, lp=2000, crush=(11025, 7)),
                                              layer((1, gen('saw', 45, 50, .25, attack=.01, sustain=.1, vib=(16, .4), lp=600)), (.3, gen('noise', 250, 150, .25, lp=500))),
                                              gen('saw', 55, 150, .4, attack=.02, sustain=.15, curve=.6, lp=1100, crush=(11025, 8))]),
    'vehicule-sortie': lambda: np.concatenate([gen('square', 330, 520, .07, duty=.3, punch=.5, lp=2500),
                                               gen('noise', 1200, 300, .1, sustain=.01, punch=.9, lp=2500, crush=(11025, 7))]),
    'vehicule-ejection': lambda: np.concatenate([gen('square', 1400, 1400, .07, duty=.25, sustain=.04), np.zeros(int(.03 * SR)),
                                                 gen('square', 1050, 1050, .07, duty=.25, sustain=.04), np.zeros(int(.03 * SR)),
                                                 layer((1, gen('noise', 2600, 400, .22, sustain=.01, punch=1, crush=(11025, 8))),
                                                       (.6, gen('square', 300, 900, .2, duty=.4, curve=.6)))]),
    'vehicule-canon': lambda: layer((1, gen('noise', 1500, 60, .55, sustain=.02, punch=1.2, curve=1.6, lp=2600, crush=(11025, 8))),
                                    (.8, gen('sine', 110, 38, .45, punch=1, curve=1.3)), (.3, gen('square', 220, 70, .2, duty=.4))),
    'vehicule-rafale': lambda: layer((1, gen('noise', 2600, 700, .09, sustain=.006, punch=1, lp=5000, crush=(11025, 8))),
                                     (.6, gen('square', 260, 120, .08, duty=.35, punch=.7)), (.3, gen('sine', 90, 60, .08))),
}

if __name__ == '__main__':
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[2] / 'Assets/Resources/Audio/Sfx'
    out.mkdir(parents=True, exist_ok=True)
    for name, fn in SOUNDS.items():
        save(out / f'{name}.wav', fn())
