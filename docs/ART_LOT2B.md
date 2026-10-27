# Images et sons du lot 2b

Le lot 2b met en scène les ennemis (rencontres placées, entrées par porte, fenêtre, parachute, camion), leur donne une vie propre (inattentif, surpris, en fuite, morts selon le dégât, grenades) et ajoute un mini-boss par mission. Toutes les images ont été générées avec le générateur d'images de Codex CLI (`codex exec -m gpt-5.6-sol --enable image_generation`), en donnant des planches existantes en référence ; chaque sortie a été regardée avant d'être gardée. Elles sont toutes sorties transparentes et en grille du premier coup ; la consigne qui entoure chaque prompt (comme au lot 2a) demande à Codex de vérifier les coins transparents et la grille, d'appliquer au besoin la passe de détourage de `docs/MISSION_TOKYO.md`, et de régénérer une fois si la grille est fausse.

| Fichier | Taille | Usage |
|---|---|---|
| `Art/enemies/<ennemi>.png` (six) | 1448×1448 (pillards) ou 1680×1448 (gangsters), 4 × 4 cases | Planche de chaque ennemi : ligne 1 = ses quatre anciennes cases **copiées au pixel près** depuis les anciennes planches `ArtSources/enemies.png` ou `ArtSources/tokyo/enemies.png` (mêmes ancrages ; hors `Resources` depuis l’allègement de la 0.8.0), lignes 2 à 4 = les douze nouvelles images |
| `Art/bosses/armored-van.png` | 1920×1024, 3 × 2 cases | Fourgon blindé (mini-boss de Paris) |
| `Art/bosses/speedboat.png` | 1920×1024, 3 × 2 cases | Hors-bord des caïds (mini-boss de Marseille) |
| `Art/bosses/combat-drone.png` | 1920×1024, 3 × 2 cases | Drone de combat (mini-boss de Tokyo) |
| `Art/carriers.png` | 1920×1024, 3 × 2 cases | Camionnette rouillée (`rusty-van`) et monospace noir (`black-minivan`) qui déposent des ennemis |
| `Art/encounter-fx.png` | 1448×724, 4 × 2 cases | « ! » de surprise, parachute ouvert, se repliant, posé au sol ; porte cochère, fenêtre à volets, rideau métallique, bouche d'égout |
| `Audio/Sfx/surprise.wav`, `camion-frein.wav`, `parachute.wav`, `porte.wav` | 22 kHz | Sursaut, camion qui freine, parachute qui s'ouvre, porte (`tools/audio/sfx.py`, ajoutés en fin de table : les anciens sons sont identiques octet pour octet) |
| `Audio/Voices/cri-chute`, `cri-surprise`, `cri-panique`, `miniboss-alert` (-a, -b, -c) | 22 kHz | Cri à la mort (« Aaaargh ! Nooon ! »), « Hein ?! Là-bas ! », « Ouaaah ! Au secours ! », « Attention ! Gros calibre ! » (Gemini TTS, `tools/audio/voices.py`) |
| `Audio/Voices/bonus-grenades`, `weapon-h/s/f/r/l`, `hostage-merci` (-a, -b, -c) | 22 kHz | Lignes laissées en attente par les lots 1a, 1b et 2a, générées ici et branchées dans les données (`voice`) |

## Planches d'ennemis

Douze nouvelles images par ennemi, générées en 4 × 3 cases à la taille des cases d'origine (362 × 362 pour les pillards, 420 × 362 pour les gangsters), avec Image 1 = la ligne de l'ennemi découpée dans sa planche d'origine et Image 2 = `Art/heroes/chuck-norris.png`. `tools/art/pack_sheet.py hero tools/art/enemies/<ennemi>.json` assemble la planche finale : la source `O` (l'ancienne planche, `"exact": true`) est copiée case pour case, la source `N` (sortie de Codex) est mise à l'échelle pour que sa case de référence ait la hauteur de l'ancienne case d'attente (`heightFrom`), pieds sur la même ligne (16,29 px du bas) et milieu des jambes sur la colonne du pivot (40 %), dans les marges du recadrage (`margins`) ; un quatrième nombre d'une entrée `frames` réduit une image trop large (corps allongé). Les sources (`<ennemi>-new.png`, sorties de Codex, et les anciennes planches renommées `enemies.png` et `tokyo-enemies.png`) sont à poser à côté des fichiers de description pour refaire une planche ; elles ne sont pas versionnées. Réglages du `sheet` inchangés : pivot (0,4 ; 0,045), `cellHeight` 2,2777777, `cropLeft` 0,065, `cropRight` 0,025, `rows` 4.

