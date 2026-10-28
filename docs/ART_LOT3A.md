# Images et sons du lot 3a

Le lot 3a ajoute le relief (plateformes, escaliers, terrasses) et les décors destructibles. Toutes les images ont été générées avec le générateur d'images de Codex CLI (`codex exec -m gpt-5.6-sol --enable image_generation`), en donnant en référence le panorama de la mission, la planche d'accessoires de Saint-Rémy (`Art/saint-remy/accessoires.png` : rendu, tonneau intact et brisé) et les véhicules de dépôt (`Art/carriers.png`). Chaque sortie a été regardée avant d'être gardée. La consigne qui entoure chaque prompt demande à Codex de vérifier les coins transparents et la grille, d'appliquer au besoin la passe de détourage de `docs/MISSION_TOKYO.md` et de régénérer une fois si la grille est fausse.

| Fichier | Taille | Usage |
|---|---|---|
| `Art/props/<ville>.png` (paris, marseille, saint-remy, tokyo) | 1536×1600, 3 × 5 cases de 512 × 320 | Cinq décors destructibles par ville, une ligne chacun : barricade, véhicule, étal, caisses (distributeur à Tokyo), baril ; colonnes intact, abîmé, détruit |
| `Art/props/<ville>-terrain.png` | 2016×448, 3 cases de 672 × 448 | Relief de la ville : plateforme (échafaudage, ponton, chantier de fouilles, échafaudage d'acier), escalier montant vers la droite, terrasse |
| `Art/props/debris.png` | 1024×512, 4 × 2 cases | Éclats : planche, échardes, tôle, boulons, verre, pierres, foin, carton |
| `Art/props/objective.png` | 512×256, 2 cases | Flèche-cible qui clignote au-dessus d'une barricade-objectif |
| `Audio/Sfx/bois-coup`, `bois-casse`, `metal-coup`, `metal-casse`, `verre-casse`, `foin-casse` | 22 kHz | Coups et destruction (`tools/audio/sfx.py`, ajoutés en fin de table : les anciens sons sont identiques octet pour octet) |

## Assemblage

`tools/art/pack_sheet.py props tools/art/props/<ville>.json` découpe la sortie de Codex (pièces regroupées par case), garde une échelle par ligne (la plus grande qui fait tenir les trois états), centre chaque image, le bas à 8 px du bas de la case, et imprime la taille de l'image intacte en fractions de case : `cellHeight` d'un décor = hauteur voulue ÷ cette fraction de hauteur. Marseille : le haut de son bidon était coupé par une bande transparente ; la ligne a été effacée (`erase`) et remplacée (`replace`) par une sortie à part (`barrel.png`, prompt plus bas). Saint-Rémy : un liseré rouge laissé par le détourage est repeint (`defringe`).

`tools/art/pack_sheet.py strip tools/art/props/<ville>-terrain.json` coupe les trois pièces aux abscisses données, les rogne, les met à la largeur d'une case, le bas sur le bas, et imprime la hauteur du dessus à chaque dixième de la largeur. Mesures retenues (dessus en fraction de la hauteur de case ; escalier : dessus, début et fin des marches en fraction de la largeur) : Paris plateforme 0,527, escalier 0,56 (0,02 → 0,87), terrasse 0,40 ; Marseille 0,49, 0,75 (0 → 0,89), 0,77 ; Saint-Rémy 0,39, 0,38 (0 → 0,91), 0,34 ; Tokyo 0,28, 0,31 (0,18 → 0,84), 0,28. Une pièce de dessus `f` montre un dessus à `h` si elle est large de `1,5 × h / f` (cases 3:2). Les sources (sorties de Codex renommées `<ville>-props-codex.png`, `<ville>-terrain-codex.png`, `marseille-barrel-codex.png`) se posent à côté des fichiers de description ; elles ne sont pas versionnées.

## Prompts

### Paris : décors destructibles

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet for a run-and-gun arcade game in the spirit of Metal Slug: DESTRUCTIBLE STREET PROPS seen from the side, each in three states. Tone: comic arcade action, never gory.
Input images: Image 1 is a panorama of the mission (Paris during a comic riot: Haussmann buildings, Canal Saint-Martin quays, cobblestones, autumn trees) where these props stand: match its era, colours, light and materials so the props belong in that street. Image 2 is an existing sheet of street props of the same game (painted pixel art, ink outline, wooden barrel intact and smashed): it is the reference for rendering, outline, level of detail and for what a smashed state looks like. Image 3 is an existing vehicle sheet of the game (side view, facing right): the reference for vehicles.
Primary request: Create ONE precise sprite sheet of exactly 15 sprites arranged as exactly 3 columns x 5 rows of equal cells. Do not draw cell borders or grid lines. Each ROW is one prop; its three columns are the SAME prop, drawn at the SAME scale, from the same side view, standing on the same baseline: column 1 INTACT, column 2 DAMAGED (dented, cracked, scorched, pieces missing, but still standing and the same overall size), column 3 DESTROYED (a low wreck or pile of debris lying on the ground where the prop stood, much lower than the intact prop, same width or a little wider).
Layout and scale: the final intended canvas is 1536 x 1600 pixels, so each implied cell is 512 x 320 pixels. In every cell the bottom of the prop (wheels, legs, base) rests on a common baseline about 12 px above the cell bottom, horizontally centred. Every prop of a row keeps exactly the same scale in its three states. Keep every pixel of every sprite completely inside its own cell with transparent margins. Strict side view (no top-down, no isometric): flat tops must read as flat horizontal surfaces a character could stand on.
Rows:
Row 1 police barricade, about 6 times as wide as tall: two grey-blue steel crowd-control barriers side by side, a low wall of sandbags and a pile of square grey paving stones in front of them, an orange-and-white striped plank across the top; destroyed: bent barriers lying flat in a low heap of spilled paving stones and torn sandbags.
Row 2 a small rounded 1960s French city car facing right, faded cream body, grey canvas folding roof, round headlights, thin tyres, its flat roof line clearly horizontal; damaged: dented doors, cracked windows, one headlight hanging, a wisp of smoke from the bonnet; destroyed: a burnt black-and-rust flattened wreck on its rims with broken glass, embers.
Row 3 a Parisian flower seller's market stall: dark green painted wooden counter on four legs with buckets of flowers, a FLAT rectangular red-and-cream striped canvas awning on two iron poles above it, the awning's top a straight horizontal line at about four fifths of the stall height; damaged: torn awning sagging at one corner, buckets knocked over; destroyed: collapsed awning and smashed planks with scattered petals on the ground.
Row 4 a stack of two wooden shipping crates (one on top of the other, the upper one slightly smaller) with rope and stencil-free planks, flat top; damaged: cracked planks, one corner broken off; destroyed: a low pile of broken planks and straw.
Row 5 a red steel fuel barrel standing upright with a small flame hazard pictogram (a symbol, no text), dented rim; damaged: deeply dented, leaking dark fuel, scorched; destroyed: a ruptured, torn-open blackened drum with curled metal petals, smoking.
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 3x5 orderly grid; exactly 15 sprites; no characters; no ground plane; no floor; no cast shadow; no flames or explosions except small embers or a wisp of smoke on a destroyed wreck; no text; no letters; no numbers; no readable signs; no UI; no watermark; no logos or brand marks; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

### Paris : relief

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet for a run-and-gun arcade game in the spirit of Metal Slug: STREET TERRAIN PIECES seen strictly from the side, that the characters climb and stand on.
Input images: Image 1 is a panorama of the mission (Paris during a comic riot: Haussmann buildings, Canal Saint-Martin quays, cobblestones, autumn trees) where these pieces stand: match its era, colours, light and materials. Image 2 is an existing sheet of street props of the same game: the reference for rendering, outline and level of detail.
Primary request: Create ONE precise sprite sheet of exactly 3 sprites arranged as exactly 3 columns x 1 row of equal cells. Do not draw cell borders or grid lines. Every piece fills its whole cell width edge to edge (left and right edges of the piece touch the left and right edges of the cell) and stands on the cell bottom.
Layout and scale: the final intended canvas is 2304 x 512 pixels, so each implied cell is 768 x 512 pixels.
Cells, left to right:
1 a builders' scaffolding platform: grey steel tube scaffolding with diagonal braces and a FLAT deck of wooden planks across the whole width at about 70% of the cell height, a green safety net hanging from the deck rail, open space between the legs (transparent).
2 a stone staircase of a Paris quay rising from LEFT (bottom-left corner, at ground level) to RIGHT (top-right, at about 55% of the cell height): pale limestone steps forming a straight diagonal of small even steps, a solid stone mass under the steps down to the cell bottom, a black iron handrail following the slope.
3 a raised pale limestone quay terrace: a solid block of cut stone the full cell width, its FLAT paved top at about 55% of the cell height, a moulded stone edge, a few iron mooring rings on its face.
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like readability), dense deliberate pixel clusters, detailed painterly shading, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites (between scaffold legs too).
Constraints: exact 3x1 orderly grid; exactly 3 sprites; strict side view, never top-down or isometric; walking surfaces are clean straight lines (flat decks, straight ramp of steps); no characters; no ground plane beyond the pieces; no cast shadow; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.


