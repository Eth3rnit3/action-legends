"""Original arcade arrangement of Renard's public-domain melody; NumPy + ffmpeg.

Only the nineteenth-century melody is reused. Accompaniment, drums, sound design,
and the recordings below are newly synthesized for Action Legends.
"""
from pathlib import Path
import math, subprocess, wave
import numpy as np

OUT=Path(__file__).resolve().parents[1]/'Assets/Resources/Audio';OUT.mkdir(parents=True,exist_ok=True)
RATE=22050
# Melody transposed to G major, quarter-note units, no lyrics or existing recording.
G,A,B,C,D,F=67,69,71,72,74,66
bars=[[(G,1)]*3,[(G,2),(G,1)],[(A,1)]*3,[(A,2),(A,1)],
      [(B,1)]*3,[(B,2),(B,1)],[(C,1)]*3,[(C,2),(B,1)],
      [(B,1),(B,1),(C,1)],[(B,3)],[(A,3)],[(G,1),(0,1),(G,1)],
      [(C,1)]*3,[(C,2),(C,1)],[(C,1),(D,1),(C,1)],[(C,1),(B,1),(B,1)],
      [(B,1)]*3,[(D,2),(B,1)],[(B,1),(A,1),(G,1)],[(A,3)],
      [(A,1),(0,1),(62,1)],[(G,1)]*3,[(G,2),(G,1)],[(A,1)]*3,
      [(A,2),(A,1)],[(B,1)]*3,[(D,2),(B,1)],[(B,1),(A,1),(F,1)],
      [(G,2.5),(0,.5)],[(0,3)]]
chords=[(43,47,50),(43,47,50),(48,52,55),(48,52,55),(43,47,50),(43,47,50),
        (48,52,55),(48,52,55),(43,47,50),(48,52,55),(43,47,50),(50,54,57),
        (48,52,55),(48,52,55),(43,47,50),(43,47,50),(40,43,47),(40,43,47),
        (45,48,52),(50,54,57),(50,54,57),(43,47,50),(43,47,50),(48,52,55),
        (48,52,55),(43,47,50),(50,54,57),(50,54,57),(43,47,50),(50,54,57)]

def render(name,bpm,boss=False):
    beat=60/bpm;total_beats=96*2;n=int(total_beats*beat*RATE)
    mix=np.zeros((n,2),np.float32);rng=np.random.default_rng(202650+boss)
    def add(signal,start,volume=.2,pan=0):
        index=int(start*RATE);end=min(n,index+len(signal));size=end-index
        if index<0 or size<=0:return
        mix[index:end,0]+=signal[:size]*volume*math.sqrt((1-pan)/2)
        mix[index:end,1]+=signal[:size]*volume*math.sqrt((1+pan)/2)
    def tone(note,beats,kind='lead'):
        duration=beats*beat;t=np.arange(int(duration*RATE))/RATE
        f=440*2**((note-69)/12);phase=2*np.pi*f*t+.012*np.sin(2*np.pi*5.8*t)
        if kind=='bass':signal=np.sin(phase)+.35*np.sin(phase*2)+.12*np.sin(phase*4)
        elif kind=='arp':signal=np.sin(phase)+.22*np.sin(phase*3)
        elif kind=='pad':signal=np.sin(phase)+.3*np.sin(phase*2+.3)
        else:signal=sum(np.sin(phase*k)/(k**1.3) for k in (1,2,3,4,5,6))
        env=np.minimum(1,t/.004)*np.minimum(1,(duration-t)/.025)*(.7+.3*np.exp(-t*8))
        return (signal*env).astype(np.float32)
    for cycle in range(2):
        offset=cycle*96
        add(tone(62,1), (offset+5)*beat,.26)
        for bar,notes in enumerate(bars):
            start=offset+6+bar*3;cursor=start
            for note,length in notes:
                if note:
                    add(tone(note+12,length*.89),cursor*beat,.22 if not boss else .25)
                    add(tone(note+12,length*.89), (cursor+.18)*beat,.065,-.55)
                    if cycle==1:add(tone(note+19,length*.83,'arp'),cursor*beat,.055,.5)
                cursor+=length
    for measure in range(64):
        b=measure*3;chord=chords[(measure-2)%30]
        for k in range(6):
            root=chord[0] if k%2==0 else chord[0]+7
            add(tone(root-.0,.42,'bass'),(b+k*.5)*beat,.24 if boss else .19)
            add(tone(chord[k%3]+24,.20,'arp'),(b+k*.5+.25)*beat,.065,(-.65 if k%2 else .65))
        for note in chord:add(tone(note+12,2.8,'pad'),b*beat,.035,(-.8 if note%2 else .8))
        for k in [0,1.5,2.5] if boss else [0,1.5]:
            t=np.arange(int(.18*RATE))/RATE
            kick=np.sin(2*np.pi*(47*t+6*(1-np.exp(-t*28))))*np.exp(-t*24)
            add(kick,(b+k)*beat,.48)
        for k in [1,2]:
            t=np.arange(int(.15*RATE))/RATE;noise=rng.uniform(-1,1,len(t))
            snare=(noise-np.roll(noise,1))*.35*np.exp(-t*26)+np.sin(2*np.pi*175*t)*np.exp(-t*40)*.3
            add(snare,(b+k)*beat,.30,.12)
        for k in range(12 if boss else 6):
            t=np.arange(int(.065*RATE))/RATE;noise=rng.uniform(-1,1,len(t));hat=(noise-np.roll(noise,1))*np.exp(-t*85)
            add(hat,(b+k*(.25 if boss else .5))*beat,.045 if k%2 else .065,.45)
    # Both endpoints fall on a bar boundary; a short zero crossing avoids loop clicks.
    mix=np.tanh(mix*1.45);mix/=max(.01,np.max(np.abs(mix)));mix*=.8
    fade=int(.008*RATE);mix[:fade]*=np.linspace(0,1,fade)[:,None];mix[-fade:]*=np.linspace(1,0,fade)[:,None]
    with wave.open(str(OUT/(name+'.wav')),'wb') as f:f.setnchannels(2);f.setsampwidth(2);f.setframerate(RATE);f.writeframes((mix*32767).astype('<i2').tobytes())
    print(name,round(n/RATE,2),'seconds')

render('cerises-arcade',176)
render('cerises-boss',198,True)
phrases=['Mitrailleuse !','Triple tir !','Roquettes !','Soins !','Turbo !','Bonus !','Alerte ! Boss en approche !','Mission accomplie !']
for i,phrase in enumerate(phrases):
    name='bonus-'+str(i) if i<6 else 'boss-alert' if i==6 else 'victory'
    raw=OUT/(name+'.aiff')
    subprocess.run(['say','-v','Jacques','-r','205','-o',str(raw),phrase],check=True)
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(raw),'-af','highpass=f=150,lowpass=f=6500,acompressor=threshold=0.12:ratio=3,volume=1.4','-ar',str(RATE),'-ac','1',str(OUT/(name+'.wav'))],check=True)
    raw.unlink()
    print(name,phrase)
