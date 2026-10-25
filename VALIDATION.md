# Validation — 0.3.0-beta.1

Campagne du 25 octobre 2026 sur Windows x64.

- Compilation Release sans erreur ni avertissement ; 44 vérifications de base.
- 49 vérifications avec le collecteur autonome publié : trois snapshots réels et bornage CPU.
- Vérifications de mise à jour sur transport HTTP simulé : ordre numérique des versions, canal stable/bêta, URL hors dépôt, SHA-256 absent, taille excessive, fichier modifié, nettoyage des téléchargements corrompus et refus des versions identiques.
- Captures de la vraie fenêtre via les scénarios intégrés puis inspection des thèmes Forêt et Clair ; rendu de la page Zerionia.
- Migration réelle depuis l'installation 0.2 vers 0.3 : sessions utilisateur conservées, retrait des DLL de transcription/synthèse vocale, ouverture de l'exécutable installé et enregistrement d'une session de test.
- Packaging autonome depuis un dossier neuf, PresentMon récupéré avec empreinte figée, module de mise à jour séparé.
- Aucune dépendance Ollama, Whisper, NAudio ou System.Speech dans les projets et fichiers de verrouillage du produit courant.

Les captures ne démontrent pas une navigation clavier complète. Aucun clic automatique vers X ou mail n'est exécuté. Les serveurs MX de zerionia.com répondent, mais la boîte contact@zerionia.com n'a pas été testée par envoi d'e-mail.

Après publication : téléchargement réel de l'installateur public 0.3 depuis GitHub via ReleaseUpdateClient, vérification de son SHA-256, attente de fermeture de l'application, installation et redémarrage réussis (END_TO_END_UPDATE_OK). Le programme de mise à jour utilisé pour ce scénario était une compilation isolée portant l'identifiant inférieur 0.2.9-beta.1, non publiée, afin de tester la transition vers la release officielle sans créer de fausse version publique. La migration depuis l'ancien installateur 0.2 avait été testée séparément. Les tests simulés ne sont pas confondus avec ce scénario réel.

## Limites

Exécutables non signés Authenticode. HTTPS et l'empreinte fournie par GitHub vérifient l'intégrité via le dépôt officiel, sans signature indépendante de l'éditeur. Capture de jeux réels avec anti-cheat, configurations multi-GPU, faible puissance, lecteur d'écran et longue durée restent à valider. Le microphone et l'assistant n'existent plus dans cette version.
