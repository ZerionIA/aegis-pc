# Validation — 0.2.0-beta.2

Vérifications exécutées le 24 octobre 2026 sur Windows x64. Ce rapport ne constitue pas un audit indépendant complet ni une certification de tous les matériels.

## Résultats

- Compilation Release sans erreur ni avertissement.
- **36 vérifications automatisées de base**, dont valeurs absentes, compteurs, exports, persistance, corruption, base saine en lecture seule, seuils soutenus et discontinuités.
- **41 vérifications** avec le vrai collecteur autonome publié : trois snapshots réels et bornage CPU.
- Scénarios intégrés de l'exécutable : ouverture, enregistrement et conservation d'une session, panneau compact, réglages, déclenchement d'une alerte RAM sur les vraies mesures avec seuil de test.
- Captures de la fenêtre rendues par l'application puis inspectées. L'accès Computer Use a expiré ; aucune série de clics manuels automatisés n'est annoncée comme validée.
- Téléchargements réels de whisper.cpp et du moteur Ollama CPU via ComponentInstaller dans un dossier vierge : empreintes et marqueurs vérifiés.
- Nouveau packaging construit sans réutiliser les anciens outils du portable historique, PresentMon téléchargé avec empreinte figée, intégrité 7-Zip de l'installateur vérifiée.
- Installation silencieuse sur un emplacement initialement absent ; refus de mise à jour avec verrou actif ; refus de désinstallation avec verrou actif ; session enregistrée depuis l'exécutable installé ; désinstallation complète. Empreinte de la base utilisateur existante inchangée après désinstallation.
- NuGet ne signale aucun paquet vulnérable dans les sources d'avis consultées à cette date. Cela ne couvre pas toutes les vulnérabilités possibles des binaires ou du système.

## Corrections effectuées

La récupération SQLite ne s'applique plus à toutes les erreurs ; une base saine inaccessible n'est plus remplacée. Les déplacements de fichiers de quarantaine sont restaurés si leur séquence échoue. Les sauvegardes intermédiaires ne libèrent plus un sémaphore détruit pendant la fermeture.

Une liaison WPF de progression a été passée explicitement en OneWay après découverte d'une erreur de lancement. Les erreurs XAML fatales arrêtent le processus au lieu de boucler. L'exécutable tient un verrou que l'installateur vérifie avant mise à jour ou désinstallation.

## Non validé / limites

- Capture de jeu réelle avec anti-cheat, prise de son physique et matrice Intel/AMD/NVIDIA.
- Téléchargement de nouveau du modèle Qwen de plusieurs Go pendant cette campagne. Le modèle et la transcription synthétique avaient été testés pendant le développement précédent ; cela ne remplace pas une nouvelle campagne complète de cette bêta.
- Interfaces accessibles par lecteur d'écran et navigation clavier de bout en bout.
- Stabilité de plusieurs heures, consommation sur PC peu puissant, Windows propre en VM et antivirus variés.
- Signature Authenticode et mises à jour automatiques : non implémentées.

La bêta est proposée pour recueillir des retours, avec ces limites affichées. Aucun test sur fixture n'est présenté comme une partie de jeu réelle.
