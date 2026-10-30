# Panoramas et raccords ajoutés après la 0.9.0

L'audit visuel des quatre missions (une capture par écran, `AgentScripts/CaptureRoute.cs`) a montré deux défauts :

- **Des raccords visibles.** Deux panoramas se rejoignent par un fondu de 1,5 unité (aucun quand les deux images sont peu larges, comme `route-01` et `route-02`) : on voyait deux scènes l'une dans l'autre (une colonne fantôme à Paris, le fort Saint-Jean sous l'ombrière à Marseille, l'enseigne du Grand Café Riche coupée à Saint-Rémy).
- **Une fresque trop courte.** Paris (101 u de panoramas pour 480 u de parcours), Marseille et Tokyo faisaient défiler leurs façades à 17 à 29 % de la vitesse des héros : les mêmes images restaient cinq écrans de suite, et les objets posés semblaient glisser devant elles.

Ajouts, générés avec Codex CLI (`codex exec --skip-git-repo-check -m gpt-5.6-sol --enable image_generation`) ; chaque image a été regardée avant d'être gardée :

| Fichier | Taille | Rôle |
|---|---|---|
| `Art/paris-opera.png`, `paris-marais.png`, `paris-montmartre.png` | 2172×724 | Trois panoramas de Paris (références : `canal-panorama`, `bastille-panorama`) : la fresque passe à 213 u, défilement 41 % |
| `Art/marseille/noailles.png` | 2172×724 | Le marché de Noailles, entre le Panier et la Canebière (références : `panier`, `canebiere`) : 151 u, 37 % ; le hors-bord navigue toujours sur l'eau du Vieux-Port |
| `Art/tokyo/asakusa.png` | 2172×724 | Asakusa, entre Akihabara et Minato (références : `shibuya`, `akihabara`) : 151 u, 41 % ; la fin de la fresque (la tour) reste atteinte |
| `Art/joins.png` | 2048×2048, transparent, 4 × 2 | Les éléments de raccord (`map.joins`), deux par ville : colonne Morris et kiosque (Paris), palmiers et fontaine (Marseille), platane et cyprès (Saint-Rémy), poteau à néons et lanterne de sanctuaire (Tokyo) |

Saint-Rémy n'a pas de nouveau panorama : sa fresque (226 u) défile déjà à 50 %, la limite au-delà de laquelle la caméra n'atteindrait plus la fin.

Les fenêtres des règles (`stage.openings`, `window`) passent de 1,9 à 2,7 unités de haut : un lanceur posté à une fenêtre y tient en entier.

## Prompts des panoramas

Every prompt began with this shared block:

```text
Use case: stylized-concept
Asset type: Unity 2D arcade run-and-gun background panorama
Style/medium: Original premium hand-painted pixel-art arcade background, refined 1990s run-and-gun craftsmanship, detailed material shading, strong silhouettes, ink-navy outlines, rich polished arcade production quality. Match the supplied city references only for light, palette, finish, detail density, orthographic side view, horizon height, building scale, and ground layout; create a completely new scene.
Composition/framing: exact 3:1 wide panorama, side-on orthographic camera, no perspective tilt. The walkable baseline is a perfectly straight horizontal boundary at exactly 80% of image height from the top, unbroken across the full width. Everything architectural, all sidewalks, curbs, railings, props, vehicles, shadows and scenery end above that boundary. The bottom 20% is flat, uninterrupted clear ground only, with no objects, props, text, road markings that read as obstacles, curb intrusion, cast shadows, or vertical elements.
Edges: leftmost and rightmost 8% contain quiet generic content such as plain facade, foliage, sky or uninterrupted ground; no landmark, sign, or object is cut by either edge.
Constraints: one continuous seamless-looking scene, opaque, no frame, no panel divisions, no people or characters, no animals, no UI, no watermark, no readable real brand names or logos, no text anywhere in the bottom 20%.
```

`paris-opera.png` appended:

```text
Primary request: Paris Grands Boulevards near an opera house. Left: Haussmann apartment buildings with wrought-iron balconies and cafés with red awnings. Middle: grand ornate Second-Empire opera-house facade with gilded statues and green copper dome, set back behind a small square with ornate lamp posts. Right: department-store facade with glass canopies and plane trees.
Lighting/mood: golden late-afternoon light matching the Paris references; light recent chaos with only a few distant smoke plumes, one overturned café chair and scattered crates, all strictly above the baseline; no people.
Ground: wet dark cobblestone road in the bottom 20%, with subtle reflections but no obstacle-like markings; a pale stone sidewalk edge sits immediately above the 80% line.
Text: café/shop signs, if any, use only short generic French words.
Critical correction for this regeneration: absolutely no human-shaped mannequin, dress form, portrait, silhouette, statue in a shop window, or human figure of any kind. Architectural gilded rooftop sculptures may remain, but storefront windows must contain only abstract merchandise and warm light. Keep the straight y=80% baseline and fully empty bottom 20% exactly.
```

`paris-marais.png` appended:

