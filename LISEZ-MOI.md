# Action Legends — guide de jeu

Prototype Unity 2D — version 0.4.3 : quatre héros sélectionnables et coopération locale sur un même écran. Première mission : Panique à Paname, Paris en 2026.

## Jouer

1. Ouvrir ce dossier dans Unity 6000.4.7f1.
2. Ouvrir `Assets/Scenes/Paris2026.unity` si nécessaire.
3. Cliquer sur **Play**, choisir son héros, puis **Déployer l’équipe** ou **Entrée**.

Le joueur 1 peut jouer immédiatement au clavier. Sans manette, un seul joueur rejoint la partie. Appuyer sur **A / croix** sur une manette pour ajouter un partenaire : les places 2 à 4 sont occupées en premier ; une quatrième manette peut prendre le joueur 1. Les joueurs peuvent aussi rejoindre pendant le niveau. Cliquer sur une carte pour choisir son héros ; les pastilles J1 à J4 permettent de choisir le joueur concerné. Chaque manette peut choisir son propre héros avec la croix. Les héros peuvent être choisis plusieurs fois.

| Action | Joueur 1 au clavier | Manette |
|---|---|---|
| Déplacement | Q ou A / D | Stick gauche ou croix directionnelle |
| Saut | Espace | A / croix |
| Tir | J | X / carré ou gâchette droite |
| Attaque spéciale | K | Y / triangle ou bouton supérieur droit |
| Pause | Échap | Start / Options |
| Choisir le héros, au menu | Clic sur une carte, ← / → ou Q / E | Croix gauche / droite |
| Démarrer, au menu | Entrée | Start / Options |

Le clavier contrôle uniquement le joueur 1. Pour jouer à plusieurs, brancher des manettes.

## Première mission : Panique à Paname

Avancer vers la droite à travers douze assauts, puis rejoindre la sortie à Bastille. Le parcours est six fois plus long que celui de la version 0.4.0 ; la progression reste libre pendant les combats. Trois types d'ennemis : fonceur, lanceur de projectiles et costaud au bouclier. Les attaques spéciales se rechargent en cinq secondes. Chaque joueur commence avec six points de vie ; les renforts sont partagés. Tous les trois assauts déployés, les héros encore debout reçoivent des soins et un renfort est ajouté. Il n'y a pas de tirs entre alliés.

Ramasser les caisses pour obtenir une mitrailleuse, un triple tir, des roquettes, des soins, dix secondes de turbo ou des points. Les armes bonus ont des munitions limitées ; le fusil de base est illimité. Le chronomètre permet de comparer ses temps de parcours.

Depuis la version 0.4.3, un ennemi laisse un bonus tous les quinze ennemis éliminés, contre cinq auparavant. Les caisses du parcours sont espacées de 72 unités, contre 34, avec la première à 24 unités. La puissance et les munitions des armes restent identiques : les bonus sont des coups de pouce plus ponctuels.

## État de cette version

- Déplacements, sauts, tirs, dégâts, renforts, score, pause, victoire et défaite.
- Quatre héros aux vitesses, cadences et attaques spéciales différentes.
- Sprites et décor générés, intégrés à Unity ; animation courte par changement de poses.
- Nouveau menu avec logo illustré, cadres métalliques, sélection au clavier, souris ou manette.
- Boulevard, quais, canal Saint-Martin et Bastille : décors successifs avec fondus aux raccords, défilement en parallaxe et chaussée au premier plan.
- 24 images d'effets animés : flammes de bouche, impacts, explosions et poussière. Traceurs à cœur clair, douilles, recul au tir, étincelles sur le bouclier, flash blanc, réaction de recul et défaite de l'ennemi.
- Sons de tir, d'impact et d'explosion composés de plusieurs couches. Pas encore de musique.
- Terrain plat : plateformes, vrais décors destructibles, boss et animations plus riches restent à construire.
- Les commandes sont séparées de la simulation. Le jeu en ligne nécessite encore une implémentation réseau et n'est pas disponible.

## Vérification

La version 0.4.2 a été compilée pour navigateur. Un contrôle ciblé vérifie la sélection au clavier, une course avec tirs à deux personnages, la vitesse attendue sans retour arrière et les bonus d'arme et de turbo. Les missions passent également le validateur de contenu et ses huit tests Python. Cela ne remplace pas un essai avec quatre manettes physiques ; l'équilibrage reste à ajuster en jouant.

## Fichiers du projet

Les scripts se trouvent dans `Assets/Scripts`, les images dans `Assets/Resources/Art`, la scène dans `Assets/Scenes`. Les scripts du dossier `AgentScripts` servent à construire et vérifier le prototype dans l'éditeur ; ils ne sont pas inclus dans le jeu.

Les dossiers `Library`, `Temp`, `Logs` et `obj` sont des fichiers de travail Unity et ne doivent pas être versionnés. Unity les recrée si nécessaire.

## Missions et navigateur

Le nom du jeu est Action Legends. Les missions sont chargées depuis les JSON de `Assets/Resources/Levels` ; de nouvelles missions apparaissent automatiquement dans le menu après compilation. Voir le [README](README.md) pour la version navigateur et les contributions.
