# Images et sons du lot 2a

Le lot 2a ajoute les otages (POW) et le mode SLUG. Les quatre planches d'otages ont été générées avec le générateur d'images de Codex CLI (`codex exec -m gpt-5.6-sol --enable image_generation`), en donnant des planches existantes en référence ; le son de libération vient de `tools/audio/sfx.py`.

| Fichier | Taille | Usage |
|---|---|---|
| `Art/hostages/civilian.png` | 1360×1020, transparent | Otage générique (`Content/hostages/civilian.json`), 4 × 3 cases de 340 px |
| `Art/hostages/baker.png` | 1360×1020, transparent | Variante de Paris, un boulanger (`baker`) |
| `Art/hostages/boules-player.png` | 1360×1020, transparent | Variante de Marseille, un bouliste (`boules-player`) |
| `Art/hostages/office-worker.png` | 1360×1020, transparent | Variante de Tokyo, un employé de bureau (`office-worker`) |
| `Audio/Sfx/otage-libere.wav` | 22 kHz, 0,53 s | Libération : la corde claque puis un petit arpège montant (`sfx.py`, ajouté en fin de table pour ne pas changer les autres sons) |

Le ton voulu est celui du reste du jeu : des civils sympathiques, un peu comiques, soulagés et reconnaissants ; ni bâillon ni posture humiliante. La case 4 de la planche générique (les bras levés de joie) sert aussi d'icône des otages au HUD et au décompte de fin (`tally.icon` des règles).

L'annonce vocale « Merci ! » est prête dans `tools/audio/voices.py` (ligne `hostage-merci`) mais n'a pas été générée : il faut une clé Gemini (`GEMINI_API_KEY=... python3 tools/audio/voices.py hostage-merci`), puis ajouter `"voice": "Audio/Voices/hostage-merci"` à `Content/hostages/civilian.json` (les variantes en héritent). En attendant, la libération s'annonce par le texte OTAGE LIBÉRÉ ! et le son ci-dessus.

## Planches

Chaque planche sort de Codex en 4 × 3 cases de 362 px (1448×1086). `tools/art/pack_sheet.py hero tools/art/hostages/<otage>.json` la recale en 4 × 3 cases de 340 px : l'image de référence (le salut, case 3) est mise à 1,85 unité de haut à 108 px par unité (les héros font 2,07 à 2,21 unités), les pieds sur une même ligne (12 px du bas) et le milieu des jambes sur la colonne du pivot (45 % de la case). L'option `defringe` du fichier de description recolore les pixels semi-transparents du bord avec leur voisin opaque : les sorties de Codex gardaient un liseré rougeâtre du fond retiré, visible sur un décor clair. Réglages du `sheet` qui en découlent : `columns` 4, `rows` 3, `pivot` (0,45 ; 0,0352941), `cellHeight` 3,1481481. Les sources (`generic-A.png`…) sont les sorties de Codex, à poser à côté des fichiers de description pour refaire une planche ; elles ne sont pas versionnées.

| Case | Pose | État |
|---|---|---|
| 0–1 | Assis, ligoté ; puis se tortillant en appelant à l'aide | `tied` (0, 0, 1, 0 à 3 images/s) |
| 2–4 | Se relève, la corde tombe ; salut militaire ; bras levés de joie | `thanks` (2, 3, 3, 3, 4, 4) |
| 5–7 | Fouille son sac ; tend un cadeau ; pouce levé, mains vides | `give` (5, 6, 6, 7) |
| 8–11 | Course comique | `run` (11 images/s) |

La planche générique a été faite d'abord avec Image 1 = `Art/heroes/chuck-norris.png` (rendu et contours) et Image 2 = `Art/enemies.png` ; les trois variantes ensuite, en parallèle, avec Image 1 = la planche générique (mise en page, poses, échelle) et Image 2 = `Art/heroes/chuck-norris.png`. Les quatre sont sorties transparentes et en grille du premier coup. La consigne qui entoure chaque prompt demande à Codex de vérifier les coins transparents et la grille, d'appliquer au besoin la passe de détourage de `docs/MISSION_TOKYO.md`, et de régénérer une fois si la grille est fausse.

### Otage générique

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet (animation frames) for a run-and-gun arcade game: a tied-up hostage the heroes free, who thanks them, hands over a bonus and runs away. Tone: humorous, warm and kind, never humiliating: the character is relieved and grateful, a little comic.
Input images: Image 1 is the rendering, palette, shading, outline and craftsmanship reference of the game's cast (a hero sheet): match it exactly; Image 2 shows the game's enemies in the same style. The character below is a NEW character, drawn at a slightly smaller scale than those heroes.
Primary request: Create ONE precise sprite sheet containing exactly 12 full-body sprites of the SAME character, arranged as exactly 4 columns x 3 rows of equal square cells. Do not draw cell borders or grid lines. The character is a friendly, scruffy civilian hostage in the spirit of the classic arcade prisoners of war: kind round face, long bushy brown beard, balding head with a fringe of brown hair, white sleeveless undershirt, baggy khaki shorts held up by red suspenders, brown sandals, and a brown canvas satchel on a strap across the chest resting on his hip. The same character must be drawn identically in all 12 frames: same face, hair, outfit, colours, proportions and scale.

