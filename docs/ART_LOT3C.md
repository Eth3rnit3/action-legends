# Images du lot 3c

Le lot 3c enrichit les cycles des ennemis les plus présents : la course et la fuite passent de deux à quatre images, l'attaque gagne une image de fin de geste (ou de recul du pistolet), l'attente une respiration, la mort par balle une image de chute, le sursaut une réception accroupie. Toutes les images ont été générées avec le générateur d'images de Codex CLI (`codex exec --skip-git-repo-check -m gpt-5.6-sol --enable image_generation`), avec Image 1 = la planche actuelle de l'ennemi (4 × 4) et Image 2 = `Art/heroes/chuck-norris.png` (rendu du casting, course en six images). Les quatre sorties étaient transparentes, en grille 4 × 2 et sans débordement du premier coup ; chacune a été regardée avant d'être gardée.

Les ennemis avaient déjà, depuis le lot 2b, des morts dessinées selon le dégât (`death`, `deathExplosion`, `deathFire`, `deathKnife`, deux à trois images) : aucun ennemi ne faisait que disparaître. Ce lot allonge la mort par balle (et au couteau) d'une image de chute.

| Fichier | Taille | Usage |
|---|---|---|
| `Art/enemies/looter-rusher.png`, `looter-thrower.png` | 1448×2172, 4 × 6 cases de 362 | Lignes 1 à 4 : l'ancienne planche **copiée au pixel près** (ancrages inchangés) ; lignes 5 et 6 : les huit nouvelles images |
| `Art/enemies/gunman-pistol.png` | 1680×2172, 4 × 6 cases de 420 × 362 | Idem |
| `Art/enemies/arlesien.png` | 2048×3072, 4 × 6 cases de 512 | Idem |

