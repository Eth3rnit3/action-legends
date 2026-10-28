# Version 0.9.0 — Action Legends prend du relief

Nouveautés pour le joueur depuis la version 0.8.0. Les commandes complètes sont dans [LISEZ-MOI.md](../LISEZ-MOI.md).

## Trois nouveaux héros

Fernandel, Brigitte Bardot et Jeanne d'Arc rejoignent les quatre héros d'origine. Chacun a son arme et une spéciale d'un genre nouveau :

- **Fernandel** porte un fusil Lebel lent qui frappe fort et traverse un ennemi. Sa spéciale : il siffle, et **Marguerite**, sa vache, traverse l'écran en piétinant tout ce qu'elle croise. C'est un clin d'œil à *La Vache et le prisonnier*.
- **Brigitte Bardot** tire vite au revolver. Sa spéciale, le **feu croisé**, envoie cinq salves rapides de ses deux revolvers, devant et derrière à la fois.
- **Jeanne d'Arc** se bat à l'épée. Sa lame lance une courte vague de lumière qui traverse un ennemi, et son coup au contact porte plus loin et plus fort. Sa spéciale, la **charge à l'étendard**, la fait foncer, invulnérable, en renversant les ennemis. Elle atteint aussi un boss collé au bord de l'arène.

## Un menu à quatre cartes

Le menu montre désormais quatre cartes, une par joueur (J1 à J4), au lieu d'une carte par héros. Chaque joueur fait défiler tous les héros sur sa carte :

- à la souris, avec les flèches de la carte ;
- au clavier pour J1, avec Q / E ou ← / → ;
- à la manette, avec la croix.

La carte affiche le portrait, le rôle, la spéciale et la place du héros dans la liste (« 5 / 7 »). Plusieurs joueurs peuvent prendre le même héros. Une carte vide invite à rejoindre avec une manette ; en ligne, elle indique une place libre.

## Du relief et des décors à casser

- **Relief.** Chaque ville a ses échafaudages, pontons et quais. On y saute par-dessous, on s'y tient debout, et **bas + saut** fait redescendre. Les escaliers et les terrasses se montent en marchant. Les ennemis sautent sur les obstacles bas, et les pillards à pied-de-biche comme les Arlésiens montent sur la plateforme du héros.
- **Décors destructibles.** Vingt décors, cinq par ville :
  - voitures, triporteurs, charrettes et fourgonnettes : on grimpe dessus, elles arrêtent les balles ennemies et explosent ;
  - étals, dont l'auvent sert de plateforme ;
  - caisses, bottes de foin et distributeur, qui cachent un bonus ;
  - barils, qui explosent en chaîne et blessent aussi le héros trop proche.

  Tirs, grenades, couteau et spéciales les abîment, puis ils tombent en morceaux.
- **Barricades-objectifs.** Dans chaque mission, une barricade surmontée d'une flèche clignotante bloque un assaut. L'assaut ne finit que quand elle tombe, et il finit dès qu'elle tombe.

## Des véhicules à piloter

Chaque mission gare un véhicule au milieu du parcours : la **2CV blindée** à Paris, le **fourgon du port** à Marseille, le **taureau mécanique** à Saint-Rémy, le **mini-mécha** à Tokyo. Il suffit de le toucher pour y monter. Au volant :

