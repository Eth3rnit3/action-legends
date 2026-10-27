# Ajouter un niveau

Tout ce qui fait un niveau d’**Action Legends** — sa carte, ses ennemis et leurs animations, son boss, sa musique, ses textes — est décrit en JSON. Le moteur (`Assets/Scripts`) ne fournit que des **comportements génériques** : ennemi au contact, lanceur, tireur, boss volant, boss marcheur. Ce guide suffit pour ajouter une mission complète, avec un nouvel ennemi, ses animations, un boss et une carte, **sans toucher au C#**.

## 0. Où vont les fichiers

```
Assets/Resources/
  Levels/<mission>.json          une mission (schemaVersion 2), chargée automatiquement
  Content/heroes/<id>.json       les héros jouables (un à quatre)
  Content/enemies/<id>.json      la bibliothèque des types d’ennemis
  Content/bosses/<id>.json       la bibliothèque des boss
  Content/rules/<id>.json        règles de partie : grenades, bonus, flèche GO ("default" pour toutes les missions)
  Art/<mission>/...              images (PNG) de la mission
  Audio/Music/...                musiques (WAV, OGG ou MP3)
```

- Chaque fichier de `Content/` porte un `id` égal à son nom sans `.json`, en minuscules séparées par des tirets (`gunman-smg`). Les missions y font référence par cet identifiant : un ennemi ou un boss d’une mission peut servir dans une autre.
- Les chemins d’images et de sons sont des chemins Resources sans extension : `Assets/Resources/Art/ma-mission/quais.png` s’écrit `Art/ma-mission/quais`.
- Une entrée de la bibliothèque peut en **étendre** une autre avec `"extends": "<id>"` : le parent est lu d’abord, puis les champs de l’entrée remplacent les siens. Les objets se fusionnent champ par champ, les listes et les valeurs sont remplacées en entier. `le-direct.json` (l’hélicoptère de Marseille) ne contient ainsi que ses textes et sa nouvelle image :

```json
{
  "id": "le-direct",
  "extends": "macron-helicopter",
  "name": "LE DIRECT",
  "intro": "ÉDITION SPÉCIALE : L'HÉLICO DU DIRECT !",
  "rage": "LE DIRECT S'EMBALLE !",
  "defeat": "FIN DE L'ANTENNE !",
  "animations": {"sheet": {"texture": "Art/marseille/news-helicopter"}}
}
```

- Les nombres sont en **unités du monde** : un personnage mesure environ 2 unités, l’écran 20,6 × 11,6 unités, le sol est à `y = 0`. Les durées sont en secondes.
- Les valeurs **par difficulté** sont des listes de quatre nombres : DÉTENTE, ARCADE, DIFFICILE, LÉGENDE. Certaines valeurs livrées ont des décimales étranges (`0.45000002`, `2.3999999`) : elles reproduisent au bit près les anciens réglages du moteur, pour que les replays du classement restent valides. Une nouvelle entrée peut utiliser des valeurs rondes.
- Les textes affichés sont en français ; ceux du HUD en MAJUSCULES. La police du jeu web ne contient que les caractères jusqu’à U+00FF : pas de « → », d’emoji ni de guillemets typographiques dans les textes affichés.

## 1. La mission

Copier une mission de `Assets/Resources/Levels/`, la renommer avec son nouvel `id`. Le jeu charge toutes les missions valides du dossier : ni scène ni catalogue à modifier.

| Champ | Utilisation |
|---|---|
| `schemaVersion` | `2`. |
| `id` | Identifiant unique, identique au nom du fichier. |
| `order` | Facultatif, `1` par défaut. Position dans la campagne (0 à 999) ; à égalité, ordre des `id`. Les missions s’enchaînent dans cet ordre et la première est toujours ouverte. |
| `code` | Facultatif. 4 à 10 lettres majuscules ou chiffres, unique. Révélé quand la mission précédente est gagnée ; le saisir dans **CODE MISSION** ouvre la mission. |
| `title`, `location` | Nom et lieu affichés dans la fiche de mission (45 caractères au plus). |
| `intro`, `exitLabel`, `winTitle` | Annonce de départ, nom de la sortie (« PASSAGE LIBRE » quand les vagues sont finies), titre de victoire. |
| `music`, `bossMusic` | Musique de la mission et du combat final. `bossMusic` n’est demandé que s’il y a un boss. |
| `map` | La carte, voir plus bas. |
| `enemies` | Un à huit identifiants de `Content/enemies`. Les vagues désignent les ennemis par leur position dans cette liste (0, 1, 2…). |
| `extraEnemy` | Facultatif : `{"enemy": 1, "every": 4, "fromDifficulty": 1}` remplace un ennemi sur `every` par l’ennemi `enemy` de la liste à partir de la difficulté `fromDifficulty` (0 = DÉTENTE). `enemy` à `-1`, ou champ absent : rien. Les trois missions livrées utilisent la règle ci-dessus. |
| `length`, `exitX` | Longueur du parcours et position de sortie. Le boss apparaît quand l’équipe atteint `exitX - 18`. |
| `cameraMin`, `cameraMax` | Bornes horizontales de la caméra. |
| `waves` | Une à vingt vagues triées par `atX`. |
| `encounters` | Réservé (section 6) : laisser `[]` ou ne pas l’écrire. |
| `boss` | Le boss final, voir plus bas. |
| `rules` | Facultatif, `"default"` par défaut : identifiant de `Content/rules` (section 5 bis). Une mission peut ainsi donner plus ou moins de grenades, d’autres bonus ou une autre flèche GO. |

