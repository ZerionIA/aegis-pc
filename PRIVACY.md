# Confidentialité — Aegis 0.6

Les mesures, sessions, marqueurs et réglages sont stockés dans %LOCALAPPDATA%/AegisNext. Le journal local peut contenir des chemins de fichiers et reste sur votre PC. Aucun compte, publicité comportementale ou envoi de mesures à un serveur n'est intégré.

L'assistant IA, les moteurs de dialogue/transcription et l'accès au microphone ont été retirés. Les anciennes données de modèles ne sont plus utilisées. Elles ne sont pas effacées automatiquement.

Si les mises à jour automatiques sont activées, Aegis contacte l'API GitHub pour lire les releases officielles, puis GitHub et sa distribution pour télécharger l'installateur. Ces fournisseurs reçoivent les informations réseau nécessaires, dont l'adresse IP et l'agent utilisateur mentionnant la version. Aucun capteur, nom de session ou donnée matérielle n'est envoyé. La vérification intervient au plus une fois par jour après réussite, avec possibilité de nouvelle tentative après erreur. Les mises à jour sont désactivables.

L'installation est confirmée dans l'app et attend sa fermeture. Les téléchargements sont conservés dans le sous-dossier updates ; les copies temporaires du programme d'installation sont nettoyées ultérieurement. La désinstallation conserve volontairement les sessions.

Les liens Zerionia/X/GitHub et mailto s'ouvrent uniquement après clic. Les services externes ont leurs propres politiques. Aucune publicité tierce n'est chargée dans l'app. Les exports et rapports de bugs sont partagés par vous : relire et anonymiser avant envoi.

Contact public indiqué par le responsable du projet : contact@zerionia.com.

Le raccourci global enregistre uniquement Ctrl+Alt+F8 auprès de Windows ; aucun flux de frappe ni contenu du clavier n’est enregistré. Le démarrage Windows est une option du compte utilisateur, appliquée après clic sur Enregistrer. Le rapport support est exporté localement sur demande et n’est jamais envoyé automatiquement.

La détection des jeux inspecte localement les noms, identifiants, heures de démarrage et handles de fenêtres des processus toutes les quatre secondes. Elle ne lit pas la mémoire du jeu, ne charge aucun module dans le jeu et ne consulte pas ses comptes. Aucun inventaire de processus n’est envoyé. Les sessions automatiques peuvent être désactivées dans Jeux. Le HUD est une fenêtre externe passive ; aucune certification anti-cheat n’est revendiquée.

Les capteurs CPU complets utilisent le pilote système PawnIO officiel signé, inclus dans le paquet et installé après confirmation Windows. Le collecteur peut demander des droits administrateur pour lire ce pilote ; l’interface reste au niveau utilisateur. Le canal de mesures est limité au même utilisateur et le client élevé utilise le niveau d’identification pour ne pas transmettre de jeton d’impersonation. Aucune commande matérielle arbitraire n’est exposée par Aegis. Le mode basique reste disponible en cas de refus. Le pilote partagé n’est pas supprimé lors de la désinstallation d’Aegis.
