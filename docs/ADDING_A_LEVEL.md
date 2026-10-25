# Ajouter une mission

**Action Legends** est le jeu. **Panique à Paname** est la première mission, définie dans `Assets/Resources/Levels/panique-a-paname.json`.

## 1. Créer le JSON

Dupliquer ce fichier dans le même dossier. Lui donner un nom unique en minuscules séparées par des tirets, identique au champ `id`. Le jeu charge automatiquement toutes les missions valides de ce dossier : il n’est pas nécessaire de modifier un catalogue à la main ni d’ajouter une nouvelle scène Unity.

| Champ | Utilisation |
|---|---|
| `schemaVersion` | Garder `1`. |
| `id` | Identifiant unique, identique au nom du fichier sans `.json`. |
| `title` | Nom affiché dans le menu. |
| `location` | Ville, lieu ou époque affichés dans la fiche de mission. |
| `intro`, `exitLabel`, `winTitle` | Messages de début, de sortie et de victoire. |
| `backgrounds` | Images de décor dans leur ordre horizontal, chemins Resources sans extension. |
| `length`, `exitX` | Longueur de la mission et position à atteindre après les vagues. |
| `cameraMin`, `cameraMax` | Bornes horizontales de la caméra. |
| `waves` | Vagues triées par position `atX`. |
| `ground` | Facultatif. Image dont le bas sert de chaussée au premier plan. Par défaut : `Art/paris`. |
| `music`, `bossMusic` | Facultatifs. Musique de la mission et musique du combat final, chemins Resources sans extension. Par défaut : `Audio/Music/marseillaise-arcade` et `Audio/Music/marseillaise-boss`. |
| `boss` | Facultatif. Réglages du boss final, détaillés plus bas. Sans ce champ, la mission se termine par l’hélicoptère de Panique à Paname. |

Exemple de vague :

```json
{
  "atX": 18,
  "baseEnemies": 7,
  "enemiesPerPlayer": 3,
  "maxAlive": 10,
  "spawnInterval": 0.65,
  "enemyPattern": [0, 1, 2]
}
```

Le total de la vague vaut `baseEnemies + nombre de joueurs × enemiesPerPlayer`. `maxAlive` limite le nombre d’ennemis présents simultanément ; `spawnInterval` est l’intervalle entre les apparitions.

Les types disponibles sont `0` : fonceur, `1` : lanceur et `2` : costaud au bouclier. Leur ordre suit `enemyPattern`. Ajouter un nouveau type d’ennemi exige une modification du moteur.

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
| `name` | Nom affiché au-dessus de la barre de vie, 30 caractères au plus. |
| `sprite` | Image du boss. |
| `intro`, `rage`, `defeat` | Annonces à l’arrivée du boss, à mi-vie et à sa chute. |
| `health` | Multiplicateur de points de vie, de `0.25` à `4`. |

Le boss garde le comportement de l’hélicoptère : vol, rafales, bombes et rotor animé. Une autre image doit donc reprendre sa mise en page : vue de profil tournée vers la gauche, environ trois fois plus large que haute, sans pales de rotor principal. Les bombes restent celles de l’hélicoptère. Un boss au comportement différent exige une modification du moteur.

## 2. Ajouter les décors

Placer les PNG dans `Assets/Resources/Art/`, éventuellement dans un sous-dossier propre à la mission. Par exemple, `Assets/Resources/Art/ma-mission/quais.png` devient `Art/ma-mission/quais` dans le JSON.

Les images doivent former une fresque de hauteur cohérente, sans personnages ni interface. La ligne de sol se trouve à 80 % de la hauteur depuis le haut. Préférer un panorama large ou plusieurs portions contiguës. Un panorama découpé sans perte permet des raccords exacts, comme pour la mission de Paris.

Le moteur affiche chaque portion à treize unités de hauteur et les accole en fonction de leur largeur. Il calcule ensuite le défilement en parallaxe pour couvrir le parcours. Les proportions peuvent s’adapter sur un écran très large.

Pour `ground`, le moteur répète les 20 % inférieurs de l’image le long du parcours, devant le décor. Les musiques se placent dans `Assets/Resources/Audio/`, en WAV, OGG ou MP3, et doivent boucler proprement.

## 3. Vérifier et proposer

Exécuter les commandes Python du README. Dans Unity, tester la mission, ses vagues, sa sortie et les raccords de décor avec un puis plusieurs joueurs. Utiliser les flèches du menu ou les touches gauche/droite pour changer de mission ; avec la première manette, utiliser les boutons supérieurs gauche/droite.

Proposer le JSON, les images et leurs fichiers `.meta` dans une pull request avec des captures. Une fois la proposition fusionnée, la compilation web suivante inclut automatiquement la mission.
