# Art du lot 3H : les poses manquantes des nouveaux héros

Le propriétaire a fourni Fernandel, Brigitte Bardot et Jeanne d'Arc en planches de 12 poses (`Assets/ArtSources/NouveauxHeros/sprites/`, voir `docs/HEROES_LEGENDS.md`) et Marguerite en 4 images de galop. Les héros du jeu ont 24 poses : les 12 manquantes de chaque héros ont été générées avec Codex CLI (`codex exec -m gpt-5.6-sol --enable image_generation`), en donnant sa planche comme référence d'identité (image 1) et la planche de 24 poses de Chuck Norris comme référence de finition et de poses (image 2). Une planche d'effets a été générée pour les armes et les spéciales.

## Assemblage

`tools/art/pack_sheet.py hero tools/art/heroes/<héros>-game.json` assemble chaque planche du jeu (`Assets/Resources/Art/heroes/<héros>.png`, 6 × 4 cases) depuis `sprites/<héros>.png` (source A) et `sources/<héros>-B.png` (source B, la planche générée, mise à l'échelle de A par `scale` : les foulées de B sont aussi hautes que celles de A). Les ancrages `muzzle`, `back` et `hand` imprimés sont repris dans `Content/heroes/<héros>.json`.

| Case | Pose | Case | Pose |
|---|---|---|---|
| 0 | attente (A0) | 12–13 | marche accroupie (B5, B6) |
| 1–6 | course : A1, B0, B1, A2, B2, B3 | 14 | tir accroupi (B7) |
| 7 | saut (A3) | 15–17 | mêlée (élan, coup, fin : A7 et B8, B9 selon le héros) |
| 8 | tir vers le bas en saut (A6) | 18–19 | grenade (élan, lâcher : A8 et B10) |
| 9 | visée haute (A5) | 20 | spécial (A9) |
| 10 | visée en diagonale (B4) | 21 | blessure (A10) |
| 11 | accroupi (A4) | 22–23 | à terre (B11), victoire (A11) |

Fernandel et Brigitte : cases de 340 px, 108 px par unité, comme les quatre autres héros. Jeanne : cases de 480 × 420 (son étendard), même échelle, planche de 2880 × 1680 (`maxTextureSize` 4096 dans son `.meta`). Ajouts à `pack_sheet.py` : directions `left` (le second revolver de Brigitte, tiré vers l'arrière), `downRight` (une arme pointée en bas devant) et la limite `:unités` (`right:2.0` ignore l'étendard de Jeanne, dessiné au-dessus de son épée). Le calot de Fernandel couché, détaché de son corps dans la planche générée, a été décalé de 40 px pour rester dans sa case.

Marguerite est la planche du propriétaire telle quelle (`Art/heroes/marguerite.png`, 2 × 2). Les sons `legend-whistle` et `legend-charge` sont ceux du propriétaire ; les annonces des spéciales (`special-marguerite`, `special-etendard`, `special-feu-croise`) viennent de `tools/audio/voices.py` (Gemini TTS, trois prises chacune, vérifiées par transcription).

`Art/heroes/legend-fx.png` (3 × 2 cases de 512 px) : vague de lumière de l'épée de Jeanne (cases 0 et 1, son arme `lame-de-lumiere`), éclat doré (2 : traînée et impacts de sa charge), poussière de galop (3 et 4 : traînée de Marguerite), balle de revolver (5 : l'arme `revolver` de Brigitte).

## Prompts

Chaque prompt est enveloppé dans les consignes du lanceur (comme `run-hero.sh` au lot 1a, voir `docs/ART_LOT1A.md`) : générer une image avec les références dans l'ordre, l'enregistrer en PNG RGBA, vérifier la grille et la transparence des coins, refaire une passe « retire seulement le fond » si besoin. Aucune planche n'a eu besoin d'être régénérée ; Codex a refait une fois celle de Jeanne lui-même (un sprite touchait le bord) puis retiré son fond.

