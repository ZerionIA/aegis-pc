# Aegis PC Monitor

**Votre PC, en clair.** Monitoring Windows gratuit et open source. Sans compte, publicité, paiement ni télémétrie applicative.

**0.3.0-beta.1 · Windows 10/11 x64 · Bêta publique non signée.**

[Télécharger](https://github.com/ZerionIA/aegis-pc/releases) · [Aide et problèmes](https://github.com/ZerionIA/aegis-pc/issues) · [Confidentialité](PRIVACY.md)

## Fonctions

- Mesures réelles à environ 1 Hz : CPU, RAM, GPU et capteurs disponibles, réseau et occupation des volumes. Une mesure absente reste absente.
- Courbes sur deux minutes ; panneau compact de bureau avec option toujours au premier plan.
- Alertes à seuils personnels CPU/RAM/température après trois mesures consécutives. Historique récent de 50 événements et marqueurs en session. Ces seuils ne sont pas des limites constructeur.
- Sessions de 28 800 échantillons maximum (environ huit heures), sauvegarde intermédiaire toutes les 60 secondes après une première sauvegarde vers dix secondes.
- Comparaison descriptive, CSV protégé contre les formules et rapports HTML autonomes.
- PresentMon facultatif : cadence des présentations, FPS moyens, 1% low et p99. Ces données ne certifient pas les images affichées ni la cause des saccades.
- Bilans calculés, sans assistant IA, modèle, microphone ou moteur vocal.

## Installation

Télécharger Aegis-0.3.0-beta.1-Setup.exe depuis une GitHub Release. Le runtime .NET est inclus. Installation par utilisateur sans élévation automatique. Fermer Aegis avant mise à jour ou désinstallation. Les données restent dans %LOCALAPPDATA%/AegisNext après désinstallation.

Bêta non signée : Windows peut afficher un avertissement. Vérifier la provenance et SHA256SUMS.txt. Aegis ne demande pas de désactiver une protection Windows. Le panneau de bureau peut être masqué par le plein écran exclusif ; ce n'est pas un overlay injecté.

## Mises à jour et identité

Les nouvelles releases du dépôt officiel sont vérifiées au plus une fois par jour quand l'application est ouverte. Le téléchargement se fait automatiquement hors session ; l'installation demande un clic sur « Installer et redémarrer ». Un téléchargement est suspendu au démarrage d'une capture. Ces options sont désactivables dans Réglages.

L'intégrité est vérifiée avec le SHA-256 fourni par GitHub et une source HTTPS fixe. Ce mécanisme n'est pas une signature Authenticode ; la bêta reste non signée. La version 0.2 doit être remplacée une première fois par l'installateur 0.3 pour bénéficier de cette fonction. Les anciennes sessions restent compatibles. Un commit seul ne publie pas de mise à jour : voir [le guide de release](docs/RELEASE.md).

Thèmes Forêt, Minuit et Clair. Une page Zerionia donne accès au studio, à [@ZerioniaHQ](https://x.com/ZerioniaHQ), au GitHub et au contact public contact@zerionia.com. Aucun lien ni encart promotionnel ne s'ouvre automatiquement.

L'assistant et ses dépendances ont été retirés. Les modèles téléchargés par une ancienne version dans le dossier de données ne sont pas supprimés automatiquement pour préserver les fichiers de l'utilisateur. Ils ne sont plus lus par Aegis.

## Limites

Capteurs et ETW dépendent du matériel, des pilotes et des droits. Aucun groupe Windows ni anti-cheat n'est modifié. Capture de jeu réelle sous anti-cheat, microphone physique et configurations multi-GPU restent à tester. Une fixture CSV ne prouve pas une capture de jeu réelle.

Pas d'overclocking, ventilateurs, stress test, surveillance distante ni synchronisation cloud. Une base saine en lecture seule est préservée et l'erreur remontée. Seules les erreurs de corruption ou de format SQLite déclenchent une quarantaine conservant les fichiers.

## Construire

Windows x64, SDK .NET 10.0.401, NSIS 3 pour l'installateur.

    ./scripts/build.ps1
    ./scripts/package.ps1 -Dotnet dotnet -Makensis 'C:/Program Files (x86)/NSIS/makensis.exe'

Le packaging utilise un nouveau dossier sous artifacts/, publie Desktop et Collector, copie les notices suivies dans third-party/, télécharge PresentMon 2.6.0 avec empreinte figée et compile NSIS. Il ne réutilise pas le portable historique. Pour répéter : -Output artifacts/release-2. Aucun moteur IA ni modèle n’est utilisé. NuGet utilise les fichiers de verrouillage.

## Architecture

Domain : mesures/statistiques/seuils. Application : contrats/sessions. Infrastructure : SQLite/exports/PresentMon/Ollama. Collector : processus séparé et pipe réservé à l'utilisateur. Desktop : WPF/MVVM/panneau/thèmes. Updater : installation après fermeture. Tests : calculs, formats, persistance et régressions.

[Kit de promotion](marketing/TEXTES.md) · [Contribuer](CONTRIBUTING.md) · [Sécurité](SECURITY.md) · [Validation](VALIDATION.md) · [Comparatif](docs/COMPARATIF.md)

## Licence

Code Aegis sous [MIT](LICENSE), copyright ZerionIA et contributeurs. Les dépendances conservent leurs licences : [notices](THIRD-PARTY-NOTICES.md). Les sources MPL non modifiées sont fournies dans third-party/ et le paquet.

Projet indépendant, sans affiliation à AIDA64, HWiNFO, OCCT, Microsoft, Intel, AMD ou NVIDIA. Aucun code, visuel ou binaire propriétaire des concurrents n'est intégré. Pas de garantie de compatibilité universelle ni de diagnostic professionnel.
