# Validation — 0.6.0-beta.1

Campagne Windows x64 du 26 octobre 2026.

- 83 contrôles réussis : 78 de base, cinq avec collecteur réel. Nouveaux cas : prérequis absent/ancien/compatible/plus récent, aucun droit demandé en mode basique, élévation uniquement en mode complet lorsqu’il manque l’accès, longueur et SHA-256 du payload.
- Installation de PawnIO 2.2.0 autorisée par l’utilisateur : signature Authenticode valide, empreinte officielle vérifiée, service installé et actif.
- Cause reproduite : ouverture du pilote refusée (code Windows 5) en contexte non administrateur. Collecteur autorisé : CPU Package et températures des cœurs mesurés, environ 52–53 °C lors de l’essai.
- HUD réel inspecté avec température CPU 52 °C et température GPU 58 °C. Onglet Capteurs CPU inspecté avec lecture confirmée. L’interface elle-même reste non élevée ; seul le collecteur demande l’autorisation.
- Configurateur sans fenêtre : sortie 0, passage basique puis complet, thème et choix de mises à jour existants conservés. Pas de second téléchargement ni de réinstallation d’un pilote compatible.
- Sources exactes PawnIO et PawnPP, licence et provenance jointes ; payload officiel téléchargé avec empreinte et signature vérifiées pendant le packaging.

## Limites

Les confirmations UAC doivent être acceptées par l’utilisateur. Un refus garde l’app utilisable avec des métriques limitées ; il ne produit pas de fausse température. Aucun redémarrage forcé, changement de permission du pilote ou désactivation de protection. Le système peut demander un redémarrage après installation du pilote. Le test de refus de privilèges couvre la politique et le mode basique ; aucun clic UAC n’est automatisé. Le parcours a été exercé sur ce PC, pas sur une matrice de machines virtuelles fraîchement installées.

Bêta Aegis non signée Authenticode ; le prérequis PawnIO conserve sa signature officielle. Les limites de compatibilité matérielle, de stabilité longue durée, de plein écran exclusif et d’anti-cheat restent applicables. Pas de FPS en direct.

## Paquet public vérifié

CI et packaging GitHub réussis. Installateur public téléchargé par le client de l’app, empreinte vérifiée, installation et activation du mode complet terminées avec retour 0. Version installée 0.6 vérifiée, prérequis et sources présents, application réactive sans nouvelle erreur de journal. Site version 7 déployé et HTTP 200. Aucun pilote ajouté par la phase silencieuse : le pilote de ce PC avait été installé après accord explicite et son accès complet validé.
