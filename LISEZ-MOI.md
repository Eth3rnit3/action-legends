# Action Legends — guide de jeu

Prototype Unity 2D — version 0.7.0 : quatre héros sélectionnables et coopération locale sur un même écran. Trois missions qui s’enchaînent : Paris, Marseille, puis Tokyo.

## Jouer

1. Ouvrir ce dossier dans Unity 6000.4.7f1.
2. Ouvrir `Assets/Scenes/Paris2026.unity` si nécessaire.
3. Cliquer sur **Play**, choisir son héros, puis **Déployer l’équipe** ou **Entrée**.

Le joueur 1 peut jouer immédiatement au clavier. Sans manette, un seul joueur rejoint la partie. Appuyer sur **A / croix** sur une manette pour ajouter un partenaire : les places 2 à 4 sont occupées en premier ; une quatrième manette peut prendre le joueur 1. Les joueurs peuvent aussi rejoindre pendant le niveau. Cliquer sur une carte pour choisir son héros ; les pastilles J1 à J4 permettent de choisir le joueur concerné. Chaque manette peut choisir son propre héros avec la croix. Les héros peuvent être choisis plusieurs fois.

| Action | Joueur 1 au clavier | Manette |
|---|---|---|
| Déplacement | Q ou A / D | Stick gauche ou croix directionnelle |
| Viser vers le haut | W, Z ou flèche haut | Stick / croix vers le haut |
| S'accroupir ; en saut, viser vers le bas | S ou flèche bas | Stick / croix vers le bas |
| Saut | Espace | A / croix |
| Tir (couteau au contact) | J | X / carré ou gâchette droite |
| Grenade | L | B / rond |
| Attaque spéciale | K | Y / triangle ou bouton supérieur droit |
| Pause | Échap | Start / Options |
| Choisir le héros, au menu | Clic sur une carte, ← / → ou Q / E | Croix gauche / droite |
| Démarrer, au menu | Entrée | Start / Options |

Le clavier contrôle uniquement le joueur 1. Pour jouer à plusieurs, brancher des manettes.

**Accroupi**, le héros avance lentement et tire plus bas ; les balles tirées à hauteur de poitrine (les tireurs de Tokyo) lui passent au-dessus, mais pas les coups de près, les bouteilles ni les explosions. **En l'air**, bas + tir tire droit vers le bas. Avec le bouton de tir, un ennemi **au contact** devant le héros prend un coup de **couteau** (plus de dégâts, sans projectile). Les **grenades** partent en cloche, rebondissent une fois et explosent à l'impact sur un ennemi ou au sol : elles blessent tout ce qui est dans le rayon, boss compris, et détruisent les missiles du boss pris dedans. Le stock (affiché sous la vie, ×10) dépend de la difficulté : 10 au départ en Détente et Arcade, 8 en Difficile, 6 en Légende, avec un maximum de 20, 15, 12 et 10 ; il est rendu à chaque renfort, et les caisses vertes aux deux grenades en redonnent. Ces valeurs sont dans `Assets/Resources/Content/rules/default.json`.

## Première mission : Panique à Paname

Avancer vers la droite à travers douze assauts, puis affronter le boss en hélicoptère à Bastille. Le parcours est six fois plus long que celui de la version 0.4.0 ; à chaque assaut, l’écran se bloque jusqu’à la chute du dernier ennemi, puis une flèche **GO** clignote et le défilement reprend. Trois types d'ennemis : fonceur, lanceur de projectiles et costaud au bouclier. Les attaques spéciales se rechargent en cinq à huit secondes selon la difficulté. Les points de vie et les renforts dépendent de la difficulté ; les renforts sont partagés. En Détente, tous les trois assauts déployés, les héros encore debout reçoivent des soins et un renfort est ajouté ; en Arcade, cela arrive tous les quatre assauts. Difficile et Légende ne reçoivent pas ce réapprovisionnement. Il n'y a pas de tirs entre alliés.

