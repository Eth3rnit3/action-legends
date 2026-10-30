# Version 0.9.1 — Prêts à partir

Nouveautés pour le joueur depuis la version 0.9.0. Les commandes complètes sont dans [LISEZ-MOI.md](../LISEZ-MOI.md).

## Le menu

- **Chaque mission a son logo.** Panique à Paname, Mistral Gagnant et Tokyo Néon ont désormais un logo illustré dans leur fiche, comme Prophétie en Provence.
- **Le joueur 1 à la manette.** La première manette sur laquelle on appuie sur **A / croix** devient celle du joueur 1, qui garde aussi le clavier. Les manettes suivantes ajoutent les joueurs 2 à 4. Pour rester au clavier et donner l'unique manette à un ami, celui-ci appuie sur **Y / triangle** : il rejoint comme nouveau joueur. Si la manette du joueur 1 se débranche au menu, il reste au clavier.
- **Se déclarer prêt.** À plusieurs, la mission ne part plus sur la touche d'un seul joueur. Chacun se déclare prêt :
  - **Entrée** pour J1 ;
  - **A / croix** ou **Start** sur sa manette ;
  - le bouton **PRÊT ?** de sa carte ;
  - le grand bouton du menu, qui compte les joueurs prêts.

  Un tampon PRÊT s'affiche sur sa carte, et la même touche le retire. La mission part dès que toute l'équipe est prête. Changer de héros retire le « prêt » de ce joueur. Changer de mission ou de difficulté le retire à tous. Seul, une pression lance toujours la mission.
- En ligne, chaque joueur se déclare prêt depuis son navigateur, et l'hôte ne lance plus la mission à la place des autres.

## Les décors

- **Le décor ne glisse plus.** Les objets posés dans la rue (voitures, caisses, fontaines, arbres) restent à leur place sur le sol quand on court. Les panoramas défilent toujours plus lentement, derrière eux.
- **Des panoramas plus longs.** Cinq nouveaux décors allongent les villes : l'Opéra, le Marais et Montmartre à Paris, Noailles à Marseille, Asakusa à Tokyo. Les raccords entre deux panoramas sont cachés par un arbre, un kiosque, une fontaine ou un poteau de néons, selon la ville.
- **Les fenêtres.** Elles sont plus grandes. Un ennemi posté à une fenêtre la garde ouverte autour de lui tant qu'il vit : il ne flotte plus devant la façade.

## Classement

- **Publication automatique.** Le score est publié tout seul à la fin d'une mission, une fois la partie rejouée à l'identique : plus de bouton à ne pas oublier.
- **Nouveau classement.** Chaque version a son propre classement, et celui de la 0.9.1 repart de zéro. Voir [CLASSEMENT.md](CLASSEMENT.md).

## Jeu en ligne

Tous les joueurs doivent avoir la version 0.9.1 : la version est contrôlée à la connexion.

## Pour qui fait des missions

- **`map.joins`** : des éléments posés sur les raccords entre deux panoramas. Ils sont décrits dans [ADDING_A_LEVEL.md](ADDING_A_LEVEL.md).
- **`map.groundTop`** : la chaussée commune descend jusqu'au bas de l'écran.
