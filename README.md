# Action Legends

Jeu d’action arcade 2D pour un à quatre joueurs en coopération locale, construit avec Unity. Deux missions : **Panique à Paname**, à Paris en 2026, et **Mistral Gagnant**, à Marseille.

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

Une mission est un JSON dans `Assets/Resources/Levels/`. Le jeu détecte toutes les missions de ce dossier lors de la compilation et les présente dans le menu. Les paramètres, vagues et décors de **Panique à Paname** sont déjà séparés du moteur.

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

La vérification contrôle le format, les images référencées, la sortie, l’ordre des vagues et les ennemis disponibles. La compilation Unity valide ensuite le code et les ressources.

## Portée actuelle

Version 0.5.1 : un joueur au clavier et jusqu’à trois partenaires à la manette, quatre héros sélectionnables, trois types d’ennemis, douze assauts, six types de bonus, une première mission prolongée jusqu’à Bastille et une deuxième mission à Marseille, de dix assauts, avec son propre boss et sa musique. Quatre difficultés sélectionnables, boss final en hélicoptère, tir vers le haut, musique arcade et annonces vocales des bonus. Les bonus sont espacés selon la difficulté. Tirs renforcés, chronomètre, effets arcade et renforts partagés. Jeu en ligne jusqu’à quatre joueurs avec un lien d’invitation, sans serveur de jeu : voir [le jeu en ligne](docs/ONLINE.md). Les navigateurs de bureau sont la cible de cette version ; les commandes tactiles ne sont pas prévues.

## Licence

Le code est sous [licence MIT](LICENSE) : scripts C#, shaders, page web, outils et données de mission. Les illustrations, la musique, les voix et les bruitages (`Assets/Resources/Art`, `Assets/Resources/Audio`, `Assets/ArtSources`) restent tous droits réservés : ils peuvent servir à jouer, tester et contribuer au projet, mais pas être réutilisés ailleurs sans accord.
