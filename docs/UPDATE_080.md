# Version 0.8.0 — Action Legends passe en mode Metal Slug

Nouveautés pour le joueur depuis la version 0.7.0. Les commandes complètes sont dans [LISEZ-MOI.md](../LISEZ-MOI.md).

## Une quatrième mission : Prophétie en Provence

Saint-Rémy-de-Provence s'intercale entre Marseille et Tokyo. Elle compte neuf assauts, de la place de la République aux Alpilles. L'équipe y affronte des Arlésiens au poing, au parapluie et au fouet, puis un taureau de Camargue qui charge sur la route. Au bout, Nostradamus annonce ses prophéties par des cercles dorés au sol. Musique : la Farandole en 8 bits. Code : **FARANDOLE** ; le code de Tokyo, **SAKURA**, reste valable.

Le menu affiche désormais le logo de la mission quand elle en a un, comme Saint-Rémy.

**Déblocage :** une mission est ouverte dès qu'elle-même, ou une mission plus loin dans la campagne, est débloquée. Si vous aviez déjà ouvert Tokyo, Saint-Rémy est donc ouverte aussi. Un code de mission ouvre cette mission et toutes celles qui la précèdent.

## Commandes à la Metal Slug

- **Accroupi** : bas (S, flèche bas, stick vers le bas). Le héros avance lentement et tire plus bas. Les balles tirées à hauteur de poitrine lui passent au-dessus.
- **Tir vers le bas en saut** : bas + tir en l'air.
- **Couteau** : le bouton de tir frappe au couteau un ennemi au contact.
- **Grenades** : L, ou B / rond à la manette. Elles partent en cloche, rebondissent et explosent ; leur stock s'affiche sous la vie.

## Armes à lettres

Les caisses donnent une arme aux munitions comptées :

- **H** : mitrailleuse lourde ;
- **S** : fusil à pompe ;
- **F** : lance-flammes ;
- **R** : lance-roquettes à tête chercheuse ;
- **L** : laser.

Le HUD montre le badge et les munitions de l'arme. Une arme vide, ou un héros qui tombe, revient au fusil de base. Chaque mission choisit les armes de ses caisses.

## Otages et décompte

Des prisonniers attachés attendent le long du parcours, certains au milieu d'un assaut. Tirez sur leurs liens ou touchez-les : ils lâchent un cadeau (arme, grenades, soins ou points) avant de s'enfuir. Chaque ville a son otage : un boulanger, un bouliste, un peintre, un employé de bureau.

À la victoire, un **décompte** paie les otages sauvés et l'avance sur le temps de référence.

## Mode SLUG

C'est la cinquième difficulté :

- mort en un coup ;
- trois vies par joueur, avec retour sur place ;
- puis **CONTINUE ?** et dix secondes pour reprendre (saut ou tir) ou abandonner (grenade).

Une partie continuée ne peut pas être publiée au classement.

## Des missions mises en scène

- À chaque assaut, l'écran se bloque jusqu'à la chute du dernier ennemi, puis une flèche **GO** relance le défilement.
- Les ennemis entrent par les portes et les fenêtres, en parachute ou depuis une camionnette.
- Certains attendent sans se méfier et sursautent avec un « ! ». D'autres paniquent et fuient, ou lancent des grenades.
- Les ennemis tombent différemment selon l'arme qui les touche.
- Chaque mission a un **mini-boss** à mi-parcours : le fourgon blindé à Paris, le hors-bord à Marseille, le taureau à Saint-Rémy, le drone de combat à Tokyo.
- De nouvelles voix annoncent les armes, les grenades, les otages et les mini-boss, et les ennemis crient.

## Classement

La simulation a changé (nouvelles commandes, armes, otages, missions réécrites). Les scores publiés en 0.7.0 ne se rejouent donc plus à l'identique : ils disparaissent du classement 0.8.0, qui repart de zéro. Le mode Slug a son propre tableau, réservé aux parties sans continue. Voir [CLASSEMENT.md](CLASSEMENT.md).

## Jeu en ligne

Tous les joueurs doivent avoir la version 0.8.0 : la version est contrôlée à la connexion.

## Pour qui fait des missions

Le contenu est maintenant entièrement décrit en JSON :

- héros, ennemis, boss, armes, otages, règles, difficultés, véhicules de dépôt ;
- dans chaque mission : décor posé, rencontres, mini-boss et logo.

Voir [ADDING_A_LEVEL.md](ADDING_A_LEVEL.md).
