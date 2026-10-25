# Validation — 0.4.0-beta.1

Campagne du 25 octobre 2026 sur Windows x64. Compilation Release sans erreur ni avertissement.

- 57 vérifications réussies, dont cinq contrôles avec le collecteur réel. Les 52 vérifications de base couvrent : calculs, persistance, versions et intégrité des mises à jour, commande de démarrage avec espaces, rapport support expurgé, métadonnées de sessions interrompues/completes et rechargement.
- Scénario intégré réel : icône déclarée visible, masquage/restauration, démarrage en arrière-plan et deuxième processus réactivant la fenêtre existante.
- Enregistrement avec collecteur réel, marqueur ajouté par la même route de messages que le raccourci, dernier bilan rechargé, recherche et rapport JSON créés. Le test appelle cette route dans l'app ; aucune frappe globale dans un jeu n'est simulée.
- Valeur du démarrage Windows inchangée avant/après ces essais. L'activation réelle au prochain login Windows n'a pas été testée ; l'option n'a pas été imposée au PC de développement.
- Refus réel de sauvegarde SQLite provoqué en lecture seule dans une base isolée, puis droits restaurés : la session en attente reste récupérée après changement de sélection. Une nouvelle capture est bloquée tant que la sauvegarde précédente échoue.
- Captures de la fenêtre inspectées, notamment après correction du nom et du contraste des listes déroulantes.
- CI et packaging GitHub réussis. Paquet officiel téléchargé par le client de l’app et SHA-256 vérifié. Updater réel 0.3 → 0.4 : fermeture normale, installation retour 0, version installée et relancée vérifiée. Contenu logique de la base inchangé (base vide dans cet essai) ; migration de sessions peuplées vérifiée lors de la campagne précédente. Refus correct de l’installateur si une autre instance conserve le verrou. Site public HTTP 200 et liens de la 0.4 contrôlés.

## Limites

Bêta non signée Authenticode. Intégrité des mises à jour vérifiée via HTTPS et le SHA-256 de GitHub, sans signature indépendante de l'éditeur. Compatibilité des raccourcis et capteurs à vérifier avec chaque environnement de jeu ; aucune promesse anti-cheat universelle. Diversité matérielle, accessibilité complète et stabilité longue durée restent à éprouver. L'app ne modifie pas les réglages matériels et ne garantit pas un gain de FPS. L'assistant IA reste absent.
