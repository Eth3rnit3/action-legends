# Mission 04 — Sur le Pont d'Avignon

Mission définie dans `Assets/Resources/Levels/sur-le-pont-d-avignon.json`, à **Avignon**, pendant son festival de théâtre. Quatrième et dernière mission de la campagne (`order: 4`) : elle suit Prophétie en Provence. Gagner Saint-Rémy la débloque et révèle son code, **PAPALINE** ; la gagner termine la campagne, et l'écran de victoire donne alors le code de la mission secrète, Tokyo Néon (**SAKURA**, voir [MISSION_TOKYO.md](MISSION_TOKYO.md)).

Parcours de 420 unités en neuf vagues et quinze rencontres, à travers six panoramas :
1. le Rhône, avec le pont Saint-Bénézet et sa chapelle ;
2. l'intérieur des remparts et une porte de la ville, couverts d'affiches du festival ;
3. la rue des Teinturiers, ses roues à aubes et sa chapelle ;
4. la place des Carmes, avec l'église Saint-Symphorien et le **Bar des Carmes** ;
5. la place de l'Horloge, avec l'hôtel de ville, l'opéra et son carrousel ;
6. le Palais des Papes, sous un ciel d'orage, pour le boss.

La troupe du festival défend la ville :
- le **mime** frappe au contact, plus vif que l'Arlésien ;
- le **comédien** en toge lance des **tomates** de loin ;
- l'**Arlésien au fouet**, venu de Saint-Rémy, frappe à distance.

Un mime enfermé dans sa boîte invisible ou un comédien qui déclame devant un crâne ne voient pas venir l'équipe. Au milieu du parcours, le taureau de Camargue bloque toujours la route (mini-boss repris de Saint-Rémy). Devant le Palais attend **l'Antipape**. Il alterne cinq attaques :
- des boules de feu tirées de sa crosse ;
- des cloches annoncées par un cercle au sol, qui tombent ensuite dessus (il faut sortir du cercle ou sauter) ;
- l'appel de la troupe du Palais à la rescousse ;
- une excommunication en balayage ;
- le grand carillon, qui couvre l'arène de cercles où il faut trouver la faille.

La mission compte neuf otages, dont des **affichistes** du festival. Musique : *Sur le pont d'Avignon* en arcade, version majeure pour le parcours, mineure et plus rapide pour le boss. Les caisses d'armes donnent surtout des fusils à pompe ; `parTime` 280 s.

## Ce que la mission a demandé au moteur

| Ajout | Pourquoi |
|---|---|
| `hidden` (mission) | Tokyo sort de la campagne : une mission secrète ne s'ouvre que par son code, n'ouvre rien d'autre, ne compte ni dans la suite des missions ni dans le compteur du menu tant qu'elle n'est pas trouvée. La fin de la campagne donne son code. Voir [ADDING_A_LEVEL.md](ADDING_A_LEVEL.md). |
| `throw.sprite` (ennemi lanceur) | Les tomates du comédien : un lanceur ne savait jeter qu'une bouteille. Le dessin (`BossSprite`, comme les projectiles des boss) ne change que l'image ; `projectile` reste la règle de vol. |

Rien d'autre n'a changé dans la simulation. Le reste de la mission (rencontres, relief, décors destructibles, véhicule, accessoires) reprend le plan de Saint-Rémy, avec ses ennemis, ses otages et ses décors à elle.

## Images

Toutes les images ont été générées avec Codex CLI (`codex exec -m gpt-5.6-sol --enable image_generation`), en donnant en référence des images de Saint-Rémy. Chaque image a été regardée sur fond gris avant d'être gardée.

