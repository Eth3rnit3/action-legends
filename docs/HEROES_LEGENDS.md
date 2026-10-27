# Assets : Fernandel, Brigitte Bardot et Jeanne d’Arc

Pack préparé par le propriétaire, intégré au jeu au lot 3H : les trois héros sont jouables (`Content/heroes/fernandel.json`, `brigitte-bardot.json`, `jeanne-d-arc.json`), Marguerite est l’alliée de la spéciale de Fernandel. Les poses manquantes (12 de plus par héros) et l’intégration sont décrites dans `docs/ART_LOT3H.md`.

Dossier : `Assets/ArtSources/NouveauxHeros/` (hors `Resources`, jamais chargé tel quel par le jeu).

## Contenu

- `sprites/fernandel.png` : Fernandel en tenue inspirée de La Vache et le Prisonnier.
- `sprites/brigitte-bardot.png` : Brigitte Bardot en héroïne western inspirée de Viva Maria !.
- `sprites/jeanne-d-arc.png` : Jeanne en armure, tenue et étendard bleus, grandes fleurs de lys dorées.
- `sprites/marguerite.png` : quatre images de galop de la vache.
- `sources/` : planches originales générées, avant alignement et nettoyage des contours.
- `sources/<héros>-B.png` (lot 3H) : la seconde planche de 12 poses générée avec Codex à partir de `sprites/`.
- Les deux bruitages originaux, sifflement et charge, sont désormais dans `Assets/Resources/Audio/Sfx/legend-whistle.wav` et `legend-charge.wav`.

## Découpage

Les trois héros possèdent une grille de 4 colonnes × 3 lignes, numérotée de gauche à droite puis de haut en bas : attente, course A, course B, saut, accroupi, visée haute, visée basse, coup rapproché, lancer, spécial, blessure, victoire. Le spécial illustré représente Fernandel qui siffle, Bardot avec ses deux revolvers, Jeanne qui charge avec son étendard.

Fernandel et Bardot : 1360 × 1020 px, cases de 340 × 340. Jeanne : 1920 × 1260 px, cases de 480 × 420 pour laisser la place à son étendard. Marguerite : 960 × 640 px, grille de 2 × 2 et cases de 480 × 320. Les PNG conservent leur transparence.

Les recettes de préparation sont conservées dans `tools/art/heroes/{fernandel,brigitte-bardot,jeanne-d-arc,marguerite}.json`. Le script existant `tools/art/pack_sheet.py hero SPEC.json` aligne les pieds et nettoie les contours semi-transparents. Les planches du jeu (`Assets/Resources/Art/heroes/<héros>.png`, 24 cases) sont assemblées par `tools/art/heroes/<héros>-game.json` à partir de `sprites/<héros>.png` et `sources/<héros>-B.png`.

## Génération des visuels

Outil : ImageGen intégré, transparence demandée. Les héros utilisent la planche de Chuck Norris du projet comme référence de style. Marguerite a reçu une seconde passe de suppression du fond. Les prompts finaux sont conservés ci-dessous.

### fernandel

```text
Use case: stylized-concept. Production transparent sprite atlas for Unity side-scrolling run-and-gun Action Legends. Match reference game's richly shaded 1990s arcade caricature style, dark navy outlines, warm highlights, crisp detailed pixel clusters. ONE character: Fernandel as Charles Bailly in La Vache et le Prisonnier: recognizable long face huge toothy smile, receding dark hair, battered French soldier/prisoner khaki brown coat and trousers, cloth military cap, boots, carrying a simple old rifle. Comical warm personality. Exact regular 4 columns by 3 rows grid, 1536x1536, 12 equal square cells. All fully isolated full-body sprites face RIGHT side view, generous clear margins, feet at 90% cell height, consistent character scale. Row 1: idle aiming rifle horizontally right; run left leg forward; run right leg forward; jumping knees tucked aiming right. Row 2: crouched aiming right; aiming straight up; airborne aiming down; forward melee rifle-butt swing. Row 3: throwing grenade; whistling with fingers in mouth to summon his cow; hurt recoiling; victory waving cap. No cow in this sheet. All frames same costume and face, no background, no grid lines, no writing, NO shadows below feet, real transparent alpha. Preserve entire limbs and weapon within each cell.
```

