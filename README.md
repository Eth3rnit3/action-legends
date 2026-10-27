# Action Legends

Jeu d’action arcade 2D pour un à quatre joueurs en coopération locale, construit avec Unity. Quatre missions qui s’enchaînent : **Panique à Paname**, à Paris en 2026, **Mistral Gagnant**, à Marseille, **Prophétie en Provence**, à Saint-Rémy-de-Provence, et **Tokyo Néon**. Chaque mission gagnée débloque la suivante et révèle son code, à saisir au menu pour y revenir directement.

Le jeu est conçu pour tourner dans un navigateur de bureau à partir de fichiers statiques. Aucun compte joueur, serveur de jeu ou base de données n’est nécessaire.

**État du dépôt : public. Le jeu est publié sur [GitHub Pages](https://eth3rnit3.github.io/action-legends/).** Les vérifications des contributions sont automatiques. Chaque modification de `main` est compilée puis publiée automatiquement par GitHub Actions.

## Jouer et développer

- Unity **6000.4.7f1**, avec le module **Web Build Support** pour l’export navigateur.
- Ouvrir le projet, puis `Assets/Scenes/Paris2026.unity`.
- Cliquer sur **Play**, choisir son héros, puis **Déployer l’équipe**.
- [Commandes et fonctionnement du prototype](LISEZ-MOI.md).

Un export navigateur local est généré dans `WebBuild/` ; ce dossier n’est pas versionné. Pour le tester, depuis le projet :

```sh
python3 -m http.server 8765 --bind 127.0.0.1 --directory WebBuild
```

Ouvrir ensuite `http://127.0.0.1:8765`. Le petit serveur sert uniquement des fichiers statiques ; il ne contient aucune logique de jeu. Le bouton **Jouer** charge Unity, puis les commandes fonctionnent dans le canevas du jeu.

## Faire évoluer le jeu

Une mission est un JSON dans `Assets/Resources/Levels/`. Le jeu détecte toutes les missions de ce dossier lors de la compilation et les présente dans le menu. Héros, ennemis, boss et leurs animations forment une bibliothèque de JSON dans `Assets/Resources/Content/`, que les missions désignent par identifiant : un niveau complet, avec un nouvel ennemi, un nouveau boss et sa carte, s’ajoute sans toucher au code.

- [Ajouter une mission](docs/ADDING_A_LEVEL.md).
- [Contribuer au code ou aux visuels](CONTRIBUTING.md).
- [Compilation web et publication](docs/WEB_AND_GITHUB.md).

Après validation et fusion d’une contribution, GitHub Actions peut compiler le jeu et publier le nouvel export. Les nouvelles missions comme les améliorations du moteur suivent le même parcours. Les changements ne sont donc pas exécutés directement depuis une proposition non validée.

## Vérifier les missions sans Unity

Python 3.12, sans dépendance externe :

```sh
python3 tools/validate_content.py
python3 -m unittest discover -s tools -p 'test_*.py'
```

La vérification contrôle les missions et la bibliothèque : format, références d’ennemis et de boss, images et sons présents, états d’animation et ancrages requis, bornes des nombres, sortie et ordre des vagues. Elle refuse aussi un nom propre à une mission dans le code du moteur. Le jeu refait les mêmes contrôles au démarrage.

## Portée actuelle

Version 0.8.0 ([nouveautés](docs/UPDATE_080.md)) : un joueur au clavier et jusqu’à trois partenaires à la manette, sept héros sélectionnables (dont Fernandel, Brigitte Bardot et Jeanne d’Arc, chacun avec sa spéciale), neuf types d’ennemis, cinq armes à lettres (H, S, F, R, L), des caisses de soins, de turbo, de points et de grenades, et une campagne de quatre missions débloquées une à une, avec codes de mission. Paris compte dix assauts mis en scène jusqu’à Bastille et un fourgon blindé à mi-parcours ; Marseille, huit assauts, un hors-bord au Vieux-Port, son propre boss et sa musique ; Saint-Rémy-de-Provence, neuf assauts d’Arlésiens (dont un au fouet) de la place de la République aux Alpilles, un taureau de Camargue qui charge sur la route et Nostradamus, qui annonce ses prophéties par des cercles au sol ; Tokyo, de nuit, huit assauts d’ennemis armés de pistolets, mitraillettes et fusils à pompe, un drone de combat et un boss au sol, le Mécha Shogun. Les ennemis entrent par les portes, les fenêtres, en parachute ou en camionnette, attendent parfois sans se méfier, sursautent, paniquent, lancent des grenades et tombent selon l’arme qui les a touchés. Accroupi, tir vers le haut et vers le bas en saut, couteau au contact et grenades. Cinq difficultés sélectionnables, dont le mode Slug (mort en un coup, vies, continue), des otages à libérer et un décompte de fin de mission, un mini-boss au milieu de chaque mission, un boss final par ville (hélicoptères à Paris et Marseille, Nostradamus, le Mécha Shogun), musique arcade et annonces vocales des armes et des bonus. Les bonus sont espacés selon la difficulté. Tirs renforcés, chronomètre, effets arcade et renforts partagés. Jeu en ligne jusqu’à quatre joueurs avec un lien d’invitation, sans serveur de jeu : voir [le jeu en ligne](docs/ONLINE.md). Classement mondial par mission et difficulté, publié sur des relais Nostr publics et vérifié en rejouant chaque partie dans le navigateur : voir [le classement](docs/CLASSEMENT.md). Les navigateurs de bureau sont la cible de cette version ; les commandes tactiles ne sont pas prévues.

## Licence

Le code est sous [licence MIT](LICENSE) : scripts C#, shaders, page web, outils et données de mission. Les illustrations, la musique, les voix et les bruitages (`Assets/Resources/Art`, `Assets/Resources/Audio`, `Assets/ArtSources`) restent tous droits réservés : ils peuvent servir à jouer, tester et contribuer au projet, mais pas être réutilisés ailleurs sans accord.
