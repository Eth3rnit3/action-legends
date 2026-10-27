# Images et sons du lot 1a

Le lot 1a donne aux héros la prise en main de Metal Slug : accroupi, tir vers le bas en saut, grenades, couteau au contact, et une flèche GO dessinée. Toutes les images ont été générées avec le générateur d’images de Codex CLI (`codex exec -m gpt-5.6-sol --enable image_generation`), en donnant les planches existantes en référence ; les bruitages viennent de `tools/audio/sfx.py`.

| Fichier | Taille | Usage |
|---|---|---|
| `Art/heroes/chuck-norris.png`, `arnold.png`, `jean-claude.png`, `sylvester.png` | 2040×1360, transparent | Une planche par héros, 6 × 4 cases de 340 px (voir plus bas) |
| `Art/grenades.png` | 1024×1024, transparent | 2 × 2 : grenade lancée, icône du HUD, caisse de grenades, tas de grenades (non utilisé) |
| `Art/go-sign.png` | 1486×730, transparent | Flèche GO après chaque vague bloquée |
| `Audio/Sfx/couteau.wav`, `Audio/Sfx/grenade-lancer.wav` | 22 kHz | Coup de couteau, lancer de grenade (`sfx.py`, ajoutés en fin de table pour ne pas changer les autres sons) |

`Assets/ArtSources/heroes.png` (l’ancienne planche 4 × 4, sortie de `Resources` à l’allègement de la 0.8.0 puisque le jeu ne la charge plus) reste dans le projet : elle sert de référence d’identité aux prompts.

L’annonce vocale « Grenades ! » est prête dans `tools/audio/voices.py` (ligne `bonus-grenades`) mais n’a pas été générée : il faut une clé Gemini (`GEMINI_API_KEY=... python3 tools/audio/voices.py bonus-grenades`), puis ajouter `"voice": "Audio/Voices/bonus-grenades"` au bonus `grenades` de `Content/rules/default.json`. En attendant, le ramassage s’annonce par le texte GRENADES ! et le son des bonus.

## Planches des héros

Chaque héros est généré en deux planches de 4 × 3 (1448×1086), la seconde avec la première en référence pour garder le même dessin et la même échelle :

- **A** : attente (2 images), course (6), saut, saut en visant vers le bas, visée vers le haut, visée en diagonale ;
- **B** : accroupi, marche accroupie (2), tir accroupi, couteau (3), grenade (armer, lâcher), touché, à terre, victoire.

`tools/art/pack_sheet.py hero tools/art/heroes/<héros>.json [aperçu.png]` les assemble en une planche de 6 × 4 cases de 340 px : chaque source est mise à l’échelle pour que son image de référence (attente pour A, couteau prêt pour B) mesure la taille du héros en jeu (`height`, celle de l’ancienne planche : 2,07 à 2,21 unités, à 108 px par unité), les pieds sont posés sur une même ligne (12 px du bas) et le milieu des jambes sur la colonne du pivot (34 % de la case). Le script mesure ensuite les ancrages : `muzzle` vers l’avant (pixel le plus en avant au-dessus des hanches), vers le haut, vers le bas (le plus bas près de l’axe du corps) ou en diagonale selon la case, et `hand` (le point le plus haut et en avant de la main qui lance), puis imprime le JSON à mettre dans `animations.anchors`. Les sources nommées dans `tools/art/heroes/*.json` (`chuck-A.png`…) sont les sorties de Codex, à poser à côté du fichier de description pour refaire une planche ; elles ne sont pas versionnées. Réglages du `sheet` qui en découlent : `columns` 6, `rows` 4, `pivot` (0,34 ; 0,0352941), `cellHeight` 3,1481481 ; `portraitFrame` (0,02 ; 0 ; 0,72) cadre le héros dans la fiche du menu.

| Case | État | | Case | État |
|---|---|---|---|---|
| 0–1 | `idle` | | 12 | `crouch` |
| 2–7 | `run` (`perStride` 0,95) | | 13–14 | `crouchWalk` |
| 8 | `jump` | | 15 | `crouchShoot` |
| 9 | `jumpDown` | | 16–18 | `knife` |
| 10 | `aimUp` | | 19–20 | `throw` (`hand` sur 20) |
| 11 | `aimDiagonal` | | 21–23 | `hurt`, `down`, `victory` (pas encore lus) |

