# Logos de mission

Les fiches de mission du menu affichent le logo de la mission (champ `logo`) à la place de son titre. Prophétie en Provence avait le sien depuis la PR #1 (`Art/saint-remy/mission-logo.png`). Les trois autres ont été générés en 0.9.1 avec Codex CLI (`codex exec -m gpt-5.6-sol --enable image_generation`). Le logo de Saint-Rémy servait de référence de série, et un panorama de chaque ville donnait sa palette.

| Fichier | Taille | Mission |
|---|---|---|
| `Art/paris-logo.png` | 2172×724, transparent | Panique à Paname |
| `Art/marseille/mission-logo.png` | 2172×724, transparent | Mistral Gagnant |
| `Art/tokyo/mission-logo.png` | 2172×724, transparent | Tokyo Néon |

Chaque image a été vérifiée avant d'être gardée :
- le titre est exact, accents compris ;
- les quatre coins sont transparents (alpha 0) ;
- le contour est propre, sans liseré, vu sur fond gris.

Un premier essai pour Marseille avait inventé le titre « BASTON À MARSEILLE ». Il a été refait avec la consigne d'écrire « MISTRAL / GAGNANT » lettre pour lettre.

## Prompts finaux

### Panique à Paname

Références : `saint-remy/mission-logo.png`, `canal-panorama.png`, `paris-opera.png`.

```text
Create a new Paris mission logo belonging unmistakably to the same series as reference-logo.png. Ultra-wide 3:1 isolated emblem. Huge centered two-line title filling about 80% of the width, chunky forward-italic 3D arcade block letters, thick ink-navy outline and deep extruded navy shadow.

Text verbatim: line 1 “PANIQUE”; line 2 “À PANAME”. Preserve the grave accent on À. No other readable text.

Cream-to-gold-to-fiery-red-orange letter faces, stippled hand-painted pixel-art texture, tiny chips and bullet holes. Behind the letters: Paris skyline in golden late-afternoon light, Eiffel Tower on the left, Haussmann zinc rooftops and chimneys, Notre-Dame towers and stylized smoke plumes. Left framing: chestnut leaves and blank blue enamel street sign. Right framing: red-and-gold Morris column top, baguette and blue police beacon.

Richly detailed 1990s arcade pixel-art finish. Genuine RGBA transparency outside the emblem. Generous transparent padding. No crop, frame, panel, people, animals, brands, watermark or extra text.
```

### Mistral Gagnant

Références : `saint-remy/mission-logo.png`, `marseille/vieux-port.png`.

```text
Create one transparent 3:1 Marseille mission logo matching the supplied “Prophétie en Provence” series reference. Use exactly two title lines: “MISTRAL” and “GAGNANT”, letter for letter, with no other text. Huge forward-italic 3D arcade block lettering, ink-navy outline and extrusion, cream-to-gold-to-fiery-orange faces, chips and bullet holes. Behind it, depict Notre-Dame-de-la-Garde, the Vieux-Port, fishing boats and masts, Fort Saint-Jean, ochre and pink buildings, turquoise sea, and swirling mistral gusts in bright Mediterranean daylight. Frame the left with olive leaves and two pétanque balls; frame the right with a white-and-blue lifebuoy and red bougainvillea. Rich hand-painted pixel-art finish. No people, animals, brands, watermark, frame, box, halo, or background. Genuine alpha transparency outside the irregular logo silhouette.
```

### Tokyo Néon

Références : `saint-remy/mission-logo.png`, `tokyo/shibuya.png`.

```text
Create a Tokyo mission logo in exactly the same visual series and layout as reference-logo.png. Ultra-wide 3:1 isolated emblem. Huge centered two-line title filling about 80% of the width, chunky forward-italic 3D arcade block letters, thick ink-navy outline and deep extruded navy shadow.

Text verbatim: line 1 “TOKYO”; line 2 “NÉON”. Preserve the acute accent on É. No other readable text.

Vivid hot-magenta-to-electric-cyan neon letter faces, restrained glow, stippled hand-painted pixel-art texture, small chips and bullet holes. Behind the letters: Tokyo at night under a deep indigo sky, red-and-white lattice tower on the left, dense neon skyscrapers with abstract non-readable sign glyphs and a five-storey pagoda on the right. Left framing: blank red paper lantern and cherry blossoms. Right framing: golden samurai kuwagata crest and glowing neon tube coil.

Richly detailed 1990s arcade pixel-art finish. Genuine RGBA transparency outside the emblem. Generous transparent padding. No crop, frame, panel, people, animals, brands, watermark or extra text.
```
