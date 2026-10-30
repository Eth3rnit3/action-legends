# Mission secrète — Tokyo Néon

Mission définie dans `Assets/Resources/Levels/tokyo-neon.json`. Depuis la version 0.10.0, c'est la **mission secrète** du jeu (`"hidden": true`, `order: 5`) : la campagne reste française, de Paris à Avignon, et Tokyo n'en fait plus partie. Elle ne s'ouvre qu'avec son code **SAKURA**, saisi au menu (**CODE MISSION**). Elle n'ouvre aucune autre mission et ne mène à aucune suivante. L'écran de fin de campagne, après Avignon, donne son code. Avant la 0.10.0, elle suivait Prophétie en Provence : un navigateur qui l'avait débloquée la garde, mais ne rouvre plus par elle les missions de la campagne.

Parcours de nuit de 330 unités en huit vagues : Shibuya, puis Akihabara, Asakusa et le quartier de Minato jusqu’au pied de la tour de Tokyo. C’est la première mission où **tous les ennemis sont armés** : un gang de rue fictif équipé de pistolets, de mitraillettes et de fusils à pompe. Ils visent un court instant, figés dans leur pose de tir, puis tirent à hauteur de poitrine : un saut esquive la balle. Le boss est le **Mécha Shogun**, un robot géant au sol qui marche vers les héros et alterne trois attaques : rafale de gatling en balayage, piétinement qui envoie deux ondes de choc à sauter, et salve de missiles à esquiver ou détruire. Il a 1,3 fois les points de vie de l’hélicoptère.

## Images

Générées avec le générateur d’images intégré de Codex CLI, en prenant `canal-panorama.png` et `marseille/vieux-port.png` comme références pour les décors, `enemies.png`, `heroes.png` et `macron-helicopter.png` pour les personnages. Le gang, ses enseignes et le robot sont inventés.

Les deux planches générées passent ensuite par `tools/art/pack_sheet.py` (Pillow, NumPy et SciPy) : chaque image est replacée pour que les pieds tombent sur la ligne de sol du moteur et le milieu des jambes sur le pivot, et le script affiche les bouts de canon, reportés en ancrages `muzzle` dans `Assets/Resources/Content/enemies/gunman-*.json`, et les points du mécha (canon, lance-missiles, pied) dans `Content/bosses/mecha-shogun.json`.

| Fichier | Taille | Usage |
|---|---|---|
| `Art/tokyo/shibuya.png` | 2172×724 | Décor 1 |
| `Art/tokyo/akihabara.png` | 2172×724 | Décor 2 |
| `Art/tokyo/asakusa.png` | 2172×724 | Décor 3, ajouté après la 0.9.0 (prompt dans [ART_RACCORDS.md](ART_RACCORDS.md)) |
| `Art/tokyo/minato.png` | 2172×724 | Décor 4, arène du boss |
| `Art/tokyo/mission-logo.png` | 2172×724, transparent | Logo « Tokyo Néon » de la fiche de mission (champ `logo`), ajouté en 0.9.1 (prompt dans [ART_LOGOS.md](ART_LOGOS.md)) |
| `Art/tokyo/sol.png` | 2172×724 | Chaussée mouillée : seuls les 25 % inférieurs, sous la bordure, sont répétés |
| `ArtSources/tokyo/enemies.png` (dans `Resources` jusqu’à la 0.8.0) | 1680×1086, transparent | Trois tireurs, quatre images chacun : attente, deux foulées, tir |
| `Art/tokyo/mecha-shogun.png` | 1680×1024, transparent | Boss, six images : attente, deux pas, piétinement, gatling, missiles |

## Musique

Voir [AUDIO.md](AUDIO.md) : arrangement arcade de **Sakura Sakura**.

## Prompts finaux

### shibuya

