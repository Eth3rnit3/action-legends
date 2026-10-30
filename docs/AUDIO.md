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

La mission de Tokyo utilise `Music/sakura-arcade.wav` (4/4, 150 noires par minute) et `Music/sakura-boss.wav` (176 noires par minute), deux arrangements de **Sakura Sakura**, chanson traditionnelle japonaise de l’époque d’Edo, dans le domaine public. Seule la mélodie, dans le mode miyako-bushi sur la, est reprise ; les accords, la basse et la batterie sont écrits pour le projet. La première reprise confie le thème à un piano électrique pincé qui évoque le koto, une tom grave répond à chaque mesure comme un taiko. La version du boss abaisse le si en si bémol pour une couleur plus sombre, proche du mode in-sen.

```bash
python3 tools/audio/sakura.py
```

La mission de Saint-Rémy utilise `Music/saint-remy/farandole-8bit.wav` et `Music/saint-remy/farandole-8bit-boss.wav`, deux arrangements 8 bits de la **Farandole** de *L’Arlésienne* (Georges Bizet, 1872, sur l’air traditionnel de la Marche des Rois, domaine public). Elles ont été fournies avec les décors de la mission (pull request #1) ; aucun script du dépôt ne les régénère.

La mission d'Avignon utilise `Music/pont-avignon-arcade.wav` (4/4, 152 noires par minute, 66 s) et `Music/pont-avignon-boss.wav` (176 noires par minute, 57 s), deux arrangements FM de **Sur le pont d'Avignon**, chanson traditionnelle française dans le domaine public. Seule la mélodie du refrain, en do majeur, est reprise ; le pont, la coda, l'harmonie, la basse et la batterie sont écrits pour le projet. La boucle passe trois fois sur les quatorze mesures :
- première fois : l'accordéon musette sur un accompagnement de bal (basse sur le temps, accord à contretemps) ;
- deuxième fois : les cuivres ;
- troisième fois : les cuivres et le fifre à l'octave.

Une roulade de tambourin répond toutes les deux mesures, comme dans une farandole. La version du boss passe en do mineur harmonique, avec une basse en croches et la mélodie en grave saturé.

```bash
python3 tools/audio/pont_avignon.py
```

## Voix

Des annonces et des cris, trois prises chacun (`Voices/<ligne>-a|b|c.wav`) ; le jeu en tire une au hasard. Les annonces : bonus (`bonus-0` à `bonus-5`, `bonus-grenades`), armes à lettres (`weapon-h`, `-s`, `-f`, `-r`, `-l`), boss (`boss-alert`, `miniboss-alert`), victoire, et le « Merci ! » des otages (`hostage-merci`). Les cris des ennemis (lot 2b) : `cri-chute` à leur chute, `cri-surprise` quand ils repèrent l’équipe, `cri-panique` quand ils s’enfuient ; joués sur une source à part, ils ne coupent pas l’annonceur. Le texte de chaque ligne est dans `LINES` de `tools/audio/voices.py`. Elles sont générées avec Gemini TTS (prises a et b : `gemini-2.5-pro-preview-tts`, voix Fenrir et Orus ; prise c : `gemini-3.1-flash-tts-preview`, voix Fenrir), puis compressées et réduites en 12 bits avec un court écho. Le résultat change à chaque génération : il faut réécouter chaque prise avant de la garder.

```bash
GEMINI_API_KEY=... python3 tools/audio/voices.py            # toutes les annonces
GEMINI_API_KEY=... python3 tools/audio/voices.py bonus-2    # une seule
GEMINI_API_KEY=... python3 tools/audio/voices.py weapon-l:bc # seulement les prises b et c
```

## Bruitages

`Sfx/` contient des sons rétro générés à la manière de sfxr (oscillateur, glissement de hauteur, enveloppe, filtre, réduction de résolution). Le tir dépend de l’arme : pistolet sec pour le fusil de base (`tir-D-pistolet-sec`), mitrailleuse lourde (`tir-H-lourde`), fusil à pompe (`tir-S-pompe`), lance-flammes (`tir-F-flammes`), laser (`tir-L-laser`), départ de roquette (`roquette-depart`) ; l’hélicoptère tire au blaster. Le couteau, le lancer de grenade et la libération d’un otage ont leur son (`couteau`, `grenade-lancer`, `otage-libere`). Saint-Rémy ajoute ses bruitages dans `Sfx/saint-remy/` (fouet, taureau, parchemins, prophéties : voir [MISSION_SAINT_REMY.md](MISSION_SAINT_REMY.md)). Avignon a ses cloches dans `Sfx/avignon/` (`crosse-tir`, `cloche-alerte`, `cloche-impact`), synthétisées par `tools/audio/cloches.py` à partir des partiels d'une cloche d'église accordée (voir [MISSION_AVIGNON.md](MISSION_AVIGNON.md)). Impacts et explosions sont tirés au hasard parmi trois variantes. Les rencontres ont leurs bruitages : sursaut d’un ennemi surpris (`surprise`), camionnette qui freine (`camion-frein`), parachute (`parachute`), porte (`porte`). Le jeu joue aussi un son quand un joueur est touché, quand le boss est détruit, au ramassage d’un bonus, à l’arrivée d’une manette, sur les boutons du menu et au déploiement.

```bash
python3 tools/audio/sfx.py
```

Le hasard des sons utilise son propre générateur, distinct de celui de la simulation, pour ne pas désynchroniser le jeu en ligne.

Les scripts demandent Python 3 avec NumPy et SciPy ; `voices.py` demande aussi `ffmpeg`.