| Case | Pose | État |
|---|---|---|
| 0–3 | Anciennes cases : attente, deux pas, attaque | `idle`, `run`, `attack` |
| 4–5 | Inattentif (propre à chaque type) | `unaware` |
| 6 | Sursaut | `surprised`, `fall` (suspendu au parachute) |
| 7 | Touché (pillards, pistolero, mitrailleur) ou lancer de grenade (colosse au fusil) | `hurt` ou `throw` |
| 8–9 | Fuite paniquée, tête tournée en arrière | `flee` |
| 10–11 | Renversé par une balle, puis à terre | `death` |
| 12–13 | Soufflé par une explosion, culbute | `deathExplosion` (12, 13, 11) |
| 14–15 | En feu, puis carbonisé | `deathFire` (14, 14, 15) |
| — | Couteau : touché puis à terre | `deathKnife` (7, 10, 11) |

| Ennemi | Case | Personnage (Image 1) | Inattentif | Case 4 |
|---|---|---|---|---|
| `looter-rusher` | 362 × 362 | the hooded looter armed with a rusty crowbar: black hoodie with the hood up, black balaclava showing only the eyes, black cargo trousers, dark sneakers, rusty red-brown crowbar | 1 off guard A: standing relaxed with weight on one leg, the crowbar resting on his shoulder, smoking a cigarette held in the other hand near the mouth, eyes half closed; 2 off guard B: same pose, cigarette lowered, blowing a small puff of grey smoke, bored | 4 hurt: recoiling from a hit, torso bent back, one arm thrown up, eyes squeezed shut |
| `looter-thrower` | 362 × 362 | the hooded looter with a khaki backpack who throws bottles: black hoodie with the hood up, black balaclava, khaki backpack, black cargo trousers, dark sneakers, a glass bottle with a red rag in one hand | 1 off guard A: standing relaxed, eating a big sandwich held in both hands, the bottle tucked in the backpack strap; 2 off guard B: chewing with a full cheek, sandwich lowered, looking happy and distracted | 4 hurt: recoiling from a hit, torso bent back, one arm thrown up, eyes squeezed shut |
| `looter-shield` | 362 × 362 | the big hooded looter carrying a rusty riveted steel shield: bulky build, black hoodie, black balaclava, knee pads, heavy boots, a tall rusty riveted steel plate shield with a viewing slot | 1 off guard A: leaning lazily on his shield planted upright on the ground beside him, one elbow on its top edge, yawning widely; 2 off guard B: same lean, scratching the back of his hooded head, sleepy | 4 hurt: recoiling from a hit behind his shield, torso bent back, eyes squeezed shut |
| `gunman-pistol` | 420 × 362 | the flashy gangster with a black pompadour, sunglasses, white suit jacket over a magenta patterned shirt, white trousers, black shoes, and a black pistol | 1 off guard A: standing relaxed, the pistol tucked in his belt, combing his pompadour with a small black comb, pleased with himself; 2 off guard B: admiring himself in a small pocket mirror held up in one hand, the comb in the other | 4 hurt: recoiling from a hit, torso bent back, one arm thrown up, sunglasses askew |
| `gunman-smg` | 420 × 362 | the blond gangster in sunglasses, black suit, white shirt, red tie, tattooed forearm, carrying a black compact submachine gun | 1 off guard A: standing relaxed, the submachine gun hanging from a strap at his side, talking on a small flip phone held to his ear; 2 off guard B: same, laughing loudly into the phone, head tilted back | 4 hurt: recoiling from a hit, torso bent back, one arm thrown up, sunglasses askew |
| `gunman-shotgun` | 420 × 362 | the bald muscular bruiser with a beard and a red headband, purple sleeveless vest over a white tank top, shotgun-shell belt, purple cargo trousers, knee pads, heavy boots, and a pump shotgun | 1 off guard A: standing relaxed, the shotgun slung on his back, eating noodles from a paper cup with chopsticks; 2 off guard B: slurping a long strand of noodles, eyes closed with pleasure | 4 throwing a grenade: the shotgun held low in one hand, the other arm swung up and forward over the head in an overhand throw, a small round dark-green hand grenade just leaving the raised hand |

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet (animation frames) for a run-and-gun arcade game in the spirit of Metal Slug: extra animation states of an existing enemy. Tone: comic arcade action, slapstick, never gory: no blood, no wounds, no gore.
Input images: Image 1 is the existing animation row of this enemy (idle, two run frames, attack): draw EXACTLY the same character (face, outfit, colours, weapon, proportions, scale and rendering). Image 2 is the rendering, palette, shading, outline and craftsmanship reference of the game's cast (a hero sheet).
Primary request: Create ONE precise sprite sheet containing exactly 12 full-body sprites of the SAME character, arranged as exactly 4 columns x 3 rows of equal cells. Do not draw cell borders or grid lines. The character is <WHO>. The same character must be drawn identically in all 12 frames.

