# Composants tiers

Aegis est sous MIT ; les composants ci-dessous gardent leurs propres licences. Les textes, métadonnées NuGet, inventaire de versions et sources MPL non modifiées sont dans third-party/ (notices/ dans l'installateur).

| Composant | Version | Licence et source |
|---|---|---|
| .NET / Windows Desktop | 10.0.12 | MIT et notices du runtime Microsoft |
| LibreHardwareMonitorLib | 0.9.6 | MPL 2.0, https://github.com/LibreHardwareMonitor/LibreHardwareMonitor |
| RAMSPDToolkit-NDD | 1.4.2 | MPL 2.0, sources du commit NuGet jointes |
| DiskInfoToolkit | 1.1.2 | MPL 2.0, sources du commit NuGet jointes |
| BlackSharp.Core | 1.0.7 | MPL 2.0, sources du commit NuGet jointes |
| Microsoft.Data.Sqlite | 10.0.0 | Microsoft MIT et dépendances décrites dans les métadonnées |
| SQLitePCLRaw | 3.0.5 | MIT et notices, SQLite natif joint au runtime |
| PawnIO (installateur officiel non modifié) | 2.2.0 | GPL-2.0-or-later avec exception amont ; https://pawnio.eu/ et sources exactes dans third-party/PawnIO-2.2.0-source.zip |
| PresentMon | 2.6.0 | MIT, https://github.com/GameTechDev/PresentMon |

HidSharp conserve sa licence Apache 2.0. Mono.Posix.NETStandard conserve ses références de licence et la concession de brevets Mono. L'inventaire JSON et les fichiers nuspec détaillent les autres dépendances. Les sources des composants MPL sont fournies à côté des binaires, sans modification ; elles restent couvertes par MPL 2.0.

Aucun binaire ou modèle propriétaire de concurrent n'est distribué. Les licences des logiciels tiers ne sont pas remplacées par celle d'Aegis. L'installation de composants optionnels peut télécharger leurs dépendances selon leurs propres notices.

Le pilote PawnIO reste un composant système partagé, installé seulement après confirmation Windows. Sa désinstallation est distincte de celle d’Aegis. La provenance, l’empreinte figée, la licence et les sources correspondantes (sous-module PawnPP inclus) sont jointes. Aucune édition non signée ni modification du pilote n’est activée par Aegis.