| Fichier | Taille | Usage |
|---|---|---|
| `Art/avignon/rhone.png`, `remparts.png`, `teinturiers.png`, `carmes.png`, `horloge.png`, `palais.png` | 2172×724 | Les six panoramas, dans cet ordre. Le premier plan reprend le bas de `horloge.png` (`map.ground`). |
| `Art/enemies/mime.png`, `comedien.png` | 2048×2048, 4 × 4 cases de 512 | Attente, deux foulées, coup, deux poses d'inattention, sursaut, touché, deux fuites, renversé, à terre, soufflé ×2, en feu, carbonisé |
| `Art/avignon/tomate.png` | 2048×512, 4 × 1 | Trois images de tomate en vol (`throw.sprite`) et l'éclaboussure |
| `Art/avignon/antipape.png` | 2048×1024, 4 × 2 | Attente, deux pas, crosse pointée, crosse levée, bras écartés, touché, assis sur sa crosse (`wreck`) |
| `Art/avignon/antipape-fx.png` | 2048×1536, 4 × 3 | Cercle annoncé (4 images), boule de feu (3 images + éclat), chute de cloche (4 images) ; la cloche sert aussi de `missile` |
| `Art/hostages/affichiste.png` | 1360×1020, 4 × 3 cases de 340 | Otage local, l'affichiste (`Content/hostages/affichiste.json`, qui étend `civilian`) |
| `Art/avignon/mission-logo.png` | 2172×724 | Logo « Sur le Pont d'Avignon » de la fiche de mission (prompt dans [ART_LOGOS.md](ART_LOGOS.md)) |

Les accessoires de rue (`Art/saint-remy/accessoires`), les raccords, le relief et les décors destructibles sont ceux de Saint-Rémy. La colonne romaine brisée a été remplacée par le chevalet d'affiches.

Sons : `Audio/Sfx/avignon/crosse-tir` (chaque boule de feu), `cloche-alerte` (les cercles apparaissent), `cloche-impact` (la cloche tombe), synthétisés par `tools/audio/cloches.py`. Musiques : `Audio/Music/pont-avignon-arcade` et `pont-avignon-boss`, par `tools/audio/pont_avignon.py` (voir [AUDIO.md](AUDIO.md)).

## Réglages

- **Mime** (`Content/enemies/mime.json`) : un `melee` comme l'Arlésien, plus rapide (6 au lieu de 5,6), qui attaque plus tôt.
- **Comédien** (`Content/enemies/comedien.json`) : un `thrower` comme le lanceur de bouteilles de Paris, sans grenade. Il garde 5,5 u de distance et lance une tomate toutes les 1,5 s ; la main est mesurée sur la case du lancer.
- **Planches** : `pivot` (0,4 ; 0,0527344), `cellHeight` 2,88.
- **Antipape** (`Content/bosses/antipape.json`) : les chiffres de Nostradamus (PV 320 à 1080, +65 % par joueur), avec ses propres images, sons et textes. Renforts : mime, comédien et Arlésien au fouet.
- **Durées** mesurées par `AgentScripts/VerifyAvignon.cs` (bot invulnérable de Détente à Légende, vulnérable en Slug) : seul 92 / 117 / 135 / 170 s, Slug 121 s (4 continues) ; à quatre 99 / 100 / 112 / 114 s, Slug 107 s (1 continue).

## Prompts finaux

Assemblage :
- `tools/art/pack_sheet.py hero tools/art/enemies/mime.json` (et `comedien.json`) : la planche de l'Arlésien sert de mesure de hauteur (`arlesien-ref.png`), la sortie de Codex est `mime-new.png` ;
- `tools/art/bosses/antipape.json` : la planche de Nostradamus sert de mesure ;
- `tools/art/hostages/avignon.json` : pour l'affichiste.

Comme aux lots précédents, les sources ne sont pas versionnées.

### Panoramas

Références : `saint-remy/centre-ville.png`, `ruelles-favier.png`, `alpilles.png`.

