# Jeu en ligne

Jusqu’à quatre joueurs, chacun sur son navigateur, sans serveur de jeu.

## Pour les joueurs

1. Sur la page du jeu, choisir un pseudo puis **Créer une partie en ligne**.
2. Copier le lien d’invitation affiché sous le jeu et l’envoyer à ses amis.
3. Ils ouvrent le lien, choisissent leur pseudo et cliquent sur **Rejoindre la partie**.
4. Chacun choisit son héros. L’hôte choisit la mission et la difficulté, puis déploie l’équipe.

On rejoint uniquement depuis le salon : un joueur qui arrive pendant une mission doit attendre le retour au menu. L’hôte doit garder son onglet visible ; un onglet en arrière-plan met le jeu en pause pour tout le monde. Si l’hôte quitte, les autres joueurs peuvent continuer en local.

Sur certains réseaux très fermés (entreprise, certains forfaits mobiles), la connexion directe entre navigateurs peut échouer : aucun relais n’est configuré pour l’instant.

## Fonctionnement

- [Trystero](https://github.com/dmotz/trystero) (licence MIT, inclus dans `Assets/WebGLTemplates/ActionLegends/trystero.js`) met les navigateurs en relation par des relais Nostr publics, puis les connecte directement en WebRTC. Le code de la partie sert aussi de mot de passe de mise en relation.
- `net.js` est un simple tuyau entre Trystero et Unity ; `Assets/Plugins/WebGL/ActionLegendsNet.jslib` fait le pont côté Unity.
- `Assets/Scripts/PanameOnline.cs` porte le protocole. L’hôte rassemble à chaque pas de 1/60 s les commandes de tous les joueurs (déplacement, tir, visée haut et bas, sauts, spéciaux et grenades comptés pour n’en perdre aucun) et diffuse ces « images de commandes ». Chaque navigateur exécute la même simulation à partir d’elles.
- Aucun état de jeu n’est envoyé : un nouveau héros, ennemi, bonus ou boss fonctionne en ligne sans code réseau supplémentaire.
- Toutes les secondes, chaque navigateur calcule une empreinte de l’état (score, positions, points de vie…). L’hôte les compare ; une différence affiche « Désynchronisation détectée ».
- Les joueurs doivent avoir exactement la même version du jeu (contrôlée à la connexion).

## Règle pour faire évoluer le jeu

La simulation (`TickSimulation` et tout ce qu’elle appelle) doit donner exactement le même résultat dans chaque navigateur. Dans ce code :

- ne pas lire `Time.deltaTime`, `Time.time` ni l’horloge : utiliser le `dt` reçu ;
- ne pas utiliser `UnityEngine.Random` : utiliser un `System.Random` à graine fixe, réinitialisé en début de mission ;
- ne pas dépendre de la taille de la fenêtre (`gameCamera.aspect`, `Screen`) : utiliser `SimHalfWidth` ;
- ne lire aucune entrée directement : passer par `PlayerCommand`, qui est la seule donnée échangée ;
- pas d’ordre d’itération non garanti (`Dictionary`, `HashSet`) pour une décision de jeu.

Les effets visuels et sonores peuvent ignorer ces règles tant qu’ils n’influencent pas la partie. Une nouvelle commande de joueur s’ajoute dans `PlayerCommand`, `LocalControls`, puis dans les bits partagés par le replay et le jeu en ligne (`HeldMask`, `FlagMask`, `HeldFlags`, `Command` dans `PanameReplay.cs`) ; une commande d’appui (comme saut, spécial ou grenade) a aussi son compteur dans le message d’entrée en ligne (`GuestInput`, `ParseInput` dans `PanameOnline.cs`).
