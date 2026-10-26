"""Arcade FM arrangement of Sakura Sakura for the Tokyo mission.
Traditional Japanese song of the Edo period, public domain. Only the melody is used, in the
miyako-bushi mode on A (A B C E F), 4/4; the harmony, bass and drums are our own.
usage: sakura.py [OUT.wav bpm stage|boss]  (no arguments renders both game tracks)"""
import sys
from pathlib import Path
import numpy as np
from scipy.signal import butter, sosfilt
from fmsynth import SR, Mix, fm, drum_cached, pingpong, reverb, master, write_wav, chord

# (midi, beats) with a quarter note as the beat. Fourteen bars that loop on themselves.
CALL = [(69, 1), (69, 1), (71, 2), (69, 1), (69, 1), (71, 2)]
VERSE = [(69, 1), (71, 1), (72, 1), (71, 1), (69, 1), (71, .5), (69, .5), (65, 2),
         (64, 1), (60, 1), (64, 1), (65, 1), (64, 1), (64, .5), (60, .5), (59, 2)]
MELODY = CALL + VERSE + VERSE + CALL + [(64, 1), (65, 1), (71, .5), (69, .5), (65, 1), (64, 4)]
assert abs(sum(d for _, d in MELODY) - 56) < 1e-9
# One or two chords per bar, each lasting a half note when there are two.
CHORDS = ['Am', 'Am', 'F', 'Dm', 'Am F', 'Am E', 'F', 'Dm', 'Am F', 'Am E', 'Am', 'Am', 'Dm E', 'E7']
CHORDS = [(c.split() * 2)[:2] for c in CHORDS]
BARS = len(CHORDS)
# The boss version swaps the sweet sixth for a darker flat second (in-sen flavour): B becomes B-flat.
BOSS_NOTES = {71: 70, 59: 58}
BOSS_CHORDS = {'Dm': 'Bb', 'F': 'Bb'}


def render(out, bpm=150, mode='stage'):
    beat = 60 / bpm
    bar_s = 4 * beat
    passes = 3
    mix = Mix(passes * BARS * bar_s + 2.0)
    boss = mode == 'boss'

    for p in range(passes):
        t0 = p * BARS * bar_s
        # Lead: a plucked koto-like epiano first, then brass, then the fife an octave up over brass.
        cur = 0.0
        for n, d in MELODY:
            n = BOSS_NOTES.get(n, n) if boss else n
            gate = d * beat * (.9 if d >= 1 else .75)
            st = t0 + cur * beat
            if boss:
                mix.add('lead', fm(n - 12, gate, 'growl'), st, .30, -.1)
                if p > 0:
                    mix.add('lead', fm(n + 12, gate, 'fife'), st, .11, .35)
            elif p == 0:
                mix.add('lead', fm(n + 12, min(gate, beat * 1.2), 'epiano'), st, .34, -.05)
                mix.add('lead', fm(n + 24, min(gate, beat * .6), 'epiano'), st + beat * .02, .08, .4)
            elif p == 1:
                mix.add('lead', fm(n, gate, 'brass'), st, .30, -.1)
            else:
                mix.add('lead', fm(n, gate, 'brass'), st, .24, -.1)
                mix.add('lead', fm(n + 24, gate, 'fife'), st, .12, .35)
            cur += d
        for bi in range(BARS):
            bt = t0 + bi * bar_s
            for half, name in enumerate(CHORDS[bi]):
                if boss:
                    name = BOSS_CHORDS.get(name, name)
                root, shape = chord(name)
                hs = bt + half * 2 * beat
                # Driving eighth-note bass, octave jumps on the off-beats.
                low = root if root <= 44 else root - 12
                for k in range(4):
                    mix.add('bass', fm(low + (12 if k % 2 else 0), beat * .3, 'slap'), hs + k * beat / 2, .2)
                tones = [root + 24 + s for s in shape[:3]]
                if p > 0 or boss:
                    seq = tones + [tones[0] + 12, tones[1] + 12]
                    for k in range(8):
                        mix.add('arp', fm(seq[k % len(seq)] + 12, beat * .18, 'stab' if boss else 'epiano'), hs + k * beat / 4, .035, .55 if k % 2 else -.55)
                if p == 2 or boss:
                    for k in (.5, 1.5):
                        for tn in tones:
                            mix.add('stab', fm(tn + 12, beat * .25, 'stab'), hs + k * beat, .04, 0)
                else:
                    for tn in tones:
                        mix.add('pad', fm(tn + 12, 2 * beat * .95, 'pad'), hs, .035, (-.6 if tn % 2 else .6))
            # Drums: kick on one and three (every beat for the boss), snare on two and four, eighth hats.
            for k in range(8):
                st = bt + k * beat / 2
                if k in (0, 4) or (boss and k in (2, 6)) or (p > 0 and k == 7):
                    mix.add('kick', drum_cached('kick'), st, .75)
                if k in (2, 6):
                    mix.add('drums', drum_cached('snare'), st, .5, .1)
                mix.add('drums', drum_cached('ohat' if k == 7 and not boss else 'hat'), st, .06 if k % 2 else .08, .4)
            # A taiko-like low tom answers each bar.
            mix.add('drums', drum_cached('tom2'), bt + beat * 3.5, .24, -.35)
            if bi in (5, 9, BARS - 1):
                for k in range(8):
                    mix.add('drums', drum_cached('snare'), bt + 3 * beat + k * beat / 8, .12 + .04 * k, .1)
            if bi == 0:
                mix.add('drums', drum_cached('crash'), bt, .28, -.3)

    def eq(x, lo=None, hi=None):
        if lo:
            x = sosfilt(butter(2, lo, 'high', fs=SR, output='sos'), x, axis=0)
        if hi:
            x = sosfilt(butter(2, hi, 'low', fs=SR, output='sos'), x, axis=0)
        return x
    lead = pingpong(eq(mix.bus('lead'), 220, 7500), beat * .75, fb=.35, wet=.22)
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
        render(sys.argv[1], float(sys.argv[2]) if len(sys.argv) > 2 else 150, sys.argv[3] if len(sys.argv) > 3 else 'stage')
    else:
        out = Path(__file__).resolve().parents[2] / 'Assets/Resources/Audio/Music'
        out.mkdir(parents=True, exist_ok=True)
        render(out / 'sakura-arcade.wav', 150, 'stage')
        render(out / 'sakura-boss.wav', 176, 'boss')
