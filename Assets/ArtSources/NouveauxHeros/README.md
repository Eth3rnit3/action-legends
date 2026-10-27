# Nouveaux héros — assets uniquement

Sprites des nouveaux héros ; aucun de ces fichiers n’est chargé tel quel par le jeu : les planches jouées (24 cases) sont dans `Assets/Resources/Art/heroes/`, assemblées par `tools/art/heroes/<héros>-game.json` (lot 3H).

- `sprites/` : quatre atlas PNG transparents prêts à découper.
- `sources/` : planches originales, et `<héros>-B.png`, la seconde planche de 12 poses générée au lot 3H.
- Le sifflement et la charge sont maintenant dans `Assets/Resources/Audio/Sfx/` (`legend-whistle.wav`, `legend-charge.wav`).

Les héros utilisent une grille 4 × 3 : attente, course A, course B, saut, accroupi, visée haute, visée basse, mêlée, lancer, spécial, blessure, victoire. Marguerite utilise une grille 2 × 2 de galop.

Les dimensions, recettes et prompts ImageGen sont documentés dans `docs/HEROES_LEGENDS.md` à la racine du projet.
