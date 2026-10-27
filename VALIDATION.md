# Validation — Aegis 0.7.0-beta.1
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
