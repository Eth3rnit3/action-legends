# Ajouter un niveau

Tout ce qui fait un niveau d’**Action Legends** — sa carte, ses ennemis et leurs animations, son boss, sa musique, ses textes — est décrit en JSON. Le moteur (`Assets/Scripts`) ne fournit que des **comportements génériques** : ennemi au contact, lanceur, tireur, boss volant, boss marcheur. Ce guide suffit pour ajouter une mission complète, avec un nouvel ennemi, ses animations, un boss et une carte, **sans toucher au C#**.

## 0. Où vont les fichiers

```
Assets/Resources/
  Levels/<mission>.json          une mission (schemaVersion 2), chargée automatiquement
  Content/weapons/<id>.json      les armes des héros : fusils de base et armes à lettres (H, S, F, R, L…)
  Content/heroes/<id>.json       les héros jouables (autant que voulu : chaque joueur les fait défiler sur sa carte du menu)
  Content/enemies/<id>.json      la bibliothèque des types d’ennemis
  Content/bosses/<id>.json       la bibliothèque des boss
  Content/rules/<id>.json        règles de partie : grenades, bonus, caisses d’armes, décompte de fin, flèche GO
  Content/hostages/<id>.json     les otages (POW) que les missions placent sur leur carte
  Content/difficulties/<id>.json les difficultés du menu (DÉTENTE, ARCADE, DIFFICILE, LÉGENDE, SLUG)
  Content/carriers/<id>.json     les véhicules qui déposent les ennemis d’une rencontre (camionnette, monospace)
  Art/<mission>/...              images (PNG) de la mission
  Audio/Music/...                musiques (WAV, OGG ou MP3)
```

- Chaque fichier de `Content/` porte un `id` égal à son nom sans `.json`, en minuscules séparées par des tirets (`gunman-smg`). Les missions y font référence par cet identifiant : un ennemi ou un boss d’une mission peut servir dans une autre.
- Les chemins d’images et de sons sont des chemins Resources sans extension : `Assets/Resources/Art/ma-mission/quais.png` s’écrit `Art/ma-mission/quais`.
- **Tout ce qui est sous `Assets/Resources` part dans l’export web**, chargé ou non. Planches sources, images intermédiaires et variantes non retenues vont dans `Assets/ArtSources/<mission>/` (avec leur `.meta`) ; `tools/validate_content.py` refuse une image de `Resources/Art` qu’aucun JSON ni script ne nomme.
- **Réglages d’import d’une nouvelle image** (inspecteur Unity, ou `.meta` copié d’une image équivalente avec un nouveau `guid`) : *Read/Write* et *Generate Mip Maps* décochés (le jeu ne lit pas les pixels et n’affiche pas ses images en réduction lointaine), filtre *Bilinear*, *Max Size* 4096 pour garder la taille native, *Compression* *Normal Quality* sans *Crunch* : DXT5 pour une image transparente, DXT1 pour un panorama, soit quatre à six fois moins que l’image brute. La compression par blocs exige une largeur et une hauteur **multiples de 4** ; sinon Unity garde l’image non compressée. Compléter alors la marge transparente (ou dupliquer la dernière colonne d’un panorama) avant l’import ; une planche garde des cases de taille entière. *Crunch* a été essayé et refusé à la 0.8.0 : il bave les motifs fins des personnages (camouflage, jean).
- **Musiques** : WAV en qualité 0,6 (l’export web les encode en AAC, environ 150 kb/s), *Preload Audio Data* décoché ; bruitages et voix gardent les réglages par défaut.
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
- Les valeurs **par difficulté** sont des listes de quatre nombres, les colonnes DÉTENTE, ARCADE, DIFFICILE, LÉGENDE. Une difficulté du menu (section 5 quinquies) dit quelle colonne elle lit : SLUG lit celle d’ARCADE, et une nouvelle difficulté n’allonge aucune liste. Certaines valeurs livrées ont des décimales étranges (`0.45000002`, `2.3999999`) : elles reproduisent au bit près les anciens réglages du moteur, pour que les replays du classement restent valides. Une nouvelle entrée peut utiliser des valeurs rondes.
- Les textes affichés sont en français ; ceux du HUD en MAJUSCULES. La police du jeu web ne contient que les caractères jusqu’à U+00FF : pas de « → », d’emoji ni de guillemets typographiques dans les textes affichés.
- Les **annonces** (`intro` d’une mission, `announce` d’une rencontre, d’une arme ou d’un bonus, `intro`, `rage`, `defeat` et `announce` des attaques d’un boss) sont validées et gardées en données, mais le HUD n’affiche plus de bandeau d’annonce depuis la version 0.5.1 (HUD épuré) : seules les voix associées (`voice`) s’entendent. Les écrire quand même : elles servent de référence et reviendraient si un bandeau est rétabli.

## 1. La mission

Copier une mission de `Assets/Resources/Levels/`, la renommer avec son nouvel `id`. Le jeu charge toutes les missions valides du dossier : ni scène ni catalogue à modifier.

| Champ | Utilisation |
|---|---|
| `schemaVersion` | `2`. |
| `id` | Identifiant unique, identique au nom du fichier. |
| `order` | Facultatif, `1` par défaut. Position dans la campagne (0 à 999) ; à égalité, ordre des `id`. Les missions s’enchaînent dans cet ordre et la première est toujours ouverte. Livrées : Paris (1), Marseille (2), Saint-Rémy (3), Tokyo (4). Une mission est ouverte dès qu’elle-même ou une mission placée après elle est débloquée : insérer une mission ne ferme rien (les déblocages, les replays et le classement désignent une mission par son `id`), et une mission insérée avant une mission déjà atteinte s’ouvre d’elle-même (Saint-Rémy pour qui avait débloqué Tokyo en 0.7.0). |
| `code` | Facultatif. 4 à 10 lettres majuscules ou chiffres, unique. Révélé quand la mission précédente est gagnée ; le saisir dans **CODE MISSION** ouvre la mission, et donc celles qui la précèdent. |
| `title`, `location` | Nom et lieu affichés dans la fiche de mission (45 caractères au plus). |
| `logo` | Facultatif : image (`Art/…`) affichée dans la fiche de mission du menu à la place du titre, le lieu en dessous ; un logo large d’environ 3 × 1 s’y lit le mieux. Saint-Rémy : `Art/saint-remy/mission-logo`. |
| `intro`, `exitLabel`, `winTitle` | Annonce de départ, nom de la sortie (« PASSAGE LIBRE » quand les vagues sont finies), titre de victoire. |
| `music`, `bossMusic` | Musique de la mission et du combat final. `bossMusic` n’est demandé que s’il y a un boss. |
| `map` | La carte, voir plus bas. |
| `enemies` | Un à huit identifiants de `Content/enemies`. Les vagues désignent les ennemis par leur position dans cette liste (0, 1, 2…). |
| `extraEnemy` | Facultatif : `{"enemy": 1, "every": 4, "fromDifficulty": 1}` remplace un ennemi sur `every` par l’ennemi `enemy` de la liste à partir de la difficulté `fromDifficulty` (0 = DÉTENTE). `enemy` à `-1`, ou champ absent : rien. Les quatre missions livrées utilisent cette règle (Saint-Rémy avec `"enemy": 2`, l’Arlésien au fouet). |
| `length`, `exitX` | Longueur du parcours et position de sortie. Le boss apparaît quand l’équipe atteint `exitX - 18`. |
| `cameraMin`, `cameraMax` | Bornes horizontales de la caméra. |
| `waves` | Une à vingt vagues triées par `atX`. |
| `encounters` | Facultatif : les rencontres mises en scène (ennemis placés, entrées, mini-boss), voir plus bas. |
| `boss` | Le boss final, voir plus bas. |
| `rules` | Facultatif, `"default"` par défaut : identifiant de `Content/rules` (section 5 bis). Une mission peut ainsi donner plus ou moins de grenades, d’autres bonus, moins de caisses ou une autre flèche GO. Les quatre missions livrées jouent `pow` : `default` avec deux fois moins de caisses sur le parcours, puisque les otages donnent aussi des bonus. |
| `parTime` | Facultatif, `0` par défaut : temps de référence en secondes. Au décompte de fin de mission, chaque seconde gagnée sur lui rapporte `tally.perSecond` points (règles, section 5 bis). Paris 300, Marseille 240, Saint-Rémy 280, Tokyo 220. |
| `weapons` | Facultatif : les armes que donnent les caisses d’armes de la mission et leurs proportions, `[{"weapon": "laser", "weight": 3}, {"weapon": "heavy-machinegun", "weight": 2}]`, à la place de la table des règles (section 5 ter). Marseille, Saint-Rémy et Tokyo ont la leur. |

### Vagues

```json
{"atX": 18, "baseEnemies": 7, "enemiesPerPlayer": 3, "maxAlive": 10, "spawnInterval": 0.65, "enemyPattern": [0, 1, 2], "lock": true}
```