Layout and scale: the final intended canvas is <W> x <H> pixels, so each implied cell is <CELL> pixels. In every implied cell the feet (or the body when lying on the ground) rest on a common baseline about 16 px above the cell bottom; the body is centred at about 40% of the cell width. STANDING HEIGHT from soles to the top of the head is the same as in Image 1 (about 88% of the cell height) and this scale must be identical in all 12 frames. Keep generous transparent separation and keep every pixel of every sprite completely inside its own cell. The character ALWAYS FACES RIGHT, like Image 1.
Frames, left to right then top to bottom:
Row 1: <RELAX>; 3 surprised: a startled jump on the spot, both feet just off the ground, eyes wide open, mouth open in a shout, arms thrown up, weapon still in hand; <FOUR>.
Row 2: 5 panic run A: fleeing in a panic, running to the RIGHT with long comic strides, head turned back over the shoulder with a terrified face, arms flailing, weapon held loosely; 6 panic run B: the other leg forward, same panic; 7 shot down A: knocked backward by a hit, falling onto his back, arms flung forward, eyes shut, weapon flying out of his hand; 8 shot down B: lying flat on his back on the ground, knocked out, eyes as crosses, the weapon on the ground beside him.
Row 3: 9 blown up A: blasted high into the air by an explosion, body twisted and spread-eagled, clothes torn and smoking, face and clothes blackened with soot, comic shocked face; 10 blown up B: tumbling head over heels upside down in the air, sooty and smoking; 11 burning: on fire, flailing both arms in a comic panic, bright orange cartoon flames licking up his clothes and back, soot on the face; 12 burnt: completely charred dark grey-black from head to toe with small wisps of smoke and two white blinking eyes, standing stiffly about to crumble.
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like animation readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette, high-detail game-ready sprite art.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 4x3 orderly grid; exactly 12 sprites of the same character; no ground; no floor; no shadow under feet; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background; no speed lines or motion trails; no blood. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

## Mini-boss

Image 1 = `Art/tokyo/mecha-shogun.png` (mise en page des boss, rendu), Image 2 = la ligne d'un ennemi de la ville (l'équipage). Sorties en 3 × 2 cases de 560 × 512, recalées par `pack_sheet.py hero tools/art/bosses/<boss>.json` en cases de 640 × 512 à 110 px par unité (pivot (0,5 ; 0,0273438), `cellHeight` 4,6545455), le bas de chaque image sur une même ligne. Les trois sont tournés vers la gauche, comme tous les boss.

### Fourgon blindé

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet (animation frames) for a run-and-gun arcade game in the spirit of Metal Slug: a mid-mission boss.
Input images: Image 1 is an existing boss sheet of the game: copy its rendering, palette, outline, level of detail and its layout (3 columns x 2 rows of equal cells); Image 2 shows the game's enemies in the same style.
Primary request: Create ONE precise sprite sheet containing exactly 6 sprites of the SAME boss, arranged as exactly 3 columns x 2 rows of equal cells. Do not draw cell borders or grid lines. The boss is a mid-mission boss vehicle: a hijacked armoured cash-in-transit van, boxy dark steel-grey body with rivets, small barred slit windows, heavy front bull bar, thick black tyres, a small round roof turret with a twin machine gun manned by a hooded looter in a black balaclava (same outfit as the looters of Image 2), and a rear double door. The same boss must be drawn identically in all 6 frames (same design, colours, proportions and scale).