Ramasser les caisses pour obtenir une mitrailleuse, un triple tir, des roquettes, des soins, dix secondes de turbo ou des points. Les armes bonus ont des munitions limitées ; le fusil de base est illimité. Le chronomètre permet de comparer ses temps de parcours.

La fréquence des bonus, les munitions et la durée du turbo dépendent de la difficulté. Détente conserve le réglage de la version 0.4.3 : un bonus pour quinze ennemis et des caisses espacées de 72 unités. Les autres modes distribuent moins de ressources.

## Deuxième mission : Mistral Gagnant

Marseille en 2026 : dix assauts du Vieux-Port à la Canebière en passant par le Panier, puis l'hélicoptère d'une chaîne d'info en continu au pied de Notre-Dame-de-la-Garde. Musique arcade sur La Coupo Santo. Détails dans [docs/MISSION_MARSEILLE.md](docs/MISSION_MARSEILLE.md).

## Troisième mission : Tokyo Néon

Tokyo de nuit : neuf assauts de Shibuya à la tour de Tokyo en passant par Akihabara. Tous les ennemis sont armés — pistolet, mitraillette, fusil à pompe. Ils s’arrêtent pour viser, puis tirent à hauteur de poitrine : sauter au bon moment évite la balle, passer derrière eux aussi. Le boss, le Mécha Shogun, marche vers l’équipe et alterne rafale de gatling, piétinement qui lance deux ondes de choc à sauter, et salve de missiles. Musique arcade sur Sakura Sakura. Détails dans [docs/MISSION_TOKYO.md](docs/MISSION_TOKYO.md).

## Campagne et codes

Les missions s’enchaînent. Au départ, seule Panique à Paname est ouverte. Une mission gagnée débloque la suivante dans ce navigateur, et l’écran de victoire affiche son code ; **MISSION SUIVANTE** (ou Entrée / Start) la lance aussitôt avec les mêmes héros et le score acquis. Au menu, **CODE MISSION** permet de saisir un code pour rejoindre directement une mission, par exemple sur un autre ordinateur. Les flèches du menu ne parcourent que les missions débloquées. En ligne, seul l’hôte choisit la mission et saisit les codes.

## Classement mondial

À la fin d’une mission, **PUBLIER MON SCORE** envoie le score et le replay de la partie sur des relais Nostr publics, sous le pseudo de la page d’accueil. **CLASSEMENT**, au menu, montre le top 10 de chaque mission et difficulté ; chaque score est rejoué dans le navigateur avant d’être affiché. Depuis la version 0.7.0, le jeu local avance par pas fixes de 1/60 s comme le jeu en ligne. Détails et limites dans [docs/CLASSEMENT.md](docs/CLASSEMENT.md).

## État de cette version

- Déplacements, sauts, tirs, dégâts, renforts, score, pause, victoire et défaite.
- Quatre héros aux vitesses, cadences et attaques spéciales différentes.
- Sprites et décor générés, intégrés à Unity ; animation courte par changement de poses.
- Nouveau menu avec logo illustré, cadres métalliques, sélection au clavier, souris ou manette.
- Boulevard, quais, canal Saint-Martin et Bastille : décors successifs avec fondus aux raccords, défilement en parallaxe et chaussée au premier plan.
- 24 images d'effets animés : flammes de bouche, impacts, explosions et poussière. Traceurs à cœur clair, douilles, recul au tir, étincelles sur le bouclier, flash blanc, réaction de recul et défaite de l'ennemi.
- Bruitages rétro différents selon l'arme, impacts et explosions variés, musique en boucle et annonces vocales françaises des bonus.
- Terrain plat : plateformes, vrais décors destructibles et animations plus riches restent à construire.
- Les commandes sont séparées de la simulation. En ligne, chaque navigateur exécute la même simulation à partir des commandes de tous les joueurs : voir [docs/ONLINE.md](docs/ONLINE.md).

## Vérification

