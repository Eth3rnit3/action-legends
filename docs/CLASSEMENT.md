# Classement mondial

Le jeu publie et lit les scores sur des **relais Nostr publics**, sans serveur de jeu ni compte joueur. La sécurité ne vient pas des relais, que n’importe qui peut alimenter, mais du **replay** : chaque score transporte les entrées de la partie, et chaque navigateur la rejoue avant de l’afficher.

## Côté joueur

- À la fin d’une mission, gagnée ou perdue, le jeu rejoue d’abord la partie en arrière-plan (« VÉRIFICATION DU REPLAY »). S’il retrouve le même score, **PUBLIER MON SCORE** devient disponible.
- Le score est signé avec une clé Nostr créée pour ce navigateur et gardée dans son stockage local. Il est publié sous le pseudo saisi sur la page d’accueil, suivi de « J2 », « J3 »… pour les autres joueurs locaux ; en ligne, sous les pseudos de l’équipe. Seul l’hôte publie pour une partie en ligne.
- Chaque navigateur garde **un score par classement** : publier un meilleur score remplace l’ancien, un score inférieur n’est pas envoyé.
- **CLASSEMENT**, au menu, affiche le top 10 par mission et par difficulté. Les scores sont triés, puis rejoués un par un dans le navigateur ; seuls ceux qui se reproduisent à l’identique sont affichés. Le verdict est mémorisé pour chaque version du jeu.
- Une partie rejointe par une manette en cours de mission n’est pas publiable : l’arrivée du joueur ne fait pas partie du replay.

## Fonctionnement

- **Simulation à pas fixes.** Depuis la version 0.7.0, le jeu local avance par pas de 1/60 s, comme le jeu en ligne, au lieu de suivre la durée de chaque image. Le stick est arrondi au dixième. Sur un écran à 120 ou 144 Hz, certaines images ne font donc pas avancer le jeu, exactement comme en ligne.
- **Replay** (`Assets/Scripts/PanameReplay.cs`) : pour chaque pas, le déplacement et les boutons de chaque joueur, compressés en suites de pas identiques, plus le score, la durée, le nombre de pas et une empreinte de l’état final. Une partie de quelques minutes tient en quelques kilo-octets avant compression.
- **Vérification** : un second `PanameGame`, inactif et invisible, rejoue la partie quatre millisecondes par image, jamais pendant une mission. Il doit finir sur le même nombre de pas, le même score, la même empreinte et la même issue. Il ne débloque aucune mission.
- **Publication** (`Assets/WebGLTemplates/ActionLegends/scores.js`, `nostr-tools.js`) : un événement Nostr de type 30078 (données d’application, NIP-78), remplaçable, avec l’étiquette `d` = `action-legends/<mission>/<difficulté>`. Le replay est compressé (`deflate`) dans le contenu.
- **Relais** : `relay.damus.io`, `nos.lol`, `relay.primal.net` et `nostr.mom`. Le jeu publie sur tous et lit tous, avec un délai maximal de sept secondes. Pour tester sans toucher aux relais publics : `?relais=ws://127.0.0.1:7777` dans l’adresse de la page.

## Limites

- Un replay ne se rejoue qu’avec la même simulation. Quand une version change le gameplay, les anciens scores ne se reproduisent plus et disparaissent du classement ; ils réapparaissent si la simulation redevient identique. Les verdicts sont mémorisés par version.
- Un replay vérifié prouve que la partie a été jouée avec ces entrées, pas qu’un humain les a saisies : un programme qui joue à la place du joueur reste possible.
- Les relais publics ne garantissent ni la conservation ni la disponibilité. Un afflux de faux scores en tête de classement peut ralentir l’affichage : le navigateur vérifie au plus 25 replays par ouverture du classement.
- Les pseudos et les parties publiés sont publics et ne peuvent pas être effacés de façon garantie.

## Vérifier sans navigateur

`AgentScripts/VerifyReplays.cs` enregistre des parties scriptées à un, deux et quatre joueurs, les fait passer par JSON comme la page, puis les rejoue : la même partie doit être acceptée, un score gonflé ou des entrées modifiées refusés.
