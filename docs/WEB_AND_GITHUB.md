# Web et GitHub

## Ce qui fonctionne sans backend

L’export Unity contient HTML, JavaScript, WebAssembly, images et données. GitHub Pages ou n’importe quel hébergement statique peut les servir. La coopération à plusieurs sur le même écran reste locale ; aucun serveur multijoueur n’est inclus.

Le template `Assets/WebGLTemplates/ActionLegends` propose une page de lancement, une barre de chargement, le plein écran et les commandes. Les chemins sont relatifs afin de fonctionner aussi sous `/action-legends/` sur GitHub Pages.

L’export utilise gzip avec décompression de secours intégrée : il ne dépend pas d’une configuration spéciale des en-têtes du serveur. Le moteur est configuré sans threads WebAssembly pour ne pas exiger les en-têtes d’isolation croisée. Passer dans un autre onglet met une partie en pause.

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

Les deux variables sont volontairement désactivées par défaut. La compilation ne s’exécute pas avec les secrets Unity pour les pull requests ; celles-ci passent par la validation et la revue avant d’être intégrées.

## Activer plus tard la compilation GitHub

GameCI requiert une activation Unity adaptée au compte utilisé pour compiler. Pour une licence Personal, sa documentation décrit les secrets `UNITY_LICENSE`, `UNITY_EMAIL` et `UNITY_PASSWORD`. Ils doivent être ajoutés depuis les paramètres GitHub Actions du dépôt, jamais au code, à un ticket ou à une conversation publique. L’assistant n’a ni extrait ni envoyé ces identifiants.

Après configuration, définir `UNITY_CI_ENABLED=true` puis lancer le workflow. Une compilation réussie fournit l’archive `action-legends-web` dans les artifacts GitHub Actions, même sans site publié.

Références : [activation GameCI](https://game.ci/docs/github/activation/), [action Unity Builder](https://github.com/game-ci/unity-builder).

## Activer plus tard GitHub Pages

Le dépôt reste privé et aucun site n’est activé. **La confidentialité du dépôt et celle du site sont distinctes** : un site Pages peut être public même si sa source est privée. Pages sur un dépôt privé demande une offre GitHub compatible ; publier un site Pages à accès privé est une fonctionnalité d’organisation Enterprise Cloud.

Lorsque la publication aura été décidée, choisir GitHub Actions comme source Pages, vérifier la visibilité du site et définir `PUBLISH_PAGES=true`. Après une contribution fusionnée et une compilation réussie, l’export remplacera automatiquement la version publiée.

Références : [GitHub Pages](https://docs.github.com/en/pages/getting-started-with-github-pages/what-is-github-pages), [visibilité d’un site](https://docs.github.com/en/enterprise-cloud@latest/pages/getting-started-with-github-pages/changing-the-visibility-of-your-github-pages-site), [décompression Unity Web](https://docs.unity3d.com/6000.4/Documentation/Manual/webgl-deploying.html).
