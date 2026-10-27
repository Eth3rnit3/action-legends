# Ajouter une mission

**Action Legends** est le jeu. **Panique à Paname** est la première mission, définie dans `Assets/Resources/Levels/panique-a-paname.json`.

## 1. Créer le JSON

Dupliquer ce fichier dans le même dossier. Lui donner un nom unique en minuscules séparées par des tirets, identique au champ `id`. Le jeu charge automatiquement toutes les missions valides de ce dossier : il n’est pas nécessaire de modifier un catalogue à la main ni d’ajouter une nouvelle scène Unity.

| Champ | Utilisation |
|---|---|
| `schemaVersion` | Garder `1`. |
| `id` | Identifiant unique, identique au nom du fichier sans `.json`. |
| `order` | Facultatif. Position dans la campagne, de `0` à `999`, `1` par défaut. À égalité, les missions sont classées par `id`. Les missions s’enchaînent dans cet ordre. |
| `code` | Facultatif. Code de la mission, 4 à 10 lettres majuscules ou chiffres, unique. Il est révélé quand la mission précédente est gagnée et permet de la rejoindre depuis le menu. Sans code, la mission ne s’ouvre qu’en gagnant la précédente. La première mission est toujours ouverte. |
| `title` | Nom affiché dans le menu. |
| `location` | Ville, lieu ou époque affichés dans la fiche de mission. |
| `intro`, `exitLabel`, `winTitle` | Messages de début, de sortie et de victoire. |
| `backgrounds` | Images de décor dans leur ordre horizontal, chemins Resources sans extension. |
| `length`, `exitX` | Longueur de la mission et position à atteindre après les vagues. |
| `cameraMin`, `cameraMax` | Bornes horizontales de la caméra. |
| `waves` | Vagues triées par position `atX`. |
| `ground` | Facultatif. Image dont le bas sert de chaussée au premier plan. Par défaut : `Art/paris`. |
| `music`, `bossMusic` | Facultatifs. Musique de la mission et musique du combat final, chemins Resources sans extension. Par défaut : `Audio/Music/marseillaise-arcade` et `Audio/Music/marseillaise-boss`. |
| `enemySheet`, `enemies` | Facultatifs. Planche et types d’ennemis de la mission, détaillés plus bas. Par défaut : les trois pilleurs de Panique à Paname. |
| `boss` | Facultatif. Réglages du boss final, détaillés plus bas. Sans ce champ, la mission se termine par l’hélicoptère de Panique à Paname. |

Exemple de vague :

```json
{
  "atX": 18,
  "baseEnemies": 7,
  "enemiesPerPlayer": 3,
  "maxAlive": 10,
  "spawnInterval": 0.65,
  "enemyPattern": [0, 1, 2],
  "lock": true
}
```

Le total de la vague vaut `baseEnemies + nombre de joueurs × enemiesPerPlayer`. `maxAlive` limite le nombre d’ennemis présents simultanément ; `spawnInterval` est l’intervalle entre les apparitions.

`lock` est facultatif et vaut `true` par défaut : quand l’équipe atteint la vague, l’écran se bloque, les ennemis entrent par les bords et la vague ne se termine qu’à la chute du dernier, avant que la flèche **GO** ne rende le défilement. Avec `"lock": false`, la caméra continue de suivre l’équipe et la vague se termine dès que tous ses ennemis sont apparus : de quoi alterner combats imposés et passages en course.

Les nombres de `enemyPattern` désignent les types d’ennemis de la mission, dans l’ordre de `enemies`. Par défaut, `0` : fonceur, `1` : lanceur et `2` : costaud au bouclier. À partir de la difficulté Arcade, un ennemi sur quatre est le type `1`.

### Types d’ennemis

```json
"enemySheet": "Art/tokyo/enemies",
"enemies": [
  {"role": "pistol", "row": 0, "muzzle": {"x": 1.33, "y": 1.43}},
  {"role": "smg", "row": 1, "muzzle": {"x": 1.18, "y": 1.39}},
  {"role": "shotgun", "row": 2, "muzzle": {"x": 1.52, "y": 1.61}}
]
```

