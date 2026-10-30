"""Arcade FM arrangement of Sur le pont d'Avignon for the Avignon mission.
Traditional French song (15th-century tune, popular since the 19th century), public domain. Only the refrain's
melody is used, in C major, 4/4 (two of the song's 2/4 bars per bar); the bridge, the tag, the harmony, bass and
drums are our own.
usage: pont_avignon.py [OUT.wav bpm stage|boss]  (no arguments renders both game tracks)"""
import sys
from pathlib import Path
import numpy as np
from scipy.signal import butter, sosfilt
from fmsynth import SR, Mix, fm, drum_cached, pingpong, reverb, master, write_wav, chord

# (midi, beats) with a quarter note as the beat. Fourteen bars that loop on themselves.
PONT = [(60, .5), (60, .5), (60, 1), (62, .5), (62, .5), (62, 1)]
REFRAIN = (PONT + [(64, .5), (65, .5), (67, .5), (60, .5), (59, .5), (60, .5), (62, .5), (55, .5)]
           + PONT + [(64, .5), (65, .5), (67, .5), (60, .5), (62, .5), (55, .5), (60, 1)])
BRIDGE = [(67, .5), (67, .5), (69, .5), (67, .5), (65, .5), (64, .5), (62, 1),
          (64, .5), (64, .5), (65, .5), (64, .5), (62, .5), (60, .5), (59, .5), (55, .5),
          (67, .5), (67, .5), (69, .5), (71, .5), (72, 1), (67, 1),
          (69, .5), (67, .5), (65, .5), (64, .5), (62, 1), (55, 1)]
TAG = [(60, .5), (64, .5), (67, .5), (72, .5), (67, 1), (64, 1), (65, .5), (64, .5), (62, .5), (59, .5), (60, 2)]
MELODY = REFRAIN + BRIDGE + REFRAIN + TAG
assert abs(sum(d for _, d in MELODY) - 56) < 1e-9
# One or two chords per bar, each lasting a half note when there are two.
CHORDS = ['C G', 'C G', 'C G', 'C G', 'G C', 'C G', 'G C', 'F G', 'C G', 'C G', 'C G', 'C G', 'C', 'G7 C']
CHORDS = [(c.split() * 2)[:2] for c in CHORDS]
BARS = len(CHORDS)
# The boss version turns to C harmonic minor: E and A go flat, the major dominant keeps its leading tone.
BOSS_NOTES = {64: 63, 69: 68}
BOSS_CHORDS = {'C': 'Cm', 'F': 'Fm'}


def render(out, bpm=152, mode='stage'):
    beat = 60 / bpm
    bar_s = 4 * beat
    passes = 3
    mix = Mix(passes * BARS * bar_s + 2.0)
    boss = mode == 'boss'

    for p in range(passes):
        t0 = p * BARS * bar_s
        # Lead: a musette accordion first (the bal of the bridge), then brass, then the fife an octave up over brass.
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
                mix.add('lead', fm(n + 12, gate, 'musette'), st, .26, -.05)
            elif p == 1:
                mix.add('lead', fm(n + 12, gate, 'brass'), st, .28, -.1)
            else:
                mix.add('lead', fm(n + 12, gate, 'brass'), st, .22, -.1)
                mix.add('lead', fm(n + 24, gate, 'fife'), st, .12, .35)
            cur += d
        for bi in range(BARS):
            bt = t0 + bi * bar_s
            for half, name in enumerate(CHORDS[bi]):
                if boss:
                    name = BOSS_CHORDS.get(name, name)
                root, shape = chord(name)
                hs = bt + half * 2 * beat
                # A bal-musette oom-pah: bass on the beat, chord on the off-beat; the boss drives eighths instead.
                low = root if root <= 44 else root - 12
                tones = [root + 24 + s for s in shape[:3]]
                if boss:
                    for k in range(4):
                        mix.add('bass', fm(low + (12 if k % 2 else 0), beat * .3, 'slap'), hs + k * beat / 2, .2)
                else:
                    for k in range(2):
                        mix.add('bass', fm(low + (7 if k else 0), beat * .45, 'slap'), hs + k * beat, .22)
                        for tn in tones:
                            mix.add('stab', fm(tn + 12, beat * .22, 'stab'), hs + k * beat + beat / 2, .035, 0)
                if p > 0 or boss:
                    seq = tones + [tones[0] + 12, tones[1] + 12]
                    for k in range(8):
                        mix.add('arp', fm(seq[k % len(seq)] + 12, beat * .18, 'stab' if boss else 'epiano'), hs + k * beat / 4, .03, .55 if k % 2 else -.55)
                if boss or p == 2:
                    for tn in tones:
                        mix.add('pad', fm(tn + 12, 2 * beat * .95, 'pad'), hs, .03, (-.6 if tn % 2 else .6))
            # Drums: kick on one and three (every beat for the boss), snare on two and four, eighth hats.
            for k in range(8):
                st = bt + k * beat / 2
                if k in (0, 4) or (boss and k in (2, 6)) or (p > 0 and k == 7):
                    mix.add('kick', drum_cached('kick'), st, .75)
                if k in (2, 6):
                    mix.add('drums', drum_cached('snare'), st, .5, .1)
                mix.add('drums', drum_cached('ohat' if k == 7 and not boss else 'hat'), st, .06 if k % 2 else .08, .4)
            # A tambourin-like tom roll answers every other bar, as in a farandole band.
            if bi % 2:
                for k in range(3):
                    mix.add('drums', drum_cached('tom2'), bt + beat * 3 + k * beat / 3, .2, -.35)
            if bi in (3, 7, BARS - 1):
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
        render(sys.argv[1], float(sys.argv[2]) if len(sys.argv) > 2 else 152, sys.argv[3] if len(sys.argv) > 3 else 'stage')
    else:
        out = Path(__file__).resolve().parents[2] / 'Assets/Resources/Audio/Music'
        out.mkdir(parents=True, exist_ok=True)
        render(out / 'pont-avignon-arcade.wav', 152, 'stage')
        render(out / 'pont-avignon-boss.wav', 176, 'boss')