### Vagues

```json
{"atX": 18, "baseEnemies": 7, "enemiesPerPlayer": 3, "maxAlive": 10, "spawnInterval": 0.65, "enemyPattern": [0, 1, 2], "lock": true}
```

La vague démarre quand l’équipe arrive à 9 unités de `atX`. Elle compte `baseEnemies + joueurs × enemiesPerPlayer` ennemis (davantage selon la difficulté) ; `maxAlive` limite les ennemis présents à la fois, `spawnInterval` espace les apparitions. `enemyPattern` est répété dans l’ordre. Avec `lock` (vrai par défaut), l’écran se bloque, les ennemis entrent par les bords et la vague ne finit qu’à la chute du dernier, puis une flèche **GO** relance le défilement ; `"lock": false` laisse l’équipe courir et finit la vague quand tous ses ennemis sont apparus.

### Carte

```json
"map": {
  "backgrounds": ["Art/tokyo/shibuya", "Art/marseille/panier", "Art/canal-panorama"],
  "ground": "Art/marseille/sol",
  "terrain": [], "props": [], "hostages": [], "vehicles": []
}
```

- `backgrounds` : une à huit images de décor, dans l’ordre de gauche à droite. Chacune est affichée sur 13 unités de hauteur, la ligne de sol à 80 % de sa hauteur depuis le haut ; les portions sont accolées, et une portion large (plus de deux fois plus large que haute) se fond sur 1,5 unité dans la précédente. Le moteur fait défiler la fresque en parallaxe pour couvrir tout le parcours, quel que soit le format d’écran. Les images ne contiennent ni personnages ni interface ; un panorama découpé sans perte donne des raccords exacts (Paris).
- `ground` : image dont les 20 % du bas sont répétés le long de la rue, devant le décor.
- `terrain`, `props`, `hostages`, `vehicles` : réservés (section 6), à laisser vides.

Une carte peut réutiliser les décors d’autres missions.

### Boss de mission

```json
"boss": {"enabled": true, "id": "mecha-shogun", "health": 1.3}
```

| Champ | Utilisation |
|---|---|
| `enabled` | `false` : pas de boss, la mission est gagnée quand toutes les vagues sont vaincues et `exitX` atteint. |
| `id` | Un boss de `Content/bosses`. |
| `health` | Multiplicateur de ses points de vie pour cette mission, de `0.25` à `4`, `1` par défaut. |
| `name`, `intro`, `rage`, `defeat` | Facultatifs : remplacent les textes du boss pour cette mission (nom sur la barre de vie, 30 caractères ; annonces d’arrivée, de mi-vie et de chute, 80 caractères). |

Pour un boss plus différent (autres attaques, autre vitesse), créer une entrée de bibliothèque qui l’étend (section 4).

## 2. Les animations

Héros, ennemis et boss ont tous un bloc `animations` : une planche, des états nommés et des points d’ancrage.

```json
"animations": {
  "sheet": {"texture": "Art/tokyo/enemies", "columns": 4, "rows": 3, "cropLeft": 0.065, "cropRight": 0.025,
            "pivot": {"x": 0.4, "y": 0.045}, "cellHeight": 2.2777777},
  "layer": 30,
  "states": [
    {"name": "idle", "cells": [4]},
    {"name": "run", "cells": [5, 6], "perStride": 0.7},
    {"name": "attack", "cells": [7]}
  ],
  "anchors": [{"name": "muzzle", "cell": 7, "x": 1.18, "y": 1.39}]
}
```