La vague démarre quand l’équipe arrive à 9 unités de `atX`. `baseEnemies` peut valoir 0 : la vague n’est alors faite que de ses rencontres. Elle compte `baseEnemies + joueurs × enemiesPerPlayer` ennemis (davantage selon la difficulté) ; `maxAlive` limite les ennemis présents à la fois, `spawnInterval` espace les apparitions. `enemyPattern` est répété dans l’ordre. Avec `lock` (vrai par défaut), l’écran se bloque, les ennemis entrent par les bords et la vague ne finit qu’à la chute du dernier, puis une flèche **GO** relance le défilement ; `"lock": false` laisse l’équipe courir et finit la vague quand tous ses ennemis sont apparus. Dans les deux cas, la vague attend aussi les ennemis placés de ses rencontres (`"wave"`, ci-dessous).

### Carte

Exemple (positions à adapter aux vagues de la mission : un otage de vague se place près du `atX` de sa vague, voir « Otages ») :

```json
"map": {
  "backgrounds": ["Art/tokyo/shibuya", "Art/marseille/panier", "Art/canal-panorama"],
  "ground": "Art/marseille/panier",
  "horizon": 0.1, "groundBand": 0.12, "groundTop": -0.9, "groundFade": 0.25,
  "terrain": [], "props": [], "vehicles": [],
  "decor": [
    {"sprite": {"texture": "Art/saint-remy/accessoires", "columns": 4, "rows": 3, "cell": 6}, "x": 79, "y": -0.4, "height": 1.85},
    {"sprite": {"texture": "Art/saint-remy/accessoires", "columns": 4, "rows": 3, "cell": 11}, "x": 178, "y": -0.74, "height": 4.4, "flip": true}
  ],
  "hostages": [
    {"id": "baker", "x": 34, "y": 0},
    {"id": "baker", "x": 49, "y": 0, "wave": 1, "reward": "weapon"},
    {"id": "civilian", "x": 238, "y": 0, "wave": 6, "weapon": "heavy-machinegun"}
  ]
}
```

- `backgrounds` : une à huit images de décor, dans l’ordre de gauche à droite. Chacune est affichée sur 13 unités de hauteur, la ligne de marche (`y = 0`, où les pieds se posent) à `horizon` de sa hauteur depuis le bas ; les portions sont accolées, et une portion large (plus de deux fois plus large que haute) se fond sur 1,5 unité dans la précédente. Le moteur fait défiler la fresque en parallaxe pour couvrir tout le parcours, quel que soit le format d’écran. Les images ne contiennent ni personnages ni interface ; un panorama découpé sans perte donne des raccords exacts (Paris).
- `ground` : image dont le bas (`groundBand`) est répété le long de la rue, devant le décor ; souvent l’un des panoramas lui-même, dont le bas ne montre que le sol.
- Cadrage du sol, facultatif et purement visuel (la simulation ne le lit pas) :

| Champ | Défaut | Rôle |
|---|---|---|
| `horizon` | `0.2` | Hauteur de la ligne de marche dans chaque panorama, en fraction de sa hauteur depuis le bas. Mesurer où le sol peint (pavé, chaussée, quai) commence et finit, et placer la ligne **au milieu** de cette bande : à `0.2`, le bord du fond du trottoir, les personnages paraissent collés aux façades. |
| `groundBand` | `0.2` | Part du bas de l’image `ground` répétée devant les acteurs (13 unités × `groundBand` de haut). |
| `groundTop` | `0` | Hauteur du bord haut de cette bande. La placer **sous** les pieds pour qu’elle ne cache pas le sol peint du panorama, mais au-dessus du bas du panorama (`-13 × horizon`) pour qu’il n’y ait pas de trou. |
| `groundFade` | `0.22` | Part de la bande fondue vers le haut ; le fondu doit finir avant le bas du panorama. |

Les quatre missions livrées utilisent `horizon` 0,1, `groundBand` 0,12, `groundTop` -0,9 et `groundFade` 0,25 : les pieds tombent au milieu du pavé ou de la chaussée peints, et la bande (le bas d’un panorama de la mission à Marseille et Saint-Rémy, la chaussée `sol` à Paris et Tokyo) prolonge ce sol jusqu’au bas de l’écran. L’écran va de `y = -2,2` à `y = 9,4`.
- `hostages` : les otages de la mission (voir plus bas).
- `decor` : facultatif, jusqu’à 120 accessoires dessinés dans la rue, devant le décor : un tonneau, une fontaine, une terrasse de café. Ils ne font que se voir (rien ne les touche, ils ne gênent personne). Chacun est une case d’une planche (`sprite`, comme une icône : `{"texture", "columns", "rows", "cell"}`) affichée sur `height` unités de haut (la hauteur de la case), le milieu du bas de la case posé en (`x`, `y`) ; un `y` négatif enfonce dans la rue un dessin qui n’atteint pas le bas de sa case (le tonneau de Saint-Rémy finit à 17 % du bas de la sienne : `y` = −0,17 × `height`). `flip` le retourne, `front` le dessine devant les personnages (derrière par défaut). Un ennemi peut en sortir : une entrée `door` dont `fromX` est celui de l’accessoire et `"opening": "none"`.
- `terrain`, `props`, `vehicles` : réservés (section 6), à laisser vides.

Une carte peut réutiliser les décors d’autres missions.

### Otages

Chaque élément de `map.hostages` attache un otage de `Content/hostages` (section 5 quater) dans la rue :

| Champ | Utilisation |
|---|---|
| `id` | Un otage de la bibliothèque. |
| `x`, `y` | Position (de 1 à `length - 1`), `y` au-dessus de la rue (0 en général). |
| `wave` | Facultatif, `-1` par défaut. L’otage n’apparaît qu’au départ de cette vague (indice dans `waves`), dans son écran bloqué : il doit être placé entre `atX - 14` et `atX + 2`. |
| `reward` | Facultatif : l’`id` d’un bonus des règles de la mission (`"grenades"`, `"health"`, `"score"`, `"weapon"` pour une arme tirée de la table). Sans `reward` ni `weapon`, l’otage tire sa récompense de sa propre table. |
| `weapon` | Facultatif : une arme à munitions de `Content/weapons`, donnée dans sa caisse. |

Un otage se libère quand un héros tire sur ses liens ou le touche (selon son `release`) ; seul un otage visible à l’écran peut être libéré. Libéré, il salue, lâche sa récompense devant lui (un bonus à ramasser, comme une caisse) puis s’enfuit hors de l’écran : dans une vague bloquée, il donne toujours sa récompense dans l’écran avant de partir. Le héros qui l’a libéré le compte (HUD, sous sa fiche) ; il perd son compte s’il tombe. À la victoire, le **décompte de fin de mission** paie chaque otage encore compté et l’avance sur `parTime`. Les missions livrées placent 8 ou 9 otages, dont quatre ou cinq dans des vagues.

### Rencontres

Une rencontre place des ennemis précis, qui font leur entrée là où l’équipe les voit arriver. Elle démarre quand l’équipe atteint `atX`, ou avec une vague (`"wave"`), ou quand cette vague est vaincue (`"on": "clear"`). Les ennemis d’une rencontre de vague s’ajoutent à sa horde : la vague ne finit qu’à leur chute. Exemple (les `x` sont à adapter aux vagues de la mission, voir les règles de placement plus bas) :

```json
"encounters": [
  {"wave": 0, "spawns": [
    {"enemy": "looter-thrower", "entry": "door", "x": 12, "fromX": 13},
    {"enemy": "looter-thrower", "entry": "window", "x": 45, "y": 2, "fromX": 45, "fromY": 2, "delay": 1},
    {"enemy": "looter-rusher", "entry": "walk", "x": 10, "side": "right", "delay": 1.2}]},
  {"wave": 2, "truck": {"id": "rusty-van", "side": "right", "x": 97},
   "spawns": [{"enemy": "looter-rusher", "entry": "truck", "x": 96}, {"enemy": "looter-thrower", "entry": "truck", "x": 95}]},
  {"atX": 70, "spawns": [{"enemy": "looter-rusher", "entry": "none", "x": 85, "unaware": true, "facing": -1}]},
  {"atX": 150, "lock": true, "announce": "EMBUSCADE !", "spawns": [{"enemy": "looter-rusher", "entry": "drop", "x": 156}]},
  {"wave": 8, "on": "clear", "lock": true, "spawns": [{"enemy": "looter-rusher", "entry": "walk", "x": 398, "side": "left"}]},
  {"atX": 215, "boss": {"id": "armored-van"}}
]
```