Third image, debris.png: a sheet of exactly 8 small debris pieces, 4 columns x 2 rows of equal cells of 256 x 256 px (canvas 1024 x 512), each piece centred in its cell and filling about 60% of it, same rendering as Image 2, transparent background: 1 a broken wooden plank, 2 a cluster of wooden splinters, 3 a bent sheet of painted metal, 4 a few bolts and sharp metal shards, 5 a few glass shards with highlights, 6 a cluster of grey stone and paving-stone chunks, 7 a tuft of golden hay, 8 scraps of cardboard and paper. No text, no grid lines, alpha 0 in all corners.

At the end, report the final file names and their pixel sizes.
```

### Éclats (avec la commande de Paris)

```text
Third image, debris.png: a sheet of exactly 8 small debris pieces, 4 columns x 2 rows of equal cells of 256 x 256 px (canvas 1024 x 512), each piece centred in its cell and filling about 60% of it, same rendering as Image 2, transparent background: 1 a broken wooden plank, 2 a cluster of wooden splinters, 3 a bent sheet of painted metal, 4 a few bolts and sharp metal shards, 5 a few glass shards with highlights, 6 a cluster of grey stone and paving-stone chunks, 7 a tuft of golden hay, 8 scraps of cardboard and paper. No text, no grid lines, alpha 0 in all corners.
```

### Marseille : décors destructibles

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet for a run-and-gun arcade game in the spirit of Metal Slug: DESTRUCTIBLE STREET PROPS seen from the side, each in three states. Tone: comic arcade action, never gory.
Input images: Image 1 is a panorama of the mission (Marseille: the Vieux-Port, the Panier hillside lanes, ochre and pink facades, blue shutters, bright Mediterranean sun) where these props stand: match its era, colours, light and materials so the props belong in that street. Image 2 is an existing sheet of street props of the same game (painted pixel art, ink outline, wooden barrel intact and smashed): it is the reference for rendering, outline, level of detail and for what a smashed state looks like. Image 3 is an existing vehicle sheet of the game (side view, facing right): the reference for vehicles.
Primary request: Create ONE precise sprite sheet of exactly 15 sprites arranged as exactly 3 columns x 5 rows of equal cells. Do not draw cell borders or grid lines. Each ROW is one prop; its three columns are the SAME prop, drawn at the SAME scale, from the same side view, standing on the same baseline: column 1 INTACT, column 2 DAMAGED (dented, cracked, scorched, pieces missing, but still standing and the same overall size), column 3 DESTROYED (a low wreck or pile of debris lying on the ground where the prop stood, much lower than the intact prop, same width or a little wider).
Layout and scale: the final intended canvas is 1536 x 1600 pixels, so each implied cell is 512 x 320 pixels. In every cell the bottom of the prop (wheels, legs, base) rests on a common baseline about 12 px above the cell bottom, horizontally centred. Every prop of a row keeps exactly the same scale in its three states. Keep every pixel of every sprite completely inside its own cell with transparent margins. Strict side view (no top-down, no isometric): flat tops must read as flat horizontal surfaces a character could stand on.
Rows:
Row 1 a dock barricade, about 6 times as wide as tall: stacked blue plastic fish crates, a heap of fishing net with cork floats, a wooden pallet and a yellow-and-black striped barrier plank on top; destroyed: a low scattered heap of broken crates, net and pallet planks.
Row 2 a small pale blue three-wheeled Italian-style delivery mini-truck facing right, with a small cab and a flat open cargo bed carrying crates, its cab roof flat; damaged: dented cab, cracked windscreen, a crate fallen, smoke; destroyed: burnt black wreck tipped on its side wheels, crushed cab, embers.
Row 3 a Vieux-Port fishmonger's market stall: a white wooden counter with crushed ice and silver fish, a FLAT rectangular blue-and-white striped canvas awning on two poles, the awning top a straight horizontal line at about four fifths of the stall height; damaged: torn sagging awning, spilled ice; destroyed: collapsed awning over smashed planks, scattered fish.
Row 4 a stack of wooden crates of soap bars and olive oil tins (two crates, one on top of the other), flat top; damaged: cracked, a crate corner broken, a few soap bars falling; destroyed: a low pile of broken planks and soap bars.
Row 5 a blue steel oil drum standing upright with a small flame hazard pictogram (symbol only), rust streaks; damaged: deeply dented, leaking, scorched; destroyed: ruptured torn-open blackened drum with curled metal, smoking.
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 3x5 orderly grid; exactly 15 sprites; no characters; no ground plane; no floor; no cast shadow; no flames or explosions except small embers or a wisp of smoke on a destroyed wreck; no text; no letters; no numbers; no readable signs; no UI; no watermark; no logos or brand marks; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

### Marseille : relief

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet for a run-and-gun arcade game in the spirit of Metal Slug: STREET TERRAIN PIECES seen strictly from the side, that the characters climb and stand on.
Input images: Image 1 is a panorama of the mission (Marseille: the Vieux-Port, the Panier hillside lanes, ochre and pink facades, blue shutters, bright Mediterranean sun) where these pieces stand: match its era, colours, light and materials. Image 2 is an existing sheet of street props of the same game: the reference for rendering, outline and level of detail.
Primary request: Create ONE precise sprite sheet of exactly 3 sprites arranged as exactly 3 columns x 1 row of equal cells. Do not draw cell borders or grid lines. Every piece fills its whole cell width edge to edge (left and right edges of the piece touch the left and right edges of the cell) and stands on the cell bottom.
Layout and scale: the final intended canvas is 2304 x 512 pixels, so each implied cell is 768 x 512 pixels.
Cells, left to right:
1 a harbour loading platform: a FLAT deck of weathered wooden boards across the whole width at about 70% of the cell height, held by thick wooden posts and cross braces with rope coils, open space between the posts (transparent).
2 a Panier limestone stairway rising from LEFT (bottom-left, ground level) to RIGHT (top-right, at about 55% of the cell height): worn ochre stone steps in a straight diagonal of small even steps, a solid stone mass under them down to the cell bottom, a few terracotta flower pots on the side.
3 a raised ochre stone terrace wall: a solid block of rough ochre limestone the full cell width, its FLAT top paved with terracotta tiles at about 55% of the cell height, a little green plant spilling over the edge.
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like readability), dense deliberate pixel clusters, detailed painterly shading, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites (between scaffold legs too).
Constraints: exact 3x1 orderly grid; exactly 3 sprites; strict side view, never top-down or isometric; walking surfaces are clean straight lines (flat decks, straight ramp of steps); no characters; no ground plane beyond the pieces; no cast shadow; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.


At the end, report the final file names and their pixel sizes.
```

