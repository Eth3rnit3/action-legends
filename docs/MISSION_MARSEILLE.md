# Mission 02 — Mistral Gagnant, Marseille

Mission définie dans `Assets/Resources/Levels/mistral-gagnant.json`. Elle suit Panique à Paname dans le menu (`order: 2`) et n’utilise aucun code propre : décors, sol, musiques et boss passent par les champs du JSON décrits dans [ADDING_A_LEVEL.md](ADDING_A_LEVEL.md).

Parcours de 360 unités en huit vagues : le Vieux-Port avec surtout des fonceurs, le Panier avec plus de lanceurs, puis la Canebière avec des costauds au bouclier, avant l’hélicoptère d’une chaîne d’info en continu fictive au pied de Notre-Dame-de-la-Garde. Ce boss garde les attaques de l’hélicoptère de Paris, avec 1,2 fois ses points de vie.

## Images

Générées avec le générateur d’images intégré de Codex CLI, en prenant `macron-helicopter.png` et `canal-panorama.png` comme références de style et de cadrage. Le boss est une retouche de l’hélicoptère de Paris : même silhouette, mêmes canons et soutes, ce qui garde valables les positions de tir, la zone touchable et les bombes. Le présentateur et la chaîne sont inventés.

| Fichier | Taille | Usage |
|---|---|---|
| `Art/marseille/vieux-port.png` | 2172×724 | Décor 1 |
| `Art/marseille/panier.png` | 2172×724 | Décor 2 |
| `Art/marseille/noailles.png` | 2172×724 | Décor 3, ajouté après la 0.9.0 (prompt dans [ART_RACCORDS.md](ART_RACCORDS.md)) |
| `Art/marseille/mission-logo.png` | 2172×724, transparent | Logo « Mistral Gagnant » de la fiche de mission (champ `logo`), ajouté en 0.9.1 (prompt dans [ART_LOGOS.md](ART_LOGOS.md)) |
| `Art/marseille/canebiere.png` | 2172×724 | Décor 4 |
| `ArtSources/marseille-sol.png` | 2172×724 | Ancienne chaussée répétée devant les acteurs ; depuis 0.8.0 la bande de premier plan reprend le bas de `panier.png` (`map.groundBand`), dont le pavé raccorde |
| `Art/bosses/speedboat-wake.png` | 2048×512, 4 × 1, transparent | Écume du hors-bord (`flight.wake`, 4 images en boucle, derrière la coque) |
| `Art/marseille/news-helicopter.png` | 2161×728, transparent | Boss |

## Musique

Voir [AUDIO.md](AUDIO.md) : arrangement arcade de **La Coupo Santo**.

## Prompts finaux

### vieux-port

```text
Use case: stylized-concept
Asset type: Unity 2D arcade run-and-gun opaque scrolling background panorama
Primary request: Create one single uninterrupted side-on panorama of Marseille's Vieux-Port in 2026, matching the supplied Action Legends Paris assets in finish, palette logic, lighting, framing, detail density, orthographic side view, and horizon height. Show fishing boats and sailboat masts on deep blue water, the polished mirrored ombrière canopy on slim pillars, empty fish-market stalls, Fort Saint-Jean far in the left distance, and the right edge naturally transitioning into the foot of old steep streets.
Scene/backdrop: Bright Mediterranean late afternoon, ochre and cream limestone, terracotta roofs, deep blue sea, turquoise shadows; light mistral in flags and awnings; light recent chaos with a few distant smoke plumes, a toppled scooter and scattered crates, but no people.
Style/medium: Original premium hand-painted pixel-art arcade background, refined 1990s run-and-gun craftsmanship, detailed material shading, strong silhouettes, ink-navy outlines, warm cream and amber highlights, muted teal shadows, rich polished arcade production quality.
Composition/framing: exact 3:1 wide panorama, about 2172x724. Side-on orthographic camera. The walkable baseline is a perfectly straight horizontal boundary at exactly 80% of image height from the top across the entire width. The bottom 20% is uninterrupted pale Marseille limestone pavement/road, flat and fully clear with no foreground objects, props, shadows that read as obstacles, curb intrusions, or characters. All boats, stalls, canopy pillars, scooter, crates and scenery end above the 80% line. No panels, frames, seams, split-screen, borders or vignettes.
Lighting/mood: warm, saturated Mediterranean late-afternoon sunlight, brighter than Paris references.
Constraints: opaque image; one continuous scene; no people or characters; no UI; no watermark; no copied game logos or ripped sprites; no foreground objects in bottom 20%.
```