Ces planches dépassent 2048 px : leur `.meta` passe à `maxTextureSize` 4096 (comme Jeanne d'Arc) ; leur hauteur reste un multiple de 4, donc compressée en DXT. Réglages du `sheet` inchangés, sauf `rows` 6.

Assemblage : `tools/art/pack_sheet.py hero tools/art/enemies/<ennemi>-3c.json` ; source `P` = la planche du lot 2b (`<ennemi>-2b.png`, copie exacte case pour case), source `C` = la sortie de Codex (`<ennemi>-3c.png`, mise à l'échelle sur la respiration, sa case 3, pour avoir la hauteur de l'ancienne attente). Les sources ne sont pas versionnées.

| Case | Pose | État |
|---|---|---|
| 16, 17 | Course, positions de passage | `run` = 1, 16, 2, 17 (`perStride` 1,4) |
| 18 | Fin du coup (pied-de-biche, poing, bouteille lancée) ou recul du pistolet | `attack` = 3, 18 (8 i/s ; en boucle pour le pistolero, qui a un état `aim` = 3) |
| 19 | Attente, respiration | `idle` = 0, 19 (2 i/s ; pas pour le pillard au pied-de-biche, dont la barre change de position) |
| 20 | Renversé, à mi-chute | `death` = 10, 20, 11 ; `deathKnife` = 7, 10, 20, 11 |
| 21, 22 | Fuite paniquée, positions de passage | `flee` = 8, 21, 9, 22 |
| 23 | Réception accroupie après le sursaut | `surprised` = 6, 23 |

| Ennemi | Personnage (`{who}`) | Fin du coup (`{attack}`) |
|---|---|---|
| `looter-rusher` | the hooded looter armed with a rusty crowbar: black hoodie with the hood up, black balaclava showing only the eyes, black cargo trousers, dark sneakers, rusty red-brown crowbar | the crowbar swung all the way down and through in front of him after a big overhead strike, body leaning forward, follow-through |
| `looter-thrower` | the hooded looter with a khaki backpack who throws bottles: black hoodie with the hood up, black balaclava, khaki backpack, black cargo trousers, dark sneakers, a glass bottle with a red rag in one hand | follow-through just after throwing: the throwing arm extended forward and down, the hand empty and open, body leaning forward |
| `gunman-pistol` | the flashy gangster with a black pompadour, sunglasses, white suit jacket over a magenta patterned shirt, white trousers, black shoes, and a black pistol | the recoil of a shot: same aiming stance as his attack frame but the pistol kicked up a little by the recoil, the arm slightly bent, a thin wisp of grey smoke at the muzzle (no flash) |
| `arlesien` | the burly Provencal brawler in traditional Arlesian costume: wide-brimmed flat black felt hat, thick black moustache, white shirt with rolled-up sleeves, black waistcoat, red neckerchief, wide red sash belt, dark grey striped trousers, black buckled shoes, bare fists (no weapon) | follow-through of his punch: the punching arm fully extended forward, the fist past the target, body leaning into it, the other fist pulled back near the chest |

Prompt (celui du pillard, avec `{who}` et `{attack}` remplacés ; les autres changent aussi la taille : 1680 × 724 en cases de 420 × 362 pour le pistolero, 2048 × 1024 en cases de 512, pieds à 27 px, pour l'Arlésien) :

```text
Use case: stylized-concept
Asset type: production-ready transparent 2D game sprite sheet (animation frames) for a run-and-gun arcade game in the spirit of Metal Slug: in-between animation frames of an existing enemy, to make its cycles smoother. Tone: comic arcade action, slapstick, never gory: no blood, no wounds, no gore.
Input images: Image 1 is the existing sprite sheet of this enemy, 4 columns x 4 rows: cell 1 idle, cells 2 and 3 the two run strides, cell 4 attack, then off guard, surprised, hurt, panic run (cells 9 and 10), shot down and lying (cells 11 and 12), blown up, burning. Draw EXACTLY the same character (face, outfit, colours, weapon, proportions, scale and rendering) as Image 1. Image 2 is the rendering, palette, shading, outline and craftsmanship reference of the game's cast (a hero sheet with a six-frame run cycle).
Primary request: Create ONE precise sprite sheet containing exactly 8 full-body sprites of the SAME character, arranged as exactly 4 columns x 2 rows of equal cells. Do not draw cell borders or grid lines. The character is the hooded looter armed with a rusty crowbar: black hoodie with the hood up, black balaclava showing only the eyes, black cargo trousers, dark sneakers, rusty red-brown crowbar. The same character must be drawn identically in all 8 frames, at the same scale as in Image 1.

Layout and scale: the final intended canvas is 1448 x 724 pixels, so each implied cell is 362 x 362 pixels. In every implied cell the feet rest on a common baseline about 16 px above the cell bottom (the lying or falling body too: its lowest point on that baseline, except frame 5 which is in the air); the body is centred at about 40% of the cell width. STANDING HEIGHT from soles to the top of the head is the same as in Image 1 (about 88% of the cell height) and this scale must be identical in all 8 frames. Keep generous transparent separation and keep every pixel of every sprite completely inside its own cell. The character ALWAYS FACES RIGHT, like Image 1.
Frames, left to right then top to bottom:
Row 1: 1 run passing A: the in-between of his run cycle between the two strides of Image 1 (cells 2 and 3): legs passing each other under the body, the rear foot lifted high behind, the front knee bent, body at its highest point, arms and weapon in mid-swing, same running attitude as Image 1; 2 run passing B: the opposite in-between: the OTHER leg lifted behind, arms swung the other way; 3 attack follow-through: the crowbar swung all the way down and through in front of him after a big overhead strike, body leaning forward, follow-through; 4 idle breathing: the idle pose of Image 1 cell 1, shoulders and chest slightly lowered as he breathes out, everything else identical.
Row 2: 5 shot down mid-fall: the in-between of Image 1 cells 11 and 12: knocked off his feet by a hit, body horizontal in the air about knee height above the baseline, falling onto his back, arms and legs flung up, eyes shut, weapon slipping from his hand; 6 panic run passing A: the in-between of his panic run (Image 1 cells 9 and 10): fleeing to the RIGHT, head turned back over the shoulder with a terrified face, legs passing under the body, one foot lifted high behind, arms flailing; 7 panic run passing B: the opposite in-between, the other leg lifted, arms flailing the other way; 8 startled landing: just after the surprised jump of Image 1, landing in a crouch, knees bent, eyes wide, arms still raised, weapon in hand.
Style/medium: match the reference images exactly in visual language: original premium hand-painted pixel-art arcade sprites, refined 1990s run-and-gun craftsmanship (Metal Slug-like animation readability), dense deliberate pixel clusters, detailed painterly shading, strong readable silhouettes, crisp ink-navy outlines, warm edge highlights, muted teal shadows, cohesive palette, high-detail game-ready sprite art.
Scene/backdrop: TRUE fully transparent alpha background everywhere outside the sprites.
Constraints: exact 4x2 orderly grid; exactly 8 sprites of the same character; no ground; no floor; no shadow under feet; no text; no letters; no numbers; no UI; no watermark; no logos; no grid lines; no cell borders; no cell backgrounds; no opaque or checkerboard background; no speed lines or motion trails; no blood. Preserve genuine RGBA transparency with alpha 0 in all empty areas and all four corners.
```

Consigne autour du prompt (comme aux lots 2a et 2b) : enregistrer `<ennemi>-new.png` à la taille exacte, vérifier avec Python que la toile, les coins et les séparations entre cases sont transparents et qu'aucun sprite ne déborde de sa case, passer au besoin un détourage qui ne retire que le fond (`docs/MISSION_TOKYO.md`), régénérer une fois si la grille est fausse.
