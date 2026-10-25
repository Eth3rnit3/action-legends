"""Arcade FM arrangement of La Coupo Santo for the Marseille mission.
Traditional Provençal air (the 17th-century noël "Guihaume, Tòni, Pèire", sung to Frédéric Mistral's
1867 text), public domain. Only the melody is used, in A-flat major, 6/8; the harmony is our own.
usage: coupo_santo.py [OUT.wav bpm stage|boss]  (no arguments renders both game tracks)"""
import sys
from pathlib import Path
import numpy as np
from scipy.signal import butter, sosfilt
from fmsynth import SR, Mix, fm, drum_cached, pingpong, reverb, master, write_wav, chord

# (midi, beats) with a quarter note as the beat; one 6/8 bar is 3 beats. Form A A' B C, 8 bars each.
# Each section ends with the pickup of the next one, so the 96 beats loop on themselves.
A = [(68, 1.5), (68, 1.5), (68, 1.5), (63, 1.5), (68, 3), (68, 1.5), (70, 1), (72, .5),
     (70, 1.5), (68, 1.5), (67, 1.5), (65, 1.5), (63, 4.5)]
MELODY = (A + [(63, 1), (63, .5)]
          + A + [(72, 1), (73, .5)]
          + [(75, 3), (75, 1.5), (72, 1), (73, .5), (75, 3), (75, 3), (75, 3), (73, 1.5), (72, 1.5), (73, 4.5), (73, 1), (73, .5)]
          + [(72, 1.5), (75, 1.5), (73, 1.5), (72, 1.5), (70, 4.5), (68, 3), (67, 1), (68, .5), (70, 1), (72, .5), (68, 6), (63, 1), (63, .5)])
assert abs(sum(d for _, d in MELODY) - 96) < 1e-9
# One or two chords per bar, each lasting a dotted quarter when there are two.
CHORDS = ['Ab', 'Ab Eb', 'Ab', 'Ab', 'Eb Ab', 'Eb Db', 'Ab', 'Ab Eb',
          'Ab', 'Ab Eb', 'Ab', 'Ab', 'Eb Ab', 'Eb Db', 'Ab', 'Ab',
          'Ab', 'Ab Db', 'Cm', 'Ab', 'Eb', 'Db Ab', 'Db', 'Bbm Eb7',
          'Ab', 'Db Ab', 'Eb', 'Eb Fm', 'Fm Eb', 'Eb7 Ab', 'Ab', 'Ab Eb']
CHORDS = [(c.split() * 2)[:2] for c in CHORDS]
BARS = len(CHORDS)
REFRAIN = 16  # zero-based bar where the high B section starts
# Boss mode turns to A-flat harmonic minor: C becomes C-flat, F becomes F-flat; G stays as the leading tone.
MINOR = {72: 71, 65: 64}
MINOR_CHORDS = {'Ab': 'Abm', 'Db': 'Dbm', 'Cm': 'B', 'Fm': 'E', 'Bbm': 'Dbm'}