**La planche (`sheet`).** Une image découpée en `columns` × `rows` cases de même taille. Les cases sont numérotées de gauche à droite puis de haut en bas : la première ligne d’une planche de quatre colonnes contient les cases 0 à 3, la deuxième 4 à 7. Le dessin est tourné vers la **droite** pour les héros et les ennemis, vers la **gauche** pour les boss ; le moteur le retourne quand le personnage change de sens.

| Champ | Utilisation |
|---|---|
| `texture` | Image de la planche, fond réellement transparent. |
| `columns`, `rows` | Nombre de cases (1 à 32 chacun). |
| `cropLeft`, `cropRight`, `cropTop`, `cropBottom` | Fractions de case retirées de chaque côté (0 par défaut), pour ne pas voir un débordement de la case voisine. |
| `pivot` | Point de la case posé sur la position du personnage, en fractions de la case depuis son coin inférieur gauche. Pour un personnage : le milieu des jambes, au niveau des pieds (`{"x": 0.4, "y": 0.045}` sur les planches livrées). Centre par défaut. |
| `cellHeight` | Hauteur d’une case en unités du monde. Ou `cellWidth`, sa largeur, quand `cellHeight` vaut 0 (l’hélicoptère : `"cellWidth": 8.8`). |
| `layer` | Ordre d’affichage (30 pour les personnages ; les décors sont en dessous de 0, les effets au-dessus de 40). |

**Les états (`states`).** Chaque état a un `name` unique et une liste `cells` de cases, d’une à 64 images : le nombre d’images par état est libre.

- `fps` : images par seconde, comptées depuis l’entrée dans l’état.
- `perStride` : à la place de `fps`, les images avancent avec la **foulée** du personnage, qui ne court que lorsqu’il se déplace : les pieds suivent le sol. La foulée d’un héros avance de `strideRate` par seconde à pleine vitesse ; celle d’un ennemi de `strideRate` par seconde de marche ; celle d’un boss marcheur d’une unité par pas (`walk.stepLength`). `0.7` sur les planches livrées.
- Sans `fps` ni `perStride`, l’état montre sa première image.
- `loop` : `true` par défaut ; `false` s’arrête sur la dernière image.

Les états lus par le moteur :

| Acteur | Requis | Facultatifs |
|---|---|---|
| Héros | `idle` (immobile au sol), `run` (court), `jump` (en l’air), `crouch` (accroupi), `aimUp` (vise en haut), `jumpDown` (en l’air, vise en bas), `knife` (coup de couteau, joué pendant `knife.pose`), `throw` (lancer de grenade, joué pendant `grenade.pose`) | `crouchWalk` (avance accroupi, sinon `crouch`), `crouchShoot` (tire accroupi, sinon `crouch`), `aimDiagonal` (vise en haut en avançant, sinon `aimUp`) |
| Ennemi | `idle`, `run`, `attack` (frappe, lancer, tir) | `aim` (visée d’un tireur, sinon `attack`), `hurt` (sous les balles, sinon l’image courante penche), `death` (joué pendant que le corps est projeté, sinon l’image courante) |
| Boss | `idle` ; `walk` pour un marcheur | ceux que nomment ses attaques (`state`, `aimState`, `recoverState`) |

Un état qui n’est pas dans ce tableau est accepté et ignoré : une planche peut déjà contenir les poses que lira un lot à venir (les héros livrés ont déjà `hurt`, `down` et `victory`). Pour en ajouter un, il suffit d’ajouter les images à la planche et l’état au JSON ; le moteur le lira quand la mécanique correspondante existera.

**Les ancrages (`anchors`).** Des points dessinés sur une case : `{"name": "muzzle", "cell": 7, "x": 1.18, "y": 1.39}`, en unités du monde depuis le pivot, dans le sens du dessin. Sans `cell` (ou `-1`), le point vaut pour toutes les cases ; un point propre à une case l’emporte. Les noms lus :

| Acteur | Ancrage | Où |
|---|---|---|
| Héros | `muzzle` | Bout du canon, **sur chaque case** des états d’où il tire : `idle`, `run`, `jump`, `crouch`, `crouchWalk`, `crouchShoot`, `aimUp`, `aimDiagonal`, `jumpDown`. Les tirs partent de l’arme dessinée ; pendant `knife` et `throw`, il ne tire pas. |
| Héros | `hand` | Main qui lâche la grenade, sur la **dernière** case de `throw`. |
| Ennemi tireur | `muzzle` | Sur la première case de `attack`. Les balles partent à l’horizontale. |
| Ennemi lanceur | `hand` | Main qui lâche le projectile, sur la première case de `attack`. |
| Boss | `smoke` | D’où sort la fumée sous la moitié de ses PV. |
| Boss | ceux de ses attaques | `muzzles`, `rack`, `foot`, `walk.foot` (section 4). |

