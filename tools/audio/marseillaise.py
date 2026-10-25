"""Arcade FM arrangement of La Marseillaise (Rouget de Lisle, 1792, public domain).
Melody transcribed from the Wikimedia Commons engraving (Marseillaise-page1/2.png), G major, 4/4.
usage: marseillaise.py [OUT.wav bpm stage|boss]  (no arguments renders both game tracks)"""
import sys
from pathlib import Path
import numpy as np
from scipy.signal import butter, sosfilt
from fmsynth import SR, Mix, fm, drum_cached, pingpong, reverb, master, write_wav, chord

# (midi or 0 for rest, beats) per bar; bar 1 is the anacrusis bar.
BARS = [
    [(0, 2.75), (62, .25), (62, .75), (62, .25)],
    [(67, 1), (67, 1), (69, 1), (69, 1)],
    [(74, 1.5), (71, .5), (67, .5), (0, .25), (67, .25), (71, .75), (67, .25)],
    [(64, 1), (72, 2), (69, .75), (66, .25)],
    [(67, 2), (0, 1), (67, .75), (69, .25)],
    [(71, 1), (71, 1), (71, 1), (72, .75), (71, .25)],
    [(71, 1), (69, 1), (0, 1), (69, .75), (71, .25)],
    [(72, 1), (72, 1), (72, 1), (74, .75), (72, .25)],
    [(71, 2), (0, 1), (74, .75), (74, .25)],
    [(74, 1), (71, .75), (67, .25), (74, 1), (71, .75), (67, .25)],
    [(62, 2), (0, .75), (62, .25), (62, .75), (66, .25)],
    [(69, 2), (72, 1), (69, .75), (66, .25)],
    [(69, 1), (67, 1), (65, 2)],
    [(64, 1), (67, .75), (67, .25), (67, 1), (66, .75), (67, .25)],
    [(69, 3), (0, .5), (69, .5)],
    [(70, 1.5), (70, .5), (70, .5), (70, .5), (72, .5), (74, .5)],
    [(69, 3), (70, .5), (69, .5)],
    [(67, 1.5), (67, .5), (67, .5), (70, .5), (69, .5), (67, .5)],
    [(67, 1), (66, .5), (0, .5), (0, 1.75), (74, .25)],
    [(74, 2.75), (74, .25), (71, .75), (67, .25)],
    [(69, 3), (0, .75), (74, .25)],
    [(74, 2.75), (74, .25), (71, .75), (67, .25)],
    [(69, 2.5), (0, .5), (62, 1)],
    [(67, 2), (0, 1), (69, 1)],
    [(71, 2), (0, 2)],
    [(72, 2), (74, 1), (76, 1)],
    [(69, 2.5), (0, .5), (76, 1)],
    [(74, 2.75), (71, .25), (72, .75), (69, .25)],
    [(67, 3), (0, 1)],
]
assert all(abs(sum(d for _, d in b) - 4) < 1e-9 for b in BARS), [i + 1 for i, b in enumerate(BARS) if abs(sum(d for _, d in b) - 4) > 1e-9]
# Two chords per bar, following the piano reduction's bass line.
CHORDS = ['G G', 'G D', 'G G', 'C D', 'G G', 'G C', 'G D', 'Am D', 'G G', 'G G', 'D D', 'D7 D7', 'G Dm',
          'C F#dim7', 'D D', 'Gm Gm', 'D Gm', 'Gm Gm', 'D D7', 'G G', 'D D', 'G G', 'D D7', 'G D', 'G7 G7',
          'C Am', 'D D', 'G D7', 'G G']
CHORDS = [c.split() for c in CHORDS]
REFRAIN = 19  # zero-based index of bar 20 ("Aux armes")
# Boss mode re-harmonises in G minor: lower 3rds/6ths/7ths of the melody.
MINOR = {71: 70, 76: 75, 64: 63, 66: 66}


