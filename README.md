# Aegis PC Monitor

**Votre PC, en clair.** Monitoring Windows gratuit et open source. Sans compte, publicité, paiement ni télémétrie applicative.

**0.6.0-beta.1 · Windows 10/11 x64 · Bêta publique non signée.**

[**Télécharger l’installateur Windows (.exe)**](https://github.com/ZerionIA/aegis-pc/releases/download/v0.6.0-beta.1/Aegis-0.6.0-beta.1-Setup.exe) · [Guide d’installation et première session](https://aegis-pc-monitor.raptorafc.chatgpt.site/guide/) · [Site officiel](https://aegis-pc-monitor.raptorafc.chatgpt.site/)

Installateur Windows, runtime et prérequis CPU officiel signés inclus. [Notes de version et SHA-256](https://github.com/ZerionIA/aegis-pc/releases/tag/v0.6.0-beta.1) · [Aide et problèmes](https://github.com/ZerionIA/aegis-pc/issues) · [Confidentialité](PRIVACY.md)

## Installation complète, sans téléchargement manuel

Le paquet contient .NET, PresentMon et l’installateur officiel signé de **PawnIO 2.2.0**. Pendant l’installation, l’option « Capteurs CPU complets » est proposée et explique la confirmation administrateur. Si PawnIO est déjà compatible, il est réutilisé ; une version plus récente n’est pas remplacée par une ancienne. Aegis peut être lancé depuis la fin de l’assistant.

La lecture des températures CPU par ce pilote exige également une confirmation Windows au lancement du **collecteur**, sans élever l’interface. Refuser conserve un mode basique avec un diagnostic explicite. Aucun réglage de sécurité n’est désactivé. Les mises à jour silencieuses n’ajoutent pas de pilote en secret : les utilisateurs existants peuvent terminer cette configuration depuis **Réglages → Capteurs CPU**.

Les sources et licences de PawnIO sont jointes aux notices. Ce pilote peut être partagé par d’autres outils ; désinstaller Aegis ne le retire pas. Les capteurs disponibles dépendent toujours du matériel et des droits ; aucune garantie universelle n’est annoncée.

## Pensé pour jouer

- **HUD discret en jeu** : sans bordure, en barre horizontale, centrée en haut par défaut, CPU/GPU/RAM et températures CPU/GPU disponibles. Clics traversants, sans prise de focus. Position dans les quatre coins ou au centre, taille et opacité réglables.
- **Sessions automatiques** : détection locale de jeux reconnus, dont le processus de partie LoL, et noms d’exécutables personnalisables. Deux observations avant démarrage ; environ 12 secondes d’absence avant arrêt. Un arrêt manuel bloque la reprise jusqu’au prochain lancement.
- **Capteurs classés** par CPU, GPU, RAM, réseau, stockage et système, avec recherche et filtre. Valeurs disponibles d’abord, puis tri température/charge/puissance/fréquence. Groupes repliables sans réinitialisation à chaque mesure.
- **Réglages par onglets** : Jeux, Affichage en jeu, Général, Apparence et langues, Alertes, Mises à jour, Avancé, Capteurs CPU. Packs d’interface français et anglais intégrés, changement immédiat. Les noms de capteurs, de sessions et les détails techniques externes restent natifs.

Pour LoL, utiliser **Sans bordure** ou **Fenêtré**. Le HUD externe peut être masqué en plein écran exclusif ; aucune garantie universelle anti-cheat. Les FPS ne sont pas affichés en direct : les statistiques de présentation sont calculées après capture PresentMon, activable séparément.

## En trois gestes

1. Démarrez une session avant votre jeu ou votre travail.
2. Marquez un moment avec **Ctrl+Alt+F8**, depuis la fenêtre ou le menu de l'icône près de l'horloge.
3. Arrêtez la capture et retrouvez le dernier bilan. Les marqueurs donnent le contexte ; les mesures ne prouvent pas à elles seules une cause.

## Au quotidien

Icône près de l'horloge avec commandes de session, panneau compact et ouverture de l'app. La réduction peut masquer la fenêtre dans cette zone ; la croix quitte réellement l'application après sauvegarde. Une seconde ouverture ramène la fenêtre existante.

Le démarrage discret avec Windows est facultatif : cocher l'option puis Enregistrer. Il utilise uniquement la clé Run du compte utilisateur, sans élévation. Il n'est pas activé à l'installation. Le raccourci est libéré à la fermeture ; un conflit de touche est signalé. Il ne s'agit pas d'une injection dans les jeux.

La bibliothèque propose recherche par nom/date, accès au dernier bilan et signalement des sauvegardes intermédiaires interrompues. Les deux sélections de comparaison sont indépendantes. Jusqu'à 200 sessions récentes sont listées.

Le rapport support est créé localement après choix d'un fichier ; il contient version, OS, architecture et compteurs de disponibilité. Il exclut les noms de sessions, chemins, identifiants et valeurs des capteurs. Relisez-le avant de l'envoyer à contact@zerionia.com. Aucun envoi automatique.

## Fonctions

- Mesures réelles à environ 1 Hz : CPU, RAM, GPU et capteurs disponibles, réseau et occupation des volumes. Une mesure absente reste absente.
- Courbes sur deux minutes ; panneau compact de bureau avec option toujours au premier plan.
- Alertes à seuils personnels CPU/RAM/température après trois mesures consécutives. Historique récent de 50 événements et marqueurs en session. Ces seuils ne sont pas des limites constructeur.
- Sessions de 28 800 échantillons maximum (environ huit heures), sauvegarde intermédiaire toutes les 60 secondes après une première sauvegarde vers dix secondes.
- Comparaison descriptive, CSV protégé contre les formules et rapports HTML autonomes.
- PresentMon facultatif : cadence des présentations, FPS moyens, 1% low et p99. Ces données ne certifient pas les images affichées ni la cause des saccades.
- Bilans calculés, sans assistant IA, modèle, microphone ou moteur vocal.

## Installation

Télécharger Aegis-0.6.0-beta.1-Setup.exe depuis une GitHub Release. Le runtime .NET est inclus. Installation par utilisateur sans élévation automatique. Fermer Aegis avant mise à jour ou désinstallation. Les données restent dans %LOCALAPPDATA%/AegisNext après désinstallation.

Bêta non signée : Windows peut afficher un avertissement. Vérifier la provenance et SHA256SUMS.txt. Aegis ne demande pas de désactiver une protection Windows. Le panneau de bureau peut être masqué par le plein écran exclusif ; ce n'est pas un overlay injecté.

## Mises à jour et identité

Les nouvelles releases du dépôt officiel sont vérifiées au plus une fois par jour quand l'application est ouverte. Le téléchargement se fait automatiquement hors session ; l'installation demande un clic sur « Installer et redémarrer ». Un téléchargement est suspendu au démarrage d'une capture. Ces options sont désactivables dans Réglages.

L'intégrité est vérifiée avec le SHA-256 fourni par GitHub et une source HTTPS fixe. Ce mécanisme n'est pas une signature Authenticode ; la bêta reste non signée. La version 0.2 doit être remplacée une première fois par l’installateur courant pour bénéficier de cette fonction. Les anciennes sessions restent compatibles. Un commit seul ne publie pas de mise à jour : voir [le guide de release](docs/RELEASE.md).

Thèmes Forêt, Minuit et Clair. Une page Zerionia donne accès au studio, à [@ZerioniaHQ](https://x.com/ZerioniaHQ), au GitHub et au contact public contact@zerionia.com. Aucun lien ni encart promotionnel ne s'ouvre automatiquement.

L'assistant et ses dépendances ont été retirés. Les modèles téléchargés par une ancienne version dans le dossier de données ne sont pas supprimés automatiquement pour préserver les fichiers de l'utilisateur. Ils ne sont plus lus par Aegis.

## Limites

Capteurs et ETW dépendent du matériel, des pilotes et des droits. Aucun groupe Windows ni anti-cheat n'est modifié. Capture de jeu réelle sous anti-cheat et configurations multi-GPU restent à tester. Une fixture CSV ne prouve pas une capture de jeu réelle.

Pas d'overclocking, ventilateurs, stress test, surveillance distante ni synchronisation cloud. Une base saine en lecture seule est préservée et l'erreur remontée. Seules les erreurs de corruption ou de format SQLite déclenchent une quarantaine conservant les fichiers.

## Construire

Windows x64, SDK .NET 10.0.401, NSIS 3 pour l'installateur.

    ./scripts/build.ps1
    ./scripts/package.ps1 -Dotnet dotnet -Makensis 'C:/Program Files (x86)/NSIS/makensis.exe'

Le packaging utilise un nouveau dossier sous artifacts/, publie Desktop et Collector, copie les notices suivies dans third-party/, télécharge PresentMon 2.6.0 avec empreinte figée et compile NSIS. Il ne réutilise pas le portable historique. Pour répéter : -Output artifacts/release-2. Aucun moteur IA ni modèle n’est utilisé. NuGet utilise les fichiers de verrouillage.

## Architecture

Domain : mesures/statistiques/seuils. Application : contrats/sessions. Infrastructure : SQLite/exports/PresentMon/mises à jour. Collector : processus séparé et pipe réservé à l'utilisateur. Desktop : WPF/MVVM/panneau/thèmes. Updater : installation après fermeture. Tests : calculs, formats, persistance et régressions.

[Prise en main](docs/PRISE-EN-MAIN.md) · [Kit de promotion](marketing/TEXTES.md) · [Contribuer](CONTRIBUTING.md) · [Sécurité](SECURITY.md) · [Validation](VALIDATION.md) · [Comparatif](docs/COMPARATIF.md)

## Licence

Code Aegis sous [MIT](LICENSE), copyright ZerionIA et contributeurs. Les dépendances conservent leurs licences : [notices](THIRD-PARTY-NOTICES.md). Les sources MPL non modifiées sont fournies dans third-party/ et le paquet.

Projet indépendant, sans affiliation à AIDA64, HWiNFO, OCCT, Microsoft, Intel, AMD ou NVIDIA. Aucun code, visuel ou binaire propriétaire des concurrents n'est intégré. Pas de garantie de compatibilité universelle ni de diagnostic professionnel.