### Saint-Rémy : décors destructibles

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet for a run-and-gun arcade game in the spirit of Metal Slug: DESTRUCTIBLE STREET PROPS seen from the side, each in three states. Tone: comic arcade action, never gory.
Input images: Image 1 is a panorama of the mission (Saint-Remy-de-Provence: Provencal village of pale limestone, plane trees, olive groves, the Roman ruins of Glanum, bright summer sun) where these props stand: match its era, colours, light and materials so the props belong in that street. Image 2 is an existing sheet of street props of the same game (painted pixel art, ink outline, wooden barrel intact and smashed): it is the reference for rendering, outline, level of detail and for what a smashed state looks like. Image 3 is an existing vehicle sheet of the game (side view, facing right): the reference for vehicles.
Primary request: Create ONE precise sprite sheet of exactly 15 sprites arranged as exactly 3 columns x 5 rows of equal cells. Do not draw cell borders or grid lines. Each ROW is one prop; its three columns are the SAME prop, drawn at the SAME scale, from the same side view, standing on the same baseline: column 1 INTACT, column 2 DAMAGED (dented, cracked, scorched, pieces missing, but still standing and the same overall size), column 3 DESTROYED (a low wreck or pile of debris lying on the ground where the prop stood, much lower than the intact prop, same width or a little wider).
Layout and scale: the final intended canvas is 1536 x 1600 pixels, so each implied cell is 512 x 320 pixels. In every cell the bottom of the prop (wheels, legs, base) rests on a common baseline about 12 px above the cell bottom, horizontally centred. Every prop of a row keeps exactly the same scale in its three states. Keep every pixel of every sprite completely inside its own cell with transparent margins. Strict side view (no top-down, no isometric): flat tops must read as flat horizontal surfaces a character could stand on.
Rows:
Row 1 a Provencal farmers' barricade, about 6 times as wide as tall: stacked rectangular hay bales, an overturned wooden wheelbarrow, wooden planks and two pitchforks stuck in the hay; destroyed: a low scattered heap of hay and broken planks.
Row 2 a wooden farm cart (charrette) facing right with two big spoked wooden wheels, loaded with a FLAT-topped heap of hay bales, no horse; damaged: one wheel cracked, hay spilling, planks broken; destroyed: collapsed cart with a broken wheel, scattered hay, a wisp of smoke.
Row 3 a Provencal market stall: a wooden counter with bunches of lavender, baskets of olives and soap, a FLAT rectangular yellow-and-blue striped canvas awning on two wooden poles, its top a straight horizontal line at about four fifths of the stall height; damaged: torn sagging awning, baskets knocked over; destroyed: collapsed awning over smashed planks and scattered lavender.
Row 4 a stack of rectangular golden hay bales tied with twine (three bales: two below, one on top centred), flat top; damaged: loosened, twine broken, hay sticking out; destroyed: a low flattened spread of loose hay.
Row 5 an old dark wooden gunpowder keg with iron hoops and a short fuse on top (no text); damaged: cracked staves, black powder trickling; destroyed: burst keg, scattered staves and hoops, black soot, smoking.
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 3x5 orderly grid; exactly 15 sprites; no characters; no ground plane; no floor; no cast shadow; no flames or explosions except small embers or a wisp of smoke on a destroyed wreck; no text; no letters; no numbers; no readable signs; no UI; no watermark; no logos or brand marks; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

