# Aegis PC Monitor

**Votre PC, en clair.** Monitoring Windows gratuit et open source. Sans compte, publicité, paiement ni télémétrie applicative.

**0.2.0-beta.2 · Windows 10/11 x64 · Bêta publique non signée.**

[Télécharger](https://github.com/ZerionIA/aegis-pc/releases) · [Aide et problèmes](https://github.com/ZerionIA/aegis-pc/issues) · [Confidentialité](PRIVACY.md)

## Fonctions

- Mesures réelles à environ 1 Hz : CPU, RAM, GPU et capteurs disponibles, réseau et occupation des volumes. Une mesure absente reste absente.
- Courbes sur deux minutes ; panneau compact de bureau avec option toujours au premier plan.
- Alertes à seuils personnels CPU/RAM/température après trois mesures consécutives. Historique récent de 50 événements et marqueurs en session. Ces seuils ne sont pas des limites constructeur.
- Sessions de 28 800 échantillons maximum (environ huit heures), sauvegarde intermédiaire toutes les 60 secondes après une première sauvegarde vers dix secondes.
- Comparaison descriptive, CSV protégé contre les formules et rapports HTML autonomes.
- PresentMon facultatif : cadence des présentations, FPS moyens, 1% low et p99. Ces données ne certifient pas les images affichées ni la cause des saccades.
- Bilan calculé sans IA ; dialogue Qwen, transcription Whisper, voix Windows et avatar 3D facultatifs.

## Installation

Télécharger Aegis-0.2.0-beta.2-Setup.exe depuis une GitHub Release. Le runtime .NET est inclus. Installation par utilisateur sans élévation automatique. Fermer Aegis avant mise à jour ou désinstallation. Les données restent dans %LOCALAPPDATA%/AegisNext après désinstallation.

Bêta non signée : Windows peut afficher un avertissement. Vérifier la provenance et SHA256SUMS.txt. Aegis ne demande pas de désactiver une protection Windows. Le panneau de bureau peut être masqué par le plein écran exclusif ; ce n'est pas un overlay injecté.

## IA et voix

Aucun moteur ou modèle IA/voix dans le paquet de base. Réglages propose Ollama CPU, Qwen3.5 2B (~2,7 Go), whisper.cpp et le modèle vocal base (~148 Mo), avec progression et annulation. Fichiers vérifiés par SHA-256 ; couches du modèle vérifiées par Ollama. Le tag du modèle peut évoluer sur son registre.

Internet est nécessaire aux téléchargements, puis les analyses restent locales. Le moteur Aegis utilise 127.0.0.1:11539 ; un Ollama externe local peut être utilisé. Aucun fournisseur cloud d'IA n'est appelé. Les opérations IA/voix et téléchargements sont suspendus ou refusés pendant la capture. Le modèle sélectionne des observations calculées et ne modifie pas les réglages matériels. La suppression des composants n'est pas encore dans l'interface.

## Limites

Capteurs et ETW dépendent du matériel, des pilotes et des droits. Aucun groupe Windows ni anti-cheat n'est modifié. Capture de jeu réelle sous anti-cheat, microphone physique et configurations multi-GPU restent à tester. Une fixture CSV ne prouve pas une capture de jeu réelle.

Pas d'overclocking, ventilateurs, stress test, surveillance distante ou mise à jour automatique. Une base saine en lecture seule est préservée et l'erreur remontée. Seules les erreurs de corruption ou de format SQLite déclenchent une quarantaine conservant les fichiers.

## Construire

Windows x64, SDK .NET 10.0.401, NSIS 3 pour l'installateur.

    ./scripts/build.ps1
    ./scripts/package.ps1 -Dotnet dotnet -Makensis 'C:/Program Files (x86)/NSIS/makensis.exe'

Le packaging utilise un nouveau dossier sous artifacts/, publie Desktop et Collector, copie les notices suivies dans third-party/, télécharge PresentMon 2.6.0 avec empreinte figée et compile NSIS. Il ne réutilise pas le portable historique. Pour répéter : -Output artifacts/release-2. Aucun modèle IA nécessaire. NuGet utilise les fichiers de verrouillage.

## Architecture

Domain : mesures/statistiques/seuils. Application : contrats/sessions. Infrastructure : SQLite/exports/PresentMon/Ollama. Collector : processus séparé et pipe réservé à l'utilisateur. Desktop : WPF/MVVM/panneau/voix. Tests : calculs, formats, persistance et régressions.

[Contribuer](CONTRIBUTING.md) · [Sécurité](SECURITY.md) · [Validation](VALIDATION.md) · [Comparatif](docs/COMPARATIF.md)

## Licence

Code Aegis sous [MIT](LICENSE), copyright ZerionIA et contributeurs. Les dépendances conservent leurs licences : [notices](THIRD-PARTY-NOTICES.md). Les sources MPL non modifiées sont fournies dans third-party/ et le paquet.

Projet indépendant, sans affiliation à AIDA64, HWiNFO, OCCT, Microsoft, Intel, AMD ou NVIDIA. Aucun code, visuel ou binaire propriétaire des concurrents n'est intégré. Pas de garantie de compatibilité universelle ni de diagnostic professionnel.