def render(out, bpm=174, mode='stage'):
    beat = 60 / bpm
    bar_s = 3 * beat
    passes = 2
    mix = Mix(passes * BARS * bar_s + 2.0)
    boss = mode == 'boss'

    for p in range(passes):
        t0 = p * BARS * bar_s
        # Lead: galoubet fife an octave up first, then brass with a fife descant; growling lead for the boss.
        cur = 0.0
        for n, d in MELODY:
            n = MINOR.get(n, n) if boss else n
            gate = d * beat * (.9 if d >= 1 else .75)
            st = t0 + cur * beat
            if boss:
                mix.add('lead', fm(n - 12, gate, 'growl'), st, .30, -.1)
                if p == 1:
                    mix.add('lead', fm(n + 12, gate, 'fife'), st, .12, .35)
            elif p == 0:
                mix.add('lead', fm(n + 12, gate, 'fife'), st, .30, -.05)
            else:
                mix.add('lead', fm(n, gate, 'brass'), st, .30, -.1)
                mix.add('lead', fm(n + 24, gate, 'fife'), st, .10, .35)
            cur += d
        for bi in range(BARS):
            bt = t0 + bi * bar_s
            for half, name in enumerate(CHORDS[bi]):
                if boss:
                    name = MINOR_CHORDS.get(name, name)
                root, shape = chord(name)
                hs = bt + half * 1.5 * beat
                # Bass in eighths: root, octave, fifth, like a tambourin-and-bass farandole.
                low = root if root <= 44 else root - 12
                for k, step in enumerate((0, 12, shape[2])):
                    mix.add('bass', fm(low + step, beat * .32, 'slap'), hs + k * beat / 2, .20)
                tones = [root + 24 + s for s in shape[:3]]
                if p == 1 or boss:
                    seq = tones + [tones[0] + 12]
                    for k in range(6):
                        mix.add('arp', fm(seq[k % len(seq)] + (0 if boss else 12), beat * .2, 'stab' if boss else 'epiano'), hs + k * beat / 4, .035, .55 if k % 2 else -.55)
                if bi >= REFRAIN or boss:
                    for k in (.5, 1):
                        for tn in tones:
                            mix.add('stab', fm(tn + 12, beat * .25, 'stab'), hs + k * beat, .045, 0)
                else:
                    for tn in tones:
                        mix.add('pad', fm(tn + 12, 1.5 * beat * .95, 'pad'), hs, .035, (-.6 if tn % 2 else .6))
            # Drums in 6/8: kick on each dotted quarter, snare on the second, eighth-note hats.
            for k in range(6):
                st = bt + k * beat / 2
                if k in (0, 3) or (boss and k in (2, 5)) or (p == 1 and k == 5):
                    mix.add('kick', drum_cached('kick'), st, .75)
                if k == 3:
                    mix.add('drums', drum_cached('snare'), st, .5, .1)
                mix.add('drums', drum_cached('ohat' if k == 5 and not boss else 'hat'), st, .06 if k % 3 else .08, .4)
                if boss:
                    mix.add('drums', drum_cached('hat'), st + beat / 4, .04, .4)
            # The tambourin: a dry tom answering every bar.
            mix.add('drums', drum_cached('tom2'), bt + beat * 2.5, .22, -.35)
            if bi in (7, 15, 23, BARS - 1):
                for k in range(6):
                    mix.add('drums', drum_cached('snare'), bt + 2 * beat + k * beat / 6, .14 + .04 * k, .1)
            if bi in (0, REFRAIN):
                mix.add('drums', drum_cached('crash'), bt, .28, -.3)

    def eq(x, lo=None, hi=None):
        if lo:
            x = sosfilt(butter(2, lo, 'high', fs=SR, output='sos'), x, axis=0)
        if hi:
            x = sosfilt(butter(2, hi, 'low', fs=SR, output='sos'), x, axis=0)
        return x
    lead = pingpong(eq(mix.bus('lead'), 220, 7500), beat * 1.5, fb=.35, wet=.2)
    bass = eq(mix.bus('bass'), 40, 2500)
    music = (lead + bass + reverb(eq(mix.bus('arp') + mix.bus('pad') + mix.bus('stab'), 300, 6000), .25)
             + reverb(eq(mix.bus('drums'), 150, 8500), .08, .6) + mix.bus('kick'))
    music = reverb(music, .06)
    if boss:
        music = sosfilt(butter(2, 6500, 'low', fs=SR, output='sos'), music, axis=0)
    # Fold the effect tail back onto the start for a seamless loop.
    loop_n = int(passes * BARS * bar_s * SR)
    tail = music[loop_n:]
    music = music[:loop_n].copy()
    music[:len(tail)] += tail
    music = master(music)
    write_wav(out, music, fade=False)
    print('  loop seam jump', round(float(np.abs(music[-1] - music[0]).max()), 4))
    print(out, round(loop_n / SR, 1), 's')


if __name__ == '__main__':
    if len(sys.argv) > 1:
        render(sys.argv[1], float(sys.argv[2]) if len(sys.argv) > 2 else 174, sys.argv[3] if len(sys.argv) > 3 else 'stage')
    else:
        out = Path(__file__).resolve().parents[2] / 'Assets/Resources/Audio/Music'
        out.mkdir(parents=True, exist_ok=True)
        render(out / 'coupo-santo-arcade.wav', 174, 'stage')
        render(out / 'coupo-santo-boss.wav', 192, 'boss')