| Champ | Utilisation |
|---|---|
| `atX` | Rencontre sans vague : elle démarre quand l’équipe atteint `atX`. |
| `wave`, `on` | Rencontre d’une vague (indice dans `waves`) : `"start"` (défaut) au départ de la vague, `"clear"` quand elle est vaincue. |
| `lock` | L’écran se bloque jusqu’à la chute de ses ennemis (une vague bloquée le tient déjà). |
| `announce` | Facultatif : annonce du départ (40 caractères ; non affichée par le HUD actuel, voir section 0). |
| `truck` | Un véhicule de `Content/carriers` (section 5 sexies) qui arrive du côté `side` (`right` par défaut) et s’arrête le centre en `x` ; les apparitions `"entry": "truck"` en descendent. |
| `spawns` | Une à seize apparitions (ci-dessous). |
| `boss` | Un boss de mi-parcours (ci-dessous) ; il se déclenche seul, sans `wave`, `spawns`, `lock` ni `truck`. |

Chaque apparition :

| Champ | Utilisation |
|---|---|
| `enemy` | Un ennemi de la bibliothèque (pas forcément de la liste `enemies` de la mission). |
| `x`, `y` | Sa place. `y` au-dessus de la rue : il y reste (rebord, fenêtre, toit) et tire vers le bas ; un ennemi de corps à corps reste dans la rue. |
| `delay` | Secondes après le départ de la rencontre (pour un camion : après son arrêt, en plus de son intervalle). |
| `entry` | `walk` : entre à pied par le bord `side` de l’écran (sinon le bord opposé à l’équipe) ; `door`, `window` : sort en fondu d’une ouverture des règles dessinée en (`fromX`, `fromY`), puis saute dans la rue s’il est plus bas, ou reste à la fenêtre si `y` = `fromY` ; `parachute` : descend du haut de l’écran sous un parachute ; `drop` : tombe d’au-dessus de l’écran ; `truck` : descend du véhicule de la rencontre ; `none` : déjà là, hors de l’écran devant l’équipe (rencontre `atX` sans `lock` seulement, entre `atX + 14` et `atX + 40`) ; un ennemi inattentif hors de l’écran qu’une vague bloque ne la retient pas, et les tirs le traversent tant qu’il n’est pas à l’écran. |
| `opening` | Pour `door` et `window` : l’ouverture dessinée, un `id` des `stage.openings` des règles (`door`, `window`, `shutter`, `manhole` dans `default`) ; par défaut celle qui porte le nom de l’entrée ; `"none"` : aucune. |
| `unaware`, `facing` | Inattentif au départ (section 3), regardant à gauche (`-1`) ou à droite (`1`) ; `0` : vers l’équipe. |

Une fenêtre à `fromY` 2 (les missions livrées) reste à portée d’un héros qui saute, vise en haut ou lance une grenade ; plus haut, l’ennemi n’est atteint qu’en visant en haut. Pour que l’équipe voie chaque entrée, `x` (et `fromX`, et le `x` d’un camion) doit être dans l’écran de la rencontre : de `atX - 15` à `atX + 2,5` pour une rencontre de vague, de `atX - 7` à `atX + 12` pour une rencontre `atX`. Le validateur le vérifie.

Les missions livrées : Paris 10 vagues et 16 rencontres (portes cochères, fenêtres, camionnettes de pillards, une pause de pillards, des embuscades, le fourgon blindé), Marseille 8 vagues et 12 rencontres (parachutistes au-dessus du Vieux-Port, fenêtres du Panier, le hors-bord sur l’eau du Vieux-Port), Saint-Rémy 9 vagues et 15 rencontres (Arlésiens qui sortent de derrière les tonneaux, les oliviers et les colonnes, fenêtres à volets, camionnette, le taureau sur la route), Tokyo 8 vagues et 11 rencontres (rideaux métalliques, bouches d’égout, monospace noir, parachutistes des toits, le drone de combat).

### Boss de mi-parcours

```json
{"atX": 30, "boss": {"id": "speedboat", "health": 1, "lane": {"left": -7, "right": -1, "y": 1.9}}}
```

`lane` (facultatif, boss volant seulement, mi-parcours ou final) le garde dans une bande de l’arène : il va et vient de `left` à `right` (en unités depuis le centre de l’arène, de -10 à 10, au moins 1 d’écart) sur la ligne `y` (sa ligne de flottaison ou de vol, de 0 à 12), au lieu de balayer tout l’écran à l’`altitude` de sa fiche. Le hors-bord de Marseille navigue ainsi sur l’eau visible derrière le quai : mesurer la zone sur une capture de l’arène (le panorama ne bouge pas pendant le combat) et vérifier que la zone touchée (`hitbox`) descend jusqu’aux tirs droits des héros (vers `y` = 1,5).

