# Confidentialité — Aegis 0.8

Les mesures, sessions, marqueurs et réglages sont stockés dans %LOCALAPPDATA%/AegisNext. Le journal local peut contenir des chemins de fichiers et reste sur votre PC. Aucun compte, publicité comportementale ou envoi de mesures à un serveur n'est intégré.

L'assistant IA, les moteurs de dialogue/transcription et l'accès au microphone ont été retirés. Les anciennes données de modèles ne sont plus utilisées. Elles ne sont pas effacées automatiquement.

Si les mises à jour automatiques sont activées, Aegis contacte l'API GitHub pour lire les releases officielles, puis GitHub et sa distribution pour télécharger l'installateur. Ces fournisseurs reçoivent les informations réseau nécessaires, dont l'adresse IP et l'agent utilisateur mentionnant la version. Aucun capteur, nom de session ou donnée matérielle n'est envoyé. La vérification intervient au plus une fois par jour après réussite, avec possibilité de nouvelle tentative après erreur. Les mises à jour sont désactivables.

L'installation est confirmée dans l'app et attend sa fermeture. Les téléchargements sont conservés dans le sous-dossier updates ; les copies temporaires du programme d'installation sont nettoyées ultérieurement. La désinstallation conserve volontairement les sessions.

Les liens Zerionia/X/GitHub et mailto s'ouvrent uniquement après clic. Les services externes ont leurs propres politiques. Aucune publicité tierce n'est chargée dans l'app. Les exports et rapports de bugs sont partagés par vous : relire et anonymiser avant envoi.

Contact public indiqué par le responsable du projet : contact@zerionia.com.

Le raccourci global enregistre uniquement Ctrl+Alt+F8 auprès de Windows ; aucun flux de frappe ni contenu du clavier n’est enregistré. Le démarrage Windows est une option du compte utilisateur, appliquée après clic sur Enregistrer. Le rapport support est exporté localement sur demande et n’est jamais envoyé automatiquement.

La détection des jeux inspecte localement les noms, identifiants, heures de démarrage et handles de fenêtres des processus chaque seconde. Elle ne lit pas la mémoire du jeu, ne charge aucun module dans le jeu et ne consulte pas ses comptes. Aucun inventaire de processus n’est envoyé. Les sessions automatiques peuvent être désactivées dans Jeux. Le HUD est une fenêtre externe passive ; aucune certification anti-cheat n’est revendiquée.

Les capteurs CPU complets utilisent le pilote système PawnIO officiel signé, inclus dans le paquet et installé après confirmation Windows. Le collecteur peut demander des droits administrateur pour lire ce pilote ; l’interface reste au niveau utilisateur. Le canal de mesures est limité au même utilisateur et le client élevé utilise le niveau d’identification pour ne pas transmettre de jeton d’impersonation. Aucune commande matérielle arbitraire n’est exposée par Aegis. Le mode basique reste disponible en cas de refus. Le pilote partagé n’est pas supprimé lors de la désinstallation d’Aegis.


## Statistiques d’utilisation facultatives (à partir de 0.7)

Désactivées tant que vous n’avez pas accepté dans l’application. Un refus conserve toutes les fonctions. Votre choix est enregistré dans usage-consent.json, séparément des mesures.

Après accord, Aegis crée un identifiant d’installation aléatoire (aucun identifiant matériel). Au plus toutes les 15 minutes pendant que l’app est ouverte, même réduite, un signal HTTPS est envoyé à https://zerionia.com/api/aegis/usage : identifiant, version et révision du consentement. Aucun capteur, nom de jeu, processus, session, chemin, compte ou contenu personnel n’est joint. Le serveur remplace l’identifiant par une empreinte avec clé : ces données sont pseudonymisées, pas un anonymat garanti. Il conserve l’activité récente, les dates distinctes d’utilisation et la version afin de compter les appareils actifs et ceux qui reviennent.

Les enregistrements inactifs depuis 35 jours sont purgés au prochain signal ou à la prochaine consultation du tableau de bord privé. L’adresse IP est nécessaire au transport et visible de l’hébergeur ; l’application ne la collecte pas. Le point de réception stocke seulement une empreinte réseau quotidienne pour limiter les abus, pendant au plus deux tranches horaires. Les journaux techniques Vercel/Supabase relèvent de leurs politiques. Aucun IP brut ou agent utilisateur n’est ajouté dans les tables statistiques.

Dans Réglages → Statistiques et retours, désactiver arrête les signaux et demande la suppression de l’enregistrement. Si vous êtes hors ligne, la demande est conservée puis réessayée lors des prochaines ouvertures ; l’ancien identifiant est supprimé localement après confirmation. Une désinstallation ne peut pas garantir cette demande : désactivez avant de désinstaller, ou attendez la purge d’inactivité. Un changement de clé serveur peut empêcher un rapprochement ; les anciennes empreintes expirent par inactivité.

Les constructions non installées, les builds Debug, les essais --smoke, --data-dir, --no-usage-reporting et la variable AEGIS_DISABLE_USAGE_REPORTING=1 n’envoient aucun signal, même si une préférence existe. Pour nos essais de l’app installée, cette exclusion doit rester activée.

Le bouton de retour ouvre une page Zerionia expliquant trois questions, puis laisse choisir l’e-mail ou GitHub. Aucun avis ni rapport n’est envoyé automatiquement. GitHub rend les issues publiques ; retirez toute information personnelle avant publication.

Depuis 0.8, le HUD s’affiche au lancement par défaut. La croix masque la fenêtre dans la zone de notification ; collecte, HUD et éventuelle session continuent. Une notification le précise. Utiliser Quitter Aegis ou le menu de l’icône pour arrêter réellement l’app. Ces options sont réglables. Aucun nouveau lancement automatique avec Windows n’est activé par cette modification.
