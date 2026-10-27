# Images et sons du lot 1b

Le lot 1b remplace les trois armes bonus codées en dur par la bibliothèque `Content/weapons` et les armes à lettres de Metal Slug. Les images ont été générées avec le générateur d’images de Codex CLI (`codex exec --skip-git-repo-check -m gpt-5.6-sol --enable image_generation`), en donnant les planches existantes en référence ; les bruitages viennent de `tools/audio/sfx.py`.

| Fichier | Taille | Usage |
|---|---|---|
| `Art/weapon-crates.png` | 1536×1024, transparent | 3 × 2 : caisses H (orange), S (bleue), F (rouge), R (verte), L (violette), caisse grise sans emblème (non utilisée) |
| `Art/weapon-letters.png` | 1536×1024, transparent | 3 × 2 : badges du HUD H, S, F, R, L, badge fusil (armes de base) |
| `Art/weapon-fx.png` | 1536×1024, transparent | 3 × 2 : trois images du jet de flamme, segment de rayon laser (étiré sur la longueur du rayon), roquette, plomb |
| `Audio/Sfx/tir-H-lourde.wav`, `tir-S-pompe.wav`, `tir-F-flammes.wav`, `tir-L-laser.wav` | 22 kHz | Tirs de la mitrailleuse lourde, du fusil à pompe (avec le bruit de pompe), du lance-flammes et du laser (`sfx.py`, ajoutés en fin de table : les autres sons restent identiques à l’octet) |

Le lance-roquettes reprend `roquette-depart.wav` et les fusils de base `tir-D-pistolet-sec.wav`, comme avant. Les balles des fusils et de la mitrailleuse lourde restent le traceur dessiné par le moteur, teinté en jaune pour la mitrailleuse lourde (`color`).

Retouches : sur `weapon-fx.png`, le rayon de la case 4 débordait de 8 pixels dans la case de la roquette ; ces colonnes (x 512 à 535 de la rangée du bas) ont été effacées. Sur les trois planches, les alphas résiduels de 1 à 5 sur 255 ont été mis à 0. Les `.meta` sont ceux de `Art/grenades.png`, sans ses découpes de sprite.

**Voix.** `GEMINI_API_KEY` n’étant pas disponible, aucune annonce n’a été générée. Les lignes sont prêtes dans `tools/audio/voices.py` (`weapon-h` « Mitrailleuse lourde ! », `weapon-s` « Fusil à pompe ! », `weapon-f` « Lance-flammes ! », `weapon-r` « Lance-roquettes ! », `weapon-l` « Laser ! ») : `GEMINI_API_KEY=... python3 tools/audio/voices.py weapon-h weapon-s weapon-f weapon-r weapon-l`, écouter les prises, puis mettre `"voice": "Audio/Voices/weapon-<lettre>"` dans chaque arme de `Content/weapons/`. En attendant, H reprend la voix « Mitrailleuse ! » (`bonus-0`) et R « Roquettes ! » (`bonus-2`) ; S, F et L s’annoncent par le texte et le son des bonus.

## Caisses et badges

Références : `Art/bonus-icons.png` (Image 1) et `Art/grenades.png` (Image 2). La première planche de caisses n’était pas transparente : Codex a fait la passe de détourage (« Remove ONLY the background… », voir `docs/MISSION_TOKYO.md`), puis l’a régénérée une fois ; les badges sont sortis transparents. La consigne autour des prompts demande de vérifier les coins transparents, la grille de 3 × 2 et l’ordre des lettres.

### Caisses (`weapon-crates.png`)

```text
Use case: stylized-concept
Asset type: production transparent sprite atlas for a polished hand-painted pixel-art 2D arcade run-and-gun game: weapon supply crates that the player picks up.
Input images: Image 1 (bonus crate atlas) and Image 2 (grenade atlas) are the exact rendering, palette, outline, shading and craftsmanship references; the crates must have exactly the same design, size, perspective and finish as the crates of Image 1 (rectangular military case, handle slot on the lid, steel corner brackets, gold latches).
Primary request: EXACT grid of THREE COLUMNS by TWO ROWS, six equal square cells on a 1536 x 1024 canvas (each implied cell 512 x 512). Each isolated crate fully inside its cell, exactly centred, occupying 65% of the cell like the crates of Image 1, with generous transparent gutters. Do not draw grid lines or cell borders.
On the front face of each of the first five crates, a large round gold-rimmed emblem plate with ONE huge bold capital letter in thick chunky arcade lettering (cream-white letter with a thick ink-navy outline), filling most of the plate, perfectly readable at tiny size:
Cell 1 (top left): bright orange crate, letter "H" (heavy machine gun).
Cell 2 (top middle): royal blue crate, letter "S" (shotgun).
Cell 3 (top right): crimson red crate with a few soot marks, letter "F" (flame thrower).
Cell 4 (bottom left): green crate, letter "R" (rocket launcher).
Cell 5 (bottom middle): violet-purple crate with cyan trims, letter "L" (laser).
Cell 6 (bottom right): the same crate in plain gunmetal grey, no emblem and no letter.
Style/medium: beautifully shaded original premium hand-painted pixel art, refined 1990s arcade craftsmanship, bold ink-navy outline, warm gold highlights, muted teal shadows, chunky arcade readability at small size, consistent front-isometric angle identical to Image 1.
Constraints: exactly 6 crates; the only text is the five single letters H, S, F, R, L, one per crate, correctly drawn; no other letters, labels, numbers, words, grid lines, background haze, glow, drop shadows or rectangular backgrounds; no explosions, smoke or sparks. True transparent alpha (alpha 0) outside each crate silhouette and in all four corners of the canvas.
```