La première planche B de Chuck avait un accroupi trop haut (tête à 76 % de la taille debout : les balles à hauteur de poitrine lui traversaient la tête) ; le prompt B ci-dessous exige une tête à 55 % et toutes les planches B ont été faites avec lui. En jeu, la tête d’un héros accroupi est vers 1,3 à 1,4 unité, juste sous les balles des tireurs (1,39 à 1,61).

Codex reçoit les images de référence avec `-i` dans cet ordre : la ligne du héros découpée dans `ArtSources/heroes.png` (Image 1), `ArtSources/heroes.png` entier (Image 2) et, pour la planche B, sa planche A (Image 3). La consigne qui entoure le prompt lui demande de vérifier les coins transparents et la grille, d’appliquer au besoin la passe de détourage de `docs/MISSION_TOKYO.md`, et de régénérer une fois si la grille est fausse (la planche B d’Arnold a été régénérée puis détourée ; les autres sont sorties transparentes du premier coup).

`<DESCRIPTION>` vaut, selon le héros :

| Héros | Description |
|---|---|
| `chuck-norris` | a rugged ranger hero: feathered auburn-red hair, full red-brown beard, khaki short-sleeved ranger shirt with a gold star badge on the chest, brown belt with holster, blue jeans, brown leather boots, black assault rifle with a long barrel |
| `arnold` | a colossal bodybuilder commando: slicked-back dark brown hair, black sunglasses, square jaw, enormous bare arms, olive-green tactical vest over a black tank top, green camouflage trousers, black combat boots, a heavy black machine gun with a perforated barrel shroud and a brass ammunition belt |
| `jean-claude` | an athletic martial-artist commando: brown swept hair, teal-blue headband with two long tails flowing behind, white tank top, muscular arms, black fingerless gloves, brown holster belt, blue jeans, black combat boots, compact black assault rifle |
| `sylvester` | a muscular jungle commando: long black curly shoulder-length hair, red headband with two long tails flowing behind, olive-green tank top, very muscular arms, dark charcoal cargo trousers, holster belt, black combat boots, black assault rifle |

### Planche A

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet (animation frames) for a run-and-gun arcade game.
Input images: Image 1 shows this exact character in the current game (idle, two run strides, jump; facing right): it is the IDENTITY reference, keep the same face, hair, outfit, colours, weapon and body proportions so he is instantly recognisable. Image 2 is the rendering, palette, shading, outline and craftsmanship reference for the whole cast. 
Primary request: Create ONE precise sprite sheet containing exactly 12 full-body sprites of the SAME character, arranged as exactly 4 columns x 3 rows of equal square cells. Do not draw cell borders or grid lines. The character is <DESCRIPTION>. The same character must be drawn identically in all 12 frames: same face, hair, outfit, colours, proportions, weapon design and scale.

Layout and scale: the final intended canvas is 1448 x 1086 pixels, so each implied cell is 362 x 362 pixels. In every implied cell the feet (or knees when kneeling) rest on a common baseline about 16 px above the cell bottom; the character's body (hips and legs) is centred at about 40% of the cell width so that the weapon can extend to the right. STANDING HEIGHT from soles to the top of the head is about 72% of the cell height (about 260 px) in every standing frame, leaving empty room above the head for a raised weapon; this scale must be identical in all 12 frames. Keep generous transparent separation and keep every pixel of every sprite completely inside its own cell. The character ALWAYS FACES RIGHT.
Frames, left to right then top to bottom:
Row 1: 1 idle stance, gun held level pointing RIGHT at chest height, feet apart; 2 idle breathing frame, identical to frame 1 but torso and gun about 3 px lower; 3 run frame 1: right foot forward touching ground, left leg behind; 4 run frame 2: legs passing under the body, body slightly lowered.
Row 2: 5 run frame 3: left leg extended forward in the air, right leg pushing behind (full stride); 6 run frame 4: left foot forward touching ground, right leg behind (mirror of frame 3); 7 run frame 5: legs passing (mirror of frame 4); 8 run frame 6: right leg extended forward in the air, left leg pushing behind. In all six run frames the gun stays level pointing RIGHT at chest height with both hands.
Row 3: 9 jump: airborne, knees tucked up, gun level pointing RIGHT; 10 jump shooting DOWN: airborne, legs spread apart, gun held with both hands pointing perfectly VERTICALLY DOWN between the legs, barrel tip below the feet; 11 standing aiming UP: feet planted, gun held with both hands pointing perfectly VERTICALLY UP, barrel tip well above the head; 12 standing aiming diagonally up: gun pointing up-right at exactly 45 degrees.
Style/medium: match Image 1 and Image 2 exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like animation readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette, high-detail game-ready sprite art.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 4x3 orderly grid; exactly 12 sprites of the same character; no extra objects; no ground; no floor; no shadow under feet; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Absolutely NO muzzle flash, bullets, smoke, sparks, shell casings, explosions, speed lines or motion trails. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

