# Validation — Aegis 0.8.0-beta.1
Campagne du 27 octobre 2026.

- Construction Release réussie, zéro avertissement. 93 contrôles automatisés de base réussis.
- Cas nouveaux : client LeagueClient/LeagueClientUx identifié séparément, jamais classé comme match ; casse et helpers vérifiés ; deux observations requises ; douze absences conservées à cadence d’une seconde ; délai invalide refusé.
- WPF réel : HUD présent au lancement, fermeture × laissant HUD, icône et session active, restauration de fenêtre puis arrêt de session et sortie complète explicite.
- Processus de jeu factice réel : démarrage automatique observé en moins de quatre secondes dans le scénario local, HUD passif, arrêt après le délai de grâce, session retrouvée. Ce délai n’est pas une garantie sur tous les PC.
- Client LoL factice réel : interface anglaise en attente de partie, aucune session lancée. Captures FR/EN inspectées avec données matérielles réellement reçues du collecteur en mode basique.
- Préférences CloseToTray et ShowHudOnStartup enregistrées, anciennes préférences désérialisables par paramètres par défaut. Aucun démarrage Windows ajouté.
- Sortie explicite, menu de notification et demande d’installation de mise à jour passent par la même sortie complète ; la croix seule masque la fenêtre si l’icône est disponible.
- Aucune campagne de partie LoL complète ni certification anti-cheat. Le plein écran exclusif peut masquer la fenêtre externe.
- Tests internes exclus du reporting d’usage. Aucun avis ni signal capteur envoyé automatiquement.
- SmartScreen reste possible : bêta Aegis non signée Authenticode. Aucun contournement ni désactivation de Windows Defender. PawnIO conserve sa signature officielle distincte.