### panier

```text
Use case: stylized-concept
Asset type: Unity 2D arcade run-and-gun opaque scrolling background panorama
Primary request: Create one single uninterrupted side-on panorama of Marseille's old Le Panier quarter in 2026, matching the supplied Action Legends Paris assets in finish, palette logic, lighting, framing, detail density, orthographic side view, and horizon height. Show narrow ochre and pastel facades with green shutters, laundry lines blowing across the streets, stairs climbing in the background, and a small square with a plane tree and stone fountain. The left edge begins at the foot of old streets near the port; the right edge opens naturally onto a wide boulevard.
Scene/backdrop: Bright Mediterranean late afternoon, ochre and cream limestone, dusty pink and pale yellow walls, terracotta roofs, turquoise shadows; mistral wind moving flags and laundry; light recent chaos with a few distant smoke plumes, a toppled scooter and scattered crates, but no people.
Style/medium: Original premium hand-painted pixel-art arcade background, refined 1990s run-and-gun craftsmanship, detailed material shading, strong silhouettes, ink-navy outlines, warm cream and amber highlights, muted teal shadows, rich polished arcade production quality.
Composition/framing: exact 3:1 wide panorama, about 2172x724. Side-on orthographic camera. Same horizon and scale as a linked scrolling level. The walkable baseline is a perfectly straight horizontal boundary at exactly 80% of image height from the top across the entire width. The bottom 20% is uninterrupted pale Marseille limestone pavement/road, flat and fully clear with no foreground objects, props, shadows that read as obstacles, curb intrusions, or characters. All architecture, tree, fountain, scooter, crates and scenery end above the 80% line. No panels, frames, seams, split-screen, borders or vignettes.
Lighting/mood: warm, saturated Mediterranean late-afternoon sunlight, brighter than Paris references.
Constraints: opaque image; one continuous scene; no people or characters; no UI; no watermark; no copied game logos or ripped sprites; no foreground objects in bottom 20%.
```

### canebiere

```text
Use case: stylized-concept
Asset type: Unity 2D arcade run-and-gun opaque scrolling background panorama
Primary request: Create one single uninterrupted side-on panorama of La Canebière in Marseille in 2026, matching the supplied Action Legends Paris assets in finish, palette logic, lighting, framing, detail density, orthographic side view, and horizon height. Show a wide Haussmann-era boulevard, empty café terraces, tram rails, palm trees, and across the right half a rising distant hill crowned by the basilica Notre-Dame-de-la-Garde with its small golden statue high against the sky. The left edge begins as a wide boulevard for a natural transition from the previous scene.
Scene/backdrop: Bright Mediterranean late afternoon, ochre and cream limestone, terracotta roofs, clear blue sky, turquoise shadows; light mistral in flags and awnings; light recent chaos with a few distant smoke plumes, a toppled scooter and scattered crates, but no people.
Style/medium: Original premium hand-painted pixel-art arcade background, refined 1990s run-and-gun craftsmanship, detailed material shading, strong silhouettes, ink-navy outlines, warm cream and amber highlights, muted teal shadows, rich polished arcade production quality.
Composition/framing: exact 3:1 wide panorama, about 2172x724. Side-on orthographic camera. Same horizon and scale as a linked scrolling level. The walkable baseline is a perfectly straight horizontal boundary at exactly 80% of image height from the top across the entire width. The bottom 20% is uninterrupted pale Marseille limestone pavement/road, flat and fully clear with no foreground objects, props, shadows that read as obstacles, curb intrusions, or characters. All terraces, rails, palms, scooter, crates and scenery end above the 80% line. No panels, frames, seams, split-screen, borders or vignettes.
Lighting/mood: warm, saturated Mediterranean late-afternoon sunlight, brighter than Paris references.
Constraints: opaque image; one continuous scene; no people or characters; no UI; no watermark; no copied game logos or ripped sprites; no foreground objects in bottom 20%.
```

