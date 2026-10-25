# Web et GitHub

## Ce qui fonctionne sans backend

L’export Unity contient HTML, JavaScript, WebAssembly, images et données. GitHub Pages ou n’importe quel hébergement statique peut les servir. La coopération se joue sur le même écran ou en ligne : les navigateurs se connectent directement entre eux, sans serveur multijoueur (voir [le jeu en ligne](ONLINE.md)).

Le template `Assets/WebGLTemplates/ActionLegends` propose une page de lancement, une barre de chargement, le plein écran et les commandes. Les chemins sont relatifs afin de fonctionner aussi sous `/action-legends/` sur GitHub Pages.

L’export utilise gzip avec décompression de secours intégrée : il ne dépend pas d’une configuration spéciale des en-têtes du serveur. Le moteur est configuré sans threads WebAssembly pour ne pas exiger les en-têtes d’isolation croisée. Passer dans un autre onglet met une partie locale en pause.

## Construire localement

1. Installer Unity 6000.4.7f1 et Web Build Support.
2. Arrêter Play, puis activer la plateforme Web dans Build Profiles.
3. Utiliser **Action Legends → Construire la version navigateur**.
4. Servir `WebBuild` avec la commande du README, puis ouvrir l’adresse locale.

En automatisation, l’entrée de compilation est `ActionLegends.Editor.WebBuilder.Build`. Elle valide les missions et génère `WebBuild/`, `.nojekyll` et `build-info.json`.

## État actuel de GitHub Actions

Le workflow `.github/workflows/community.yml` prévoit trois étapes :

1. **Validate missions** : automatique sur les propositions et les modifications de `main`, sans secret Unity.
2. **Build browser game** : seulement sur `main` ou un lancement manuel, lorsque la variable de dépôt `UNITY_CI_ENABLED` vaut `true`.
3. **Publish GitHub Pages** : seulement après une compilation réussie et lorsque `PUBLISH_PAGES` vaut `true`.

`UNITY_CI_ENABLED` et `PUBLISH_PAGES` valent `true` : chaque modification de `main` est compilée puis publiée. La compilation ne s’exécute pas avec les secrets Unity pour les pull requests ; celles-ci passent par la validation et la revue avant d’être intégrées.

## Configuration de la compilation GitHub

GameCI requiert une activation Unity adaptée au compte utilisé pour compiler. Pour une licence Personal, sa documentation décrit les secrets `UNITY_LICENSE`, `UNITY_EMAIL` et `UNITY_PASSWORD`. Ils doivent être ajoutés depuis les paramètres GitHub Actions du dépôt, jamais au code, à un ticket ou à une conversation publique. L’assistant n’a ni extrait ni envoyé ces identifiants.

Ces secrets sont configurés et `UNITY_CI_ENABLED` vaut `true`. Passer la variable à `false` suspend la compilation, par exemple le temps de renouveler la licence. Une compilation réussie fournit l’archive `action-legends-web` dans les artifacts GitHub Actions, même sans site publié.

Références : [activation GameCI](https://game.ci/docs/github/activation/), [action Unity Builder](https://github.com/game-ci/unity-builder).

## GitHub Pages

Le dépôt est public et GitHub Pages est activé avec GitHub Actions comme source. Le jeu est servi à l’adresse <https://eth3rnit3.github.io/action-legends/>. Le site est public.

Après chaque modification de `main` et une compilation réussie, l’export remplace automatiquement la version publiée. Un nouveau push sur `main` n’interrompt pas une publication en cours ; seules les vérifications de pull requests sont annulées lorsqu’une version plus récente arrive.

Références : [GitHub Pages](https://docs.github.com/en/pages/getting-started-with-github-pages/what-is-github-pages), [visibilité d’un site](https://docs.github.com/en/enterprise-cloud@latest/pages/getting-started-with-github-pages/changing-the-visibility-of-your-github-pages-site), [décompression Unity Web](https://docs.unity3d.com/6000.4/Documentation/Manual/webgl-deploying.html).
