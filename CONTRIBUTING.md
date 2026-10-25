# Contribuer à Action Legends

Le dépôt reste privé pour le moment. Ces règles préparent son futur fonctionnement communautaire.

## Proposer une modification

Créer une branche, faire une modification ciblée et ouvrir une pull request. Décrire le résultat attendu et les étapes pour le vérifier. Pour une modification visuelle, joindre une capture. Les contributions sont intégrées après revue du mainteneur.

Pour une nouvelle mission, consulter [le guide des niveaux](docs/ADDING_A_LEVEL.md). Pour le moteur, ouvrir le projet dans Unity 6000.4.7f1 et vérifier le mode Play et l’export Web. Maintenir les commandes de joueur séparées de la logique de simulation.

## Vérifications

Lancer les deux commandes Python du README. Le workflow GitHub exécute les mêmes contrôles. Les compilations Unity ne tournent sur GitHub qu’après configuration de leur activation et ne reçoivent pas les secrets du mainteneur depuis une pull request.

Ne pas ajouter `Library`, `Temp`, `Logs`, `UserSettings`, `WebBuild`, licences Unity ou fichiers d’identifiants au dépôt. Les fichiers `.meta` créés par Unity pour les nouveaux assets doivent accompagner ces assets.

## Images et missions

Conserver le style arcade et des silhouettes lisibles. Les décors s’enchaînent horizontalement avec une ligne de sol cohérente. Le moteur gère le défilement en parallaxe et adapte sa couverture au cadrage.

N’ajouter que des contenus que vous avez le droit de partager. Les conditions de redistribution du projet seront précisées avant l’ouverture publique du dépôt.