### sol

```text
Use case: stylized-concept
Asset type: Unity 2D horizontally tiled foreground surface texture
Primary request: Create one opaque 3:1 image, about 2172x724, matching the Marseille Action Legends panoramas. The bottom 20% is the critical game surface: a perfectly seamless horizontally tileable band of pale Marseille limestone paving slabs with subtle cracks, restrained joints, warm late-afternoon light, and muted turquoise shadows, designed so the left and right edges join invisibly. The band must be visually consistent across its full width and entirely free of objects.
Scene/backdrop: The upper 80% may be a soft out-of-focus continuation of pale pavement and a low limestone wall base, with no distinct objects or scenery.
Style/medium: Original premium hand-painted pixel-art arcade asset, refined 1990s run-and-gun craftsmanship, detailed stone shading, ink-navy micro-outlines, warm cream and amber highlights, muted teal shadows, rich polished arcade production quality.
Composition/framing: exact 3:1 wide image. A straight horizontal transition at exactly 80% from the top. Bottom 20% is a seamless horizontally tileable foreground strip, consistent scale with the other panoramas, no perspective discontinuity, no border, frame, panel or seam.
Lighting/mood: bright warm Mediterranean late afternoon.
Constraints: opaque image; no people or characters; no props; no text; no UI; no watermark; no logos; bottom strip must tile horizontally without a visible join.
```

### news-helicopter

```text
Use case: precise-object-edit
Asset type: Unity 2D arcade boss sprite with true transparent alpha
Primary request: Repaint the provided Macron helicopter reference as a satirical fictional 24-hour TV news helicopter while preserving its exact geometry.
Input image: the provided macron-helicopter.png is the strict edit target and geometric reference.
Subject: A comic boss helicopter with white and sky-blue livery, one strong red stripe, a large fictional badge reading exactly "DIRECT", and a simple generic fictional channel emblem that does not resemble any real broadcaster. Add a compact dish antenna on the roof and a broadcast camera mounted beside the side door. In the open cockpit, replace the pilot with a clearly fictional caricatured TV anchor: over-styled swept hair, huge white smile, suit and tie, holding a microphone.
Style/medium: Original premium hand-painted pixel-art arcade sprite, refined 1990s run-and-gun craftsmanship, detailed metal shading, strong expressive silhouette, ink-navy outlines, warm cream and amber highlights, muted teal shadows, rich polished arcade production quality. Match the source rendering exactly.
Composition/framing: Preserve the source helicopter's EXACT silhouette, scale, position, proportions and orientation within the 3:1 frame. It faces LEFT; cockpit remains on the left; two gun barrels remain under the nose in exactly the same place; bomb racks remain under the belly; skids and tail rotor remain; NO main rotor blades because the game animates them. Keep the same surrounding transparent margins and do not crop any extremity.
Text (verbatim): "DIRECT"
Constraints: Change only the livery, fictional cockpit anchor, roof dish, and side broadcast camera. Preserve every other geometric invariant. True transparent alpha background around the complete helicopter silhouette. No scene, no ground, no sky, no glow, no smoke, no effects, no drop shadow, no UI, no watermark, no real person, no real channel logo, no copied game logo.
```

### speedboat-wake