### Planche B

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet (animation frames) for a run-and-gun arcade game.
Input images: Image 1 shows this exact character in the current game (idle, two run strides, jump; facing right): it is the IDENTITY reference, keep the same face, hair, outfit, colours, weapon and body proportions so he is instantly recognisable. Image 2 is the rendering, palette, shading, outline and craftsmanship reference for the whole cast. 
Primary request: Create ONE precise sprite sheet containing exactly 12 full-body sprites of the SAME character, arranged as exactly 4 columns x 3 rows of equal square cells. Do not draw cell borders or grid lines. The character is <DESCRIPTION>. The same character must be drawn identically in all 12 frames: same face, hair, outfit, colours, proportions, weapon design and scale.

Layout and scale: the final intended canvas is 1448 x 1086 pixels, so each implied cell is 362 x 362 pixels. In every implied cell the feet (or knees when kneeling) rest on a common baseline about 16 px above the cell bottom; the character's body (hips and legs) is centred at about 40% of the cell width so that the weapon can extend to the right. STANDING HEIGHT from soles to the top of the head is about 72% of the cell height (about 260 px) in every standing frame, leaving empty room above the head for a raised weapon; this scale must be identical in all 12 frames. Keep generous transparent separation and keep every pixel of every sprite completely inside its own cell. The character ALWAYS FACES RIGHT.
Image 3 is the first animation sheet of this same character: match its exact character design, colours, proportions and SCALE (standing height about 72% of cell height).
Frames, left to right then top to bottom:
Row 1 (VERY LOW crouch, Metal Slug style, so that bullets fired at chest height fly over him): 1 crouch: kneeling low with one knee ON the ground, torso hunched forward, head pulled down between the shoulders; the TOP OF THE HEAD is only about 55% of the standing height above the baseline (about 145 px, compared with 260 px standing); gun held level pointing RIGHT at about 35% of the standing height; 2 crouch-walk (duck-walk) frame A: same low height (top of head at 55% of standing height), squatting with the right foot stepping forward, gun level pointing RIGHT; 3 crouch-walk frame B: same low height, left foot stepping forward; 4 crouch shooting: identical low kneeling pose to frame 1, braced, shoulders leaning into the level gun.
Row 2: 5 knife ready: standing, gun lowered and held in the left hand along the body, right hand holds a large combat knife drawn back at the shoulder; 6 knife slash: lunging forward, right arm fully extended horizontally to the RIGHT at chest height, knife blade pointing RIGHT; 7 knife follow-through: knife swept down and across in front of the body after the slash; 8 grenade wind-up: standing, gun in the left hand, right arm raised high back behind the head holding a small round olive hand grenade.
Row 3: 9 grenade release: right arm swung forward and up to the RIGHT, open hand above shoulder height, the grenade has just left (the hand is EMPTY, no grenade in the image); 10 hurt: recoiling backwards, head thrown back, gun held loosely; 11 knocked down: lying on his back on the ground, gun beside him, body horizontal along the baseline; 12 victory: standing, fist and gun raised in triumph.
Style/medium: match Image 1 and Image 2 exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like animation readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette, high-detail game-ready sprite art.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 4x3 orderly grid; exactly 12 sprites of the same character; no extra objects; no ground; no floor; no shadow under feet; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Absolutely NO muzzle flash, bullets, smoke, sparks, shell casings, explosions, speed lines or motion trails. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