- **tir** fait tourner et tirer la mitrailleuse vers la visée ;
- **grenade** tire un obus du canon (leur nombre s'affiche à la place des grenades) ;
- **saut** fait sauter le véhicule ;
- **bas + saut** fait sortir le héros.

Le véhicule encaisse les coups à la place du pilote, sur sa propre jauge. Il écrase étals, caisses et barils, enfonce voitures et barricades, et aplatit les ennemis en leur retombant dessus. À zéro, le pilote est éjecté et le véhicule explose. Un véhicule vide peut être repris par n'importe quel joueur.

Contre les boss, la mitrailleuse tape moins fort que contre la rue et les obus sont comptés : le véhicule protège surtout, il n'expédie pas les mini-boss.

## Un jeu plus vivant

- **Poses des héros.** Un héros touché grimace un instant. En tombant, il est projeté en arrière et reste à terre jusqu'à son renfort ; il clignote juste avant de se relever. Il lève le poing quand un boss tombe et à la fin de la mission.
- **Effets d'écran.** Les grosses explosions secouent l'écran aussi de haut en bas. Une bouffée de poussière accompagne chaque atterrissage, de héros ou de véhicule. Les ennemis sursautent sous les balles, et les douilles rebondissent sur le relief.
- **Ennemis mieux animés.** Les quatre ennemis les plus présents (pillard au pied-de-biche, lanceur de bouteilles, pistolero de Tokyo, Arlésien) gagnent huit images. Ils courent et fuient en quatre images, finissent leur coup, respirent à l'arrêt et tombent en trois temps.
- **Bandeau d'annonces.** Retiré en 0.5.1, il revient : une ligne courte sous le score, qui s'efface après deux secondes et demie. Il annonce les assauts, les otages libérés, l'arrivée et la colère des boss, et les armes ramassées.
- **HUD plus lisible.** Le score, le chrono, les renforts et les numéros des joueurs sont cernés de sombre, lisibles même sur le ciel clair de Saint-Rémy. Au volant, la fiche du joueur montre l'icône et le nom du véhicule, sa jauge et ses obus.
- Les petites marches au pied des escaliers de Tokyo ont leur relief : on ne les traverse plus.

## Classement

Le relief, les décors, les véhicules, les nouveaux héros et l'équilibrage changent la simulation. Les parties de la 0.8.0 ne se rejouent donc plus à l'identique.

Chaque version a désormais **son propre classement**, et celui de la 0.9.0 repart de zéro. Les scores 0.8.0 ne sont ni rejoués, ni affichés, ni marqués comme invalides : ils sont simplement laissés de côté. Le bas du tableau rappelle la version du classement. Voir [CLASSEMENT.md](CLASSEMENT.md).

## Jeu en ligne

Tous les joueurs doivent avoir la version 0.9.0 : la version est contrôlée à la connexion.

## Pour qui fait des missions

Héros, décors destructibles et véhicules s'ajoutent sans toucher au code, comme le reste. Voir [ADDING_A_LEVEL.md](ADDING_A_LEVEL.md).

Nouveaux fichiers et champs :

- **Héros** (`Content/heroes`) :
  - autant de héros que voulu (jusqu'à 99), rangés par `order` ;
  - trois nouveaux types de spéciales : `summon` (un allié qui traverse l'écran), `dash` (une charge du héros) et `salvo` (des salves dans plusieurs directions) ;
  - des champs communs : `pose`, `voice`, `wound`, `bossDamage`, `trail` et `impact` ;
  - de nouveaux états d'animation : `special`, et `hurt`, `down`, `victory` désormais joués ;
  - un portrait qui reste carré dans une case qui ne l'est pas.

  Les replays nomment leurs héros par leur `id` (`heroIds`) : ajouter un héros n'en casse aucun.
- **Relief** (`map.terrain`) : pièces `platform`, `block`, `slope` et `pit`, toujours dessinées. Une petite pièce peut se passer de son propre dessin si elle tient dans celui d'une autre.
- **Ennemis** : bloc facultatif `terrain {climb, leap, follow}`.
- **Décors destructibles** :
  - la bibliothèque `Content/props` : PV par difficulté, états `intact`, `damaged` et `destroyed`, `blocks`, `wall`, `cover`, auvent, explosion en chaîne, éclats, récompenses, `crush` ;
  - leur placement dans la mission, `map.props`, avec `wave` pour en faire l'objectif d'un assaut.
- **Véhicules pilotables** :
  - la bibliothèque `Content/vehicles` : jauge, vitesse, saut, mitrailleuse en tourelle, canon et obus, pilote, montée et sortie, éperonnage, épave, icônes et sons ;
  - leur placement, `map.vehicles` : il n'y a plus d'emplacement réservé.
- **Armes** : `bossFactor`, qui règle ce qu'une arme fait aux boss. Les armes des véhicules l'utilisent.
- **Règles** (`Content/rules`) :
  - `poses` : durées des poses blessée, à terre et de victoire ;
  - `screen` : secousse, poussière, sursaut des ennemis, douilles et `banner` (le bandeau d'annonces) ;
  - `objective` : le signal au-dessus d'une barricade-objectif.

Tout est vérifié par le jeu au démarrage et par `tools/validate_content.py`.