La version 0.4.2 a été compilée pour navigateur. Un contrôle ciblé vérifie la sélection au clavier, une course avec tirs à deux personnages, la vitesse attendue sans retour arrière et les bonus d'arme et de turbo. Les missions passent également le validateur de contenu et ses huit tests Python. Cela ne remplace pas un essai avec quatre manettes physiques ; l'équilibrage reste à ajuster en jouant.

## Fichiers du projet

Les scripts se trouvent dans `Assets/Scripts`, les images dans `Assets/Resources/Art`, la scène dans `Assets/Scenes`. Les scripts du dossier `AgentScripts` servent à construire et vérifier le prototype dans l'éditeur ; ils ne sont pas inclus dans le jeu.

Les dossiers `Library`, `Temp`, `Logs` et `obj` sont des fichiers de travail Unity et ne doivent pas être versionnés. Unity les recrée si nécessaire.

## Missions et navigateur

Le nom du jeu est Action Legends. Les missions sont chargées depuis les JSON de `Assets/Resources/Levels`, et leurs héros, ennemis et boss depuis la bibliothèque `Assets/Resources/Content` ; de nouvelles missions apparaissent automatiquement dans le menu après compilation. Voir [docs/ADDING_A_LEVEL.md](docs/ADDING_A_LEVEL.md). Voir le [README](README.md) pour la version navigateur et les contributions.

## Difficultés et boss — version 0.5.0

Choisir Détente, Arcade (par défaut), Difficile ou Légende dans le menu. La difficulté reste fixe pendant une partie. Elle modifie la résistance, le nombre et l’agressivité des ennemis, les bonus, les dégâts en Légende et la résistance du boss.

| Difficulté | PV | Renforts par joueur | Bonus par éliminations |
|---|---:|---:|---:|
| Détente | 6 | 3 | 15 |
| Arcade | 5 | 2 | 22 |
| Difficile | 4 | 1 | 28 |
| Légende | 3 | 1 | 35 |

À Bastille, la caméra cadre une arène et le boss caricatural apparaît. Les repères rouges annoncent ses attaques. Bouger pour éviter le mitraillage, sauter au-dessus des explosions ou détruire les bombes en vol. Maintenir W / Z / flèche haut avec J pour tirer verticalement ; courir en visant vers le haut donne un tir diagonal. À la manette, diriger le stick vers le haut avec X / RT. La victoire exige de détruire l’hélico. Sa résistance augmente avec le nombre de joueurs.

La musique est un arrangement arcade de La Marseillaise en synthèse FM ; le boss a sa propre version en mineur. Chaque annonce vocale existe en trois prises jouées au hasard. Voir [l'audio](docs/AUDIO.md). Le menu permet de couper séparément musique et voix. Les voix baissent temporairement le volume des tirs et de la musique. Voir [les crédits et le prompt du visuel](docs/UPDATE_050.md).

La vérification ciblée `AgentScripts/VerifyDifficultyAndBoss.cs` contrôle les différences de difficulté, les dégâts doublés en Légende, la cadence des bonus, les tirs verticaux contre le boss, les bombes destructibles, l’alternance des attaques, la victoire et la présence des sons.

## Boss — version 0.5.1

Les projectiles largués réutilisent directement les pixels des munitions dessinées sur l’hélicoptère. Ils partent désormais des supports et rejoignent les zones rouges annoncées au sol. L’hélico traverse plus vite l’arène, tire des rafales plus denses en balayage et laisse moins de répit entre ses attaques. À mi-vie, le mode Furie accélère encore ses déplacements et ses attaques et ajoute un projectile par largage. Les sons ne sont pas modifiés par cette mise à jour.

Le HUD 0.5.1 affiche seulement des informations compactes : nom court et vie des joueurs, arme et munitions, spécial, score, chrono et renforts. Les bandeaux d’attaque et les consignes permanentes sont supprimés. La barre de vie du boss reste discrète en bas de l’écran. Échap / Start ouvre la pause avec le rappel des commandes.