## Grenades

Références : `Art/bonus-icons.png` (Image 1) et `ArtSources/heroes.png` (Image 2). Le fond n’est pas sorti transparent : Codex a fait la passe de détourage (« Remove ONLY the background… », voir `docs/MISSION_TOKYO.md`), puis effacé les alphas résiduels (1 à 5 sur 255).

```text
Use case: stylized-concept
Asset type: production transparent sprite atlas for a polished hand-painted pixel-art 2D arcade run-and-gun game.
Input images: Image 1 (bonus crate atlas) and Image 2 (hero sprite sheet) are the exact rendering, palette, outline, shading and craftsmanship references; create new objects in the same style.
Primary request: EXACT grid of TWO COLUMNS by TWO ROWS, four equal square cells on a 1024 x 1024 canvas (each implied cell 512 x 512). Each isolated object fully inside its cell, exactly centred, with generous transparent gutters. Do not draw grid lines or cell borders.
Cell 1 (top left): a classic olive-green military hand grenade with a segmented "pineapple" body, a steel fuse lever (spoon) along its side and a pin ring on top, seen from the side, tilted 20 degrees, occupying 45% of the cell; this is the thrown projectile.
Cell 2 (top right): the same hand grenade seen straight from the front, upright, very bold and readable at tiny size, chunky shapes, thick navy outline, occupying 70% of the cell; this is a HUD counter icon.
Cell 3 (bottom left): a military supply crate in exactly the same design, size, perspective and finish as the crates of Image 1, but painted dark olive-khaki with gold corner brackets, with a detailed emblem of TWO crossed hand grenades on its front face, occupying 65% of the cell like the icons of Image 1.
Cell 4 (bottom right): a small pile of three hand grenades, the same grenade design, occupying 55% of the cell.
Style/medium: beautifully shaded original premium hand-painted pixel art, refined 1990s arcade craftsmanship, bold ink-navy outline, warm gold highlights, muted teal shadows, chunky arcade readability at small size, consistent front-isometric angle.
Constraints: exactly 4 objects; no letters, labels, numbers, text, grid lines, background haze, glow, shadows or rectangular backgrounds; no explosions, smoke or sparks. True transparent alpha (alpha 0) outside each object silhouette and in all four corners of the canvas.
```

## Flèche GO

Références : `Art/action-legends-logo.png` (Image 1) et `ArtSources/heroes.png` (Image 2). Même passe de détourage ; l’image a ensuite été recadrée sur son contenu (6 px de marge) pour `Art/go-sign.png`.

```text
Use case: stylized-concept
Asset type: production transparent HUD graphic for a 2D arcade run-and-gun game: the "GO" sign that blinks at the right edge of the screen when the player must move on.
Input images: Image 1 is the game's logo: copy its exact lettering style — chunky italic heavy display letters, hot orange to golden-cream vertical gradient fill, scuffed metal-plate texture with a few bullet holes, thick ink-navy outline, deep navy-blue 3D extrusion going down-left, and a warm golden explosion burst behind. Image 2 shows the game's sprite rendering and palette for consistency.
Primary request: On a 1536 x 1024 canvas, one single composition, centred: the word "GO" in that exact logo lettering, big and bold, followed on its right by a large thick arrow pointing RIGHT in the same material (orange-gold gradient metal plate, ink-navy outline, navy 3D extrusion), the arrow as tall as the letters, made of one solid chevron-headed shaft. A compact golden spark burst behind the letters like the logo, kept within the silhouette area. The whole graphic occupies about 85% of the canvas width and stays fully inside the canvas.
Constraints: exactly the two letters G and O, correctly spelled, no other text; no UI frame; no watermark; no background, no rectangle, no glow halo filling the canvas. True transparent alpha (alpha 0) everywhere outside the graphic and in all four corners.
```
