# Découverte et téléchargement

Mise à jour du 26 octobre 2026. Site officiel : https://zerionia.com/apps/aegis

## Éléments publiés

- Présentation et guide en anglais dans la rubrique Apps du site Zerionia. L’application inclut les packs français et anglais.
- Contenu HTML, URL canonique, métadonnées Open Graph et X, visuel original en anglais.
- Boutons vers /apps/aegis/download, redirection vers l’installateur de la release GitHub officielle. Le binaire reste distribué par GitHub.
- Guide : https://zerionia.com/apps/aegis#installation ; prérequis, permissions, choix de langue, limites et FAQ.
- Données structurées SoftwareApplication et Article : gratuit, Windows, version, langues de l’app, téléchargement et licence. Aucun avis, note ou nombre d’utilisateurs inventé.
- Page ajoutée au sitemap de zerionia.com. Le point de téléchargement porte noindex.
- Liens publics GitHub vers la présentation sur Zerionia.

## Indexation

Ces éléments facilitent l’exploration ; ils ne prouvent pas une indexation ou un classement. Aucune soumission Google Search Console n’est revendiquée ici. Utiliser la propriété zerionia.com du responsable du site, soumettre https://zerionia.com/sitemap.xml et inspecter /apps/aegis.

## À chaque release

Le site lit automatiquement les releases publiques du dépôt officiel. Une release doit avoir un tag vMAJOR.MINOR.PATCH ou vMAJOR.MINOR.PATCH-beta.N, son installateur Aegis-VERSION-Setup.exe, une taille valide et le digest SHA-256 fourni par GitHub. Brouillons, releases incomplètes et URL étrangères sont ignorés. La page, les données structurées, la taille, l’empreinte et les boutons suivent automatiquement, avec un délai de cache habituel de cinq à dix minutes après publication et consultation. Un commit de code seul ne suffit pas.

Le bouton présent sur la page est lié à la version affichée afin de ne pas mélanger une empreinte ancienne et un nouveau fichier. Le lien stable /apps/aegis/download, aussi utilisé dans le README GitHub, résout la release la plus récente. Le snapshot dans src/lib/data/aegis.ts est une version de secours vérifiée, pas le processus normal de mise à jour.

Le guide doit rester cohérent avec les fonctions de l’app. Ne pas modifier un installateur déjà publié.