Layout and scale: the final intended canvas is 1680 x 1024 pixels, so each implied cell is 560 x 512 pixels. The boss fills about 85% of the cell width in every frame, at the same scale in every frame; its lowest point rests on a common baseline about 13 px above the cell bottom (a hovering drone is centred in its cell instead). Keep every pixel of every sprite completely inside its own cell. The boss ALWAYS FACES LEFT (front towards the left of the image).
Frames, left to right then top to bottom:
Row 1: 1 driving A: the van rolling forward to the LEFT, turret gunner watching; 2 driving B: same with the wheels turned a quarter turn and the body bouncing slightly; 3 turret firing: the turret gunner leaning on the twin machine gun aiming forward-left and slightly down, the van stopped.
Row 2: 4 revving to charge: the van stopped, front lifted slightly, thick exhaust smoke at the back, gunner ducking into the hatch; 5 rear doors open: the van stopped with its rear double door swung open showing a dark interior, a hooded looter leaping out of the back; 6 wrecked: heavily damaged, dented panels, a flat tyre, broken bull bar, black smoke and small flames from the engine, the turret empty and bent.
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites (water spray belongs to the boat sprite).
Constraints: exact 3x2 orderly grid; exactly 6 sprites of the same boss; no ground; no road; no sea surface beyond the spray touching the hull; no shadow under it; no muzzle flashes; no projectiles in flight except the grenade in the hand; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

### Hors-bord

Même prompt que le fourgon, où la description et les poses sont :

```text
Create ONE precise sprite sheet containing exactly 6 sprites of the SAME boss, arranged as exactly 3 columns x 2 rows of equal cells. Do not draw cell borders or grid lines. The boss is a mid-mission boss vehicle: a gangsters' powerful offshore speedboat, sleek white and red hull with chrome rails and a tinted windscreen, two outboard motors at the stern, a white water line splash along its hull; on board a pilot at the wheel and a gangster in a loud flowered shirt and sunglasses. The same boss must be drawn identically in all 6 frames (same design, colours, proportions and scale).
Row 1: 1 cruising A: the boat speeding to the LEFT, bow lifted, white spray along the hull bottom; 2 cruising B: same with the spray pattern changed and the hull bobbing slightly lower; 3 gunner firing: the gangster standing at the side rail aiming a submachine gun forward-left.
Row 2: 4 grenade throw: the gangster swinging his arm up overhand throwing a small round grenade; 5 boarding: the boat slowed, a gangster in a striped shirt leaping off the side rail toward the viewer; 6 wrecked: the boat badly damaged, cracked windscreen, black smoke and flames from the outboard motors, listing to one side.
```

### Drone de combat

```text
Create ONE precise sprite sheet containing exactly 6 sprites of the SAME boss, arranged as exactly 3 columns x 2 rows of equal cells. Do not draw cell borders or grid lines. The boss is a mid-mission boss: a large black and gunmetal military combat drone with four ducted rotors on short arms, a glowing red sensor eye at the front, a rotating gatling gun under its nose and two small missile pods on its sides, yakuza-style gold dragon decal on its body, neon pink and cyan running lights. The same boss must be drawn identically in all 6 frames (same design, colours, proportions and scale).
Row 1: 1 hovering A: level hover facing LEFT, rotors blurred; 2 hovering B: same with the rotor blur changed and the body tilted very slightly; 3 gatling firing: nose tilted down-left, the gatling gun barrels pointing forward-down.
Row 2: 4 missile pods open: the side pods opened showing red-tipped missiles; 5 dashing: the body tilted strongly forward to the left as it flies fast; 6 wrecked: heavily damaged, one rotor broken, sparks, black smoke, the red eye cracked.
```

## Véhicules de dépôt

Image 1 = `Art/tokyo/mecha-shogun.png`, Image 2 = la ligne du pillard au pied-de-biche ; recalés comme les mini-boss (`tools/art/bosses/carriers.json`). Tournés vers la droite.

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet (animation frames) for a run-and-gun arcade game in the spirit of Metal Slug: vans that drive in, stop and drop off enemy thugs.
Input images: Image 1 is an existing sheet of the game: copy its rendering, palette, outline and level of detail; Image 2 shows the game's enemies in the same style and scale.
Primary request: Create ONE precise sprite sheet containing exactly 6 vehicle sprites, arranged as exactly 3 columns x 2 rows of equal cells. Do not draw cell borders or grid lines. Row 1 is a battered white delivery van with rust patches, dented panels, a sliding side door and a roof rack, no markings; row 2 is a glossy black minivan with tinted windows, chrome trims, neon purple underglow and a sliding side door, no markings. Each van is seen exactly from the side, nose to the RIGHT, and is drawn identically in its three frames.