def render(out, bpm=144, mode='stage'):
    beat = 60 / bpm
    bar_s = 4 * beat
    passes = 2
    total_bars = len(BARS) * passes
    mix = Mix(total_bars * bar_s + 2.0)
    boss = mode == 'boss'

    def note_map(n):
        return MINOR.get(n, n) if boss else n

    def chord_notes(name):
        if boss and name == 'Am':
            name = 'Cm'  # the minor-mode melody has Eb here, which clashes with A minor's E
        root, shape = chord(name)
        if boss and shape == (0, 4, 7) and name[0] in 'GC':
            shape = (0, 3, 7)
        return root, shape

    for p in range(passes):
        for bi, bar in enumerate(BARS):
            t0 = (p * len(BARS) + bi) * bar_s
            first_bar = bi == 0
            # Lead melody: brass, second pass an octave up with a musette (accordion) double.
            cur = 0.0
            for n, d in bar:
                if n:
                    n = note_map(n)
                    gate = d * beat * (.92 if d >= 1 else .78)
                    st = t0 + cur * beat
                    if boss:
                        mix.add('lead', fm(n - 12, gate, 'growl'), st, .30, -.1)
                        if p == 1:
                            mix.add('lead', fm(n, gate, 'brass'), st, .14, .3)
                    elif p == 0:
                        mix.add('lead', fm(n, gate, 'brass'), st, .34, -.05)
                    else:
                        mix.add('lead', fm(n + 12, gate, 'lead2'), st, .24, .05)
                        mix.add('lead', fm(n, gate, 'musette'), st, .15, -.35)
                cur += d
            # Harmony
            for half, name in enumerate(CHORDS[bi]):
                root, shape = chord_notes(name)
                hs = t0 + half * 2 * beat
                # Octave slap bass in 8ths (the classic CPS1 disco bass).
                if not (first_bar and half == 0 and p == 0):
                    for k in range(4):
                        bn = (root if root <= 44 else root - 12) + (12 if k % 2 else 0)
                        if boss and k == 3:
                            bn = (root if root <= 44 else root - 12) + shape[2]
                        mix.add('bass', fm(bn, beat * .34, 'slap'), hs + k * beat / 2, .20)
                tones = [root + 24 + s for s in shape]
                if p == 1 or boss:
                    # 16th-note arpeggio, panned, quiet.
                    seq = tones + [tones[1] + 12] if len(tones) == 3 else tones
                    for k in range(8):
                        mix.add('arp', fm(seq[k % len(seq)] + (0 if boss else 12), beat * .2, 'stab' if boss else 'epiano'), hs + k * beat / 4, .035, .55 if k % 2 else -.55)
                if bi >= REFRAIN or boss:
                    # Off-beat brass stabs on the refrain.
                    for k in (.5, 1.5):
                        for tn in tones:
                            mix.add('stab', fm(tn + 12, beat * .25, 'stab'), hs + k * beat, .05, 0)
                else:
                    for tn in tones:
                        mix.add('pad', fm(tn + 12, 2 * beat * .95, 'pad'), hs, .035, (-.6 if tn % 2 else .6))
            # Drums
            fill = bi in (0, 18) or (bi == len(BARS) - 1)
            for k in range(4):
                bt = t0 + k * beat
                if first_bar and k < 3:
                    continue
                four = p == 1 or boss
                if k in (0, 2) or four:
                    mix.add('kick', drum_cached('kick'), bt, .75)
                if k in (1, 3) and not (fill and k == 3):
                    mix.add('drums', drum_cached('snare'), bt, .5, .1)
                for h in range(2 if not boss else 4):
                    sub = beat / (2 if not boss else 4)
                    mix.add('drums', drum_cached('ohat' if (h == 1 and not boss and k == 3) else 'hat'), bt + h * sub, .05 if h else .07, .4)
            if fill:
                # Military snare roll leading into the next phrase.
                last = t0 + 3 * beat
                for k in range(8):
                    mix.add('drums', drum_cached('snare'), last + k * beat / 8, .14 + .03 * k, .1)
                if first_bar:
                    for k, tom in enumerate(['tom1', 'tom1', 'tom2', 'tom2', 'tom3', 'tom3']):
                        mix.add('drums', drum_cached(tom), t0 + 1.5 * beat + k * beat / 4, .35, -.4 + .16 * k)
            if bi == REFRAIN or (bi == 1):
                mix.add('drums', drum_cached('crash'), t0, .28, -.3)

    def eq(x, lo=None, hi=None):
        if lo:
            x = sosfilt(butter(2, lo, 'high', fs=SR, output='sos'), x, axis=0)
        if hi:
            x = sosfilt(butter(2, hi, 'low', fs=SR, output='sos'), x, axis=0)
        return x
    # Carve the low-mids so only bass and kick live below ~200 Hz; tame lead and hat brightness.
    lead = pingpong(eq(mix.bus('lead'), 220, 7000), beat * .75, fb=.4, wet=.22)
    bass = eq(mix.bus('bass'), 40, 2500)
    music = (lead + bass + reverb(eq(mix.bus('arp') + mix.bus('pad') + mix.bus('stab'), 300, 6000), .25)
             + reverb(eq(mix.bus('drums'), 150, 8500), .08, .6) + mix.bus('kick'))
    music = reverb(music, .06)
    if boss:
        music = sosfilt(butter(2, 6500, 'low', fs=SR, output='sos'), music, axis=0)
    # Loop: fold the tail (delays/reverb past the last bar) back onto the start for a seamless loop.
    loop_n = int(total_bars * bar_s * SR)
    tail = music[loop_n:]
    music = music[:loop_n].copy()
    music[:len(tail)] += tail
    music = master(music)
    write_wav(out, music, fade=False)
    seam = np.abs(music[-1] - music[0]).max()
    print('  loop seam jump', round(float(seam), 4))
    print(out, round(loop_n / SR, 1), 's')


if __name__ == '__main__':
    if len(sys.argv) > 1:
        render(sys.argv[1], float(sys.argv[2]) if len(sys.argv) > 2 else 144, sys.argv[3] if len(sys.argv) > 3 else 'stage')
    else:
        out = Path(__file__).resolve().parents[2] / 'Assets/Resources/Audio/Music'
        out.mkdir(parents=True, exist_ok=True)
        render(out / 'marseillaise-arcade.wav', 144, 'stage')
        render(out / 'marseillaise-boss.wav', 160, 'boss')
