# Publier une mise à jour

1. Modifier le code et les versions VersionPrefix/VersionSuffix de Directory.Build.props.
2. Exécuter scripts/build.ps1 et les scénarios de l'exécutable ; documenter les essais réels.
3. Envoyer le commit sur main. La CI compile et teste ; aucun utilisateur n'est mis à jour à ce stade.
4. Créer et pousser un tag correspondant exactement, par exemple v0.3.0-beta.2.
5. Le workflow release.yml construit le paquet Windows, recalcule son SHA-256 et crée une release brouillon. Il la publie seulement après l'envoi des assets. Les versions -beta.N sont des préreleases.
6. Vérifier la réussite du workflow et le contenu de la release avant communication.

Aegis utilise uniquement les releases publiées de ZerionIA/aegis-pc, un nom de fichier attendu, une version supérieure et le SHA-256 fourni par l'API GitHub. Une version stable ne reçoit pas une bêta. Les versions bêta suivent les versions bêta et stables plus récentes. La vérification et le téléchargement sont automatiques, l'installation est confirmée, jamais lancée au milieu d'une session.

Le programme de mise à jour est copié hors du dossier installé afin de pouvoir remplacer ses propres binaires. Il vérifie à nouveau les métadonnées officielles et l'empreinte, attend la fermeture d'Aegis, lance l'installateur par utilisateur, puis ouvre l'application installée. Les données de session sont conservées.

HTTPS et SHA-256 vérifient l'intégrité via GitHub. Ce n'est pas une signature indépendante de l'éditeur ni Authenticode. Le compte GitHub et les droits de publication doivent donc rester protégés. Aucune clé privée supplémentaire n'est stockée dans le dépôt ou exportée vers GitHub par cette configuration.