```text
You are generating six background panoramas for "Action Legends", a Unity 2D arcade run-and-gun game, for a new mission in AVIGNON (Provence, France) during its summer theatre festival. Use your image generation tool once per panorama, then save each result in the current directory under the exact file name given, as an opaque PNG resized to exactly 2172x724 pixels (3:1). Look at every result before keeping it: if the line where the facades meet the pavement is not a straight horizontal line at the same height as in the references (about 84% of the image height), if the bottom band is not a clear, flat, uninterrupted light pavement, if there are people, if there are frames or seams, regenerate it once. At the end, list the six files with their pixel sizes and give the final prompt you used for each.

Reference images in the current directory (style, finish, detail density, light, orthographic side view, horizon height, building scale and ground layout; every panorama is a NEW scene): centre-ville.png, ruelles-favier.png, alpilles.png (the Saint-Rémy-de-Provence mission of the same game). Match them exactly: original premium hand-painted pixel-art arcade background, refined 1990s run-and-gun craftsmanship, detailed material shading, ink-navy outlines, bright Provençal summer daylight, deep blue sky with small white clouds, warm pale limestone, plane trees with mottled bark.

Common brief for every panorama:
Composition/framing: exact 3:1 wide panorama, side-on orthographic camera, same horizon height and building scale as the references. The facades' base line is a perfectly straight horizontal boundary at the same height as in centre-ville.png across the entire width; below it, the same flat light limestone paving as centre-ville.png, fully clear: no objects, no people, no shadows of objects, no curb intrusion.
Festival touch: the Avignon theatre festival is on: many colourful theatre posters tied to railings, plane trunks and walls (generic illustrations with short invented French show titles, not real ones), a few garlands and banners. Light recent chaos (a few distant smoke plumes, an overturned café chair or scattered posters on the far facades only, no people).
Edges: the leftmost and rightmost 8% of the image show quiet, generic content (plain facade wall, plane tree foliage or sky) with no landmark, sign or object cut in half, so the panorama can sit next to another one.
Constraints: opaque image; one continuous scene; no people or characters; no animals; no UI; no watermark; no real brand names or logos except the bar sign requested below; shop signs use short generic French words.

1. avignon-rhone.png — Primary request: the bank of the Rhône at the foot of the city walls: on the left half, seen across the wide green-blue Rhône river in the middle distance, the famous broken medieval Pont Saint-Bénezet with its four remaining stone arches stopping in mid-river and the small Romanesque Saint-Nicolas chapel standing on it; plane trees and a riverside promenade with an iron railing; on the right half, the high crenellated limestone city ramparts of Avignon with a square machicolated tower, and the Rocher des Doms garden with cypresses on top behind.

2. avignon-remparts.png — Primary request: walking along the inside of the crenellated medieval ramparts: a monumental city gate in the walls (two machicolated towers framing a pointed arch with a portcullis), the battlements and wall-walk running across the whole image, plane trees along the boulevard, a few old townhouses with green shutters and terracotta roofs, festival posters stuck all along the wall.

3. avignon-teinturiers.png — Primary request: the picturesque Rue des Teinturiers: a narrow canal (the Sorgue) running along the back of the scene with large old wooden paddle waterwheels turning beside it, small stone footbridges, tall plane trees, old dyers' houses with ochre and pink facades, wooden shutters and climbing vines, a small Gothic chapel of penitents with a bell-gable, café tables under the trees in the background.

4. avignon-carmes.png — Primary request: the Place des Carmes: on the left, the tall Gothic Église Saint-Symphorien-les-Carmes with its square stone bell tower and rose window; in the middle and right, the lively square with plane trees and festival theatre posters, and a friendly corner bar with a wide terrace, a burgundy awning and a hand-painted sign reading exactly "BAR DES CARMES" (this exact text, nicely lettered and correctly spelled), next to small houses with balconies.

5. avignon-horloge.png — Primary request: the Place de l'Horloge: the Hôtel de Ville with its tall Gothic clock tower (Jacquemart tower with a bell), the 19th-century theatre-opera facade with statues of Molière and Corneille seated by the entrance, an old-fashioned painted carousel with horses under a striped canopy, café terraces with parasols, plane trees, festival banners.

6. avignon-palais.png — Primary request: the immense Place du Palais in front of the Palais des Papes: the colossal Gothic fortress-palace of the Popes fills most of the width, with its massive pale stone walls, tall pointed blind arcades, two pointed towers framing the main gate (Porte des Champeaux), machicolations and battlements; on the left, the Romanesque cathedral Notre-Dame-des-Doms with its bell tower topped by a golden statue of the Virgin; on the right, the Petit Palais. Dramatic late-afternoon light with a few darker storm clouds gathering over the palace (it is the final boss arena).
```

### Mime, comédien et affichiste