`tools/art/pack_sheet.py` recale une planche générée (pieds sur une même ligne, jambes sur le pivot) et affiche les bouts de canon. Pour un héros, `pack_sheet.py hero tools/art/heroes/<héros>.json` assemble ses planches générées en une seule (une taille commune fixée par une image de référence de chaque planche), mesure les `muzzle` (vers l’avant, le haut, le bas ou en diagonale selon la case) et la `hand`, et imprime les ancrages JSON ; voir `docs/ART_LOT1A.md`. `tools/art/measure_muzzles.py` mesure ceux de l’ancienne planche `Art/heroes.png`.

## 3. Les ennemis

Un type d’ennemi = un comportement du moteur + ses chiffres + ses animations. Exemple, le pistolero de Tokyo (`Content/enemies/gunman-pistol.json`, animations abrégées) :

```json
{
  "id": "gunman-pistol",
  "behavior": "gunman",
  "health": 3, "speed": 4.6, "keepDistance": 6,
  "hitRadius": 0.5, "knockback": 3.5, "bonusScore": 0, "armored": false,
  "firstAttack": 1, "attackPose": 0.3, "strideRate": 11,
  "gun": {"range": 11, "aimTime": 0.42, "cooldown": 1.7, "burst": 1, "burstInterval": 0.11, "pellets": 1, "spread": 0, "spreadStep": 0,
          "speed": [11, 12.5, 14, 15.5], "life": 1.25, "pose": 0.25, "shake": 0, "damage": 1, "projectile": "bullet",
          "sound": "Audio/Sfx/tir-D-pistolet-sec", "heavyFlash": false},
  "animations": {"...": "voir section 2"}
}
```

| `behavior` | Comportement | Blocs lus |
|---|---|---|
| `melee` | Marche vers le héros le plus proche et frappe au contact. | `melee` |
| `thrower` | Garde ses distances et lance un projectile en cloche vers le héros ; frappe aussi au contact. | `melee`, `throw` |
| `gunman` | S’arrête à distance, vise (le héros peut passer derrière lui), puis tire à l’horizontale. | `gun` |

Champs communs :

| Champ | Utilisation |
|---|---|
| `health` | Points de vie en DÉTENTE ; le moteur les multiplie selon la difficulté (×1, ×1,8, ×2,5, ×3,2, arrondi au-dessus). |
| `speed` | Vitesse de marche (unités/s), multipliée selon la difficulté. |
| `keepDistance` | Distance au héros sous laquelle il s’arrête. |
| `hitRadius` | Rayon touché par les tirs autour de son torse (0,5 ; 0,68 pour un costaud). |
| `knockback` | Recul sous un tir (3,5 ; 1,2 pour un costaud). |
| `bonusScore` | Points en plus à sa chute. |
| `armored` | Impacts métalliques et explosion à sa chute. |
| `firstAttack` | Délai avant sa première attaque. |
| `attackPose` | Durée de la pose `attack` après une frappe ou un lancer. |
| `strideRate` | Vitesse de sa foulée (section 2). |

`melee` : `reach` (portée), `height` (un héros sauté plus haut n’est pas touché), `cooldown` (multiplié selon la difficulté), `damage`.

`throw` : `range` (portée de lancer), `cooldown`, `aimHeight` (hauteur visée au-dessus des pieds du héros), `speed` (par difficulté), `damage`, `projectile`.

`gun` : `range`, `aimTime` (raccourci de 8 % par cran de difficulté), `cooldown` après la rafale, `burst` balles par rafale espacées de `burstInterval`, `pellets` plombs par balle en éventail depuis `spread` degrés par pas de `spreadStep` (positif : vers le bas), `speed` (par difficulté), `life` (durée de vie d’une balle), `pose` (pose de tir après chaque balle), `shake` (secousse de l’écran), `damage`, `projectile`, `sound` (chaque tir), `heavyFlash` (grande flamme de bouche).

`projectile` : `bullet` (balle rouge), `molotov` (bouteille) ou `tracer` (traceur des héros).

### Créer un nouvel ennemi