### Saint-Rémy : relief

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet for a run-and-gun arcade game in the spirit of Metal Slug: STREET TERRAIN PIECES seen strictly from the side, that the characters climb and stand on.
Input images: Image 1 is a panorama of the mission (Saint-Remy-de-Provence: Provencal village of pale limestone, plane trees, olive groves, the Roman ruins of Glanum, bright summer sun) where these pieces stand: match its era, colours, light and materials. Image 2 is an existing sheet of street props of the same game: the reference for rendering, outline and level of detail.
Primary request: Create ONE precise sprite sheet of exactly 3 sprites arranged as exactly 3 columns x 1 row of equal cells. Do not draw cell borders or grid lines. Every piece fills its whole cell width edge to edge (left and right edges of the piece touch the left and right edges of the cell) and stands on the cell bottom.
Layout and scale: the final intended canvas is 2304 x 512 pixels, so each implied cell is 768 x 512 pixels.
Cells, left to right:
1 an archaeologists' wooden scaffold platform used to restore Roman ruins: a FLAT deck of pale wooden planks across the whole width at about 70% of the cell height, wooden posts and cross braces, a rope and a bucket hanging, open space between the posts (transparent).
2 an old stone stairway rising from LEFT (bottom-left, ground level) to RIGHT (top-right, at about 55% of the cell height): worn pale limestone steps in a straight diagonal of small even steps, tufts of grass between the stones, solid stone under them down to the cell bottom.
3 a raised terrace of big ancient Roman cut stone blocks: a solid block the full cell width, its FLAT top at about 55% of the cell height, weathered pale limestone with a carved moulding and a few cracks.
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like readability), dense deliberate pixel clusters, detailed painterly shading, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites (between scaffold legs too).
Constraints: exact 3x1 orderly grid; exactly 3 sprites; strict side view, never top-down or isometric; walking surfaces are clean straight lines (flat decks, straight ramp of steps); no characters; no ground plane beyond the pieces; no cast shadow; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.