```text
Use case: stylized-concept
Asset type: Unity 2D arcade run-and-gun opaque scrolling background panorama, mission 3 "Tokyo Néon".
Input images: Image 1 and Image 2 are style, finish, detail-density, orthographic side-view, horizon-height, building-scale, and exact ground-layout references only; create a new Tokyo scene.
Style/medium: Original premium hand-painted pixel-art arcade background, refined 1990s run-and-gun craftsmanship, detailed material shading, strong silhouettes, ink-navy outlines, rich polished arcade production quality matching the reference images.
Scene/backdrop: Tokyo in 2026 at NIGHT, deep indigo and violet sky, glowing magenta, cyan and amber neon signs, wet asphalt with neon reflections, light recent chaos: a few distant smoke plumes, a toppled scooter, scattered crates and traffic cones, but no people.
Composition/framing: exact 3:1 wide panorama. Side-on orthographic camera. Match the references' horizon height and scale of buildings. The walkable baseline is a perfectly straight horizontal boundary at exactly 80% of image height from the top across the entire width. The bottom 20% is uninterrupted dark wet asphalt road, flat and fully clear with no foreground objects, props, road markings that read as obstacles, curb intrusion or shadows.
Constraints: opaque image; one continuous scene; no people or characters; no UI; no watermark; no real brand names or logos; neon signs use generic invented katakana/kanji-like shapes or simple pictograms, never real company names; no foreground objects in the bottom 20%.
Primary request: A side-on panorama of a Shibuya-like district: a huge scramble crossing area seen from the side, tall buildings covered with giant video screens and stacked neon signboards, an elevated railway viaduct with a parked commuter train, a small dog statue on a plinth, vending machines, pedestrian barriers. Keep every prop above the straight 80% baseline and preserve the fully clear bottom road band.
```

### akihabara

```text
Use case: stylized-concept
Asset type: Unity 2D arcade run-and-gun opaque scrolling background panorama, mission 3 "Tokyo Néon".
Input images: Image 1 and Image 2 are style, finish, detail-density, orthographic side-view, horizon-height, building-scale, and exact ground-layout references only; create a new Tokyo scene.
Style/medium: Original premium hand-painted pixel-art arcade background, refined 1990s run-and-gun craftsmanship, detailed material shading, strong silhouettes, ink-navy outlines, rich polished arcade production quality matching the reference images.
Scene/backdrop: Tokyo in 2026 at NIGHT, deep indigo and violet sky, glowing magenta, cyan and amber neon signs, wet asphalt with neon reflections, light recent chaos: a few distant smoke plumes, a toppled scooter, scattered crates and traffic cones, but no people.
Composition/framing: exact 3:1 wide panorama. Side-on orthographic camera. Match the references' horizon height and scale of buildings. The walkable baseline is a perfectly straight horizontal boundary at exactly 80% of image height from the top across the entire width. The bottom 20% is uninterrupted dark wet asphalt road, flat and fully clear with no foreground objects, props, road markings that read as obstacles, curb intrusion or shadows.
Constraints: opaque image; one continuous scene; no people or characters; no UI; no watermark; no real brand names or logos; neon signs use generic invented katakana/kanji-like shapes or simple pictograms, never real company names; no foreground objects in the bottom 20%.
Primary request: A side-on panorama of an Akihabara-like electric town: multi-storey arcade and electronics shops with bright vertical banners, giant generic anime-style mascot billboards featuring invented characters, capsule-toy machines, an arcade entrance glowing pink, overhead cables, and a steel railway bridge crossing the background. Keep every prop above the straight 80% baseline and preserve the fully clear bottom road band.
```

### minato

```text
Use case: stylized-concept
Asset type: Unity 2D arcade run-and-gun opaque scrolling background panorama, mission 3 "Tokyo Néon".
Input images: Image 1 and Image 2 are style, finish, detail-density, orthographic side-view, horizon-height, building-scale, and exact ground-layout references only; create a new Tokyo scene.
Style/medium: Original premium hand-painted pixel-art arcade background, refined 1990s run-and-gun craftsmanship, detailed material shading, strong silhouettes, ink-navy outlines, rich polished arcade production quality matching the reference images.
Scene/backdrop: Tokyo in 2026 at NIGHT, deep indigo and violet sky, glowing magenta, cyan and amber neon signs, wet asphalt with neon reflections, light recent chaos: a few distant smoke plumes, a toppled scooter, scattered crates and traffic cones, but no people.
Composition/framing: exact 3:1 wide panorama. Side-on orthographic camera. Match the references' horizon height and scale of buildings. The walkable baseline is a perfectly straight horizontal boundary at exactly 80% of image height from the top across the entire width. The bottom 20% is uninterrupted dark wet asphalt road, flat and fully clear with no foreground objects, props, road markings that read as obstacles, curb intrusion or shadows.
Constraints: opaque image; one continuous scene; no people or characters; no UI; no watermark; no real brand names or logos; neon signs use generic invented katakana/kanji-like shapes or simple pictograms, never real company names; no foreground objects in the bottom 20%.
Primary request: A side-on panorama of the Minato district by the bay: on the left a quiet street with red paper lanterns and a small Shinto torii gate; on the right half a wide open plaza in front of a tall red-and-white lattice steel tower, Tokyo-Tower-like but not branded, fully lit at night, with construction cranes and warning lights. The plaza is a dramatic boss arena. Keep the rightmost 45% visually open and uncluttered in the middle band so a giant boss silhouette reads clearly in front of it. Keep every prop above the straight 80% baseline and preserve the fully clear bottom road band.
```