### Badges du HUD (`weapon-letters.png`)

La planche des caisses est aussi donnée en référence pour les couleurs.

```text
Use case: stylized-concept
Asset type: production transparent HUD icon atlas for a polished hand-painted pixel-art 2D arcade run-and-gun game: the badge shown next to the ammunition counter for the weapon the player holds.
Input images: Image 1 (bonus crate atlas) and Image 2 (grenade atlas) are the exact rendering, palette, outline and craftsmanship references.
Primary request: EXACT grid of THREE COLUMNS by TWO ROWS, six equal square cells on a 1536 x 1024 canvas (each implied cell 512 x 512). Each badge fully inside its cell, exactly centred, occupying 72% of the cell, with transparent gutters. Do not draw grid lines or cell borders.
Each of the first five badges is a chunky round metal medal with a thick gold rim, filled with one colour, carrying ONE huge bold capital letter in thick chunky arcade lettering (cream-white with a thick ink-navy outline) that fills most of the badge, extremely readable when shown 24 pixels tall:
Cell 1 (top left): bright orange badge, letter "H".
Cell 2 (top middle): royal blue badge, letter "S".
Cell 3 (top right): crimson red badge, letter "F".
Cell 4 (bottom left): green badge, letter "R".
Cell 5 (bottom middle): violet-purple badge with cyan rim highlights, letter "L".
Cell 6 (bottom right): the same badge in gunmetal grey showing, instead of a letter, the side silhouette of a black assault rifle pointing right.
Style/medium: beautifully shaded original premium hand-painted pixel art, refined 1990s arcade craftsmanship, bold ink-navy outline, warm gold highlights, very chunky simple shapes for small-size readability, flat front view.
Constraints: exactly 6 badges; the only text is the five single letters H, S, F, R, L, one per badge; no other letters, numbers, words, grid lines, background haze, glow, drop shadows or rectangular backgrounds. True transparent alpha (alpha 0) outside each badge and in all four corners of the canvas.
```

## Projectiles (`weapon-fx.png`)

Références : `Art/arcade-fx.png` (Image 1) et `Art/bonus-icons.png` (Image 2). Sortie transparente du premier coup, halos en alpha partiel.

```text
Use case: stylized-concept
Asset type: production transparent sprite atlas of weapon projectiles for a polished hand-painted pixel-art 2D arcade run-and-gun game (Metal Slug-like weapons).
Input images: Image 1 (the game's effect atlas: muzzle flashes, impacts, explosions, smoke) is the exact rendering, palette, outline and craftsmanship reference for fire, light and smoke; Image 2 (bonus crate atlas) is the reference for solid objects (the rocket of its green crate).
Primary request: EXACT grid of THREE COLUMNS by TWO ROWS, six equal square cells on a 1536 x 1024 canvas (each implied cell 512 x 512). Each object exactly centred in its cell and fully inside it (except the beam of cell 4, see below). Do not draw grid lines or cell borders. Every projectile travels to the RIGHT.
Cell 1 (top left): flame thrower fire, frame 1 of 3: a young tongue of fire just out of the nozzle, compact, white-yellow hot core, bright orange edges, stretched horizontally with its rounded head on the right and short licks trailing to the left, occupying 45% of the cell width.
Cell 2 (top middle): frame 2 of 3: the same fire grown into a big rolling fireball of orange and yellow flame tongues, rounded head on the right, licks trailing to the left, occupying 70% of the cell width.
Cell 3 (top right): frame 3 of 3: the same fire at its end, large, deep orange-red with darker red edges and a few dark navy-grey smoke puffs, breaking up, occupying 80% of the cell width.
Cell 4 (bottom left): a laser beam segment: a perfectly straight horizontal energy beam that runs across the FULL width of the cell from its left edge to its right edge, centred vertically, uniform all along its length (it will be stretched and repeated), white-hot core line with an electric cyan-blue glow on both sides, total thickness about 16% of the cell height, no end caps, no sparks.
Cell 5 (bottom middle): a rocket projectile flying right, same design as the rocket on the green crate of Image 1 (steel and gold body, red nose cone and red tail fins), with a short bright orange-yellow exhaust flame at its tail on the left, occupying 65% of the cell width.
Cell 6 (bottom right): a single shotgun pellet: a small glowing white-orange hot lead ball with a short tapered orange motion streak trailing to the left, occupying 35% of the cell width.
Style/medium: beautifully shaded original premium hand-painted pixel art in the exact visual language of Image 1, refined 1990s arcade craftsmanship, dense pixel clusters, bold readable shapes, ink-navy outlines on solid objects, warm hot fire palette, chunky arcade readability at small size.
Constraints: exactly 6 objects; no text, letters, numbers, grid lines, ground, background haze, background colour or rectangular backgrounds; nothing else in the cells. True transparent alpha (alpha 0) everywhere outside the effects (soft glow may fade with partial alpha) and in all four corners of the canvas.
```