1. Générer sa planche (section 7) et la placer dans `Art/<mission>/`.
2. Écrire `Content/enemies/<id>.json` : copier l’ennemi livré du même comportement et changer ses chiffres, ou l’étendre (`"extends": "looter-shield"`) et ne changer que ce qui diffère.
3. Décrire ses états et ses ancrages ; mesurer le canon ou la main avec `pack_sheet.py`.
4. L’ajouter à la liste `enemies` d’une mission et dans ses `enemyPattern`.

Exemple d’ennemi entièrement nouveau, un tireur qui utilise la planche des héros avec ses propres états (repris par `AgentScripts/VerifyDataDriven.cs`) :

```json
{
  "id": "test-sniper", "behavior": "gunman",
  "health": 5, "speed": 4.2, "keepDistance": 5, "hitRadius": 0.55, "knockback": 2, "firstAttack": 0.5, "attackPose": 0.3, "strideRate": 9, "bonusScore": 25,
  "gun": {"range": 10, "aimTime": 0.3, "cooldown": 1.2, "burst": 2, "burstInterval": 0.15, "pellets": 2, "spread": -2, "spreadStep": 4,
          "speed": [10, 11, 12, 13], "life": 1, "pose": 0.2, "damage": 1, "projectile": "bullet", "sound": "Audio/Sfx/tir-D-pistolet-sec"},
  "animations": {
    "sheet": {"texture": "Art/heroes", "columns": 4, "rows": 4, "cropLeft": 0.065, "cropRight": 0.025, "pivot": {"x": 0.4, "y": 0.045}, "cellHeight": 2.5},
    "states": [
      {"name": "idle", "cells": [12, 13], "fps": 4},
      {"name": "run", "cells": [13, 14, 15], "perStride": 0.5},
      {"name": "aim", "cells": [15]},
      {"name": "attack", "cells": [12]},
      {"name": "hurt", "cells": [14]},
      {"name": "death", "cells": [15, 12], "fps": 8, "loop": false}],
    "anchors": [{"name": "muzzle", "cell": 12, "x": 1.33, "y": 1.54}]}
}
```

## 4. Les boss

Un boss est un comportement — `flyer` (vole et attaque en mouvement) ou `walker` (marche vers les héros et s’arrête pour chaque attaque) — et une liste d’attaques jouées à tour de rôle. Son dessin est tourné vers la **gauche** ; ses ancrages et sa zone touchée sont retournés avec lui. Voir `Content/bosses/macron-helicopter.json` et `mecha-shogun.json` pour deux exemples complets.

| Champ | Utilisation |
|---|---|
| `behavior` | `flyer` ou `walker`. |
| `name`, `intro`, `rage`, `defeat` | Nom sur la barre de vie et annonces (une mission peut les remplacer). |
| `health` | PV par difficulté pour un joueur, `healthPerExtraPlayer` en plus par joueur (0,65 = +65 %), puis × `boss.health` de la mission. |
| `firstAttack` | Délai avant la première attaque. |
| `ragePace`, `rageDelay` | Sous la moitié de ses PV, le boss s’emballe : ses délais sont multipliés par `ragePace` (0,72) et sa prochaine attaque arrive au plus tard dans `rageDelay` s. |
| `flash` | Le dessin blanchit sous les tirs (marcheur livré : oui ; hélicoptère : non). |
| `shadow` | `{"x": largeur, "y": hauteur}` d’une ombre au sol, `0` pour aucune. |
| `smokeSize` | Taille de la fumée émise à l’ancrage `smoke` sous la moitié des PV. |
| `animations` | Section 2. |
| `rotor` | Volant : `x`, `y`, `width`, `height` d’un rotor animé dessiné par le moteur, `speed` de rotation ; `width` 0 : aucun. |
| `flight` | Volant : trajectoire `x = centre + sin(t) × min(sweep, demi-écran − sweepMargin)`, `y = altitude + sin(t × bobRate) × bob`, avec `t` qui avance à `speed` (par difficulté) ; `rageCatchUp` accélère la trajectoire quand il s’emballe. |
| `walk` | Marcheur : `speed` (par difficulté), `stopDistance` (s’arrête à cette distance du héros), `margin` (reste à cette distance des bords de l’arène), `stepLength` (un pas, avec secousse `stepShake` et poussière à l’ancrage `foot`), `entry` (entre à cette distance du bord droit). |
| `hitbox` | `count` cercles de rayon `radius`, le premier en (`x`, `y`) depuis le pivot, les suivants décalés de (`stepX`, `stepY`). |
| `death` | Chute : descend de `fall` u/s, tourne de `spin` °/s (dans le sens de son regard si `spinWithFacing`), explosions autour de `core` sur `spreadX` × `spreadY`. |
| `missile` | Image des bombes et missiles (ci-dessous). |
| `attacks` | Une à douze attaques. |

