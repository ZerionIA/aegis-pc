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

Le téléchargement d'une future release et la réinstallation au moyen du programme de mise à jour doivent aussi être éprouvés en situation réelle. Les résultats après publication sont consignés dans la livraison locale et pourront compléter ce rapport. Les tests HTTP simulés seuls ne constituent pas une preuve de redémarrage après mise à jour.

## Limites

Exécutables non signés Authenticode. HTTPS et l'empreinte fournie par GitHub vérifient l'intégrité via le dépôt officiel, sans signature indépendante de l'éditeur. Capture de jeux réels avec anti-cheat, configurations multi-GPU, faible puissance, lecteur d'écran et longue durée restent à valider. Le microphone et l'assistant n'existent plus dans cette version.
