# Validation — 0.5.0-beta.1

Campagne Windows x64 du 25 octobre 2026.

- 74 contrôles réussis : 69 de base et cinq avec le collecteur réel. Quinze régressions de jeu couvrent les lanceurs, correspondances exactes, temporisation, arrêt manuel, relancement et réutilisation de PID.
- Scénario réel avec un processus factice isolé : détection, début de session, HUD affiché, fin du processus, arrêt automatique et bilan sauvegardé. Les styles Windows sans activation et transparence aux clics sont inspectés sur le handle du HUD. Ce n’est pas un test de frappe ou de clic physique dans LoL.
- Une vraie instance de League of Legends a été reconnue pendant les essais ; le scénario reproductible a ensuite été isolé sur le processus factice. Aucun processus de jeu réel n’a été fermé ou modifié.
- Captures inspectées : HUD, réglages par onglets en anglais et capteurs groupés. Recherche capteur, stabilité de l’objet de groupe entre deux mesures, complétude des packs et sérialisation des préférences vérifiées par le scénario intégré.
- Les tests précédents de sauvegarde refusée, quarantaine SQLite, mises à jour et conservation des données restent présents. Démarrage Windows jamais activé par ces essais.

## Limites

Bêta non signée Authenticode. HUD externe destiné aux modes fenêtré/sans bordure ; le plein écran exclusif peut le masquer. Pas de compatibilité anti-cheat universelle ou certification Riot revendiquée. Aucune partie LoL complète, clic physique traversant en partie ou stabilité longue durée ne sont certifiés par cette campagne. Pas de FPS en direct dans le HUD : PresentMon produit des statistiques après capture selon les droits ETW et le jeu. Les capteurs matériels indisponibles restent absents. Les noms matériels, contenus utilisateur et diagnostics externes gardent leur langue d’origine ; l’interface principale et les observations courantes ont leurs packs français/anglais. L’assistant IA reste retiré.

## Diffusion vérifiée

CI et packaging GitHub réussis. Installateur officiel téléchargé et SHA-256 vérifié, puis mise à jour 0.4 → 0.5 installée avec fermeture normale et relance de l’app. Version installée et réactivité du processus contrôlées sans nouvelle erreur de journal. Site et guide publics en HTTP 200 après déploiement de la version 5.