### sol

```text
Use case: stylized-concept
Asset type: Unity 2D horizontally tiled foreground surface texture for mission 3 "Tokyo Néon".
Input images: Image 1 and Image 2 are finish, palette, pixel-art craft, surface-detail, and exact ground-layout references only; create a new surface texture matching the Tokyo night panoramas.
Primary request: One opaque 3:1 image matching the night Tokyo panoramas. The bottom 20% is the critical game surface: a perfectly seamless horizontally tileable band of dark wet asphalt with subtle cracks, faint magenta and cyan neon reflections and restrained texture, designed so the left and right edges join invisibly. The upper 80% may be a soft out-of-focus continuation of dark asphalt and a low concrete curb base, with no distinct objects or scenery.
Style/medium: Original premium hand-painted pixel-art arcade surface, refined 1990s run-and-gun craftsmanship, detailed material shading, ink-navy accents, polished arcade production quality matching the references.
Composition/framing: exact 3:1 wide panorama. Straight horizontal transition at exactly 80% from the top. Left and right edges must tile seamlessly without a visible seam.
Constraints: opaque image; no people; no props; no text; no UI; no watermark; no logos; no border; no frame; no seam; no distinct scenery; no foreground objects.
```

### enemies

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet for a run-and-gun arcade game
Input images: Image 1 is the exact layout, pose, scale, baseline, silhouette, and rendering reference; Images 2 and 3 are additional rendering, palette, shading, outline, and craftsmanship references only.
Primary request: Create ONE precise sprite sheet containing exactly 12 full-body sprites arranged as exactly 4 columns x 3 rows of equal square cells. The final intended canvas is 1448 x 1086 pixels, so each implied cell is 362 x 362 pixels. Do not draw cell borders or grid lines. One character per ROW; within each row, the same character must be drawn identically with exactly the same face, hair, outfit, colors, proportions, weapon design, and scale across all four animation frames. Every character faces RIGHT.
Layout and animation: In every implied cell, feet share a common baseline about 16 px above the cell bottom; character body mass is centered around 40% of cell width so the weapon may extend right; character total height about 85% of cell height. Keep generous transparent separation and keep every pixel of every sprite inside its own cell. Columns left to right in every row: 1 idle stance holding weapon; 2 running stride A; 3 running stride B with opposite arms and legs; 4 shooting pose with weapon held perfectly straight and HORIZONTAL toward the RIGHT at chest height, arms extended. No muzzle flash, bullets, smoke, recoil effects, shell casings, or motion trails.
Row 1 character: a fictional slim Tokyo neon street-gang pistol gunman, white suit jacket over a loud magenta-and-cyan patterned shirt, black sunglasses, tall black pompadour, pointed shoes, one pistol.
Row 2 character: a fictional submachine-gun gunner, black suit, red tie, rolled sleeves revealing colorful dragon tattoos on both forearms, compact boxy submachine gun held with two hands, short bleached hair, sunglasses.
Row 3 character: a fictional large bulky heavy, mass and silhouette comparable to the shield brute in Image 1 but without shield, padded dark-purple tactical vest over white undershirt, red headband, scarred bald head, pump-action shotgun held with two hands.
Style/medium: match the attached references exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship, dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive limited palette, high-detail game-ready sprite art. Original characters, not copies of any franchise.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 4x3 orderly grid; exactly 12 sprites; no extra objects; no ground; no floor; no shadow under feet; no text; no letters; no UI; no watermark; no logos; no real gang symbols; no grid lines; no cell backgrounds; no opaque or checkerboard background. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