At the end, report the final file names and their pixel sizes.
```

### Tokyo : décors destructibles

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet for a run-and-gun arcade game in the spirit of Metal Slug: DESTRUCTIBLE STREET PROPS seen from the side, each in three states. Tone: comic arcade action, never gory.
Input images: Image 1 is a panorama of the mission (Tokyo at night: neon signs of Shibuya and Akihabara, wet asphalt, elevated railway, cherry trees) where these props stand: match its era, colours, light and materials so the props belong in that street. Image 2 is an existing sheet of street props of the same game (painted pixel art, ink outline, wooden barrel intact and smashed): it is the reference for rendering, outline, level of detail and for what a smashed state looks like. Image 3 is an existing vehicle sheet of the game (side view, facing right): the reference for vehicles.
Primary request: Create ONE precise sprite sheet of exactly 15 sprites arranged as exactly 3 columns x 5 rows of equal cells. Do not draw cell borders or grid lines. Each ROW is one prop; its three columns are the SAME prop, drawn at the SAME scale, from the same side view, standing on the same baseline: column 1 INTACT, column 2 DAMAGED (dented, cracked, scorched, pieces missing, but still standing and the same overall size), column 3 DESTROYED (a low wreck or pile of debris lying on the ground where the prop stood, much lower than the intact prop, same width or a little wider).
Layout and scale: the final intended canvas is 1536 x 1600 pixels, so each implied cell is 512 x 320 pixels. In every cell the bottom of the prop (wheels, legs, base) rests on a common baseline about 12 px above the cell bottom, horizontally centred. Every prop of a row keeps exactly the same scale in its three states. Keep every pixel of every sprite completely inside its own cell with transparent margins. Strict side view (no top-down, no isometric): flat tops must read as flat horizontal surfaces a character could stand on.
Rows:
Row 1 a Japanese road-works barricade, about 6 times as wide as tall: yellow-and-black striped folding barriers, orange traffic cones, a small blinking red warning lamp on top and a row of sandbags at their feet; destroyed: barriers knocked flat, crushed cones and spilled sandbags in a low heap.
Row 2 a small boxy white Japanese kei van facing right, tall boxy body with a FLAT roof, small wheels, a little neon reflection on its windows; damaged: dented, cracked windscreen, a door hanging open, smoke; destroyed: burnt black crushed wreck on its rims, broken glass, embers.
Row 3 a Japanese street food stall (yatai) on wheels: wooden counter with steaming pots, a FLAT rectangular red cloth roof on wooden poles with two glowing red paper lanterns hanging under it (no writing), the roof top a straight horizontal line at about four fifths of the stall height; damaged: torn roof sagging, a lantern fallen; destroyed: collapsed stall with smashed planks and a crumpled lantern.
Row 4 a brightly lit red drinks vending machine (tall box, rows of small coloured cans behind glass, no text); damaged: cracked glass, flickering, dented; destroyed: a smashed toppled vending machine on its side with spilled cans.
Row 5 a yellow steel chemical drum standing upright with a black hazard pictogram (symbol only); damaged: deeply dented, leaking green liquid, scorched; destroyed: ruptured torn-open blackened drum with curled metal, green smoke wisp.
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 3x5 orderly grid; exactly 15 sprites; no characters; no ground plane; no floor; no cast shadow; no flames or explosions except small embers or a wisp of smoke on a destroyed wreck; no text; no letters; no numbers; no readable signs; no UI; no watermark; no logos or brand marks; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

### Tokyo : relief

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet for a run-and-gun arcade game in the spirit of Metal Slug: STREET TERRAIN PIECES seen strictly from the side, that the characters climb and stand on.
Input images: Image 1 is a panorama of the mission (Tokyo at night: neon signs of Shibuya and Akihabara, wet asphalt, elevated railway, cherry trees) where these pieces stand: match its era, colours, light and materials. Image 2 is an existing sheet of street props of the same game: the reference for rendering, outline and level of detail.
Primary request: Create ONE precise sprite sheet of exactly 3 sprites arranged as exactly 3 columns x 1 row of equal cells. Do not draw cell borders or grid lines. Every piece fills its whole cell width edge to edge (left and right edges of the piece touch the left and right edges of the cell) and stands on the cell bottom.
Layout and scale: the final intended canvas is 2304 x 512 pixels, so each implied cell is 768 x 512 pixels.
Cells, left to right:
1 a steel construction scaffolding platform: grey steel tubes with diagonal braces and a FLAT steel grating deck across the whole width at about 70% of the cell height, with a yellow-and-black striped edge board, open space between the legs (transparent).
2 metal stairs of a pedestrian bridge rising from LEFT (bottom-left, ground level) to RIGHT (top-right, at about 55% of the cell height): dark grey steel steps in a straight diagonal of small even steps with a solid concrete-and-steel base under them down to the cell bottom, a thin railing following the slope, a small neon reflection.
3 a raised concrete loading dock: a solid grey concrete block the full cell width, its FLAT top at about 55% of the cell height, a yellow-and-black striped safety edge on its top corner, a few stains.
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like readability), dense deliberate pixel clusters, detailed painterly shading, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites (between scaffold legs too).
Constraints: exact 3x1 orderly grid; exactly 3 sprites; strict side view, never top-down or isometric; walking surfaces are clean straight lines (flat decks, straight ramp of steps); no characters; no ground plane beyond the pieces; no cast shadow; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.


At the end, report the final file names and their pixel sizes.
```

