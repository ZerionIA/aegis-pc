# Mesurer l’usage d’Aegis

Tableau privé du propriétaire : https://zerionia.com/studio/aegis (super_admin).
Le menu Aegis usage apparaît dans Zerion Studio. Aucun compteur public n’est publié.

- Running recently : dernier signal depuis moins de 20 minutes.
- Active 24h/7d/30d : appareils consentants distincts ayant envoyé un signal dans la fenêtre glissante.
- Used on 2+ days : appareils présents sur au moins deux dates UTC distinctes des sept derniers jours. Ce n’est pas une cohorte de rétention.
- L’app réduite compte comme ouverte. Le réseau, le consentement et la version limitent la couverture.
- Aucun total d’utilisateurs ni d’installations ne peut être extrapolé sans connaître cette couverture.

## Tests internes
Définir AEGIS_DISABLE_USAGE_REPORTING=1 avant de lancer une installation officielle pour nos vérifications. Les binaires non installés, builds Debug, --smoke, --data-dir et --no-usage-reporting sont aussi exclus.
Ne jamais activer le compteur sur un test puis le présenter comme un utilisateur externe.
Les fixtures API de validation doivent être supprimées à la fin. Les tests SQL doivent être dans une transaction annulée.

## Confidentialité et exploitation
Voir PRIVACY.md et /apps/aegis#usage-privacy.
La clé HMAC est exclusivement côté serveur et dérivée de la clé service Supabase déjà configurée. Sa rotation rompt le rapprochement des anciennes empreintes ; celles-ci expirent à l’inactivité.
Les tables ne sont accessibles ni à anon ni aux utilisateurs authentifiés du Data API. Les fonctions sont SECURITY INVOKER, exécutables par service_role uniquement.
Un endpoint public ne permet pas de prouver qu’un événement provient d’un utilisateur humain : limites réseau, validation stricte et compteurs idempotents réduisent les erreurs sans supprimer cette limite.
Le bouton d’avis ouvre le choix e-mail/GitHub, sans envoi automatique.