### mecha-shogun

```text
Use case: stylized-concept
Asset type: production-ready transparent boss sprite sheet for a 2D run-and-gun arcade game
Input images: Images 1 and 2 establish the exact premium hand-painted pixel-art sprite rendering, animation-sheet discipline, outlines, shading, palette handling, and 1990s arcade craftsmanship; Image 3 establishes the large boss-scale mechanical detailing and visual density. They are references only.
Primary request: Create ONE precise boss sprite sheet containing exactly 6 full-body views of the SAME original giant robot, arranged as exactly 3 columns x 2 rows of equal square cells. Final intended canvas is 1536 x 1024 pixels, so every implied cell is exactly 512 x 512 pixels. Do not draw borders or grid lines. The robot must be identical in design, colors, proportions, details, size, and scale in all six frames, and always FACES LEFT.
Subject/design: “Mecha Shogun,” an original giant bipedal combat robot styled as samurai armor, not copied from any existing robot, anime, game, toy, or franchise. Red and black lacquered armor plates with gold trim; kabuto helmet with one large golden crescent crest; glowing cyan visor eyes; layered armored shoulder plates, each carrying a box missile pod; RIGHT arm ends in a rotary gatling cannon; LEFT arm ends in a large armored fist; heavy armored legs; broad three-toed feet. Strong compact silhouette, clearly mechanical joints, rivets, worn painted metal, subtle battle scuffs.
Layout and scale: robot faces LEFT in every frame. Feet share a common baseline about 12 px above the bottom of each implied 512 px cell. Body horizontally centered. Total robot height about 92% of cell height, including crest. Maintain the exact same apparent scale across frames. Keep generous transparent separation and keep every pixel of every robot completely inside its own cell.
Frames, left to right then top to bottom: 1 idle standing guard; 2 walking step A with the front leg forward; 3 walking step B with the other leg forward; 4 stomp wind-up with one leg raised high and knee bent, poised to slam; 5 gatling firing POSE, with the right-arm gatling cannon pointing perfectly straight and HORIZONTAL toward the LEFT at chest height; 6 missile launch POSE with shoulder missile-pod hatches visibly open and missile tips visible inside, chest pushed forward.
Style/medium: match the attached references exactly in rendering language: original premium hand-painted pixel-art arcade sprite, refined 1990s run-and-gun craftsmanship, dense deliberate pixel clusters, detailed painterly shading, strong readable silhouette, crisp ink-navy outlines, warm highlights, muted teal shadows, richly detailed game-ready metalwork, cohesive palette.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the robot sprites.
Constraints: exact 3x2 orderly grid; exactly 6 robot sprites; no pilots or people; no extra objects; no ground; no floor; no shadow; no text; no letters; no UI; no watermark; no logos; no grid lines; no cell backgrounds. Absolutely NO muzzle flash, flames, smoke, sparks, projectiles, missiles in flight, bullets, shell casings, or effects in any frame. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

Passe de détourage appliquée ensuite à la planche du robot :

```text
Use case: background-extraction
Asset type: production-ready transparent boss sprite sheet
Input image: the immediately preceding generated 3-column x 2-row Mecha Shogun sprite sheet is the edit target.
Primary request: Remove ONLY the entire brown gradient background and replace it with genuine fully transparent RGBA alpha. Preserve all six robot sprites exactly as they are: identical positions, dimensions, poses, anatomy, armor design, colors, outlines, pixel-art texture, scale, and 3x2 layout. Carefully retain clean antialiased/pixel-art silhouette edges and all internal robot details.
Constraints: change only the background; do not redraw, move, resize, crop, recolor, relight, simplify, or add anything to the robots. The empty area between and around sprites must have alpha 0. All four corners must be fully transparent. No checkerboard, no matte color, no halo, no ground, no shadow, no text, no watermark, no grid lines. Output a true transparent-background PNG.
```