### Marseille : bidon refait, et signal d'objectif

Références : les lignes de la planche de Marseille, le panorama du Panier, `Art/go-sign.png`.

```text
Use case: stylized-concept. Asset type: production-ready transparent 2D game sprite strip for a run-and-gun arcade game in the spirit of Metal Slug: one destructible prop in three states.
Primary request: exactly 3 sprites in exactly 3 columns x 1 row of equal cells, no grid lines. The SAME prop in every cell, same scale, strict side view, standing on a common baseline about 12 px above the cell bottom, horizontally centred: a blue steel oil drum standing upright with a small flame hazard pictogram (symbol only, no text), rust streaks, its top rim drawn as one piece with the drum. Column 1 INTACT; column 2 DAMAGED: deeply dented, leaking dark oil onto the ground, scorched, still standing; column 3 DESTROYED: a ruptured torn-open blackened drum lying on the ground with curled metal petals and a wisp of smoke. The drum is about 0.55 of the cell height tall, the WHOLE drum including its top rim is inside the cell.
Layout and scale: final intended canvas 1536 x 320 pixels, each implied cell 512 x 320 pixels. Match Image 1 exactly in rendering: premium hand-painted pixel art, crisp ink-navy outline, warm edge highlights, muted teal shadows. TRUE transparent background, alpha 0 in all four corners. No text, no logos, no ground plane, no cast shadow, no grid lines.
```

```text
Use case: stylized-concept. Asset type: production-ready transparent 2D game UI marker for a run-and-gun arcade game: the sign floating above a barricade that the players must destroy.
Primary request: exactly 2 sprites in 2 columns x 1 row of equal cells of 256 x 256 px (canvas 512 x 256), no grid lines: cell 1 a bold chunky downward-pointing arrow in bright arcade yellow-orange with a thick dark ink outline and a small white highlight, with a red-and-white circular target crosshair ring under its tip; cell 2 the same marker slightly squashed and brighter (a blink frame). Each fills about 70% of its cell, centred. Style matching Image 3 (chunky arcade sign, outline, shading). No text, no letters, no numbers. TRUE transparent background, alpha 0 in all four corners.
```