### Début commun (planches B)

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet (animation frames) for a run-and-gun arcade game.
Input images: Image 1 is the first animation sheet of this exact character (12 poses: idle, two run strides, jump, crouch, aim up, airborne aim down, melee, grenade, special, hurt, victory; facing right): it is the IDENTITY reference, keep exactly the same face, hair, outfit, colours, weapon, body proportions and SCALE so the new frames can be mixed with those 12 in one animation. Image 2 is a finished 24-pose sheet of another hero of the same game: it is the reference for the rendering, outline and craftsmanship, and shows what each kind of pose (six-frame run, crouch walk, crouch shooting, melee frames, lying down) should look like.
```

### Fernandel

```text
Primary request: Create ONE precise sprite sheet containing exactly 12 NEW full-body sprites of the SAME character as Image 1, arranged as exactly 4 columns x 3 rows of equal square cells. Do not draw cell borders or grid lines. The character is Fernandel as the soldier-prisoner of a 1959 French comedy: long face with a huge toothy smile, cloth side cap, worn khaki-brown French army coat with patches, khaki trousers with puttees, brown boots, satchel and belt, and an old long bolt-action rifle with a wooden stock. Same character drawn identically in all 12 frames.
Frames, left to right then top to bottom (they complete the poses of Image 1):
Row 1 (run cycle, rifle held level pointing RIGHT at chest height with both hands in all four): 1 run contact: left foot planted forward on the ground, right leg behind; 2 run passing: legs passing under the body, right knee coming forward, body slightly lowered; 3 run contact: right foot planted forward on the ground, left leg behind; 4 run passing: left knee coming forward, body slightly lowered.
Row 2: 5 standing aiming diagonally: feet planted, rifle pointing up-right at exactly 45 degrees; 6 crouch-walk (duck-walk) frame A: as low as the crouch of Image 1, squatting with the right foot stepping forward, rifle level pointing RIGHT; 7 crouch-walk frame B: same low height, left foot stepping forward; 8 crouch shooting: kneeling low exactly like the crouch of Image 1, braced, rifle level pointing RIGHT, shoulders leaning into the shot.
Row 3: 9 rifle-butt strike: lunging forward, rifle held horizontally, wooden butt thrust forward to the RIGHT at chest height; 10 melee follow-through: rifle swung down and across in front of the body after the strike; 11 grenade release: right arm swung forward and up to the RIGHT, open EMPTY hand above shoulder height (the grenade has just left, no grenade in the image), rifle in the left hand; 12 knocked down: lying flat on his back along the baseline, cap beside his head, rifle beside him.
```

### Brigitte Bardot

```text
Primary request: Create ONE precise sprite sheet containing exactly 12 NEW full-body sprites of the SAME character as Image 1, arranged as exactly 4 columns x 3 rows of equal square cells. Do not draw cell borders or grid lines. The character is Brigitte Bardot as a 1960s western heroine: voluminous golden blonde hair, black eyeliner, cream blouse with rolled sleeves, burnt orange waistcoat, dark teal trousers, leather gunbelt with holster, brown cowboy boots, steel revolvers. Same character drawn identically in all 12 frames.
Frames, left to right then top to bottom (they complete the poses of Image 1):
Row 1 (run cycle, right arm extended aiming the revolver level to the RIGHT at chest height in all four, hair flowing behind): 1 run contact: left foot planted forward on the ground, right leg behind; 2 run passing: legs passing under the body, right knee coming forward, body slightly lowered; 3 run contact: right foot planted forward on the ground, left leg behind; 4 run passing: left knee coming forward, body slightly lowered.
Row 2: 5 standing aiming diagonally: feet planted, revolver arm pointing up-right at exactly 45 degrees; 6 crouch-walk (duck-walk) frame A: as low as the crouch of Image 1, squatting with the right foot stepping forward, revolver level pointing RIGHT; 7 crouch-walk frame B: same low height, left foot stepping forward; 8 crouch shooting: kneeling low exactly like the crouch of Image 1, both hands on the revolver, arms extended level to the RIGHT.
Row 3: 9 pistol-whip wind-up: standing, revolver raised high above the shoulder, gripped by the barrel, ready to strike; 10 pistol-whip follow-through: arm swept down and across in front of the body after the strike; 11 grenade wind-up: standing, right arm raised high back behind the head holding a small round olive hand grenade, revolver in the left hand; 12 knocked down: lying flat on her back along the baseline, hair spread out, revolver beside her.
```

### Jeanne d'Arc

```text
Primary request: Create ONE precise sprite sheet containing exactly 12 NEW full-body sprites of the SAME character as Image 1, arranged as exactly 4 columns x 3 rows of equal square cells. Do not draw cell borders or grid lines. The character is Jeanne d'Arc: young woman with a short dark brown bob, polished steel plate armour, royal blue tabard with large gold fleurs de lys, brown boots, a longsword in her right hand and, in her left hand, a tall wooden staff with a gold fleur-de-lys finial carrying a royal blue banner with large gold fleurs de lys. NO firearms. She keeps the banner in every frame, exactly as designed in Image 1 (same blue, same gold fleurs de lys). Because of the banner, draw the character a little smaller than in a plain hero sheet so the banner fits inside each cell: STANDING HEIGHT of the body (soles to top of head, banner not counted) about 55% of the cell height, banner top at most 92% of the cell height. Same character drawn identically in all 12 frames.
Frames, left to right then top to bottom (they complete the poses of Image 1):
Row 1 (run cycle, sword held level pointing RIGHT at chest height, banner streaming behind her in all four): 1 run contact: left foot planted forward on the ground, right leg behind; 2 run passing: legs passing under the body, right knee coming forward, body slightly lowered; 3 run contact: right foot planted forward on the ground, left leg behind; 4 run passing: left knee coming forward, body slightly lowered.
Row 2: 5 standing aiming diagonally: feet planted, sword pointing up-right at exactly 45 degrees, banner upright; 6 crouch-walk (duck-walk) frame A: as low as the crouch of Image 1, squatting with the right foot stepping forward, sword level pointing RIGHT, banner held low behind; 7 crouch-walk frame B: same low height, left foot stepping forward; 8 crouch thrust: kneeling low exactly like the crouch of Image 1, sword thrust straight forward to the RIGHT, arm fully extended.
Row 3: 9 sword wind-up: standing, sword raised high above and behind the head, ready to slash; 10 sword follow-through: sword swept down and across in front of the body after the slash; 11 grenade release: right arm swung forward and up to the RIGHT, open EMPTY hand above shoulder height (the bomb has just left, nothing in the hand), sword sheathed at the hip, banner in the left hand; 12 knocked down: lying flat on her back along the baseline, sword and banner on the ground beside her.
```

### Fin commune

```text
Layout and scale: the final intended canvas is 1448 x 1086 pixels, so each implied cell is 362 x 362 pixels. In every implied cell the feet (or knees when kneeling, or the back when lying) rest on a common baseline about 16 px above the cell bottom; the character's body (hips and legs) is centred at about 40% of the cell width so that the weapon can extend to the right. STANDING HEIGHT from soles to the top of the head is about 70% of the cell height (about 250 px) in every standing frame, exactly as tall as in Image 1; this scale must be identical in all 12 frames. Keep generous transparent separation and keep every pixel of every sprite completely inside its own cell. The character ALWAYS FACES RIGHT.
Style/medium: match Image 1 exactly in visual language and Image 2 in finish: premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like animation readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, cohesive palette.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 4x3 orderly grid; exactly 12 sprites of the same character; no extra objects; no ground; no floor; no shadow under feet; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Absolutely NO muzzle flash, bullets, smoke, sparks, shell casings, explosions, speed lines, swooshes or motion trails. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