Un boss de la bibliothèque (section 4), avec les mêmes champs que le boss final (`health`, `name`, `intro`, `rage`, `defeat`). Quand l’équipe atteint `atX`, l’écran se bloque autour d’elle, la barre de vie s’affiche et les vagues attendent ; à sa chute (il rapporte son `score`), le défilement repart, la flèche GO s’affiche et les renforts qu’il a lâchés s’enfuient. Il se place au moins 30 unités avant la sortie et hors de l’écran d’une vague (pas entre `atX - 10` et `atX + 5` d’une vague) ; trois au plus par mission. Les bonus ne se ramassent pas pendant un combat de boss : ne pas placer d’otage dans son arène. Paris : `armored-van` (x 215), Marseille : `speedboat` (x 62, là où le décor montre le Vieux-Port), Saint-Rémy : `taureau` (x 196, sur la route qui sort de la ville), Tokyo : `combat-drone` (x 176).

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
  "sheet": {"texture": "Art/enemies/gunman-pistol", "columns": 4, "rows": 4, "cropLeft": 0.065, "cropRight": 0.025,
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
| Héros | `idle` (immobile au sol), `run` (court), `jump` (en l’air), `crouch` (accroupi), `aimUp` (vise en haut), `jumpDown` (en l’air, vise en bas), `knife` (coup de couteau, joué pendant `knife.pose`), `throw` (lancer de grenade, joué pendant `grenade.pose`) | `crouchWalk` (avance accroupi, sinon `crouch`), `crouchShoot` (tire accroupi, sinon `crouch`), `aimDiagonal` (vise en haut en avançant, sinon `aimUp`), `special` (montré pendant la `pose` de sa spéciale ; sinon la pose courante) |
| Ennemi | `idle`, `run`, `attack` (frappe, lancer, tir) | `aim` (visée d’un tireur, sinon `attack`), `hurt` (sous les balles, sinon l’image courante penche), `unaware` (inattentif, sinon `idle`), `surprised` (sursaut, sinon `idle`), `flee` (fuite, sinon `run`), `fall` (sous un parachute, sinon `idle`), `throw` (lancer de grenade, sinon `attack`), `death` (mort par balle), `deathExplosion`, `deathFire`, `deathKnife` (morts par explosion, feu, couteau, sinon `death`) ; sans aucun état de mort, l’image courante est projetée |
| Boss | `idle` ; `walk` pour un marcheur | ceux que nomment ses attaques (`state`, `aimState`, `recoverState`) ; `wreck` (montré à sa chute) |
| Véhicule de dépôt | `drive`, `open` | — |

Un état qui n’est pas dans ce tableau est accepté et ignoré : une planche peut déjà contenir les poses que lira un lot à venir (les héros livrés ont déjà `hurt`, `down` et `victory`). Pour en ajouter un, il suffit d’ajouter les images à la planche et l’état au JSON ; le moteur le lira quand la mécanique correspondante existera.

**Les ancrages (`anchors`).** Des points dessinés sur une case : `{"name": "muzzle", "cell": 7, "x": 1.18, "y": 1.39}`, en unités du monde depuis le pivot, dans le sens du dessin. Sans `cell` (ou `-1`), le point vaut pour toutes les cases ; un point propre à une case l’emporte. Les noms lus :

| Acteur | Ancrage | Où |
|---|---|---|
| Héros | `muzzle` | Bout du canon, **sur chaque case** des états d’où il tire : `idle`, `run`, `jump`, `crouch`, `crouchWalk`, `crouchShoot`, `aimUp`, `aimDiagonal`, `jumpDown`. Les tirs partent de l’arme dessinée ; pendant `knife` et `throw`, il ne tire pas. |
| Héros | `hand` | Main qui lâche la grenade, sur la **dernière** case de `throw`. |
| Ennemi tireur | `muzzle` | Sur la première case de `attack`. Les balles partent à l’horizontale. |
| Ennemi lanceur | `hand` | Main qui lâche le projectile, sur la première case de `attack`. |
| Ennemi à grenades | `hand` | Main qui lâche la grenade, sur la première case de `throw` (sinon celle d’`attack`, sinon 0,4 ; 1,9). |
| Véhicule de dépôt | `door` | Où descendent ses passagers, sur la case `open`. |
| Boss | `smoke` | D’où sort la fumée sous la moitié de ses PV. |
| Boss | ceux de ses attaques | `muzzles`, `rack`, `foot`, `walk.foot` (section 4). |

Les neuf ennemis livrés ont chacun leur planche, `Art/enemies/<id>.png` (4 × 4 cases) : la première ligne reprend au pixel près leurs quatre anciennes cases (pour les Arlésiens, leur ligne de `Assets/ArtSources/saint-remy/enemies.png`), les trois suivantes ajoutent inattentif, sursaut, touché ou lancer, fuite et les morts (`docs/ART_LOT2B.md`, `docs/MISSION_SAINT_REMY.md`) ; `pack_sheet.py hero tools/art/enemies/<id>.json` la refait. Les planches des Arlésiens ont des cases de 512 px : `pivot` (0,4 ; 0,0527344), `cellHeight` 2,88 (3,2 pour l’Arlésien au fouet, dessiné plus petit).

`tools/art/pack_sheet.py` recale une planche générée (pieds sur une même ligne, jambes sur le pivot) et affiche les bouts de canon. Pour un héros, `pack_sheet.py hero tools/art/heroes/<héros>.json` assemble ses planches générées en une seule (une taille commune fixée par une image de référence de chaque planche), mesure les `muzzle` (vers l’avant `right`, l’arrière `left`, le haut `up`, le bas `down`, en bas devant `downRight` ou en diagonale `diagonal` selon la case ; `right:2.0` ignore ce qui est dessiné à plus de 2 u au-dessus des pieds, comme l’étendard de Jeanne au-dessus de son épée) et la `hand`, et imprime les ancrages JSON ; voir `docs/ART_LOT1A.md` et `docs/ART_LOT3H.md` (une planche de 12 poses fournie, complétée par 12 poses générées). `tools/art/measure_muzzles.py` mesure ceux de l’ancienne planche `Assets/ArtSources/heroes.png`.

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

`melee` : `reach` (portée), `height` (un héros sauté plus haut n’est pas touché), `cooldown` (multiplié selon la difficulté), `damage`, et `sound`, facultatif, joué à chaque coup. Un ennemi au contact frappe dès qu’un héros est à moins de `reach` et s’arrête à `keepDistance` : un `keepDistance` proche de `reach` en fait un ennemi qui frappe à distance (l’Arlésien au fouet de Saint-Rémy : `keepDistance` 1,8, `reach` 2,2, le claquement du fouet en `sound`).

`throw` : `range` (portée de lancer), `cooldown`, `aimHeight` (hauteur visée au-dessus des pieds du héros), `speed` (par difficulté), `damage`, `projectile`.

`gun` : `range`, `aimTime` (raccourci de 8 % par cran de difficulté), `cooldown` après la rafale, `burst` balles par rafale espacées de `burstInterval`, `pellets` plombs par balle en éventail depuis `spread` degrés par pas de `spreadStep` (positif : vers le bas), `speed` (par difficulté), `life` (durée de vie d’une balle), `pose` (pose de tir après chaque balle), `shake` (secousse de l’écran), `damage`, `projectile`, `sound` (chaque tir), `heavyFlash` (grande flamme de bouche).

`projectile` : `bullet` (balle rouge), `molotov` (bouteille) ou `tracer` (traceur des héros).

Blocs facultatifs, valables pour tout comportement (les ennemis livrés les utilisent) :

| Bloc | Utilisation |
|---|---|
| `awareness` | Un ennemi placé inattentif (`unaware` d’une rencontre) joue `unaware` sans bouger ni attaquer ; il remarque un héros devant lui à moins de `sight` (8), ou à moins de `hearing` (2,5) de n’importe quel côté, ou quand il est touché, ou quand un allié à moins de `alarm` (5) le remarque ou tombe. Il sursaute alors `surprise` s (0,6 ; état `surprised` et le « ! » des règles) avant d’attaquer. |
| `panic` | Sous `health` (fraction de ses PV ; 0 : jamais), ou quand `allies` alliés (0 : jamais) tombent à moins de `radius`, il s’enfuit loin du héros le plus proche pendant `time` s à `speed` fois sa vitesse (état `flee`), une seule fois ; puis il revient si `returns` (vrai par défaut). Dans un écran bloqué il reste dans l’écran et revient toujours. |
| `grenade` | Lance une grenade en cloche (physique et image de la grenade des règles) vers un héros entre `minRange` et `range` (0 : jamais), toutes les `cooldown` s (multiplié selon la difficulté), la première après `first` s ; vitesse verticale `throwY`, vitesse horizontale choisie pour retomber sur le héros, au plus `maxX`. Elle explose à son `bounces`-ième rebond (0 : au premier contact avec le sol), au contact d’un héros ou au bout de `fuse` s, et retire `damage` aux héros à moins de `radius`. Pose `throw` pendant `pose` s. |
| `cries` | Sons ou voix (prises `-a`, `-b`, `-c`) joués de temps en temps à sa chute (`Audio/Voices/cri-chute` pour les livrés). |

La **mort** dépend de ce qui l’a touché : balle, explosion (grenade, roquette, spécial), feu (lance-flammes) ou couteau. L’état joué est `death`, `deathExplosion`, `deathFire` ou `deathKnife` (sinon `death`) et le mouvement du corps vient des règles (`deaths`, section 5 bis). Une arme dit la blessure qu’elle donne (`damageType`, section 5 ter).

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

Un boss est un comportement — `flyer` (vole et attaque en mouvement) ou `walker` (marche ou roule vers les héros et s’arrête pour chaque attaque) — et une liste d’attaques jouées à tour de rôle. Le même boss peut servir de boss final ou de boss de mi-parcours (section 1). Les mini-boss livrés : `armored-van` (marcheur sans secousse de pas : un fourgon qui roule, charge et lâche des pillards), `speedboat` (volant à fleur d’eau, sans rotor, dessiné derrière les héros avec `layer` 20), `combat-drone` (volant) et `taureau` (marcheur qui gratte le sol, charge et se cabre pour piétiner). Boss finals : `macron-helicopter` et `le-direct` (volants), `mecha-shogun` et `nostradamus` (marcheurs ; le devin de Saint-Rémy lance des parchemins dessinés, annonce ses prophéties par des cercles au sol puis les frappe, et appelle des Arlésiens). Son dessin est tourné vers la **gauche** ; ses ancrages et sa zone touchée sont retournés avec lui. Voir `Content/bosses/macron-helicopter.json` et `mecha-shogun.json` pour deux exemples complets.

| Champ | Utilisation |
|---|---|
| `behavior` | `flyer` ou `walker`. |
| `name`, `intro`, `rage`, `defeat` | Nom sur la barre de vie et annonces (une mission peut les remplacer). |
| `health` | PV par difficulté pour un joueur, `healthPerExtraPlayer` en plus par joueur (0,65 = +65 %), puis × `boss.health` de la mission. |
| `firstAttack` | Délai avant la première attaque. |
| `score` | Points à sa chute (10 000 par défaut ; 5 000 pour les mini-boss livrés). |
| `ragePace`, `rageDelay` | Sous la moitié de ses PV, le boss s’emballe : ses délais sont multipliés par `ragePace` (0,72) et sa prochaine attaque arrive au plus tard dans `rageDelay` s. |
| `flash` | Le dessin blanchit sous les tirs (marcheur livré : oui ; hélicoptère : non). |
| `shadow` | `{"x": largeur, "y": hauteur}` d’une ombre au sol, `0` pour aucune. |
| `smokeSize` | Taille de la fumée émise à l’ancrage `smoke` sous la moitié des PV. |
| `animations` | Section 2. |
| `rotor` | Volant : `x`, `y`, `width`, `height` d’un rotor animé dessiné par le moteur, `speed` de rotation ; `width` 0 : aucun. |
| `flight` | Volant : trajectoire `x = centre + sin(t) × min(sweep, demi-écran − sweepMargin)`, `y = altitude + sin(t × bobRate) × bob`, avec `t` qui avance à `speed` (par difficulté) ; `rageCatchUp` accélère la trajectoire quand il s’emballe. Dessin seulement : `roll` (degrés, 0 à 20) et `rollRate` font tanguer l’image (un bateau), et `wake` (comme `projectile`, section des attaques : `sprite`, `frames`, `fps`, `size`, `y`) boucle une écume derrière la coque, cachée à sa chute. Un `lane` de la mission (boss de mi-parcours) remplace le balayage et l’altitude. |
| `walk` | Marcheur : `speed` (par difficulté), `stopDistance` (s’arrête à cette distance du héros), `margin` (reste à cette distance des bords de l’arène), `stepLength` (un pas, avec secousse `stepShake` et poussière à l’ancrage `foot`), `entry` (entre à cette distance du bord droit ; négatif : arrive de hors de l’écran et entre en roulant). |
| `hitbox` | `count` cercles de rayon `radius`, le premier en (`x`, `y`) depuis le pivot, les suivants décalés de (`stepX`, `stepY`). |
| `death` | Chute : descend de `fall` u/s, tourne de `spin` °/s (dans le sens de son regard si `spinWithFacing`), explosions autour de `core` sur `spreadX` × `spreadY`. |
| `missile` | Image des bombes et missiles (ci-dessous). |
| `attacks` | Une à douze attaques. |

Chaque attaque a un `type`, une annonce `announce`, un repos `cooldown` après elle (multiplié selon la difficulté et, s’il s’emballe, par `ragePace`, sauf si `rageCooldown` donne la valeur à utiliser alors) et, facultatifs, les états d’animation `state`, `aimState`, `recoverState`, un `sound` (chaque tir d’un `gun`, le lancer des `bombs`, le départ d’une `charge`) et un `startSound`, joué quand l’attaque commence (le souffle du taureau avant sa charge).

| `type` | Déroulé | Champs |
|---|---|---|
| `gun` | Vise `aimTime` s (une mire de `sightWidth` marque le sol sous le héros, `aimHeight` au-dessus de ses pieds), puis tire pendant `duration` s une balle toutes les `interval` s depuis ses `muzzles` à tour de rôle, en balayant de ± `sweep` à la cadence `sweepRate`. | `aimTime`, `interval`, `speed` (par difficulté), `duration`, `sweep`, `sweepRate`, `aimHeight`, `sightWidth`, `muzzles` (ancrages), `damage` |
| `bombs` | Marque `count` points au sol autour du héros (+`rageExtra` s’il s’emballe), espacés de `spacing`, puis lance un missile vers chacun depuis l’ancrage `rack` après `delay` s (+`stagger` par missile) ; vol de `flight` s en cloche de hauteur `arc`, explosion de rayon `blastRadius` sous `blastHeight`. `duration` 0 : les bombes partent tout de suite et le boss continue ; sinon il prend la pose `state` pendant `duration` s et lance après `launch` s (`smoke` : fumée au lancement). Les missiles peuvent être abattus. | `count`, `delay`, `flight` (par difficulté), `rageExtra`, `spacing`, `stagger`, `arc`, `blastRadius`, `blastHeight`, `marker` (largeur de la marque), `rack`, `duration`, `launch`, `smoke`, `damage` |
| `charge` | Marcheur seulement. Fait rugir son moteur (`aimState`) `windup` s tourné vers un héros, puis fonce (`state`) à `speed` (par difficulté) jusqu’au bord opposé de l’arène ; les héros au sol sur son passage (à moins de `reach` de son centre, sous `clearance`) perdent `damage` : un saut par-dessus l’évite. Puis il souffle `recover` s (`recoverState`, secousse `shake`). | `windup`, `speed`, `damage`, `clearance`, `reach`, `recover`, `shake` |
| `troops` | Prend la pose `state` pendant `duration` s et lâche `count` ennemis (par difficulté) de la liste `enemies`, à tour de rôle, un toutes les `interval` s (par difficulté) depuis l’ancrage `door`, tant qu’il en a moins de `maxAlive` en vie ; ils sautent à terre et courent `spread` unités vers l’équipe. À sa chute, ceux qui restent s’enfuient. | `enemies`, `count`, `interval`, `duration`, `door`, `spread`, `maxAlive` |
| `stomp` | Lève le pied (`state`) pendant `windup` s, puis envoie une onde de choc de chaque côté depuis l’ancrage `foot` ; seuls les héros au sol (sous `waveHeight`) sont touchés à moins de `waveReach`. Il reprend son souffle `recover` s (`recoverState`) et recommence `repeat` fois (`rageRepeat` s’il s’emballe), relevant le pied en `repeatWindup` s. | `windup`, `repeatWindup`, `recover`, `repeat`, `rageRepeat`, `speed` (par difficulté, vitesse des ondes), `waveReach`, `waveHeight`, `shake`, `foot`, `damage` |

**Attaques dessinées.** Trois blocs facultatifs remplacent les dessins du moteur par des cases d’une planche ; ils ne changent rien d’autre (trajectoires, dégâts, portée) :

| Bloc | Attaque | Remplace |
|---|---|---|
| `projectile` | `gun` | la balle rouge : chaque tir est dessiné, tourné dans le sens de sa course, sans flamme de bouche |
| `warning` | `bombs` | la barre rouge qui marque chaque point d’impact : l’image clignote au sol pendant le `delay` |
| `impact` | `bombs` | l’explosion : l’image est jouée une fois à l’impact (le `impactSound` de l’attaque, sinon le bruit d’explosion) |

Chacun s’écrit `{"sprite": {"texture", "columns", "rows", "cell"}, "frames": 4, "fps": 8, "size": 2.8, "y": 0.05}` : `frames` cases à partir de `cell` (de gauche à droite puis de haut en bas), jouées à `fps` images par seconde (en boucle pour `projectile` et `warning`, une fois pour `impact`), affichées sur `size` unités de haut, le centre à `y` au-dessus de la rue (pour `warning` et `impact`). Sans `sprite.texture`, le bloc est absent. Nostradamus (`Content/bosses/nostradamus.json`) : parchemins (`Art/saint-remy/prophetie-fx`, cases 4 à 6), cercles annoncés (cases 0 à 3) et éclats de prophétie (cases 8 à 11), avec une étoile filante comme `missile`.

`missile` : un rectangle d’une image, `x`, `y`, `width`, `height` en fractions de l’image depuis son coin **inférieur** gauche, affiché sur `size` unités de large ; `outline`, facultatif, liste de points (fractions du rectangle) qui détoure le missile pour cacher ce qui l’entoure dans l’image ; `hitRadius` et `tipRadius` (rayons touchés au centre et aux extrémités, à `hitLength` du centre). L’hélico, Le Direct, le Mécha Shogun et le drone prennent le missile dessiné sur `Art/macron-helicopter.png`, le hors-bord une grenade de `Art/grenades.png`, Nostradamus une étoile de `Art/saint-remy/prophetie-fx.png` (le fourgon et le taureau n’en lancent pas) ; une image à part, réduite au missile, convient aussi (rectangle `0, 0, 1, 1`, sans `outline`).

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

Une planche de boss dessinée tournée vers la droite (le taureau de Saint-Rémy) se retourne avec `tools/art/pack_sheet.py hero` et `"mirror": true` sur sa source, qui retourne chaque case sur place (`tools/art/bosses/taureau.json`).

Un nouveau boss volant reprend la mise en page de l’hélicoptère : vue de profil tournée vers la gauche, sans pales de rotor principal si le moteur dessine le rotor. Un nouveau marcheur suit celle du mécha : une case par pose (attente, deux pas, pied levé, tir, lance-missiles), même ligne de pieds ; `pack_sheet.py mecha` la met en place. Un comportement de boss autre que voler ou marcher demande une modification du moteur.

## 5. Les héros

Les héros sont dans `Content/heroes/`, **autant que voulu** (jusqu’à 99), triés par `order` puis `id` : les quatre d’origine (`order` 0 à 3) puis Fernandel, Brigitte Bardot et Jeanne d’Arc (4 à 6). Le menu a toujours quatre cartes, une par joueur (J1 à J4) : chacun y fait défiler tous les héros (flèches de la carte à la souris, Q / E ou ← / → pour J1 au clavier, croix gauche / droite de sa manette) ; plusieurs joueurs peuvent prendre le même. **Les replays nomment leurs héros par leur `id`** (`heroIds`, depuis le lot 3H) : ajouter un héros n’en casse aucun ; les anciens replays, sans ids, les désignent par leur position. Le jeu en ligne envoie la position (même version du jeu chez tous les joueurs). Changer les chiffres d’un héros change le jeu : les scores publiés auparavant avec lui ne se rejouent plus.

Un nouveau héros n’a besoin que de sa planche (`Art/heroes/<id>.png`), de son JSON et, s’il en faut, de son arme (section 5 ter) et des images de sa spéciale. Il peut étendre un héros livré (`"extends"`) et n’en changer qu’une partie.

| Champ | Utilisation |
|---|---|
| `name`, `shortName`, `role`, `specialName` | Nom de la fiche, nom court du HUD, rôle et nom du spécial. |
| `speed`, `jump` | Vitesse de course, vitesse verticale au saut. |
| `weapon` | Son arme de base, un identifiant de `Content/weapons` **sans limite de munitions** (section 5 ter) : il commence chaque vie avec elle et la retrouve quand une arme à lettre est vide ou à sa mort. Cadence, dégâts et recul y sont réglés (les anciens champs `fireRate`, `damage`, `recoilShake` du héros sont refusés). |
| `strideRate`, `bob` | Vitesse de la foulée (section 2) et balancement vertical en courant. |
| `portraitCell`, `portraitFrame` | Case de la planche affichée dans la carte du menu, et le carré de cette case qui y est montré : `{"x": 0.02, "y": 0, "size": 0.72}` en fractions de la case depuis son coin inférieur gauche (toute la case par défaut). `size` est une fraction de la **largeur** de la case et le carré reste carré : dans une case plus large que haute (Jeanne : 480 × 420), il monte de `size × largeur / hauteur` et doit rester dans la case. |
| `hitbox` | Où le touchent les tirs ennemis, debout : un cercle de rayon `radius` à `y` au-dessus des pieds (`{"y": 1, "radius": 0.53}`). |
| `crouch` | Accroupi : vitesse `speed`, et le cercle touché `y`, `radius` (`{"speed": 3.6, "y": 0.55, "radius": 0.45}` : les balles tirées à hauteur de poitrine passent au-dessus). |
| `knife` | Avec le bouton de tir, un ennemi à moins de `reach` devant le héros (qui est à moins de `height` du sol) reçoit `damage` au couteau au lieu d’une balle ; `cooldown` avant le coup suivant, pose `knife` pendant `pose` s, son `sound`. |
| `special` | Un des types génériques ci-dessous (`type`). Recharge : `cooldown + difficulté × cooldownPerDifficulty` s. |
| `animations` | Les états de la section 2, un `muzzle` sur chaque case d’où il tire et une `hand` sur la dernière case de `throw`. |

### Les spéciales

Chaque spéciale est un comportement générique, réglé par le JSON du héros ; n’importe quel héros peut prendre n’importe lequel.

| `type` | Ce qu’il fait | Champs |
|---|---|---|
| `blast` | Frappe tout ce qui est à moins de `radius` (`damage`), le boss à moins de `radius + bossReach` (`bossDamage`), rend invulnérable `invulnerable` s et fait bondir de `leap` (0 : non). | `radius`, `bossReach`, `invulnerable`, `leap` |
| `volley` | `shots` tirs en éventail dans la direction visée, à `speed`, écartés de `spread`, `damage` chacun, explosifs si `explosive`. | `shots`, `speed`, `spread`, `explosive` |
| `summon` | Après `delay` s, un allié (`ally`) traverse l’écran depuis son bord derrière le héros (`from` `edge`) ou depuis `margin` u derrière lui (`behind`), à `speed` u/s, à `y` au-dessus de la rue ; il piétine une fois chaque ennemi dont le milieu est à moins de `reach` devant ou derrière son milieu et sous `height`, et le boss une fois (`bossDamage`). Il disparaît de l’autre côté de l’écran ou au bout de `life` s ; `sound` joue à son arrivée. Son dessin est une animation (section 2) avec l’état `run` (`perStride` : images par unité parcourue). | `delay`, `ally {from, speed, reach, height, y, life, margin, sound, animations}` |
| `dash` | Le héros fonce devant lui à `speed` pendant `time` s, quoi que dise le stick, invulnérable `time + invulnerable` s ; il renverse une fois chaque ennemi à moins de `radius` de son torse et le boss une fois (`bossDamage`). | `speed`, `time`, `radius`, `invulnerable` |
| `salvo` | `bursts` salves, une toutes les `interval` s ; chaque salve tire `shots` balles (écartées de `spread` degrés) dans chacune des `directions` à la fois : `{"angle", "anchor"}`, angle en degrés depuis l’avant du héros (0 devant, 180 derrière, 90 en haut), depuis l’ancrage `anchor` de l’image `special` (sinon depuis le milieu du corps). `explosive` comme `volley`. | `bursts`, `interval`, `shots`, `spread`, `speed`, `directions`, `explosive` |

Champs communs, facultatifs : `pose` (s pendant lesquelles le héros montre son état `special`, s’il en a un, sans tirer ni lancer de grenade ; une charge ou une salve dure au moins le temps de la charge ou des salves), `sound` (joué à l’appui ; à chaque salve pour `salvo`), `voice` (une annonce enregistrée en prises `-a`, `-b`, `-c`, comme celles des bonus), `wound` (`bullet`, `explosion` par défaut, `fire`, `knife` : la façon dont tombent les ennemis d’une invocation ou d’une charge), `trail` et `impact` (dessins `{"sprite", "frames", "fps", "size", "y"}` comme ceux des boss : traînée laissée toutes les `trailEvery` s derrière une invocation ou une charge, éclat sur chaque ennemi frappé). Les spéciales `blast` et `volley` gardent leur son d’explosion.

Les trois héros du lot 3H en sont les exemples : Fernandel siffle (`summon`) et Marguerite, la vache, charge à travers l’écran ; Jeanne charge à l’étendard (`dash`) ; Brigitte tire aux deux revolvers des deux côtés (`salvo`, ancrages `muzzle` et `back` de sa case `special`). Un héros entièrement en JSON avec une spéciale nouvelle (repris par `AgentScripts/VerifyLot3Heroes.cs`) :

```json
{
  "id": "quatre-vents",
  "extends": "chuck-norris",
  "order": 20,
  "name": "QUATRE VENTS",
  "special": {
    "type": "salvo", "cooldown": 3, "damage": 6, "shots": 1, "speed": 25, "bursts": 2, "interval": 0.1,
    "directions": [{"angle": 0}, {"angle": 90}, {"angle": 180}, {"angle": 270}]
  }
}
```

## 5 bis. Les règles de partie

`Content/rules/default.json` fixe ce qui ne dépend ni d’un héros ni d’un ennemi ; une mission peut en nommer une autre avec `"rules"`, qui peut étendre `default` (`"extends": "default"`) et n’en changer qu’une partie.

| Champ | Utilisation |
|---|---|
| `grenades` | `start` et `max` : grenades au départ d’une vie et au plus, par difficulté ; `refillOnRespawn` (`true`) : un renfort repart avec au moins `start`. |
| `grenade` | Le lancer : vitesse `throwX` (vers l’avant), `throwY` (vers le haut), `gravity`. Au sol elle rebondit `bounces` fois (vitesse verticale × `bounce`, horizontale × `friction`), puis explose ; elle explose aussi au contact d’un ennemi ou du boss, ou au bout de `fuse` s. L’explosion retire `damage` aux ennemis à moins de `radius` (plus leur `hitRadius`), `bossDamage` au boss qu’elle touche, et détruit les missiles du boss pris dedans si `destroysBombs`. `cooldown` entre deux lancers, pose `throw` pendant `pose` s, `throwSound`. `sprite` (la grenade, dessinée `size` unités de haut) et `icon` (le compteur du HUD) : `{"texture", "columns", "rows", "cell"}`. |
| `bonuses` | Les bonus, désignés par leur position dans la liste : `{"id", "effect", "amount": [4 valeurs], "announce", "voice", "icon", "scale"}`. `effect` : `health` (points de vie), `turbo` (secondes), `score` (points), `grenades` (grenades, sans dépasser `grenades.max`) ou `weapon`. Une caisse `weapon` donne l’arme `weapon` (identifiant de `Content/weapons`) ou, si `weapon` est vide, une arme tirée de la table `weapons` ; elle n’a pas d’`amount` (les munitions sont dans l’arme), et sa caisse, son annonce et sa voix sont celles de l’arme sauf si le bonus les donne. `voice` : chemin des trois prises `-a`, `-b`, `-c` de l’annonce, facultatif. `icon` : la case dessinée, `scale` sa taille (1,25 unité). |
| `supplies`, `drops` | Les bonus des caisses posées le long du parcours, puis ceux que laissent les ennemis vaincus, à tour de rôle (indices dans `bonuses`). |
| `firstSupply`, `supplySpacing`, `killsPerDrop` | Par difficulté : position de la première caisse, distance entre deux caisses, ennemis vaincus pour un bonus lâché (`[24, 35, 45, 55]`, `[72, 90, 110, 130]`, `[15, 22, 28, 35]`). |
| `weapons` | La table des caisses d’armes qui ne nomment pas leur arme : `[{"weapon": "<id>", "weight": 3}, …]`. Sur toute suite de tirages aussi longue que la somme des poids, chaque arme sort `weight` fois, réparties (tourniquet pondéré, le même sur toutes les machines). Une mission peut la remplacer par son propre champ `weapons`. Obligatoire dès qu’un bonus `weapon` ne nomme pas d’arme. |
| `go` | L’image de la flèche GO, `{"texture": "Art/go-sign"}`. |
| `tally` | Le décompte de fin de mission : `hostage` (points par otage compté) et `perSecond` (points par seconde gagnée sur le `parTime` de la mission), par difficulté (`[1000, 1000, 1000, 1000]` et `[10, 10, 10, 10]`), et `icon`, la case qui représente les otages au HUD et au décompte. |

| `deaths` | Comment tombe un ennemi selon sa blessure : `[{"damage": "bullet" \| "explosion" \| "fire" \| "knife", "launch": {"x": 1.6, "y": 2.4}, "spin": 0, "time": 1.3, "gravity": 16, "lie": true, "burn": false}]`. Le corps part à `launch` (x : en s’éloignant du coup), tourne de `spin` °/s, retombe sous `gravity` ; `lie` : il se pose et reste dans la rue ; `burn` : il flambe et fume. Il clignote et s’efface à la fin de `time`. Une blessure absente garde la courte culbute d’avant. |
| `stage` | La mise en scène des rencontres : `alert` (la case du « ! » de surprise, `alertSize` unités), `alertSound`, `alertVoice` et `panicVoice` (voix criées par les ennemis, au plus une toutes les trois secondes), `parachute` (`open`, `folding`, `landed` : cases du parachute ouvert, se repliant, au sol ; `size` en unités, `speed` de descente, `sway` balancement, `height` au-dessus des pieds), `openings` (`[{"id": "door", "sprite": {…}, "height": 2.7, "time": 0.7}]` : ouvertures dessinées pour les entrées `door` et `window`, `height` unités de haut, ouvertes `time` s pendant que l’ennemi en sort). |

```json
{"id": "beaucoup-de-grenades", "extends": "default", "grenades": {"start": [20, 20, 15, 10], "max": [30, 30, 20, 15]}, "supplies": [4, 0, 4, 1]}
```

Des caisses d’armes qui ne donnent que des roquettes, et plus serrées :

```json
{"id": "que-des-roquettes", "extends": "default", "weapons": [{"weapon": "rocket-launcher", "weight": 1}],
 "supplySpacing": [40, 50, 60, 70]}
```

## 5 ter. Les armes

`Content/weapons/<id>.json` décrit chaque arme des héros. Le moteur ne connaît que quatre **projectiles génériques** ; une arme choisit le sien et le règle. Une arme **à munitions** (`ammo` ≥ 1) a une lettre et sort des caisses ; perdue à la mort du héros, elle revient à l’arme de base quand ses munitions sont épuisées, et ramasser la même arme les remplit à nouveau. Une arme **sans munitions** (`ammo` à 0 partout ou absent) ne s’épuise jamais : c’est l’arme de base d’un héros (`"weapon"` du héros). Les armes livrées : `rifle` et ses variantes par héros (`assault-rifle`, `heavy-rifle`, `carbine`, `battle-rifle`, par `extends`), `heavy-machinegun` (H), `shotgun` (S), `flamethrower` (F), `rocket-launcher` (R), `laser` (L).

| Champ | Utilisation |
|---|---|
| `letter`, `name` | Lettre (une majuscule, obligatoire avec des munitions) et nom affiché (24 caractères ; le HUD montre le nom d’une arme sans munitions, le badge et le compteur des autres). |
| `ammo` | Munitions par difficulté (une par tir, même pour plusieurs plombs ou un rayon), ou 0 partout : illimitée. |
| `fireRate`, `damage` | Délai entre deux tirs (compté en pas de 1/60 s : 0,055 tire tous les 4 pas) et dégâts de chaque projectile. |
| `projectile.type` | `bullet` : va tout droit. `flame` : idem, mais dessiné avec ses `frames` au fil de sa vie et grossissant de `growth`, ralenti par une `acceleration` négative. `rocket` : laisse une traînée de fumée, accélère de `acceleration`, et vire de `homing` degrés par seconde vers l’ennemi le plus proche à moins de 14 unités (le boss sinon). `beam` : un rayon instantané de `range` unités qui touche tout ce qu’il croise, affiché `life` s ; le sol l’arrête vers le bas. |
| `projectile.pellets`, `spread`, `jitter` | Nombre de projectiles par tir, écart en degrés entre deux voisins (éventail), et dispersion de chaque tir jusqu’à `jitter` degrés selon un motif fixe (sans hasard : le même partout). |
| `projectile.speed`, `life`, `acceleration` | Vitesse de départ (unités/s, jusqu’à 80), durée de vie (la portée en découle), variation de vitesse par seconde. |
| `projectile.pierce`, `radius` | Ennemis traversés après le premier (`-1` : tous, chacun une seule fois) ; rayon du projectile ajouté à celui des ennemis (grandit avec `growth`). |
| `projectile.sprite`, `frames`, `size`, `color` | La case dessinée (`{"texture", "columns", "rows", "cell"}`, obligatoire pour `flame` et `beam`), le nombre de cases consécutives jouées au fil de la vie, la hauteur en unités (pour un rayon, son épaisseur ; la case est étirée sur sa longueur), une teinte `RRGGBB`. Sans `sprite`, le moteur dessine le traceur du fusil, teinté par `color`. |
| `explosion` | `radius` (0 : pas d’explosion), `damage` (0 : celui du projectile), `bossDamage`. Un projectile explosif éclate sur le premier ennemi touché, au sol et à la fin de sa vie : tous les ennemis dont le centre est à moins de `radius` sont touchés, le boss à portée reçoit `bossDamage` (un tir direct sur le boss lui retire `damage`). |
| `recoil` | `shake` : secousse de l’écran à chaque tir ; `push` : vitesse (unités/s) à laquelle le tir repousse le héros, vite amortie. |
| `damageType` | Facultatif : la blessure qu’elle donne, `bullet`, `explosion`, `fire` ou `knife` ; par défaut `fire` pour `flame`, `explosion` si elle explose, `bullet` sinon. Elle choisit la mort des ennemis (section 3). |
| `flash`, `casings`, `sound` | Éclair de bouche `none`, `light` ou `heavy`, douilles éjectées, son du tir. |
| `announce`, `voice` | Annonce du ramassage (obligatoire avec des munitions ; non affichée par le HUD actuel, voir section 0) et voix facultative (`Audio/Voices/<ligne>`, prises `-a`, `-b`, `-c`). |
| `icon`, `crate` | Le badge du HUD à côté du compteur et la caisse posée dans la rue (obligatoire avec des munitions), `{"texture", "columns", "rows", "cell"}`. |

```json
{
  "id": "shotgun", "letter": "S", "name": "FUSIL À POMPE", "ammo": [30, 26, 22, 18], "fireRate": 0.55, "damage": 5,
  "projectile": {"type": "bullet", "pellets": 5, "spread": 7, "speed": 28, "life": 0.24, "pierce": -1, "radius": 0.25,
                 "sprite": {"texture": "Art/weapon-fx", "columns": 3, "rows": 2, "cell": 5}, "size": 1.1},
  "recoil": {"shake": 0.18, "push": 6}, "flash": "heavy", "casings": true, "sound": "Audio/Sfx/tir-S-pompe",
  "announce": "FUSIL À POMPE !",
  "icon": {"texture": "Art/weapon-letters", "columns": 3, "rows": 2, "cell": 1},
  "crate": {"texture": "Art/weapon-crates", "columns": 3, "rows": 2, "cell": 1}
}
```

Une nouvelle arme n’a besoin que de son JSON (et de ses images s’il en faut de nouvelles) : l’ajouter à la table `weapons` des règles ou d’une mission, ou la nommer dans un bonus. Les planches `Art/weapon-crates.png` (caisses H, S, F, R, L et une caisse grise sans emblème), `Art/weapon-letters.png` (badges H, S, F, R, L et un badge fusil) et `Art/weapon-fx.png` (trois images de flamme, rayon, roquette, plomb) sont en 3 × 2 cases ; voir `docs/ART_LOT1B.md`. Le code peut aussi donner une arme à un héros par `GiveWeapon(joueur, "<id>")` ; un otage dont la récompense est une arme la lâche dans sa caisse, qui arme le héros par le même chemin.

## 5 quater. Les otages

`Content/hostages/<id>.json` décrit un otage : comment on le libère, ses quatre états et ce qu’il donne. Le moteur fait le reste (attaché → remerciement → don → fuite). Les otages livrés : `civilian` (générique) et ses variantes par ville, qui l’étendent avec leur seule planche : `baker` (Paris), `boules-player` (Marseille), `peintre` (Saint-Rémy), `office-worker` (Tokyo).

| Champ | Utilisation |
|---|---|
| `name` | Nom (24 caractères). |
| `release` | `shot` : un tir sur ses liens (ou une explosion, un rayon) ; `touch` : le contact d’un héros ; `both` (par défaut). |
| `hits` | Tirs nécessaires (1 par défaut) ; un même projectile ne compte qu’une fois. |
| `hitbox` | `{"y", "radius"}` : le cercle que les tirs doivent toucher, `y` au-dessus des pieds. |
| `touchRadius` | Distance horizontale à laquelle un héros le libère au contact. |
| `thanks`, `give`, `giveAt` | Durées des états `thanks` (salut) et `give` (don) ; la récompense tombe `giveAt` s après le début du don. |
| `run` | `{"speed", "direction"}` : vitesse de fuite et sens (`-1` gauche, `1` droite, `0` à l’opposé du héros qui l’a libéré). Il disparaît hors de l’écran. |
| `rewards` | Sa table : `[{"bonus": "<id d’un bonus des règles>", "weapon": "<arme>", "weight": 2}, …]`. Sur toute suite de libérations aussi longue que la somme des poids, chaque récompense sort `weight` fois (tourniquet pondéré déterministe, comme les armes). Chaque `bonus` doit exister dans les règles des missions qui placent l’otage. |
| `sound`, `voice` | Son de libération (`Audio/Sfx/otage-libere`) et voix facultative (`Audio/Voices/<ligne>`, prises `-a`, `-b`, `-c`). |
| `animations` | Planche et états (section 2) : `tied`, `thanks`, `give`, `run` sont requis. |

```json
{
  "id": "civilian", "name": "OTAGE", "release": "both", "hits": 1, "touchRadius": 0.9,
  "thanks": 1.3, "give": 0.9, "giveAt": 0.5, "hitbox": {"y": 0.7, "radius": 0.6}, "run": {"speed": 7.5, "direction": -1},
  "sound": "Audio/Sfx/otage-libere",
  "rewards": [{"bonus": "weapon", "weight": 2}, {"bonus": "grenades", "weight": 2}, {"bonus": "health", "weight": 1}, {"bonus": "score", "weight": 1}],
  "animations": {
    "sheet": {"texture": "Art/hostages/civilian", "columns": 4, "rows": 3, "pivot": {"x": 0.45, "y": 0.0352941}, "cellHeight": 3.1481481},
    "states": [{"name": "tied", "cells": [0, 0, 1, 0], "fps": 3}, {"name": "thanks", "cells": [2, 3, 3, 3, 4, 4], "fps": 4.6, "loop": false},
               {"name": "give", "cells": [5, 6, 6, 7], "fps": 4.5, "loop": false}, {"name": "run", "cells": [8, 9, 10, 11], "fps": 11}],
    "layer": 28
  }
}
```

Une variante par ville n’a besoin que de sa planche : `{"id": "baker", "extends": "civilian", "name": "BOULANGER", "animations": {"sheet": {"texture": "Art/hostages/baker"}}}`. Les planches font 4 × 3 cases de 340 px (attaché ×2, libéré, salut, joie, fouille du sac, cadeau tendu, pouce levé, course ×4) ; `tools/art/pack_sheet.py hero tools/art/hostages/<otage>.json` les recale et `docs/ART_LOT2A.md` donne les prompts.

## 5 quinquies. Les difficultés

`Content/difficulties/<id>.json` décrit chaque difficulté du menu, rangée par `order` puis `id` : les replays, le jeu en ligne et le classement la désignent par cette position (0 DÉTENTE, 1 ARCADE, 2 DIFFICILE, 3 LÉGENDE, 4 SLUG).

| Champ | Utilisation |
|---|---|
| `name`, `tagline` | Nom du bouton (14 caractères) et ligne affichée sous les boutons quand elle est choisie (70 caractères). |
| `order`, `column` | Position dans le menu ; colonne (0 à 3) des valeurs par difficulté de tout le contenu. |
| `health` | Points de vie d’un héros (1 : mort en un coup). |
| `lives`, `livesPerPlayer` | Renforts : partagés par l’équipe (`lives` × joueurs, `false`) ou propres à chaque héros (`true`). |
| `damageTaken`, `hurtInvulnerable` | Multiplicateur des dégâts reçus ; invulnérabilité après un coup (s). |
| `enemyHealth`, `enemyCount`, `enemySpeed`, `enemyCadence`, `gunmanAim` | Facteurs des PV, du nombre, de la vitesse et du délai entre deux attaques des ennemis, et du temps de visée des tireurs. |
| `waveReward` | `{"every", "lives", "health"}` : toutes les `every` vagues vaincues (0 : jamais), `lives` renforts de plus et `health` PV rendus aux héros debout. |
| `respawn` | `{"delay", "invulnerable", "inPlace"}` : délai avant le retour d’un héros tombé, invulnérabilité (clignotement), retour là où il est tombé (`true`) ou près du héros de tête. |
| `continues` | `{"enabled", "seconds", "grace", "lives"}` : quand toute l’équipe est à court de vies, un écran **CONTINUE ?** compte `seconds` s ; après `grace` s, un saut ou un nouvel appui sur tir continue (chaque héros revient sur place avec `lives` vies), une grenade abandonne. |

```json
{"id": "slug", "order": 4, "name": "SLUG", "tagline": "MORT EN UN COUP, 3 VIES PAR JOUEUR, PUIS CONTINUE ?", "column": 1,
 "health": 1, "lives": 2, "livesPerPlayer": true, "damageTaken": 1, "enemyHealth": 1.8, "enemyCount": 1.2, "enemySpeed": 1.12,
 "enemyCadence": 0.85, "gunmanAim": 0.92, "hurtInvulnerable": 0.75, "waveReward": {"every": 0, "lives": 0, "health": 0},
 "respawn": {"delay": 1.2, "invulnerable": 2.5, "inPlace": true}, "continues": {"enabled": true, "seconds": 10, "grace": 1, "lives": 2}}
```

Les quatre premières reprennent au bit près les anciennes règles du code (`0.84000003` pour la visée en DIFFICILE).

## 5 sexies. Les véhicules de dépôt

`Content/carriers/<id>.json` : un véhicule qui amène les ennemis d’une rencontre (`truck` d’une rencontre). Il arrive d’un bord de l’écran à `speed`, ralentit sur ses `brake` dernières unités, s’arrête (son `sound`), laisse descendre ses passagers par son ancrage `door` toutes les `interval` s, attend `wait` s puis repart dans le même sens et disparaît hors de l’écran. `length` : sa longueur, pour qu’il démarre hors de l’écran. Ses `animations` sont tournées vers la droite, états `drive` (roues, `perStride` : avance avec la distance) et `open` (porte ouverte, avec l’ancrage `door`). C’est un décor : rien ne le touche.

```json
{"id": "rusty-van", "speed": 10, "brake": 4, "interval": 0.45, "wait": 0.9, "length": 4.4, "sound": "Audio/Sfx/camion-frein",
 "animations": {"sheet": {"texture": "Art/carriers", "columns": 3, "rows": 2, "pivot": {"x": 0.5, "y": 0.02734375}, "cellHeight": 4.6545455}, "layer": 26,
   "states": [{"name": "drive", "cells": [0, 1], "perStride": 1.2}, {"name": "open", "cells": [2]}],
   "anchors": [{"name": "door", "cell": 2, "x": -0.25, "y": 0.45}]}}
```

`black-minivan` (Tokyo) prend la deuxième ligne de la même planche. Un nouveau véhicule peut en étendre un (`"extends": "rusty-van"`).

## 6. Emplacements réservés aux lots suivants

Ces champs existent déjà dans le schéma pour que les prochaines mécaniques s’y branchent sans changer le format. Tant que le moteur ne les lit pas, ils doivent rester **vides ou absents** : une liste non vide est refusée, pour ne pas laisser croire qu’elle agit. Formes prévues :

| Champ | Élément prévu | Bibliothèque prévue |
|---|---|---|
| `map.terrain` | `{"type": "platform" \| "slope" \| "pit", "x": 40, "width": 6, "height": 2, "toHeight": 3}` : relief le long du parcours. | — |
| `map.props` | `{"id": "<prop>", "x": 52, "y": 0, "wave": 3}` : décor destructible, objectif de la vague `wave` si elle est donnée. | `Content/props/<id>.json` |
| `map.vehicles` | `{"id": "<véhicule>", "x": 120}` : véhicule pilotable posé sur la carte. | `Content/vehicles/<id>.json` |

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

Le validateur lit la bibliothèque et les missions comme le jeu : `extends`, références d’armes, d’ennemis et de boss, images et sons présents, états d’animation requis par chaque comportement, ancrages, bornes de tous les nombres, otages placés dans l’écran de leur vague et récompenses connues des règles, rencontres vues de l’équipe (entrées dans l’écran de la rencontre, camion, ouvertures connues des règles, mini-boss hors des écrans de vague), emplacements réservés vides. Il refuse aussi qu’un nom propre à une mission apparaisse dans `Assets/Scripts`. Le jeu refait les mêmes contrôles au démarrage et refuse une mission invalide.

Dans Unity, jouer la mission avec un puis plusieurs joueurs dans plusieurs difficultés : vagues, sortie, raccords du décor, animations et boss. Pour l’ouvrir sans jouer les précédentes, saisir son code dans **CODE MISSION**. `AgentScripts/VerifyDataDriven.cs` montre comment charger une mission depuis du JSON et la faire finir par un bot ; `AgentScripts/VerifySaintRemy.cs` vérifie une mission livrée de bout en bout (décors, ennemis, mini-boss, boss, campagne, bots dans chaque difficulté) ; `AgentScripts/VerifyLot2b.cs` fait de même avec une mission dont les rencontres (toutes les entrées, un véhicule et un mini-boss qui étendent ceux de la bibliothèque) sont écrites seulement en JSON.

Proposer les JSON, les images et leurs `.meta` dans une pull request avec des captures. Une fois la proposition fusionnée, la compilation web suivante inclut la mission.
