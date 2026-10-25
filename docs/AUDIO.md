# Audio : musique, voix et bruitages

Tous les sons sont des WAV 22,05 kHz dans `Assets/Resources/Audio/`, chargés par `Assets/Scripts/PanameSoundtrack.cs`. Le jeu n’a besoin d’aucun outil externe ; les scripts de `tools/audio/` ne servent qu’à régénérer les fichiers.

## Musique

`Music/marseillaise-arcade.wav` (144 BPM) et `Music/marseillaise-boss.wav` (sol mineur, 160 BPM) sont deux arrangements arcade de **La Marseillaise** (Rouget de Lisle, 1792, domaine public). La mélodie et la basse viennent de la partition gravée de Wikimedia Commons ([page 1](https://commons.wikimedia.org/wiki/File:Marseillaise-page1.png), [page 2](https://commons.wikimedia.org/wiki/File:Marseillaise-page2.png)). L’accompagnement, la batterie et les timbres sont synthétisés pour le projet ; aucun enregistrement existant n’est utilisé.

Le rendu imite une borne de 1990 : synthèse FM façon Yamaha YM2151, batterie échantillonnée en 8 bits, écho ping-pong, réverbe et sortie 12 bits. Les deux pistes bouclent : la queue des effets est repliée sur le début.

```bash
python3 tools/audio/marseillaise.py
```

La mission de Marseille utilise `Music/coupo-santo-arcade.wav` (6/8, 174 noires par minute) et `Music/coupo-santo-boss.wav` (la bémol mineur harmonique, 192 noires par minute), deux arrangements de **La Coupo Santo**. C’est l’air traditionnel provençal du noël « Guihaume, Tòni, Pèire » (XVIIᵉ siècle), chanté sur le texte de Frédéric Mistral (1867), dans le domaine public. Seule la mélodie est reprise ; elle a été relevée sur le [fichier MIDI de l’air traditionnel du site Mtcn](http://mtcn.free.fr/lyrics/coupo-santo.php?lng=fr). Son harmonisation n’est pas utilisée : les accords, la basse et la batterie sont écrits pour le projet. Le fifre rappelle le galoubet et une tom sèche marque chaque mesure, comme le tambourin.

```bash
python3 tools/audio/coupo_santo.py
```

## Voix

Huit annonces, trois prises chacune (`Voices/<annonce>-a|b|c.wav`) ; le jeu en tire une au hasard. Elles sont générées avec Gemini TTS (prises a et b : `gemini-2.5-pro-preview-tts`, voix Fenrir et Orus ; prise c : `gemini-3.1-flash-tts-preview`, voix Fenrir), puis compressées et réduites en 12 bits avec un court écho. Le résultat change à chaque génération : il faut réécouter chaque prise avant de la garder.

```bash
GEMINI_API_KEY=... python3 tools/audio/voices.py            # toutes les annonces
GEMINI_API_KEY=... python3 tools/audio/voices.py bonus-2    # une seule
```

## Bruitages

`Sfx/` contient des sons rétro générés à la manière de sfxr (oscillateur, glissement de hauteur, enveloppe, filtre, réduction de résolution). Le tir dépend de l’arme : pistolet sec pour l’arme de base, mitraille, laser pour le triple tir, départ de roquette ; l’hélicoptère tire au blaster. Impacts et explosions sont tirés au hasard parmi trois variantes. Le jeu joue aussi un son quand un joueur est touché, quand le boss est détruit, au ramassage d’un bonus, à l’arrivée d’une manette, sur les boutons du menu et au déploiement.

```bash
python3 tools/audio/sfx.py
```

Le hasard des sons utilise son propre générateur, distinct de celui de la simulation, pour ne pas désynchroniser le jeu en ligne.

Les scripts demandent Python 3 avec NumPy et SciPy ; `voices.py` demande aussi `ffmpeg`.