Layout and scale: the final intended canvas is 1680 x 1024 pixels, so each implied cell is 560 x 512 pixels. Each van is about 90% of the cell width long and about 50% of the cell height tall, wheels resting on a common baseline about 13 px above the cell bottom. Keep every pixel inside its own cell.
Frames, left to right, for each row: 1 driving A: side door closed, wheels sharp; 2 driving B: same with the wheels turned a quarter turn and the body bouncing very slightly; 3 stopped: the sliding side door wide open showing a dark empty interior.
Style/medium: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship, dense deliberate pixel clusters, detailed painterly shading, crisp ink-navy outlines, warm edge highlights, muted teal shadows.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the vehicles.
Constraints: exact 3x2 orderly grid; no people; no ground; no road; no shadow; no text; no letters; no numbers; no logos; no licence plate text; no UI; no watermark; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

## Effets de mise en scène

Image 1 = `Art/arcade-fx.png`, Image 2 = la ligne du pillard au pied-de-biche ; utilisée telle quelle (4 × 2 cases de 362 px), chaque case découpée par un `SpriteCell` des règles (`stage`).

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet for a run-and-gun arcade game in the spirit of Metal Slug: props and effects for enemy entrances.
Input images: Image 1 is the game's effects sheet and Image 2 a sheet of its enemies: copy their rendering, palette, outline and level of detail.
Primary request: Create ONE precise sprite sheet containing exactly 8 separate objects arranged as exactly 4 columns x 2 rows of equal square cells, each object centred in its cell. Do not draw cell borders or grid lines.
Row 1: 1 a bold yellow comic exclamation mark "!" with a thick ink-navy outline inside a small white spiky comic speech burst (an enemy's surprise); 2 an open round military parachute canopy seen from the side, olive green with lighter panels, its suspension cords converging downward to a single point at the bottom centre of the cell, nobody attached; 3 the same parachute half collapsed and folding, cords slack; 4 the same parachute lying deflated and crumpled flat on the ground.
Row 2: 5 a tall open wooden carriage door of an old Parisian-style building in a stone frame, the door leaf swung inward revealing a pitch-dark hallway (a doorway enemies step out of); 6 an open window in a cream stone wall with its blue wooden shutters swung open and a dark interior behind, a small wrought-iron balcony rail at the bottom; 7 a shop's corrugated metal roller shutter raised halfway in a grey concrete frame, dark interior underneath; 8 a round sewer manhole seen slightly from above with its iron cover slid aside, dark hole.
Layout and scale: the final intended canvas is 1448 x 724 pixels, so each implied cell is 362 x 362 pixels. Objects 5 to 7 fill about 90% of the cell height; the parachutes about 80% of the cell width; the burst about 60% of the cell. Keep every pixel inside its own cell.
Style/medium: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship, crisp ink-navy outlines, warm edge highlights, muted teal shadows.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the objects.
Constraints: exact 4x2 orderly grid; no people; no text other than the single exclamation mark; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

## Voix

Générées avec `tools/audio/voices.py` (clé Gemini fournie par le propriétaire, jamais écrite dans le dépôt). Les annonces gardent les trois prises habituelles (2.5 Pro Fenrir et Orus, 3.1 Flash Fenrir) ; les cris (`CRIES`) sont joués plutôt qu'annoncés (« screaming like a cartoon henchman ») et prennent 2.5 Pro pour les trois prises (Fenrir, Orus, Puck), 3.1 Flash ne rendant pas d'audio pour un cri. `voices.py ligne:bc` ne refait que les prises b et c d'une ligne. Chaque prise a été contrôlée : durée, puis transcription aveugle par Gemini 2.5 Flash ; les prises douteuses (`weapon-l-b`, `weapon-l-c`, `weapon-s-b`) ont été refaites. Le texte de chaque ligne est dans `LINES`.