Layout and scale: the final intended canvas is 1448 x 1086 pixels, so each implied cell is 362 x 362 pixels. In every implied cell the feet (or the seat when sitting) rest on a common baseline about 16 px above the cell bottom; the body is centred at about 45% of the cell width. STANDING HEIGHT from soles to the top of the head is about 62% of the cell height (about 225 px) in every standing frame, and this scale must be identical in all 12 frames. Keep generous transparent separation and keep every pixel of every sprite completely inside its own cell. The character ALWAYS FACES RIGHT.
Frames, left to right then top to bottom:
Row 1: 1 tied up: sitting on the ground with legs crossed, a thick brown rope wound several times around his arms and chest, hands tied behind his back, looking anxiously to the right with a hopeful face; 2 tied up, wriggling: same sitting pose, shoulders hunched up and body leaning the other way, mouth open calling for help; 3 freed: springing up to standing, the cut rope pieces falling loose around his ankles, delighted surprised face, arms flung out; 4 salute: standing straight, a crisp military salute with the right hand at the forehead, big grateful grin.
Row 2: 5 thanks: standing, both arms waving happily above the head, eyes closed with joy; 6 giving A: standing, one hand rummaging inside the bag at his hip, looking into it; 7 giving B: holding out forward to the RIGHT with both hands a small wrapped gift box with a gold ribbon; 8 giving C: standing, the hands now EMPTY, giving a cheerful thumbs-up to the right.
Row 3: 9 to 12: a comic run cycle facing RIGHT, a happy hurried sprint with knees high and arms pumping, bag bouncing: 9 right foot forward touching the ground; 10 legs passing under the body, body lowest; 11 left leg stretched forward in the air, right leg pushing behind; 12 legs passing again (mirror of 10).
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like animation readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette, high-detail game-ready sprite art.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 4x3 orderly grid; exactly 12 sprites of the same character; no weapons; no extra objects other than the rope, the bag and the gift box where described; no ground; no floor; no shadow under feet; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background; no speed lines or motion trails. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

### Variantes par ville

Même prompt que ci-dessous, où `<DESCRIPTION>` vaut :

| Otage | Description |
|---|---|
| `baker` | a cheerful baker: small thin black moustache, rosy cheeks, white baker's cap, navy-and-white striped long-sleeved shirt with sleeves rolled up, flour-dusted forearms, long white apron over dark trousers, black shoes, and a brown canvas satchel on a strap with the end of a golden bread stick poking out |
| `boules-player` | a jovial retired boules player: tanned face, big white walrus moustache, grey flat cap, short-sleeved sky-blue polo shirt over a round belly, beige linen trousers, brown espadrilles, and a small woven straw shoulder bag on a strap resting on his hip |
| `office-worker` | a polite office worker: neat short black hair, round black glasses, dark navy business suit with the jacket open, white shirt, loosened red tie, black shoes, and a slim black shoulder bag on a strap resting on his hip |

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet (animation frames) for a run-and-gun arcade game: a tied-up hostage the heroes free, who thanks them, hands over a bonus and runs away. Tone: humorous, warm and kind, never humiliating: the character is relieved and grateful, a little comic.
Input images: Image 1 is the game's generic hostage sheet: copy its EXACT layout, frame order, poses, scale, baseline and timing, frame by frame, but draw the new character described below instead. Image 2 is the rendering, palette, shading, outline and craftsmanship reference of the game's cast.
Primary request: Create ONE precise sprite sheet containing exactly 12 full-body sprites of the SAME character, arranged as exactly 4 columns x 3 rows of equal square cells. Do not draw cell borders or grid lines. The character is <DESCRIPTION>. The same character must be drawn identically in all 12 frames: same face, hair, outfit, colours, proportions and scale.

Layout and scale: the final intended canvas is 1448 x 1086 pixels, so each implied cell is 362 x 362 pixels. In every implied cell the feet (or the seat when sitting) rest on a common baseline about 16 px above the cell bottom; the body is centred at about 45% of the cell width. STANDING HEIGHT from soles to the top of the head is about 62% of the cell height (about 225 px) in every standing frame, and this scale must be identical in all 12 frames. Keep generous transparent separation and keep every pixel of every sprite completely inside its own cell. The character ALWAYS FACES RIGHT.
Frames, left to right then top to bottom:
Row 1: 1 tied up: sitting on the ground with legs crossed, a thick brown rope wound several times around his arms and chest, hands tied behind his back, looking anxiously to the right with a hopeful face; 2 tied up, wriggling: same sitting pose, shoulders hunched up and body leaning the other way, mouth open calling for help; 3 freed: springing up to standing, the cut rope pieces falling loose around his ankles, delighted surprised face, arms flung out; 4 salute: standing straight, a crisp military salute with the right hand at the forehead, big grateful grin.
Row 2: 5 thanks: standing, both arms waving happily above the head, eyes closed with joy; 6 giving A: standing, one hand rummaging inside the bag at his hip, looking into it; 7 giving B: holding out forward to the RIGHT with both hands a small wrapped gift box with a gold ribbon; 8 giving C: standing, the hands now EMPTY, giving a cheerful thumbs-up to the right.
Row 3: 9 to 12: a comic run cycle facing RIGHT, a happy hurried sprint with knees high and arms pumping, bag bouncing: 9 right foot forward touching the ground; 10 legs passing under the body, body lowest; 11 left leg stretched forward in the air, right leg pushing behind; 12 legs passing again (mirror of 10).
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like animation readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette, high-detail game-ready sprite art.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 4x3 orderly grid; exactly 12 sprites of the same character; no weapons; no extra objects other than the rope, the bag and the gift box where described; no ground; no floor; no shadow under feet; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background; no speed lines or motion trails. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```
