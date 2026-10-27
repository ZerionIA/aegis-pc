# Validation — Aegis 0.7.0-beta.2
Campagne du 27 octobre 2026.

- Construction Release réussie sans avertissement.
- 87 contrôles de base réussis : 78 précédents et 9 contrôles dédiés à la présence facultative.
- Aucun identifiant ni requête avant accord ; contenu strict limité à id/version/consent/channel.
- Consentement persistant, limitation des envois, opt-out, suppression hors ligne puis reprise sans nouvel envoi d’activité, remise à zéro des identifiants, refus par défaut si fichier corrompu.
- Exclusion des essais internes ; toutes les requêtes des tests automatisés utilisent un transport factice en mémoire.
- Captures réelles WPF FR/EN de l’onglet Statistiques et retours inspectées. Retour d’expérience accessible en barre latérale et dans Réglages.
- Construction complète du site réussie ; endpoint privé de stockage avec RLS et droits réservés au service serveur, tableau de bord super_admin uniquement.
- Aucun nouveau contrôle du pilote ou matrice de matériels dans cette campagne. Les validations CPU de 0.6 restent historiques, sans prétendre couvrir tous les PC.
- Les métriques sont des appareils consentants, approximatives et falsifiables depuis un client modifié ; elles ne mesurent ni personnes exactes, ni toutes les installations, ni satisfaction.
- Les signaux n’envoient pas les capteurs, jeux, sessions, processus ou chemins. Stockage pseudonymisé décrit dans PRIVACY.md.
- Aegis reste une bêta non signée Authenticode. Installation de mise à jour sur confirmation, hors session. Un commit seul ne distribue aucune mise à jour.

## Vérifications de publication

Premier paquet 0.7 beta.1 : SHA-256 et taille du téléchargement public vérifiés. Le README joint contenait encore une ancienne mention « sans télémétrie » ; beta.2 corrige la documentation embarquée et les liens sans réécrire l’asset beta.1. La notice de consentement et PRIVACY.md étaient déjà présents. Aucun changement de politique : statistiques toujours désactivées avant accord.

Endpoint en production testé : payload malformé 400, origine navigateur 403, canal interne 204 sans insertion, fixture de présence 204, répétition 204, suppression 204. Tous les compteurs après nettoyage : 0. Accès sans connexion à /studio/aegis redirigé vers le login. Les essais SQL transactionnels de déduplication ont été annulés.