Références : `enemy-layout.png` (la planche de l'Arlésien au fouet), `hero-reference.png` (`Art/heroes/chuck-norris.png`), `hostage-layout.png` (la planche du peintre).

```text
Use case: stylized-concept
Asset type: production-ready transparent Unity 2D sprite sheet
Input images: Image 1 enemy-layout.png is the exact layout, pose, facing, scale, framing and baseline reference; Image 2 hero-reference.png is the exact rendering style, palette, detailed shading, ink-navy outline, proportions and craftsmanship reference.
Primary request: Create mime.png, exactly a 4 columns x 4 rows sprite sheet. One full-body sprite centered in each equal cell, identical character and identical scale throughout. The character is a wiry agile Avignon summer festival street mime: white face paint, black eyebrows, one painted black tear under one eye, red lips, black beret, Breton navy-and-white striped long-sleeve shirt, black suspenders, black trousers, white gloves, black slippers.
Pose order left-to-right, top-to-bottom, copying Image 1 exactly: 1 idle guard stance facing right; 2 run stride A right; 3 run stride B right; 4 huge theatrical punch forward to the right with a white-gloved fist; 5 unaware miming trapped in an invisible box, palms flat on invisible walls, facing right; 6 unaware pulling an invisible rope, facing right; 7 surprised jumping hands up facing right; 8 hurt flinching back; 9 fleeing stride A facing LEFT panicked; 10 fleeing stride B facing LEFT; 11 knocked backward off feet; 12 lying flat on back, comic knockout with crossed or swirled eyes; 13 blown into air by comic explosion, torn/scorched clothes and smoke puffs, no gore; 14 tumbling upside down with smoke; 15 running in place engulfed in comic flames, clothes scorched; 16 standing charred black with white eyes and smoke curls.
Style/medium: original premium hand-painted pixel-art game sprites, refined 1990s run-and-gun arcade craftsmanship, matching Image 2 exactly.
Composition/framing: strict uniform 4x4 orthographic grid; every sprite fully contained inside its own cell with generous clear padding; same scale; feet/base near same bottom baseline as Image 1.
Scene/backdrop: genuine alpha-transparent empty background.
Constraints: output square; EXACTLY 16 sprites and no extras; alpha transparency; all four outer image corners and all four corners of every cell empty and alpha 0; no overlap across cells; no grid lines; no borders; no labels; no text; no letters; no numbers; no UI; no watermark; no logos; no ground; no cast shadow; no speed lines; slapstick only, no blood, no wounds, no gore.
```

```text
Use case: stylized-concept
Asset type: production-ready transparent Unity 2D sprite sheet
Input images: Image 1 enemy-layout.png is the exact layout, pose, facing, scale, framing and baseline reference; Image 2 hero-reference.png is the exact rendering style, palette, detailed shading, ink-navy outline, proportions and craftsmanship reference.
Primary request: Create comedien.png, exactly a 4 columns x 4 rows sprite sheet. One full-body sprite centered in each equal cell, identical character and identical scale throughout. Character: hammy middle-aged tragic festival actor; theatrical expression; bushy grey sideburns; golden laurel wreath; white Roman toga with red drape over one shoulder, worn over modern dark trousers and leather sandals; shoulder bag visibly full of red tomatoes.
Pose order left-to-right, top-to-bottom, copying Image 1 exactly: 1 idle guard stance right; 2 run stride A right; 3 run stride B right; 4 attack, big overhand throw facing right, raised right hand with a red tomato just leaving it; 5 unaware declaiming dramatically while holding a skull at arm's length like Hamlet, facing right; 6 unaware with back of hand on forehead in tragic pose facing right; 7 surprised jumping hands up facing right; 8 hurt flinching back; 9 fleeing stride A facing LEFT panicked; 10 fleeing stride B facing LEFT; 11 knocked backward off feet; 12 lying flat on back, comic knockout crossed or swirled eyes; 13 blown into air by comic explosion, torn/scorched clothes and smoke puffs, no gore; 14 tumbling upside down with smoke; 15 running in place engulfed in comic flames, clothes scorched; 16 standing charred black with white eyes and smoke curls.
Style/medium: original premium hand-painted pixel-art game sprites, refined 1990s run-and-gun arcade craftsmanship, matching Image 2 exactly.
Composition/framing: strict uniform 4x4 orthographic grid; every sprite fully contained inside its own cell with generous clear padding; same scale; feet/base near same bottom baseline as Image 1.
Scene/backdrop: genuine alpha-transparent empty background.
Constraints: output square; EXACTLY 16 sprites and no extras; alpha transparency; all four outer image corners and all four corners of every cell empty and alpha 0; no overlap across cells; no grid lines; no borders; no labels; no text; no letters; no numbers; no UI; no watermark; no logos; no ground; no cast shadow; no speed lines; slapstick only, no blood, no wounds, no gore.
```

```text
Use case: stylized-concept
Asset type: production-ready transparent Unity 2D hostage sprite sheet
Input images: Image 3 hostage-layout.png is the exact 4x3 layout, pose, facing, scale, framing and baseline reference; Image 2 hero-reference.png is the exact rendering style, palette, detailed shading, ink-navy outline, proportions and craftsmanship reference.
Primary request: Create affichiste.png, exactly 4 columns x 3 rows. One full-body sprite centered in each equal cell, identical character and scale throughout. Character: skinny young Avignon festival poster-sticker, round glasses, messy hair, festival lanyard badge with no readable text, paint-stained grey hoodie, slim trousers and practical shoes; a roll of colourful theatre posters under one arm in standing poses; glue bucket with brush hanging from belt.
Pose order left-to-right top-to-bottom, copy Image 3 cell-by-cell: 1 tied up sitting; 2 tied up sitting variant; 3 newly freed standing with cut ropes falling; 4 salute; 5 cheering arms up; 6 rummaging in his bag; 7 handing a small red gift box; 8 thumbs up; 9 running stride A facing right; 10 running stride B facing right; 11 running stride C facing right; 12 running stride D facing right.
Style/medium: original premium hand-painted pixel-art game sprites, refined 1990s run-and-gun arcade craftsmanship, matching Image 2 exactly.
Composition/framing: strict uniform 4x3 orthographic grid; every sprite fully contained in its equal cell with generous clear padding; same scale; feet/base on baseline matching Image 3.
Scene/backdrop: genuine alpha-transparent empty background.
Constraints: 4:3 canvas; EXACTLY 12 sprites; no extras; alpha transparency; all four outer corners and all four corners of every cell empty alpha 0; no overlap; no grid lines; no borders; no labels; no text; no letters; no numbers; no UI; no watermark; no logos; no ground; no cast shadow; no speed lines; no blood, no wounds, no gore.
```

### L'Antipape

Références : `boss-layout.png` (`nostradamus.png`), `fx-layout.png` (`prophetie-fx.png`). La première planche d'effets débordait de ses cases. Elle a été refaite avec le second prompt, et la planche ratée était donnée en référence.

```text
You are generating two transparent sprite sheets for "Action Legends", a Unity 2D run-and-gun arcade game in the spirit of Metal Slug: the final boss of a new mission in Avignon, at the Palace of the Popes. Use your image generation tool, then save each sheet in the current directory under the exact file name given, as a PNG with REAL alpha transparency at the exact size given. Look at each result before keeping it: exact grid, one sprite per cell, nothing crossing into a neighbour cell, all background and the four corners of every cell fully transparent (alpha 0), no grid lines, borders or labels, no ground, no cast shadow. If the generator cannot give real transparency, generate on flat pure #FF00FF magenta and remove that colour to alpha yourself with a clean edge. If a sheet is wrong, regenerate it (up to twice). At the end, list the files with sizes, confirm the transparent corners, and give the final prompt used for each.

Tone: comic arcade action, never gory. Style: original premium hand-painted pixel-art game sprites, refined 1990s run-and-gun craftsmanship, ink-navy outlines, detailed material shading, exactly matching the reference sheets.

Reference images in the current directory:
- boss-layout.png: the 4x2 sheet of the previous boss, Nostradamus. Copy its LAYOUT EXACTLY: same cell size, same scale of the character, same baseline, the character FACING LEFT in every cell, and the same order of poses.
- fx-layout.png: the 4x3 effects sheet of Nostradamus. Copy its layout exactly for the new effects.

1. antipape.png — exactly 2048x1024, 4x2 cells of 512x512. "L'Antipape", a fictional medieval antipope who barricaded himself in the Palace of the Popes: a stout, scheming old churchman with a hooked nose, thick white eyebrows and a sly scowl, wearing a tall white-and-gold papal mitre, a crimson cope with heavy gold embroidery over white robes, a jewelled pectoral cross, and holding a tall golden crosier (papal staff) in his left hand. He faces LEFT in every cell. Cells, left to right, top to bottom: 1 idle, crosier planted; 2 walking stride A; 3 walking stride B; 4 casting: crosier pointed forward to the left, its golden crook glowing (he shoots from it); 5 calling the bells: crosier raised high above his head toward the sky, mouth open shouting; 6 invoking: both arms raised wide, cope spread, summoning his guards; 7 hurt: recoiling backwards, mitre askew; 8 defeated: sitting on the ground, dazed, mitre fallen over his eyes, crosier bent across his knees (comic).

2. antipape-fx.png — exactly 2048x1536, 4x3 cells of 512x512, same layout and roles as fx-layout.png:
Row 1 (4 frames): a warning mark on the ground where a bell will fall: a flat glowing golden-and-crimson ellipse seen in perspective with a bell silhouette inscribed in it and small papal crossed-keys motifs, growing brighter from frame 1 to 3 and fading in frame 4.
Row 2: frames 1 to 3 a glowing golden holy orb projectile (a radiant golden sphere with a small cross inside and a short trail of light toward the right, as it flies to the left), frame 4 the orb bursting into golden sparks.
Row 3: frames 1 to 4 the impact of a falling bronze church bell crashing on the pavement: a bright clang flash with the bell visible, then a dust cloud with gold sparks and stone chips, then fading dust.
Also, the falling bell itself must be readable: in row 1 frame 1 nothing changes, but make sure the bell shape in row 3 frame 1 is a complete heavy bronze bell, mouth down.

Constraints: no text, no letters, no numbers, no UI, no watermark, no real logos, no ground plane, no shadows.
```

```text
Generate ONE transparent effects sheet for "Action Legends", a Unity 2D run-and-gun arcade game, with your image generation tool, and save it in the current directory as antipape-fx.png: a PNG with REAL alpha transparency, exactly 2048x1536 pixels, 4 columns x 3 rows of equal 512x512 cells.

previous-attempt.png is a previous attempt whose CONTENT and STYLE are right but which failed: several elements touched or crossed the edges of their cells and were cut by straight lines (the glow of row 1 frame 3, the trail of row 2 frame 3, the top of the bells in row 3). Redraw the same twelve frames, the same design, SMALLER in their cells: every element, glow, spark, dust cloud and debris must stay inside the central 76% of its cell (a fully transparent margin of at least 60 px on all four sides of every cell). Nothing may be cut by a straight edge. Each element is centred in its cell. Do NOT erase or mask parts of the drawing to make margins: scale the drawing down instead.

Look at the result before keeping it: check every cell for a transparent margin of 60 px on all sides and for any straight cut edge; the four corners of every cell fully transparent (alpha 0); no grid lines, labels, ground or shadow. If the generator cannot give real transparency, generate on flat pure #FF00FF magenta and remove it to alpha yourself with a clean, soft edge. Regenerate if wrong (up to three tries).

Content, same as previous-attempt.png and fx-layout.png (the layout reference of the same game):
Row 1 (4 frames): a warning mark on the ground where a bell will fall: a flat glowing golden-and-crimson ellipse seen in perspective, a bronze bell inscribed in it and crossed papal keys motifs, growing brighter from frame 1 to 3 and fading in frame 4.
Row 2: frames 1 to 3 a glowing golden holy orb with a cross inside and a short trail of light toward the right (it flies to the left); frame 4 the orb bursting into golden sparks.
Row 3: frames 1 to 4 a heavy bronze church bell (complete, mouth down, with its crown) crashing on the pavement: bright clang flash, then dust with gold sparks and stone chips, then fading dust.

Style: original premium hand-painted pixel-art game effect sprites, ink-navy outlines, detailed shading, like fx-layout.png. No text, no UI, no watermark.
```

### Tomate

```text
Generate ONE transparent sprite strip for "Action Legends", a Unity 2D run-and-gun arcade game, with your image generation tool and save it in the current directory as tomate.png: a PNG with REAL alpha transparency, exactly 2048x512 pixels, 4 columns x 1 row of equal 512x512 cells. Look at the result before keeping it: one element centred in each cell, nothing crossing into a neighbour cell, the background and the four corners of every cell fully transparent (alpha 0), no grid lines, labels or ground. If the generator cannot give real transparency, generate on flat pure #FF00FF magenta and remove it to alpha yourself with a clean edge (no magenta fringe). Regenerate if wrong. At the end, give the final prompt and confirm size and transparent corners.

Style: original premium hand-painted pixel-art game effect sprites with ink-navy outlines and detailed shading, matching fx-reference.png (an effects sheet of the same game). Each element fills about 45% of its cell.

Cells, left to right: 1, 2, 3: a ripe, slightly overripe red tomato with a green stem, thrown by a hammy theatre actor, rotating a quarter turn between frames, with a tiny juice droplet trailing to the right (it flies to the left); 4: the same tomato squashed on impact in a comic red splat with seeds and juice droplets, no blood, clearly vegetable.

Constraints: no text, no UI, no watermark, no ground, no shadow.
```