### bardot

```text
Use case: stylized-concept. Production transparent sprite atlas for Unity side-scrolling run-and-gun Action Legends. Match reference game's richly shaded 1990s arcade caricature style, dark navy outlines, warm highlights, crisp detailed pixel clusters. ONE character: recognizable adult Brigitte Bardot inspired by Viva Maria western heroine, voluminous golden blonde half-up bouffant hair, confident mischievous face with black eyeliner, cream western blouse, burnt orange fitted waistcoat, dark teal trousers, leather gunbelt, brown cowboy boots, two steel revolvers. Athletic action-comedy heroine. Exact regular 4 columns by 3 rows grid, 1536x1536, 12 equal square cells. All isolated full-body sprites face RIGHT side view, generous clear margins, feet at 90% cell height, consistent character scale. Row 1: idle aiming revolver horizontally right; run left leg forward; run right leg forward; jumping knees tucked aiming right. Row 2: crouched aiming right; aiming straight up; airborne aiming down; close range pistol-whip swing. Row 3: throwing grenade; special firing both revolvers in opposite horizontal directions; hurt recoiling; victory with revolver raised. All frames same costume and face. No backgrounds, no grid lines, no text, NO ground shadows, real transparent alpha. Entire limbs hair and weapons contained inside each cell.
```

### jeanne

```text
Use case: stylized-concept. Production transparent sprite atlas for Unity side-scrolling arcade action game Action Legends. Match reference game's richly shaded 1990s arcade caricature style, navy outlines, warm highlights, crisp fine pixel clusters. ONE character: original Joan of Arc / Jeanne d'Arc, courageous mischievous adult young woman, short dark bob hair, slightly exaggerated polished steel plate armour, royal blue tabard with BEAUTIFUL CLEAR LARGE GOLD FLEURS DE LYS prominently on chest and shoulder, brown boots, longsword. Carries a tall royal blue flowing battle standard with LARGE ORNATE GOLD FLEURS DE LYS (classic three-petal lily motif), gold finial. Banner and costume must remain consistent. Exact regular 4 columns by 3 rows grid, 1536x1536, 12 equal square cells. All fully isolated full-body sprites face RIGHT side view, generous clear margins and room above for banner, feet at 90% cell height, consistent scale. Row 1: idle sword extended horizontally right banner held upright; run left leg forward banner streaming; run right leg forward banner streaming; jumping knees tucked sword right. Row 2: crouch sword pointing right; sword thrust straight up; airborne sword down; broad sword slash towards right. Row 3: throwing grenade with sword sheathed; special charging sword forward banner flying magnificently behind with GOLD FLEURS DE LYS visible; hurt recoiling; victory raising sword and standard. This is a melee heroine: NO firearms. All frames same face armour and emblems. No writing, no backgrounds, no grid lines, NO ground shadows, real transparent alpha. Entire limbs sword and banner inside each cell.
```

### marguerite

```text
Use case: stylized-concept. Production transparent animated cow sprite atlas for the arcade game Action Legends. Match reference richly shaded 1990s pixel-cluster arcade illustration, dark navy outlines, warm cream highlights, comical expressive faces. Marguerite: one large charming cream and reddish brown dairy cow, little horns, bell and leather collar, determined funny expression, side view facing RIGHT, charging at a gallop. EXACT 2 columns x 2 rows grid of FOUR isolated full-body animation frames, chronological gallop cycle, different leg positions: extended front legs, gathered legs, extended hind legs, airborne. Same size and markings every frame. Each animal completely contained in its square cell with generous transparent margins, feet baseline at 85%, no rider, no people, no lettering, no grid, no ground shadows, real transparent alpha.
```