Pour Jeanne, la phrase « STANDING HEIGHT … about 70% … » de la fin est remplacée par « STANDING HEIGHT of the body is about 55% of the cell height (about 200 px) as stated above, in proportion exactly as in Image 1; this scale must be identical in all 12 frames. »

### Effets (`legend-fx.png`), références : `Art/weapon-fx.png` puis la planche de Jeanne

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game effect sprite sheet for a run-and-gun arcade game.
Input images: Image 1 is the existing weapon effect sheet of the game (flames, laser, rocket, fireball): match its rendering, glow treatment and pixel-art finish exactly. Image 2 is the heroine Jeanne d'Arc of the game (royal blue and gold, fleurs de lys): the new effects use her colours.
Primary request: Create ONE sprite sheet of exactly 6 separate effect sprites arranged as exactly 3 columns x 2 rows of equal cells (final canvas 1536 x 1024, each cell 512 x 512). Every effect is drawn horizontally, travelling to the RIGHT, centred in its cell, filling about 70% of the cell width, with transparent margins all around.
Cells, left to right then top to bottom:
1 sword wave, frame A: a crescent-shaped blade of holy light thrown by a sword, like a thin curved slash (concave side facing LEFT, toward where it came from), bright white core, pale blue edge, thin gold rim, a few small golden sparkles trailing behind;
2 sword wave, frame B: the same crescent a little wider and brighter, sparkles in other places, same size and position;
3 golden impact: a burst of gold and white light rays with small fleur-de-lys shaped sparks, round, for a hit;
4 dust cloud: a puff of warm beige and grey dust kicked up by galloping hooves, rounded clumps, drifting LEFT;
5 dust cloud, second frame: the same puff larger and more spread out, thinner at the edges;
6 revolver bullet: a small bright yellow-orange bullet tracer with a short glowing streak behind it, pointing RIGHT.
Style/medium: premium hand-painted pixel-art arcade effects matching Image 1: dense pixel clusters, crisp shapes, soft additive-looking glow kept INSIDE each cell.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the effects (the glow fades to alpha 0 well before the cell edges).
Constraints: exact 3x2 grid; exactly 6 effects; no text; no letters; no numbers; no UI; no grid lines; no cell borders; no characters; no weapons; no opaque, black or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```