Chaque attaque a un `type`, une annonce `announce`, un repos `cooldown` après elle (multiplié selon la difficulté et, s’il s’emballe, par `ragePace`, sauf si `rageCooldown` donne la valeur à utiliser alors) et, facultatifs, les états d’animation `state`, `aimState`, `recoverState` et un `sound`.

| `type` | Déroulé | Champs |
|---|---|---|
| `gun` | Vise `aimTime` s (une mire de `sightWidth` marque le sol sous le héros, `aimHeight` au-dessus de ses pieds), puis tire pendant `duration` s une balle toutes les `interval` s depuis ses `muzzles` à tour de rôle, en balayant de ± `sweep` à la cadence `sweepRate`. | `aimTime`, `interval`, `speed` (par difficulté), `duration`, `sweep`, `sweepRate`, `aimHeight`, `sightWidth`, `muzzles` (ancrages), `damage` |
| `bombs` | Marque `count` points au sol autour du héros (+`rageExtra` s’il s’emballe), espacés de `spacing`, puis lance un missile vers chacun depuis l’ancrage `rack` après `delay` s (+`stagger` par missile) ; vol de `flight` s en cloche de hauteur `arc`, explosion de rayon `blastRadius` sous `blastHeight`. `duration` 0 : les bombes partent tout de suite et le boss continue ; sinon il prend la pose `state` pendant `duration` s et lance après `launch` s (`smoke` : fumée au lancement). Les missiles peuvent être abattus. | `count`, `delay`, `flight` (par difficulté), `rageExtra`, `spacing`, `stagger`, `arc`, `blastRadius`, `blastHeight`, `marker` (largeur de la marque), `rack`, `duration`, `launch`, `smoke`, `damage` |
| `stomp` | Lève le pied (`state`) pendant `windup` s, puis envoie une onde de choc de chaque côté depuis l’ancrage `foot` ; seuls les héros au sol (sous `waveHeight`) sont touchés à moins de `waveReach`. Il reprend son souffle `recover` s (`recoverState`) et recommence `repeat` fois (`rageRepeat` s’il s’emballe), relevant le pied en `repeatWindup` s. | `windup`, `repeatWindup`, `recover`, `repeat`, `rageRepeat`, `speed` (par difficulté, vitesse des ondes), `waveReach`, `waveHeight`, `shake`, `foot`, `damage` |

`missile` : un rectangle d’une image, `x`, `y`, `width`, `height` en fractions de l’image depuis son coin **inférieur** gauche, affiché sur `size` unités de large ; `outline`, facultatif, liste de points (fractions du rectangle) qui détoure le missile pour cacher ce qui l’entoure dans l’image ; `hitRadius` et `tipRadius` (rayons touchés au centre et aux extrémités, à `hitLength` du centre). Les trois boss livrés prennent le missile dessiné sur `Art/macron-helicopter.png` ; une image à part, réduite au missile, convient aussi (rectangle `0, 0, 1, 1`, sans `outline`).

Exemple de boss existant re-paramétré : il garde l’image, le rotor, la trajectoire et la zone touchée du Direct, et change ses PV, sa vitesse, son altitude et ses trois attaques (`AgentScripts/VerifyDataDriven.cs`) :

```json
{
  "id": "test-gunship", "extends": "le-direct", "name": "HÉLICO D'ESSAI", "intro": "ESSAI : HÉLICO EN APPROCHE !",
  "health": [400, 600, 800, 1000], "flight": {"speed": [2, 2, 2, 2], "altitude": 4},
  "attacks": [
    {"type": "bombs", "announce": "ESSAI : UNE BOMBE !", "count": [1, 1, 1, 1], "rageExtra": 0, "spacing": 2, "delay": [0.4, 0.4, 0.4, 0.4], "stagger": 0.1,
     "flight": [0.9, 0.9, 0.9, 0.9], "arc": 0.5, "blastRadius": 1.2, "blastHeight": 1, "damage": 1, "marker": 2.4, "rack": "rack", "cooldown": 0.8},
    {"type": "gun", "announce": "ESSAI : RAFALE !", "aimTime": [0.5, 0.5, 0.5, 0.5], "duration": 1, "interval": [0.2, 0.2, 0.2, 0.2], "speed": [12, 12, 12, 12],
     "sweep": 1, "sweepRate": 3, "aimHeight": 0.9, "sightWidth": 3, "muzzles": ["gun-b"], "sound": "Audio/Sfx/tir-C-blaster-cps1", "damage": 1, "cooldown": 0.7},
    {"type": "bombs", "announce": "ESSAI : DEUX BOMBES !", "count": [2, 2, 2, 2], "rageExtra": 1, "spacing": 3, "delay": [0.3, 0.3, 0.3, 0.3], "stagger": 0.2,
     "flight": [0.8, 0.8, 0.8, 0.8], "arc": 1, "blastRadius": 1.2, "blastHeight": 1, "damage": 1, "marker": 2.4, "rack": "rack", "cooldown": 1}]
}
```