Hors-bord sur l’eau (0.8.0) : il naviguait sur le quai, au niveau des héros. La rencontre passe de `atX` 62 à 30 : à 62, l’arène ne montre que 3 à 4 unités d’eau à gauche (le marché aux poissons occupe le reste du panorama `vieux-port`), à 30 elle en montre une douzaine. Son `lane` (-7 à -1 du centre de l’arène, ligne de flottaison `y` 1,9, juste derrière le bord du quai à 1,7) a été mesuré sur une capture de l’arène ; sa `hitbox` descend sous la ligne d’eau pour que les tirs droits et en diagonale des héros sur le quai le touchent. Références : `Art/bosses/speedboat.png`, `Art/marseille/vieux-port.png`, `Art/arcade-fx.png`.

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game-effect flipbook for a looping run-and-gun arcade animation

Input images:
- Image 1 (speedboat.png): scale, palette, crisp dark outlines, and premium hand-painted pixel-art rendering reference; the speedboat faces LEFT, so orient the wake for leftward travel. Do not include the boat or people.
- Image 2 (vieux-port.png): color reference for deep-blue Mediterranean harbour water and warm sun glints; use only to make the foam coloration feel natural on that water. Do not render any water or scenery.
- Image 3 (arcade-fx.png): reference for the game's refined 1990s hand-painted arcade effect sprites, deliberate pixel clusters, highlights, shading, and animation-sheet discipline.

Primary request:
Create ONE precise sprite sheet containing exactly FOUR animation frames arranged as exactly 4 columns × 1 row, four equal square cells, left-to-right chronological order. The final composition is 2048 × 512 pixels, implying four 512 × 512 cells. No borders, separators, grid lines, registration marks, or cell backgrounds.

Each cell contains the same side-view white sea-foam WAKE at water level from a speedboat moving LEFT:
- a bright curling bow-spray splash at the LEFT end;
- a long, low horizontal band of churning white and pale-cyan foam across the middle;
- a broader frothy propeller wash at the RIGHT end with a few small airborne spray droplets.
The four frames are successive moments of one seamless looping animation. Shift foam ripples, individual spray droplets, bubbles, and crest highlights slightly from frame to frame, while keeping the wake's overall silhouette, footprint, scale, waterline, and placement extremely stable. Frame 4 must transition cleanly back to frame 1. No camera movement and no size pulsing.

Exact layout and scale:
In every 512 × 512 cell, the wake spans approximately 92% of the cell width and is centered horizontally. Its waterline/flat lower foam baseline lies at approximately 62% of cell height measured from the top (around y=317 within each cell). Foam rises no more than roughly 22% of cell height above that baseline, except the left bow spray may rise slightly higher; foam dips no more than roughly 6% below the baseline. Keep all marks, droplets, antialiasing, glow, and pixels strictly inside their own cell, with generous transparent margins, especially at every outer canvas edge. All four wakes share identical alignment.

Style/medium:
Original premium hand-painted pixel-art arcade effect, refined 1990s run-and-gun craftsmanship, cohesive with the references. Dense, deliberate pixel clusters; crisp readable silhouette and controlled dark blue edge accents; white foam, pale cyan, light teal, and soft blue-violet shadowing; tiny warm-white sun glints; energetic but clean production sprite. Preserve visibly pixel-art construction rather than smooth vector art, painterly concept art, 3D, or photorealism.

Scene/backdrop:
TRUE fully transparent background everywhere outside the foam. Render no environment at all: no water surface, no sea, no sky, no harbour, no horizon, no ground, no shadow plane, no colored matte. Empty pixels must have genuine alpha 0. All four canvas corners must be fully transparent. Output must preserve a real RGBA alpha channel, not black, white, colored, or checkerboard transparency simulation.

Constraints:
Exactly one image; exactly four frames; exactly 4 columns × 1 row; equal cells; no fifth frame; no cropped effects; no overlap between cells; no boat; no motor; no people; no weapons; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no opaque cell backgrounds.
```