```text
Primary request: Paris Marais. Left: a formal square surrounded by red-brick and pale-stone seventeenth-century pavilions with steep slate roofs above a long vaulted stone arcade; garden with clipped lime trees and a fountain behind railings. Right: narrow old houses with timber details, small boutiques with painted wooden shopfronts, a bakery and a bookshop.
Lighting/mood: golden late-afternoon light matching the Paris references; light recent chaos with only a few distant smoke plumes, one overturned café chair and scattered crates, all strictly above the baseline; no people.
Ground: wet dark cobblestone road fills the clear bottom 20%, subtle reflections only.
Text: signs, if any, use only short generic French words.
```

`paris-montmartre.png` appended:

```text
Primary request: Paris at the foot of Montmartre. A steep stairway street with black iron handrails and lamps climbs the hill in the background; a white domed basilica sits far behind on the hilltop; small square with empty painters' easels under red umbrellas, cafés, a windmill on the right behind houses, and a vintage funicular track. No painters and no people.
Lighting/mood: golden late-afternoon light matching the Paris references; light recent chaos with only a few distant smoke plumes, one overturned café chair and scattered crates, all strictly above the baseline.
Ground: wet dark cobblestone road fills the clear bottom 20%, subtle reflections only.
Text: signs, if any, use only short generic French words.
```

`marseille-noailles.png` appended:

```text
Primary request: Marseille Noailles market street. Colourful spice and fruit stalls under faded awnings, closed and abandoned; North-African grocery shopfronts with crates of produce stacked against walls; old ochre and pink buildings with shutters and laundry lines; a tram-stop shelter; right side a small square with plane trees and a stone fountain.
Lighting/mood: bright sunny Mediterranean daylight matching the Marseille references; light recent chaos with a few distant smoke plumes and scattered crates and fruit boxes, all strictly above the baseline; no people.
Ground: pale limestone pavement slabs fill the clear bottom 20%, even and unobstructed.
Text: shop signs, if any, use only short generic French words.
```

`tokyo-asakusa.png` appended:

```text
Primary request: Tokyo Asakusa-like temple district at night. Huge temple gate with a giant red paper lantern; a five-storey pagoda lit from below behind it; shopping street of small souvenir shops with paper lanterns and noren curtains; a rickshaw parked by a wall; right side riverside promenade with a very tall slender lit broadcast tower far behind across the river.
Lighting/mood: night, deep indigo and violet sky, glowing magenta, cyan and amber neon matching the Tokyo references; light recent chaos with a few distant smoke plumes, a toppled scooter and traffic cones, all strictly above the baseline; no people.
Ground: dark wet asphalt road fills the clear bottom 20%, neon reflections only, no markings or obstacles.
Text: all signs use invented unreadable katakana/kanji-like decorative glyphs, never real brand names.
```

## Prompt des raccords

```text
Create one production-ready transparent sprite atlas for a Unity 2D arcade run-and-gun game. Exact 2048×2048 PNG with real alpha transparency, arranged as an invisible exact grid of four equal columns by two equal rows, each portrait cell 512×1024. No visible grid, labels, frames, guides or cell backgrounds.

Original premium hand-painted pixel-art arcade art, refined 1990s run-and-gun craftsmanship, ink-navy outlines and detailed material shading. Match the supplied Paris, Marseille, Saint-Rémy-de-Provence, Tokyo and prop-sheet references for lighting, palette, scale and finish.

Each of the eight elements must remain strictly inside its own cell. All four cell corners must be transparent. Each element is tall and reaches near the top. From the bottom to roughly 55% of the cell height, opaque overlapping matter must cover at least 75% of the cell width without central see-through gaps. Every base sits flat on the bottom baseline. Side-on orthographic view, no ground plane and no cast shadow.

Row 1:  
1. Paris at golden late afternoon: green Morris advertising column with generic colorful posters, large chestnut tree and dense autumn-gold crown, broad clipped boxwood planters.  
2. Paris: broad green-and-gold domed newspaper kiosk with generic magazine shapes, ornate double lamp post and chestnut crown above.  
3. Marseille in bright Mediterranean daylight: two tall date palms in an extra-wide square limestone planter overflowing with bougainvillea and oleander, with black iron lamp post.  
4. Marseille: broad pale-limestone wall fountain with lion-head spout and basin, overlapping terracotta pots and lemon crates, plane-tree trunk and crown above.

Row 2:  
5. Saint-Rémy-de-Provence in bright summer light: massive old plane tree with mottled cream-and-olive bark, dense crown, broad dry-stone bench wall and abundant lavender pots.  
6. Provence: two tall dark cypress trees behind a broad cream-stone wall, small wayside stone cross, leaning wooden cart wheel and dense wild lavender.  
7. Tokyo at night: concrete utility pole wrapped in stacked magenta, cyan and amber neon signs using invented abstract Japanese-like glyphs, compact overhead cables, two glowing vending machines, cones and utility boxes forming a dense base.  
8. Tokyo at night: tall red lacquered shrine lantern post with hanging red paper lantern, broad pink-lit cherry-blossom crown, stacked wooden sake barrels with abstract marks and small stone lantern.

Exactly eight isolated sprites in the specified order. No people, animals, readable text, real brands, logos, background scenery, ground plane, shadows, frames, labels, grid lines or watermark. No cropped elements, boundary overlap, narrow bases or holes in the lower silhouettes.
```