Un nouveau boss volant reprend la mise en page de l’hélicoptère : vue de profil tournée vers la gauche, sans pales de rotor principal si le moteur dessine le rotor. Un nouveau marcheur suit celle du mécha : une case par pose (attente, deux pas, pied levé, tir, lance-missiles), même ligne de pieds ; `pack_sheet.py mecha` la met en place. Un comportement de boss autre que voler ou marcher demande une modification du moteur.

## 5. Les héros

Les quatre héros sont dans `Content/heroes/` ; le menu en affiche un à quatre, triés par `order` puis `id`. **Les replays et le jeu en ligne désignent un héros par sa position dans cette liste** : changer l’ordre ou les chiffres d’un héros change le jeu, et les scores publiés auparavant ne se rejouent plus.

| Champ | Utilisation |
|---|---|
| `name`, `shortName`, `role`, `specialName` | Nom de la fiche, nom court du HUD, rôle et nom du spécial. |
| `speed`, `jump` | Vitesse de course, vitesse verticale au saut. |
| `fireRate`, `damage`, `recoilShake` | Délai entre deux tirs, dégâts du fusil, secousse à chaque tir. |
| `strideRate`, `bob` | Vitesse de la foulée (section 2) et balancement vertical en courant. |
| `portraitCell`, `portraitFrame` | Case de la planche affichée dans la fiche du menu, et le carré de cette case qui y est montré : `{"x": 0.02, "y": 0, "size": 0.72}` en fractions de la case depuis son coin inférieur gauche (toute la case par défaut). |
| `hitbox` | Où le touchent les tirs ennemis, debout : un cercle de rayon `radius` à `y` au-dessus des pieds (`{"y": 1, "radius": 0.53}`). |
| `crouch` | Accroupi : vitesse `speed`, et le cercle touché `y`, `radius` (`{"speed": 3.6, "y": 0.55, "radius": 0.45}` : les balles tirées à hauteur de poitrine passent au-dessus). |
| `knife` | Avec le bouton de tir, un ennemi à moins de `reach` devant le héros (qui est à moins de `height` du sol) reçoit `damage` au couteau au lieu d’une balle ; `cooldown` avant le coup suivant, pose `knife` pendant `pose` s, son `sound`. |
| `special` | `type` `blast` : frappe tout ce qui est à moins de `radius` (`damage`), le boss à moins de `radius + bossReach` (`bossDamage`), rend invulnérable `invulnerable` s et fait bondir de `leap` (0 : non). `type` `volley` : `shots` tirs en éventail à `speed`, écartés de `spread`, `damage` chacun, explosifs si `explosive`. Recharge : `cooldown + difficulté × cooldownPerDifficulty` s. |
| `animations` | Les états de la section 2, un `muzzle` sur chaque case d’où il tire et une `hand` sur la dernière case de `throw`. |

## 5 bis. Les règles de partie

`Content/rules/default.json` fixe ce qui ne dépend ni d’un héros ni d’un ennemi ; une mission peut en nommer une autre avec `"rules"`, qui peut étendre `default` (`"extends": "default"`) et n’en changer qu’une partie.

