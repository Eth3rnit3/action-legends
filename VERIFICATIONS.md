# Vérifications — Action Legends 0.4

30 contrôles Unity et 8 tests du validateur de missions réussis.

- Le jeu s'appelle Action Legends
- Panique à Paname est une mission du catalogue
- Une mission supplémentaire devient sélectionnable
- Une mission de longueur et de vagues différentes se termine
- Quitter l'onglet peut mettre le jeu en pause
- Une mission invalide est refusée par le moteur
- Adaptateur manette : déplacement et tir
- Quatre joueurs créés
- Saut du joueur 1
- Saut du joueur 2
- Saut du joueur 3
- Saut du joueur 4
- Déplacement et atterrissage J1
- Déplacement et atterrissage J2
- Déplacement et atterrissage J3
- Déplacement et atterrissage J4
- Déclenchement d'une vague
- Projectiles : dégâts et score
- Trois vagues puis arrivée à Bastille
- Retour à la sélection
- Logo et cadres du menu chargés
- Trois quartiers chargés
- Découpes de décor identiques pixel par pixel à la fresque
- Deux raccords géométriques sans trou ni chevauchement
- Panorama couvre tout le parcours et plusieurs formats d'écran
- Atlas des 24 effets chargé
- Rendu des impacts et des effets compilé sans diagnostic
- Un tir déclenche flash blanc, recul et réaction de l'ennemi
- Effets actifs dans un pool borné
- Les effets expirent et leurs objets sont réutilisables

## Version navigateur

Compilation WebGL réussie dans Unity 6000.4.7f1, sans erreur ni avertissement. Chargement réel vérifié dans le navigateur intégré, puis démarrage de mission, déplacement et tir au clavier ; une première vague a été déclenchée et le score a augmenté. Aucune erreur ou alerte dans la console du navigateur pendant cet essai.

## Limites des essais

La progression complète des missions est vérifiée par commandes simulées, avec invulnérabilité pour isoler les vagues de l’équilibrage. Le test manette utilise un périphérique virtuel. Quatre manettes physiques et plusieurs navigateurs restent à tester.

Le dépôt GitHub est privé. Aucun site Pages n’a été activé. La compilation et le déploiement distants sont préparés mais désactivés ; leur fonctionnement sur GitHub n’a pas été testé avec une activation Unity distante.
