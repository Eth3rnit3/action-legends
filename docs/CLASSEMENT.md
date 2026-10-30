# Classement mondial

Le jeu publie et lit les scores sur des **relais Nostr publics**, sans serveur de jeu ni compte joueur. La sécurité ne vient pas des relais, que n’importe qui peut alimenter, mais du **replay** : chaque score transporte les entrées de la partie, et chaque navigateur la rejoue avant de l’afficher.

## Côté joueur

- À la fin d’une mission, gagnée ou perdue, le jeu rejoue d’abord la partie en arrière-plan (« VÉRIFICATION DU REPLAY »). S’il retrouve le même score, le score est publié aussitôt, sans bouton à presser ; en cas d’échec d’envoi, **RÉESSAYER** le renvoie.
- Le score est signé avec une clé Nostr créée pour ce navigateur et gardée dans son stockage local. Il est publié sous le pseudo saisi sur la page d’accueil, suivi de « J2 », « J3 »… pour les autres joueurs locaux ; en ligne, sous les pseudos de l’équipe. Seul l’hôte publie pour une partie en ligne.
- Chaque navigateur garde **un score par classement** : publier un meilleur score remplace l’ancien, un score inférieur n’est pas envoyé.
- **CLASSEMENT**, au menu, affiche le top 10 par mission et par difficulté, y compris le mode **SLUG** (difficulté 4, son propre tableau). Les scores sont triés, puis rejoués un par un dans le navigateur ; seuls ceux qui se reproduisent à l’identique sont affichés. Le verdict est mémorisé pour chaque version du jeu.
- **Un classement par version.** Chaque replay porte la version du jeu qui l’a enregistré (`version`, par exemple `0.9.0`). Le classement n’affiche que les scores de la version en cours, rappelée au bas du tableau : ceux d’une version précédente ne sont ni rejoués ni marqués comme invalides, ils sont simplement laissés de côté (`scores.js` les écarte dès la lecture des relais, `PanameScores.cs` les ignore aussi). Ils ne consomment donc aucune des 25 vérifications, et un score de la version en cours n’est jamais caché derrière d’anciens scores plus hauts. Publier dans une nouvelle version remplace sur les relais l’ancien score de ce navigateur pour ce classement (un événement par clé et par classement).
- Une partie rejointe par une manette en cours de mission n’est pas publiable : l’arrivée du joueur ne fait pas partie du replay.
- **Continues (mode SLUG).** Le choix de continuer est une entrée de joueur (saut ou tir, grenade pour abandonner) : il est dans le replay et le jeu en ligne comme le reste, et le replay compte ses continues (`continues`), vérifiés à la relecture. Pour ne pas comparer une partie qui a acheté sa fin avec des crédits supplémentaires à une partie en un seul crédit, **seules les parties sans continue sont publiées** : après un continue, l’écran de fin l’indique au lieu de **PUBLIER MON SCORE**, et un score publié malgré tout avec des continues n’est jamais affiché.

## Fonctionnement

- **Simulation à pas fixes.** Depuis la version 0.7.0, le jeu local avance par pas de 1/60 s, comme le jeu en ligne, au lieu de suivre la durée de chaque image. Le stick est arrondi au dixième. Sur un écran à 120 ou 144 Hz, certaines images ne font donc pas avancer le jeu, exactement comme en ligne.
- **Replay** (`Assets/Scripts/PanameReplay.cs`) : pour chaque pas, le déplacement et les boutons de chaque joueur, compressés en suites de pas identiques, plus le score, la durée, le nombre de pas et une empreinte de l’état final. Une partie de quelques minutes tient en quelques kilo-octets avant compression.
- **Vérification** : un second `PanameGame`, inactif et invisible, rejoue la partie quatre millisecondes par image, jamais pendant une mission. Il doit finir sur le même nombre de pas, le même score, la même empreinte, la même issue et le même nombre de continues. Il ne débloque aucune mission.
- **Publication** (`Assets/WebGLTemplates/ActionLegends/scores.js`, `nostr-tools.js`) : un événement Nostr de type 30078 (données d’application, NIP-78), remplaçable, avec l’étiquette `d` = `action-legends/<mission>/<difficulté>` (position dans `Content/difficulties` : 0 DÉTENTE … 3 LÉGENDE, 4 SLUG). Le replay est compressé (`deflate`) dans le contenu.
- **Relais** : `relay.damus.io`, `nos.lol`, `relay.primal.net` et `nostr.mom`. Le jeu publie sur tous et lit tous, avec un délai maximal de sept secondes. Pour tester sans toucher aux relais publics : `?relais=ws://127.0.0.1:7777` dans l’adresse de la page.

## Limites

- Un replay ne se rejoue qu’avec la même simulation, et chaque version a son propre classement : à chaque nouvelle version, le classement repart de zéro, même pour un correctif qui ne toucherait pas au gameplay. Les scores publiés en 0.7.0 ne s'affichent plus depuis la 0.8.0 (commandes, armes, otages et missions nouvelles) ; ceux de la 0.8.0 ne s'affichent plus en 0.9.0 (relief, décors destructibles, véhicules, nouveaux héros, équilibrage). Ils restent sur les relais tant qu'ils n'ont pas été remplacés.
- `AgentScripts/VerifyBoardVersions.cs` vérifie qu'un tableau de trente scores 0.8.0 plus hauts (et d'un score sans version) laisse passer et vérifier en premier le score de la version en cours, sans dépenser de vérification pour les autres.
- Un replay vérifié prouve que la partie a été jouée avec ces entrées, pas qu’un humain les a saisies : un programme qui joue à la place du joueur reste possible.
- Les relais publics ne garantissent ni la conservation ni la disponibilité. Un afflux de faux scores en tête de classement peut ralentir l’affichage : le navigateur vérifie au plus 25 replays par ouverture du classement.
- Les pseudos et les parties publiés sont publics et ne peuvent pas être effacés de façon garantie.

## Vérifier sans navigateur

`AgentScripts/VerifyReplays.cs` enregistre des parties scriptées à un, deux et quatre joueurs, les fait passer par JSON comme la page, puis les rejoue : la même partie doit être acceptée, un score gonflé ou des entrées modifiées refusés.
