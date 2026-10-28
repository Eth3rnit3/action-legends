# Action Legends — guide de jeu

Prototype Unity 2D — version 0.9.0 : sept héros sélectionnables (Chuck Norris, Arnold, Jean-Claude, Sylvester, Fernandel, Brigitte Bardot et Jeanne d'Arc) et coopération locale sur un même écran. Quatre missions qui s’enchaînent : Paris, Marseille, Saint-Rémy-de-Provence, puis Tokyo. Les nouveautés de cette version pour le joueur sont résumées dans [docs/UPDATE_090.md](docs/UPDATE_090.md) (celles de la 0.8.0 dans [docs/UPDATE_080.md](docs/UPDATE_080.md)).

## Jouer

1. Ouvrir ce dossier dans Unity 6000.4.7f1.
2. Ouvrir `Assets/Scenes/Paris2026.unity` si nécessaire.
3. Cliquer sur **Play**, choisir son héros, puis **Déployer l’équipe** ou **Entrée**.

Le joueur 1 peut jouer immédiatement au clavier. Sans manette, un seul joueur rejoint la partie. Appuyer sur **A / croix** sur une manette pour ajouter un partenaire : les places 2 à 4 sont occupées en premier ; une quatrième manette peut prendre le joueur 1. Les joueurs peuvent aussi rejoindre pendant le niveau. Le menu montre quatre cartes, une par joueur (J1 à J4) : chacun fait défiler tous les héros sur la sienne avec ses flèches (souris), Q / E ou ← / → pour J1 au clavier, ou la croix gauche / droite de sa manette ; la carte indique le rôle, la spéciale et la place du héros (« 5 / 7 »). Une carte vide invite à rejoindre. Les héros peuvent être choisis plusieurs fois.

| Action | Joueur 1 au clavier | Manette |
|---|---|---|
| Déplacement | Q ou A / D | Stick gauche ou croix directionnelle |
| Viser vers le haut | W, Z ou flèche haut | Stick / croix vers le haut |
| S'accroupir ; en saut, viser vers le bas | S ou flèche bas | Stick / croix vers le bas |
| Saut ; avec bas, descendre d'une plateforme ou sortir d'un véhicule | Espace | A / croix |
| Tir (couteau au contact) | J | X / carré ou gâchette droite |
| Grenade (au volant : obus du canon) | L | B / rond |
| Attaque spéciale | K | Y / triangle ou bouton supérieur droit |
| Pause | Échap | Start / Options |
| Choisir le héros, au menu | Flèches de sa carte (souris), ← / →, Q / E ou A / D | Croix gauche / droite |
| Changer de mission, au menu | Clic sur < / > | LB / RB (manette du joueur 1) |
| Démarrer, au menu | Entrée | Start / Options |

Le clavier contrôle uniquement le joueur 1. Pour jouer à plusieurs, brancher des manettes.

**Otages.** Comme dans Metal Slug, des prisonniers attachés attendent le long du parcours, certains au milieu d'un assaut : tirer sur leurs liens, les toucher, ou faire exploser une grenade à côté les libère. L'otage salue, lâche un cadeau (une caisse d'arme, des grenades, des soins ou des points) puis s'enfuit en courant. Chaque joueur compte ses otages sous sa fiche (icône et ×N) et perd son compte s'il tombe. À la victoire, le **décompte de fin de mission** paie 1 000 points par otage compté et 10 points par seconde d'avance sur le temps de référence de la mission (5 minutes à Paris, 4 à Marseille, 4 min 40 à Saint-Rémy, 3 min 40 à Tokyo), avant l'écran de victoire ; Entrée ou **PASSER** enchaîne tout de suite. Chaque ville a son otage (un boulanger à Paris, un bouliste à Marseille, un peintre à Saint-Rémy, un employé de bureau à Tokyo) ; les caisses du parcours sont deux fois moins nombreuses qu'avant.

**Mode SLUG.** La cinquième difficulté du menu joue comme une borne d'arcade : **mort en un coup**, trois vies par joueur (deux de réserve), retour sur place après une seconde, clignotant et invulnérable, avec le fusil de base. Quand plus personne n'a de vie, **CONTINUE ?** compte dix secondes : après une seconde, **saut** (Espace, A / croix) ou un nouvel appui sur **tir** continue (chaque héros revient sur place avec trois vies, la mission reprend où elle en était), **grenade** (L, B / rond) abandonne, et la fin du compte à rebours mène à la défaite. Les boutons CONTINUER et ABANDONNER de l'écran font de même. Le nombre de continues s'affiche sur l'écran de fin ; une partie continuée n'est pas publiable au classement.

**Accroupi**, le héros avance lentement et tire plus bas ; les balles tirées à hauteur de poitrine (les tireurs de Tokyo) lui passent au-dessus, mais pas les coups de près, les bouteilles ni les explosions. **En l'air**, bas + tir tire droit vers le bas. Avec le bouton de tir, un ennemi **au contact** devant le héros prend un coup de **couteau** (plus de dégâts, sans projectile). Les **grenades** partent en cloche, rebondissent une fois et explosent à l'impact sur un ennemi ou au sol : elles blessent tout ce qui est dans le rayon, boss compris, et détruisent les missiles du boss pris dedans. Le stock (affiché sous la vie, ×10) dépend de la difficulté : 10 au départ en Détente et Arcade, 8 en Difficile, 6 en Légende, avec un maximum de 20, 15, 12 et 10 ; il est rendu à chaque renfort, et les caisses vertes aux deux grenades en redonnent. Ces valeurs sont dans `Assets/Resources/Content/rules/default.json`.

**Relief et décors destructibles.** Chaque ville a ses échafaudages, pontons et quais où l'on saute (par-dessous) et se tient debout — **bas + saut** fait redescendre —, ses escaliers et terrasses que l'on monte en marchant, et ses décors à casser : voitures, triporteurs, charrettes et fourgonnettes (on peut grimper dessus, elles arrêtent les balles ennemies et explosent), étals (leur auvent sert de plateforme), caisses et bottes de foin qui barrent le passage et cachent un bonus, barils, bidons et tonneaux de poudre qui explosent et en font sauter d'autres (attention : ils blessent aussi le héros trop proche). Tirs, grenades, couteau et spéciales les abîment, ils changent d'image puis tombent en morceaux. Dans chaque mission, une **barricade** surmontée d'une flèche clignotante bloque un assaut : l'assaut ne finit que quand elle tombe, et finit dès qu'elle tombe. Les ennemis sautent sur les obstacles bas et certains (les pillards à pied-de-biche, les Arlésiens) grimpent sur la plateforme du héros.

**Véhicules.** Comme le char SV-001 de Metal Slug, chaque mission gare un véhicule au milieu du parcours : la **2CV blindée** à Paris, le **fourgon du port** à Marseille, le **taureau mécanique** à Saint-Rémy et le **mini-mécha** à Tokyo. Il suffit de le toucher pour y monter (« Véhicule ! ») : le héros se tient dans le poste, et tant qu'il conduit c'est le véhicule qui encaisse tous les coups, sur sa propre jauge affichée à la place des PV dans la fiche du joueur. **Tir** fait tourner et tirer la mitrailleuse de bord vers la visée (devant, en haut, en diagonale en roulant, en bas, droit en bas en l'air), **grenade** tire un obus du canon (leur nombre remplace celui des grenades), **saut** fait sauter le véhicule, **bas + saut** fait sortir le héros. Le véhicule monte les pentes et les terrasses, écrase étals, caisses, foin et barils en roulant dessus, enfonce les voitures et les barricades à force de les percuter, et écrase les ennemis en leur retombant dessus. Quand sa jauge tombe à zéro, le pilote est éjecté (invulnérable un instant), le véhicule clignote puis explose en emportant les ennemis voisins. Un véhicule laissé vide peut être repris par n'importe quel joueur ; sa spéciale, son couteau et ses grenades, le héros les retrouve en sortant. Contre les boss, la mitrailleuse de bord tape moins fort que contre la rue (40 PV/s contre 30 au fusil) et les obus sont moins nombreux : le véhicule protège surtout, il n'expédie pas les mini-boss.

**Rencontres et ennemis vivants.** Les ennemis ne surgissent plus seulement des bords de l'écran : certains sortent d'une porte cochère, d'un rideau de fer ou d'une bouche d'égout, tirent depuis une fenêtre, descendent en parachute, sautent d'un toit ou d'une fenêtre, ou débarquent d'une camionnette qui s'arrête puis repart. D'autres attendent sans se méfier (cigarette, sandwich, coup de téléphone, coup de peigne…) : tant qu'ils n'ont pas vu l'équipe, ils ne tirent pas ; quand ils la remarquent — devant eux, de trop près, sous une balle ou parce qu'un voisin donne l'alerte —, ils sursautent avec un « ! » avant d'attaquer. Blessés ou voyant tomber leurs camarades, certains paniquent et s'enfuient avant de revenir. Les lanceurs de bouteilles et les colosses au fusil lancent aussi des **grenades en cloche** : s'éloigner du point de chute les évite. Les ennemis tombent selon ce qui les a touchés : renversés par les balles, soufflés en l'air par une explosion, carbonisés par le lance-flammes, pliés par le couteau. Au milieu de chaque mission, un **mini-boss** bloque l'écran avec sa barre de vie : le fourgon blindé à Paris (il charge : sautez par-dessus), le hors-bord des caïds à Marseille, le taureau de Camargue à Saint-Rémy (il charge aussi), le drone de combat à Tokyo ; sa chute relance le défilement.

## Première mission : Panique à Paname

Avancer vers la droite à travers dix assauts mis en scène, le fourgon blindé à mi-parcours, puis affronter le boss en hélicoptère à Bastille. Le parcours est six fois plus long que celui de la version 0.4.0 ; à chaque assaut, l’écran se bloque jusqu’à la chute du dernier ennemi, puis une flèche **GO** clignote et le défilement reprend. Trois types d'ennemis : fonceur, lanceur de projectiles et costaud au bouclier. Les attaques spéciales se rechargent en cinq à huit secondes selon la difficulté. Les points de vie et les renforts dépendent de la difficulté ; les renforts sont partagés (sauf en Slug, où chacun a ses vies). En Détente, tous les trois assauts déployés, les héros encore debout reçoivent des soins et un renfort est ajouté ; en Arcade, cela arrive tous les quatre assauts. Difficile et Légende ne reçoivent pas ce réapprovisionnement. Il n'y a pas de tirs entre alliés.

Ramasser les caisses pour obtenir une arme, des soins, dix secondes de turbo ou des points. Le chronomètre permet de comparer ses temps de parcours.

**Les armes à lettres**, comme dans Metal Slug : chaque héros a son fusil, illimité ; les caisses à lettre donnent une arme aux munitions comptées, affichée dans le HUD par son badge et le nombre de tirs restants :

| Caisse | Arme | Tir |
|---|---|---|
| **H** orange | Mitrailleuse lourde | rafale rapide, légère dispersion |
| **S** bleue | Fusil à pompe | éventail de cinq plombs à courte portée, qui traversent les ennemis ; recul |
| **F** rouge | Lance-flammes | jet de flammes court qui grossit et brûle tout ce qu'il traverse |
| **R** verte | Lance-roquettes | roquettes à tête chercheuse qui explosent sur un ennemi, au sol ou en fin de course |
| **L** violette | Laser | rayon continu qui traverse tout l'écran |

Ramasser la même arme remplit ses munitions ; une arme vide, ou un héros qui tombe, revient au fusil de base. Toutes les visées marchent avec chaque arme : haut, diagonale, accroupi, bas en saut. Paris tire ses caisses d'armes d'une table commune (plus de mitrailleuses lourdes), Marseille donne surtout fusils à pompe et lance-flammes, Saint-Rémy surtout des fusils à pompe, Tokyo surtout des lasers. Les armes sont décrites dans `Assets/Resources/Content/weapons/` et chaque mission choisit lesquelles ses caisses donnent (voir le guide des niveaux).

La fréquence des bonus, les munitions et la durée du turbo dépendent de la difficulté. Détente donne un bonus pour quinze ennemis et, depuis l'arrivée des otages, une caisse toutes les 150 unités (72 auparavant). Les autres modes distribuent moins de ressources : moins de caisses et des armes aux munitions plus comptées (par exemple 120 tirs de mitrailleuse lourde en Détente, 66 en Légende). Ces réglages sont dans `Assets/Resources/Content/rules/` (`default.json`, et `pow.json` que jouent les quatre missions) et `Content/weapons/`.

## Deuxième mission : Mistral Gagnant

Marseille en 2026 : huit assauts du Vieux-Port à la Canebière en passant par le Panier, le hors-bord des caïds au Vieux-Port, puis l'hélicoptère d'une chaîne d'info en continu au pied de Notre-Dame-de-la-Garde. Musique arcade sur La Coupo Santo. Détails dans [docs/MISSION_MARSEILLE.md](docs/MISSION_MARSEILLE.md).

## Troisième mission : Prophétie en Provence

Saint-Rémy-de-Provence : neuf assauts de la place de la République aux Alpilles, par les ruelles, la place Favier, les Antiques et Glanum. Les Arlésiens défendent la ville : l'Arlésien frappe du poing, l'Arlésienne pique de son parapluie, l'Arlésien au fouet frappe de plus loin — garder ses distances ne suffit pas. Ils sortent de derrière les tonneaux, les oliviers et les colonnes, ou font la sieste au bord de la route. À la sortie de la ville, un taureau de Camargue charge d'un bord à l'autre de l'écran : sauter par-dessus l'évite. Au pied des Alpilles, Nostradamus lance des parchemins, annonce ses prophéties par des cercles dorés au sol (sortir du cercle ou sauter avant l'impact) et appelle les Arlésiens à la rescousse. Neuf otages, dont des peintres de Provence. Musique : la Farandole en 8 bits. Code : **FARANDOLE**. Détails dans [docs/MISSION_SAINT_REMY.md](docs/MISSION_SAINT_REMY.md).

## Quatrième mission : Tokyo Néon

Tokyo de nuit : huit assauts de Shibuya à la tour de Tokyo en passant par Akihabara, et un drone de combat au carrefour. Tous les ennemis sont armés — pistolet, mitraillette, fusil à pompe. Ils s’arrêtent pour viser, puis tirent à hauteur de poitrine : sauter au bon moment évite la balle, passer derrière eux aussi. Le boss, le Mécha Shogun, marche vers l’équipe et alterne rafale de gatling, piétinement qui lance deux ondes de choc à sauter, et salve de missiles. Musique arcade sur Sakura Sakura. Détails dans [docs/MISSION_TOKYO.md](docs/MISSION_TOKYO.md).

## Campagne et codes

Les missions s’enchaînent : Paris, Marseille (code PASTIS), Saint-Rémy (FARANDOLE), Tokyo (SAKURA). Au départ, seule Panique à Paname est ouverte. Une mission est ouverte dès qu'elle-même ou une mission plus loin dans la campagne est débloquée : une mission déjà débloquée dans ce navigateur le reste, et une nouvelle mission qui s'intercale avant elle (comme Saint-Rémy avant Tokyo) s'ouvre aussi. Une mission gagnée débloque la suivante dans ce navigateur, et l’écran de victoire affiche son code ; **MISSION SUIVANTE** (ou Entrée / Start) la lance aussitôt avec les mêmes héros et le score acquis. Au menu, **CODE MISSION** permet de saisir un code pour rejoindre directement une mission, par exemple sur un autre ordinateur ; les missions qui la précèdent s'ouvrent avec elle. Les flèches du menu ne parcourent que les missions débloquées. En ligne, seul l’hôte choisit la mission et saisit les codes.

## Classement mondial

À la fin d’une mission, **PUBLIER MON SCORE** envoie le score et le replay de la partie sur des relais Nostr publics, sous le pseudo de la page d’accueil. **CLASSEMENT**, au menu, montre le top 10 de chaque mission et difficulté (Slug compris, parties sans continue seulement) ; chaque score est rejoué dans le navigateur avant d’être affiché. Depuis la version 0.7.0, le jeu local avance par pas fixes de 1/60 s comme le jeu en ligne. Chaque version a son propre classement, rappelé au bas du tableau : la 0.9.0 change le gameplay (relief, décors, véhicules, nouveaux héros), son classement repart donc de zéro, et les scores publiés en 0.8.0 ne sont ni rejoués ni affichés. Détails et limites dans [docs/CLASSEMENT.md](docs/CLASSEMENT.md).

## État de cette version

- Déplacements, sauts, tirs, dégâts, renforts, score, pause, victoire et défaite.
- Sept héros aux vitesses, armes et attaques spéciales différentes : coup circulaire (Chuck), rafale lourde (Arnold), double coup sauté (Jean-Claude), salve explosive (Sylvester), Marguerite à la rescousse (Fernandel siffle, sa vache charge et piétine tout ce qu'elle croise, clin d'œil à « La Vache et le prisonnier »), feu croisé (Brigitte tire aux deux revolvers des deux côtés) et charge à l'étendard (Jeanne fonce, invulnérable, et renverse les ennemis). Jeanne se bat à l'épée : sa lame lance une courte vague de lumière qui traverse un ennemi, et son coup au contact porte plus loin et plus fort. Brigitte a un revolver rapide, Fernandel un fusil lent qui frappe fort et traverse.
- Sprites et décor générés, intégrés à Unity ; animation par changement de poses. Les héros encaissent un coup (pose blessée), tombent à la renverse et restent à terre jusqu'au renfort (ils clignotent avant de se relever), et lèvent le poing quand un boss tombe ou à la fin de la mission. Les ennemis les plus nombreux (pillard au pied-de-biche, lanceur de bouteilles, pistolero de Tokyo, Arlésien) courent et fuient en quatre images, finissent leur coup et tombent en trois images.
- Nouveau menu avec logo illustré, cadres métalliques, sélection au clavier, souris ou manette.
- Boulevard, quais, canal Saint-Martin et Bastille : décors successifs avec fondus aux raccords, défilement en parallaxe et chaussée au premier plan.
- 24 images d'effets animés : flammes de bouche, impacts, explosions et poussière. Traceurs à cœur clair, douilles qui rebondissent sur le relief, recul au tir, étincelles sur le bouclier, flash blanc, ennemis qui sursautent sous les coups, poussière à l'atterrissage, secousse de l'écran aux explosions, et défaite de l'ennemi. Tout se règle dans les règles de partie (`screen`, `poses`).
- Bruitages rétro différents selon l'arme, impacts et explosions variés, musique en boucle et annonces vocales françaises des bonus.
- Relief (plateformes, escaliers, terrasses ; trous gérés par le moteur mais pas posés dans les missions) et vingt décors destructibles, cinq par ville, avec une barricade-objectif par mission. Les petites marches au pied des escaliers de Tokyo ont leur relief.
- Les commandes sont séparées de la simulation. En ligne, chaque navigateur exécute la même simulation à partir des commandes de tous les joueurs : voir [docs/ONLINE.md](docs/ONLINE.md).

## Vérification

La version 0.9.0 a été compilée pour navigateur et essayée dans un navigateur de bureau. Les scripts `AgentScripts/Verify*.cs`, lancés dans l'éditeur en mode batch, vérifient chaque fonctionnalité (blocage d'écran, commandes, armes, otages, mode Slug, rencontres, mini-boss, héros et spéciales, relief, décors destructibles, véhicules, poses, classement par version, chaque mission) et font gagner les quatre missions par des bots, seuls et à quatre, dans toutes les difficultés ; les replays enregistrés doivent se reproduire à l'identique. Les missions passent également le validateur de contenu et ses tests Python. Cela ne remplace pas un essai avec quatre manettes physiques ; l'équilibrage reste à ajuster en jouant.

## Fichiers du projet

Les scripts se trouvent dans `Assets/Scripts`, les images dans `Assets/Resources/Art`, la scène dans `Assets/Scenes`. Les scripts du dossier `AgentScripts` servent à construire et vérifier le prototype dans l'éditeur ; ils ne sont pas inclus dans le jeu.

Les dossiers `Library`, `Temp`, `Logs` et `obj` sont des fichiers de travail Unity et ne doivent pas être versionnés. Unity les recrée si nécessaire.

## Missions et navigateur

Le nom du jeu est Action Legends. Les missions sont chargées depuis les JSON de `Assets/Resources/Levels`, et leurs héros, ennemis et boss depuis la bibliothèque `Assets/Resources/Content` ; de nouvelles missions apparaissent automatiquement dans le menu après compilation. Voir [docs/ADDING_A_LEVEL.md](docs/ADDING_A_LEVEL.md). Voir le [README](README.md) pour la version navigateur et les contributions.

## Difficultés et boss — version 0.5.0

Choisir Détente, Arcade (par défaut), Difficile, Légende ou Slug dans le menu ; une ligne sous les boutons résume la difficulté choisie. La difficulté reste fixe pendant une partie. Elle modifie la résistance, le nombre et l’agressivité des ennemis, les bonus, les dégâts en Légende et la résistance du boss. Les règles de chaque difficulté sont dans `Assets/Resources/Content/difficulties/` (voir le guide des niveaux).

| Difficulté | PV | Renforts | Bonus par éliminations | Au fil des assauts | Continue |
|---|---:|---|---:|---|---|
| Détente | 6 | 3 par joueur, partagés | 15 | soins (+2) et un renfort tous les 3 assauts | non |
| Arcade | 5 | 2 par joueur, partagés | 22 | soins (+1) et un renfort tous les 4 assauts | non |
| Difficile | 4 | 1 par joueur, partagé | 28 | — | non |
| Légende | 3 | 1 par joueur, partagé ; dégâts doublés | 35 | — | non |
| Slug | 1 (mort en un coup) | 2 vies de réserve chacun, retour sur place | 22 | — | oui, 10 s, illimités |

Slug reprend les ennemis, le boss, les bonus et les munitions d'Arcade. Le classement a un tableau Slug par mission, réservé aux parties sans continue.

À Bastille, la caméra cadre une arène et le boss caricatural apparaît. Les repères rouges annoncent ses attaques. Bouger pour éviter le mitraillage, sauter au-dessus des explosions ou détruire les bombes en vol. Maintenir W / Z / flèche haut avec J pour tirer verticalement ; courir en visant vers le haut donne un tir diagonal. À la manette, diriger le stick vers le haut avec X / RT. La victoire exige de détruire l’hélico. Sa résistance augmente avec le nombre de joueurs.

La musique est un arrangement arcade de La Marseillaise en synthèse FM ; le boss a sa propre version en mineur. Chaque annonce vocale existe en trois prises jouées au hasard. Voir [l'audio](docs/AUDIO.md). Le menu permet de couper séparément musique et voix. Les voix baissent temporairement le volume des tirs et de la musique. Voir [les crédits et le prompt du visuel](docs/UPDATE_050.md).

La vérification ciblée `AgentScripts/VerifyDifficultyAndBoss.cs` contrôle les différences de difficulté, les dégâts doublés en Légende, la cadence des bonus, les tirs verticaux contre le boss, les bombes destructibles, l’alternance des attaques, la victoire et la présence des sons.

## Boss — version 0.5.1

Les projectiles largués réutilisent directement les pixels des munitions dessinées sur l’hélicoptère. Ils partent désormais des supports et rejoignent les zones rouges annoncées au sol. L’hélico traverse plus vite l’arène, tire des rafales plus denses en balayage et laisse moins de répit entre ses attaques. À mi-vie, le mode Furie accélère encore ses déplacements et ses attaques et ajoute un projectile par largage. Les sons ne sont pas modifiés par cette mise à jour.

Le HUD 0.5.1 affiche seulement des informations compactes : nom court et vie des joueurs, arme et munitions, spécial, score, chrono et renforts. Les bandeaux d’attaque et les consignes permanentes sont supprimés. Depuis le lot 3c, un bandeau court revient sous la ligne du score pour les annonces (vague, otage libéré, arrivée et rage des boss, armes), et le score, le chrono et les renforts sont cernés de sombre pour rester lisibles sur un ciel clair. La barre de vie du boss reste discrète en bas de l’écran. Échap / Start ouvre la pause avec le rappel des commandes.