| Champ | Utilisation |
|---|---|
| `grenades` | `start` et `max` : grenades au départ d’une vie et au plus, par difficulté ; `refillOnRespawn` (`true`) : un renfort repart avec au moins `start`. |
| `grenade` | Le lancer : vitesse `throwX` (vers l’avant), `throwY` (vers le haut), `gravity`. Au sol elle rebondit `bounces` fois (vitesse verticale × `bounce`, horizontale × `friction`), puis explose ; elle explose aussi au contact d’un ennemi ou du boss, ou au bout de `fuse` s. L’explosion retire `damage` aux ennemis à moins de `radius` (plus leur `hitRadius`), `bossDamage` au boss qu’elle touche, et détruit les missiles du boss pris dedans si `destroysBombs`. `cooldown` entre deux lancers, pose `throw` pendant `pose` s, `throwSound`. `sprite` (la grenade, dessinée `size` unités de haut) et `icon` (le compteur du HUD) : `{"texture", "columns", "rows", "cell"}`. |
| `bonuses` | Les bonus, désignés par leur position dans la liste : `{"id", "effect", "amount": [4 valeurs], "announce", "voice", "icon", "scale"}`. `effect` : `weapon` (`weapon` 1 mitrailleuse, 2 triple tir, 3 roquettes ; `amount` = munitions), `health` (points de vie), `turbo` (secondes), `score` (points), `grenades` (grenades, sans dépasser `grenades.max`). `voice` : chemin des trois prises `-a`, `-b`, `-c` de l’annonce, facultatif. `icon` : la case dessinée, `scale` sa taille (1,25 unité). |
| `supplies`, `drops` | Les bonus des caisses posées le long du parcours, puis ceux que laissent les ennemis vaincus, à tour de rôle (indices dans `bonuses`). |
| `go` | L’image de la flèche GO, `{"texture": "Art/go-sign"}`. |

```json
{"id": "beaucoup-de-grenades", "extends": "default", "grenades": {"start": [20, 20, 15, 10], "max": [30, 30, 20, 15]}, "supplies": [6, 0, 6, 3]}
```

## 6. Emplacements réservés aux lots suivants

Ces champs existent déjà dans le schéma pour que les prochaines mécaniques s’y branchent sans changer le format. Tant que le moteur ne les lit pas, ils doivent rester **vides ou absents** : une liste non vide est refusée, pour ne pas laisser croire qu’elle agit. Formes prévues :

| Champ | Élément prévu | Bibliothèque prévue |
|---|---|---|
| `map.terrain` | `{"type": "platform" \| "slope" \| "pit", "x": 40, "width": 6, "height": 2, "toHeight": 3}` : relief le long du parcours. | — |
| `map.props` | `{"id": "<prop>", "x": 52, "y": 0, "wave": 3}` : décor destructible, objectif de la vague `wave` si elle est donnée. | `Content/props/<id>.json` |
| `map.hostages` | `{"id": "<otage>", "x": 70, "y": 0, "reward": "<bonus ou arme>"}`. | `Content/hostages/<id>.json` |
| `map.vehicles` | `{"id": "<véhicule>", "x": 120}` : véhicule pilotable posé sur la carte. | `Content/vehicles/<id>.json` |
| `encounters` | `{"atX": 90, "wave": 2, "spawns": [{"enemy": "<id>", "x": 95, "delay": 0.5, "entry": "door" \| "parachute" \| "truck"}]}` : rencontre scénarisée. | — |

Les nouvelles bibliothèques suivront les règles de la section 0 (`id` = nom du fichier, `extends`, validation C# et Python).

## 7. Images et sons

- Toutes les images sont générées avec Codex CLI en donnant les planches existantes en référence, fond réellement transparent et grille exacte ; voir `docs/MISSION_TOKYO.md` pour des prompts complets (planche d’ennemis, mécha, décors, retrait du fond).
- Chaque PNG ou son a besoin d’un `.meta` : copier celui d’un fichier équivalent et remplacer son `guid` par 32 chiffres hexadécimaux nouveaux. Les fichiers JSON reçoivent le leur à la première ouverture de Unity ; il faut le proposer avec eux.
- Les musiques bouclent proprement ; `tools/audio/` synthétise bruitages et voix.

## 8. Vérifier et proposer

Sans Unity (Python 3.12, sans dépendance) :

```sh
python3 tools/validate_content.py
python3 -m unittest discover -s tools -p 'test_*.py'
```

Le validateur lit la bibliothèque et les missions comme le jeu : `extends`, références d’ennemis et de boss, images et sons présents, états d’animation requis par chaque comportement, ancrages, bornes de tous les nombres, emplacements réservés vides. Il refuse aussi qu’un nom propre à une mission apparaisse dans `Assets/Scripts`. Le jeu refait les mêmes contrôles au démarrage et refuse une mission invalide.

Dans Unity, jouer la mission avec un puis plusieurs joueurs dans plusieurs difficultés : vagues, sortie, raccords du décor, animations et boss. Pour l’ouvrir sans jouer les précédentes, saisir son code dans **CODE MISSION**. `AgentScripts/VerifyDataDriven.cs` montre comment charger une mission depuis du JSON et la faire finir par un bot.

Proposer les JSON, les images et leurs `.meta` dans une pull request avec des captures. Une fois la proposition fusionnée, la compilation web suivante inclut la mission.