`enemySheet` est une planche transparente de quatre images carrées par ligne, tournées vers la droite : attente, deux foulées de course, attaque. Les pieds reposent à 4,5 % du bas de chaque case et le milieu des jambes à 40 % de sa largeur ; `tools/art/pack_sheet.py` replace une planche générée selon ces règles. Chaque entrée de `enemies` (six au plus) choisit une ligne de la planche et un comportement :

| `role` | Comportement |
|---|---|
| `rusher` | Fonce au contact et frappe. |
| `thrower` | Garde ses distances et lance des cocktails Molotov. |
| `shield` | Lent et résistant, frappe au contact, recule peu sous les tirs. |
| `pistol` | Tireur rapide : vise, puis tire une balle. |
| `smg` | Reste à distance : vise, puis tire une rafale de trois balles. |
| `shotgun` | Lent et résistant : s’approche, vise, puis tire trois plombs de courte portée. |

Les ennemis armés tirent à l’horizontale depuis `muzzle`, le bout du canon dans l’image d’attaque, mesuré en unités depuis les pieds (environ 2 unités pour la hauteur d’un personnage). `pack_sheet.py` affiche ces valeurs.

Exemple de boss, chaque champ est facultatif :

```json
"boss": {
  "enabled": true,
  "name": "MACRON",
  "sprite": "Art/macron-helicopter",
  "intro": "BOSS : MACRON EN HÉLICO !",
  "rage": "MACRON S'EMBALLE !",
  "defeat": "HÉLICO HORS SERVICE !",
  "health": 1
}
```

| Champ | Utilisation |
|---|---|
| `enabled` | `false` retire le boss : la mission est gagnée quand toutes les vagues sont vaincues et que `exitX` est atteint. |
| `kind` | `helicopter` par défaut, ou `mecha` pour le robot au sol de Tokyo. |
| `name` | Nom affiché au-dessus de la barre de vie, 30 caractères au plus. |
| `sprite` | Image du boss. |
| `intro`, `rage`, `defeat` | Annonces à l’arrivée du boss, à mi-vie et à sa chute. |
| `health` | Multiplicateur de points de vie, de `0.25` à `4`. |

L’hélicoptère vole, mitraille et largue des bombes, avec un rotor animé. Une autre image doit reprendre sa mise en page : vue de profil tournée vers la gauche, environ trois fois plus large que haute, sans pales de rotor principal.

Le mécha marche vers les héros et alterne gatling en balayage, piétinement avec deux ondes de choc et salve de missiles. Son image est une planche de six cases, trois par ligne, tournée vers la gauche : attente, deux pas, jambe levée, gatling pointée, lance-missiles ouverts ; `pack_sheet.py mecha` la met en place. La position du canon et des lance-missiles est celle du Mécha Shogun. Un boss au comportement différent exige une modification du moteur.

## 2. Ajouter les décors

Placer les PNG dans `Assets/Resources/Art/`, éventuellement dans un sous-dossier propre à la mission. Par exemple, `Assets/Resources/Art/ma-mission/quais.png` devient `Art/ma-mission/quais` dans le JSON.

Les images doivent former une fresque de hauteur cohérente, sans personnages ni interface. La ligne de sol se trouve à 80 % de la hauteur depuis le haut. Préférer un panorama large ou plusieurs portions contiguës. Un panorama découpé sans perte permet des raccords exacts, comme pour la mission de Paris.

Le moteur affiche chaque portion à treize unités de hauteur et les accole en fonction de leur largeur. Il calcule ensuite le défilement en parallaxe pour couvrir le parcours. Les proportions peuvent s’adapter sur un écran très large.

Pour `ground`, le moteur répète les 20 % inférieurs de l’image le long du parcours, devant le décor. Les musiques se placent dans `Assets/Resources/Audio/`, en WAV, OGG ou MP3, et doivent boucler proprement.

## 3. Vérifier et proposer

Exécuter les commandes Python du README. Dans Unity, tester la mission, ses vagues, sa sortie et les raccords de décor avec un puis plusieurs joueurs. Pour l’ouvrir sans jouer les précédentes, saisir son code avec **CODE MISSION** ; les flèches du menu, ou les boutons supérieurs gauche/droite de la première manette, passent ensuite d’une mission débloquée à l’autre.

Proposer le JSON, les images et leurs fichiers `.meta` dans une pull request avec des captures. Une fois la proposition fusionnée, la compilation web suivante inclut automatiquement la mission.
