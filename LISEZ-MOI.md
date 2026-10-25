# Action Legends — guide de jeu

Prototype Unity 2D — version 0.4 : quatre héros sélectionnables et coopération locale sur un même écran. Première mission : Panique à Paname, Paris en 2026.

## Jouer

1. Ouvrir ce dossier dans Unity 6000.4.7f1.
2. Ouvrir `Assets/Scenes/Paris2026.unity` si nécessaire.
3. Cliquer sur **Play**, choisir son héros, puis **Déployer l’équipe** ou **Entrée**.

Le joueur 1 peut jouer immédiatement au clavier. Pour les manettes, appuyer sur **A / croix** pour rejoindre : la première prend le joueur 1, les suivantes les places libres, dans la limite de quatre joueurs. Les joueurs peuvent aussi rejoindre pendant le niveau. Les héros peuvent être choisis librement, y compris plusieurs fois.

| Action | Joueur 1 au clavier | Manette |
|---|---|---|
| Déplacement | Q ou A / D | Stick gauche ou croix directionnelle |
| Saut | Espace | A / croix |
| Tir | J | X / carré ou gâchette droite |
| Attaque spéciale | K | Y / triangle ou bouton supérieur droit |
| Pause | Échap | Start / Options |
| Choisir le héros, au menu | Q / E, ou boutons ‹ › | Croix gauche / droite |
| Démarrer, au menu | Entrée | Start / Options |

Pour tester deux joueurs sans manettes, cliquer sur **Rejoindre au clavier** dans le menu. Le joueur 2 utilise les flèches gauche/droite, Maj droite pour sauter, Ctrl droite ou Entrée pour tirer et flèche bas pour l'attaque spéciale. Certaines combinaisons simultanées dépendent de la capacité du clavier à reconnaître plusieurs touches.

## Première mission : Panique à Paname

Avancer vers la droite, éliminer trois vagues, puis rejoindre la sortie à Bastille. Trois types d'ennemis : fonceur, lanceur de projectiles et costaud au bouclier. Les attaques spéciales se rechargent en huit secondes. Chaque joueur commence avec six points de vie ; les renforts sont partagés. Une vague terminée soigne les héros encore debout et ajoute un renfort. Il n'y a pas de tirs entre alliés.

## État de cette version

- Déplacements, sauts, tirs, dégâts, renforts, score, pause, victoire et défaite.
- Quatre héros aux vitesses, cadences et attaques spéciales différentes.
- Sprites et décor générés, intégrés à Unity ; animation courte par changement de poses.
- Nouveau menu avec logo illustré, cadres métalliques, sélection au clavier, souris ou manette.
- Boulevard, quais de Seine et Bastille : trois images découpées exactement dans une fresque continue, avec défilement en parallaxe et sans répétition.
- 24 images d'effets animés : flammes de bouche, impacts, explosions et poussière. Traceurs à cœur clair, douilles, recul au tir, étincelles sur le bouclier, flash blanc, réaction de recul et défaite de l'ennemi.
- Sons de tir, d'impact et d'explosion composés de plusieurs couches. Pas encore de musique.
- Terrain plat : plateformes, vrais décors destructibles, boss et animations plus riches restent à construire.
- Les commandes sont séparées de la simulation. Le jeu en ligne nécessite encore une implémentation réseau et n'est pas disponible.

## Vérification

Dix contrôles supplémentaires vérifient les raccords exacts, la couverture du panorama, les effets, le flash d'impact, le recul et la réutilisation des objets d'effets.

La progression des trois vagues et de la sortie a été vérifiée par simulation à quatre joueurs, avec invulnérabilité pour isoler la progression de l'équilibrage. Un périphérique virtuel Input System vérifie l'adaptateur manette. Cela ne remplace pas un essai avec quatre manettes physiques ; l'équilibrage reste à ajuster en jouant.

## Fichiers du projet

Les scripts se trouvent dans `Assets/Scripts`, les images dans `Assets/Resources/Art`, la scène dans `Assets/Scenes`. Les scripts du dossier `AgentScripts` servent à construire et vérifier le prototype dans l'éditeur ; ils ne sont pas inclus dans le jeu.

Les dossiers `Library`, `Temp`, `Logs` et `obj` sont des fichiers de travail Unity et ne doivent pas être versionnés. Unity les recrée si nécessaire.

## Missions et navigateur

Le nom du jeu est Action Legends. Les missions sont chargées depuis les JSON de `Assets/Resources/Levels` ; de nouvelles missions apparaissent automatiquement dans le menu après compilation. Voir le [README](README.md) pour la version navigateur et les contributions.
