"""Announcer voices with Gemini TTS, then an arcade-board treatment through ffmpeg.
Three takes per line (a, b, c); the game picks one at random. Results vary between runs,
so listen to every take before committing. usage: GEMINI_API_KEY=... voices.py [line-id[:takes] ...]"""
import base64, json, os, subprocess, sys, tempfile, time, urllib.error, urllib.request, wave
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / 'Assets/Resources/Audio/Voices'
LINES = {'bonus-0': 'Mitrailleuse !', 'bonus-1': 'Triple tir !', 'bonus-2': 'Roquettes !', 'bonus-3': 'Soins !',
         'bonus-4': 'Turbo !', 'bonus-5': 'Bonus !', 'boss-alert': 'Alerte ! Boss en approche !', 'victory': 'Mission accomplie !',
         'bonus-grenades': 'Grenades !', 'weapon-h': 'Mitrailleuse lourde !', 'weapon-s': 'Fusil à pompe !',
         'weapon-f': 'Lance-flammes !', 'weapon-r': 'Lance-roquettes !', 'weapon-l': 'Laser !',
         'hostage-merci': 'Merci !',
         # Lot 2b: enemies' cries when they fall and when they spot the heroes, and the mid-mission boss alert.
         'cri-chute': 'Aaaargh ! Nooon !', 'cri-panique': 'Ouaaah ! Au secours !', 'cri-surprise': 'Hein ?! Là-bas !',
         'miniboss-alert': 'Attention ! Gros calibre !'}
# The 3.x models read stage directions aloud, so they get audio tags; 2.5 Pro follows a spoken instruction.
TAKES = {'a': ('gemini-2.5-pro-preview-tts', 'Fenrir', 'Say in French, shouting excitedly like an arcade announcer: {}'),
         'b': ('gemini-2.5-pro-preview-tts', 'Orus', 'Say in French, shouting excitedly like an arcade announcer: {}'),
         'c': ('gemini-3.1-flash-tts-preview', 'Fenrir', '[shouting] [excited] {}')}
# Enemies' cries are acted, not announced: another direction, and 2.5 Pro for every take (3.1 Flash returns no audio for a scream).
CRIES = {'cri-chute', 'cri-panique', 'cri-surprise'}
CRY_TAKES = {'a': ('gemini-2.5-pro-preview-tts', 'Fenrir', 'Say in French, screaming like a cartoon henchman in a comic action game: {}'),
             'b': ('gemini-2.5-pro-preview-tts', 'Orus', 'Say in French, screaming like a cartoon henchman in a comic action game: {}'),
             'c': ('gemini-2.5-pro-preview-tts', 'Puck', 'Say in French, screaming like a cartoon henchman in a comic action game: {}')}
FILTERS = ('silenceremove=start_periods=1:start_threshold=-40dB,areverse,silenceremove=start_periods=1:start_threshold=-40dB,'
           'areverse,apad=pad_dur=0.05,highpass=f=150,lowpass=f=7000,'
           'acompressor=threshold=0.1:ratio=5:attack=2:release=80:makeup=2.5,acrusher=bits=12:samples=1.3:mix=0.5:mode=lin,'
           'aecho=0.8:0.35:60:0.22,alimiter=limit=0.9')


def speak(model, voice, text):
    body = {'contents': [{'parts': [{'text': text}]}],
            'generationConfig': {'responseModalities': ['AUDIO'], 'speechConfig': {
                'languageCode': 'fr-FR', 'voiceConfig': {'prebuiltVoiceConfig': {'voiceName': voice}}}}}
    request = urllib.request.Request(f'https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent',
                                     data=json.dumps(body).encode(),
                                     headers={'Content-Type': 'application/json', 'x-goog-api-key': os.environ['GEMINI_API_KEY']})
    for attempt in range(5):
        try:
            data = json.load(urllib.request.urlopen(request, timeout=300))
            try:
                return base64.b64decode(data['candidates'][0]['content']['parts'][0]['inlineData']['data'])
            except (KeyError, IndexError):  # now and then a take comes back without audio: ask again
                time.sleep(2)
                continue
        except urllib.error.HTTPError as error:
            if error.code != 429:  # 2.5 Pro TTS allows 10 requests per minute on Tier 1
                raise
            time.sleep(20)
    raise RuntimeError('rate limited')


def main(ids):
    OUT.mkdir(parents=True, exist_ok=True)
    for line in ids:
        # "line:b" redoes only take b of a line.
        line, _, only = line.partition(':')
        for take, (model, voice, prompt) in (CRY_TAKES if line in CRIES else TAKES).items():
            if only and take not in only:
                continue
            raw = speak(model, voice, prompt.format(LINES[line]))
            with tempfile.NamedTemporaryFile(suffix='.wav') as tmp:
                if raw[:4] != b'RIFF':  # raw 24 kHz PCM
                    with wave.open(tmp.name, 'wb') as w:
                        w.setnchannels(1); w.setsampwidth(2); w.setframerate(24000); w.writeframes(raw)
                else:
                    Path(tmp.name).write_bytes(raw)
                target = OUT / f'{line}-{take}.wav'
                subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', tmp.name, '-af', FILTERS,
                                '-ar', '22050', '-ac', '1', str(target)], check=True)
            print(target.name)


if __name__ == '__main__':
    main(sys.argv[1:] or list(LINES))
