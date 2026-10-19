# Roadmap Voyl

> Surcouche d'écran d'accueil Windows 10/11 ultra-légère et personnalisable.
> Objectif : minimiser la RAM, le stockage et la latence.

---

## Légende

- [ ] À faire
- [x] Terminé
- 🔹 En cours

---

## Étape 0 : Cadrage et mesure de référence du bureau Windows actuel

**Objectif** : Établir une baseline des performances du bureau Windows standard (explorer.exe) pour comparer avec Voyl.

**Livrables** :
- `docs/METRICS.md` avec méthodes de mesure validées
- Tableau de référence des métriques du bureau Windows actuel (à remplir manuellement)

**Test manuel** :
1. Redémarrer la machine en mode propre (pas d'app tierce au démarrage)
2. Attendre 5 min après le démarrage pour stabilisation
3. Mesurer explorer.exe avec les outils définis dans METRICS.md
4. Remplir le tableau de référence dans METRICS.md

**Critères de validation** :
- [ ] Valeurs de référence documentées pour :
  - [ ] RAM idle (Private Working Set) de explorer.exe
  - [ ] CPU idle de explorer.exe
  - [ ] Temps de lancement d'une application (ex: calc.exe)
  - [ ] Temps de changement de page (simulé via Alt+Tab ou raccourcis clavier)
- [ ] Méthodes de mesure validées et reproductibles

**Sous-étapes (1-2h chacune)** :
1. [ ] Rédiger METRICS.md avec outils et méthodes
2. [ ] Mesurer et documenter la baseline explorer.exe

---

## Étape 1 : Squelette

**Objectif** : Solution .NET 8 minimaliste avec fenêtre vide, logging, et mécanisme de sortie de secours.

**Livrables** :
- `src/Voyl.sln` (solution Visual Studio)
- `src/Voyl/Voyl.csproj` (projet .NET 8)
- `src/Voyl/Program.cs` (point d'entrée)
- `src/Voyl/NativeMethods.cs` (interop Win32 via CsWin32)
- Mécanisme de sortie : touche globale (ex: Ctrl+Alt+Shift+Q) pour quitter
- Logging basique dans `%APPDATA%\Voyl\logs\`

**Test manuel** :
1. Compiler en mode Release avec AOT
2. Lancer Voyl.exe
3. Vérifier que la fenêtre vide apparaît
4. Vérifier que la touche de sortie fonctionne
5. Vérifier que le fichier de log est créé

**Critères de validation** :
- [ ] Exécutable publié ≤ 10 Mo
- [ ] RAM idle (Private Working Set) ≤ 30 Mo
- [ ] CPU idle < 1 %
- [ ] Pas de crash au lancement
- [ ] Sortie propre via touche de secours

**Sous-étapes (1-2h chacune)** :
1. [ ] Créer solution et projet .NET 8
2. [ ] Configurer Native AOT et CsWin32
3. [ ] Implémenter fenêtre vide Win32
4. [ ] Ajouter logging basique
5. [ ] Implémenter touche de sortie globale
6. [ ] Tester compilation et publication AOT

---

## Étape 2 : Hôte bureau

**Objectif** : Fenêtre sans bordure plein écran positionnée derrière les icônes du bureau, sans interférer avec explorer.exe.

**Livrables** :
- Fenêtre WS_POPUP plein écran avec HWND parent = HWND_BROADCAST (ou NULL)
- Positionnement en Z-order : derrière les icônes (HWND_BOTTOM)
- Gestion des messages Win32 pour éviter les conflits
- Sortie sûre (restauration de l'état précédent)

**Test manuel** :
1. Lancer Voyl.exe
2. Vérifier que la fenêtre couvre tout l'écran
3. Vérifier que les icônes du bureau restent visibles et cliquables
4. Vérifier que explorer.exe reste intact (pas de kill, pas de crash)
5. Quitter Voyl et vérifier que le bureau reste fonctionnel

**Critères de validation** :
- [ ] Fenêtre plein écran sans bordures
- [ ] Icônes du bureau toujours accessibles
- [ ] explorer.exe toujours actif et stable
- [ ] RAM idle ≤ 30 Mo
- [ ] CPU idle < 1 %

**Sous-étapes (1-2h chacune)** :
1. [ ] Créer fenêtre WS_POPUP plein écran
2. [ ] Positionner la fenêtre derrière les icônes (Z-order)
3. [ ] Gérer les messages Win32 de base (WM_PAINT, WM_CLOSE, etc.)
4. [ ] Implémenter sortie sûre
5. [ ] Tester la coexistence avec explorer.exe

---

## Étape 3 : Configuration JSON

**Objectif** : Chargement et validation d'un fichier de configuration JSON pour les pages et tuiles.

**Livrables** :
- `src/Voyl/Config/ConfigManager.cs` (gestion du fichier config)
- `src/Voyl/Config/Models.cs` (modèles Page, Tile, etc.)
- Fichier de config par défaut dans `%APPDATA%\Voyl\config.json`
- Validation : fichier invalide = valeurs par défaut, jamais de crash

**Test manuel** :
1. Lancer Voyl sans config : vérifier que les valeurs par défaut sont utilisées
2. Créer un config.json valide : vérifier que la config est chargée
3. Créer un config.json invalide : vérifier que les valeurs par défaut sont utilisées
4. Modifier la config à chaud : vérifier que Voyl la recharge (si implémenté)

**Critères de validation** :
- [ ] Fichier config chargé au démarrage
- [ ] Fichier invalide → valeurs par défaut sans crash
- [ ] Pas de fuite mémoire après rechargement
- [ ] RAM idle ≤ 30 Mo

**Sous-étapes (1-2h chacune)** :
1. [ ] Définir les modèles de configuration (Page, Tile, etc.)
2. [ ] Implémenter le chargeur JSON avec validation
3. [ ] Gérer les erreurs de parsing et les valeurs manquantes
4. [ ] Créer un fichier de config par défaut
5. [ ] Tester les cas valides et invalides

---

## Étape 4 : Modèle de pages

**Objectif** : Navigation en mémoire entre pages, avec une seule page chargée à la fois.

**Livrables** :
- `src/Voyl/Pages/PageManager.cs` (gestion des pages)
- `src/Voyl/Pages/Page.cs` (représentation d'une page)
- Navigation entre pages via raccourcis clavier ou clics
- Une seule page active en mémoire à la fois

**Test manuel** :
1. Lancer Voyl avec une config contenant 3 pages
2. Naviguer entre les pages via raccourcis
3. Vérifier que seule la page active est rendue
4. Vérifier que les autres pages ne consomment pas de ressources

**Critères de validation** :
- [ ] Changement de page < 100 ms
- [ ] Une seule page chargée en mémoire à la fois
- [ ] RAM idle ≤ 30 Mo
- [ ] Pas de fuite mémoire après plusieurs navigations

**Sous-étapes (1-2h chacune)** :
1. [ ] Définir la structure d'une Page
2. [ ] Implémenter le gestionnaire de pages
3. [ ] Ajouter la navigation entre pages
4. [ ] Optimiser l'utilisation mémoire (une page active max)
5. [ ] Tester les performances de navigation

---

## Étape 5 : Lanceur

**Objectif** : Lancement d'applications via raccourcis du menu Démarrer, avec mesure de latence.

**Livrables** :
- `src/Voyl/Launcher/AppLauncher.cs` (lancement des apps)
- Parsing des raccourcis (.lnk) du menu Démarrer
- Mesure du temps entre le clic et le lancement effectif de l'app
- Intégration avec les tuiles de la page active

**Test manuel** :
1. Ajouter un raccourci vers calc.exe dans la config
2. Lancer Voyl et cliquer sur la tuile
3. Vérifier que calc.exe s'ouvre
4. Mesurer la latence de lancement (via Stopwatch ou outil externe)

**Critères de validation** :
- [ ] Applications lancées correctement
- [ ] Surcoût de lancement < 50 ms
- [ ] RAM idle ≤ 30 Mo
- [ ] Pas de fuite de handles après plusieurs lancements

**Sous-étapes (1-2h chacune)** :
1. [ ] Parser les raccourcis .lnk du menu Démarrer
2. [ ] Implémenter le lanceur d'applications
3. [ ] Intégrer avec les tuiles
4. [ ] Mesurer et optimiser la latence
5. [ ] Tester avec plusieurs applications

---

## Étape 6 : Optimisation du changement de page

**Objectif** : Réduire la latence de changement de page via chargement paresseux et libération des ressources.

**Livrables** :
- Chargement paresseux des pages (lazy loading)
- Libération des ressources des pages inactives
- Cache optionnel pour les pages fréquemment utilisées
- Mesure de la latence avant/après optimisation

**Test manuel** :
1. Lancer Voyl avec 5 pages
2. Mesurer le temps de changement entre pages
3. Vérifier que la RAM reste stable après plusieurs navigations
4. Vérifier que les pages non actives ne consomment pas de CPU

**Critères de validation** :
- [ ] Changement de page < 100 ms (moyenne sur 10 changements)
- [ ] RAM idle ≤ 30 Mo
- [ ] CPU idle < 1 %
- [ ] Pas de fuite mémoire après 100 changements de page

**Sous-étapes (1-2h chacune)** :
1. [ ] Implémenter le chargement paresseux
2. [ ] Libérer les ressources des pages inactives
3. [ ] Ajouter un cache optionnel
4. [ ] Mesurer les performances avant/après
5. [ ] Tester la stabilité mémoire

---

## Étape 7 : Gestionnaire de ressources (opt-in)

**Objectif** : Contrôle du Working Set et de la priorité des processus d'arrière-plan, avec liste d'exclusion.

**Livrables** :
- `src/Voyl/ResourceManager/ResourceManager.cs`
- Fonctionnalité opt-in (désactivée par défaut)
- Liste d'exclusion pour les processus protégés (ex: explorer.exe)
- Ajustement du Working Set via SetProcessWorkingSetSize
- Ajustement de la priorité via SetPriorityClass
- **Jamais de kill de processus**

**Test manuel** :
1. Activer le gestionnaire de ressources dans la config
2. Lancer plusieurs applications en arrière-plan
3. Vérifier que leur Working Set est réduit (si autorisé)
4. Vérifier que les processus exclus ne sont pas affectés
5. Vérifier que les processus ne sont pas tués

**Critères de validation** :
- [ ] Réduction du Working Set mesurable pour les processus cibles
- [ ] Processus exclus non affectés
- [ ] Aucun processus tué
- [ ] RAM idle de Voyl ≤ 30 Mo
- [ ] CPU idle < 1 %

**Sous-étapes (1-2h chacune)** :
1. [ ] Implémenter la détection des processus
2. [ ] Ajouter la liste d'exclusion
3. [ ] Implémenter l'ajustement du Working Set
4. [ ] Implémenter l'ajustement de la priorité
5. [ ] Tester avec des processus réels

---

## Étape 8 : Personnalisation

**Objectif** : Thèmes, fond d'écran, disposition des tuiles, sans dépasser les budgets.

**Livrables** :
- `src/Voyl/Themes/ThemeManager.cs`
- Support des thèmes clairs/sombres
- Fond d'écran personnalisable (image ou couleur unie)
- Disposition des tuiles (grille, libre)
- Configuration via JSON

**Test manuel** :
1. Changer le thème dans la config
2. Changer le fond d'écran
3. Réorganiser les tuiles
4. Vérifier que tout s'affiche correctement
5. Mesurer la RAM et le CPU

**Critères de validation** :
- [ ] Thèmes fonctionnels
- [ ] Fond d'écran personnalisable
- [ ] Disposition des tuiles modifiable
- [ ] RAM idle ≤ 30 Mo
- [ ] CPU idle < 1 %
- [ ] Pas de fuite mémoire après changement de thème

**Sous-étapes (1-2h chacune)** :
1. [ ] Implémenter le système de thèmes
2. [ ] Ajouter le support des fonds d'écran
3. [ ] Implémenter la disposition des tuiles
4. [ ] Intégrer avec la config JSON
5. [ ] Tester les performances

---

## Étape 9 : Packaging

**Objectif** : Packaging portable (ZIP), désinstallation propre, mode sans échec.

**Livrables** :
- Script de build pour générer le ZIP portable
- Script de désinstallation (suppression des fichiers dans %APPDATA%\Voyl)
- Mode sans échec (démarrage minimal sans personnalisation)
- Documentation pour l'utilisateur final

**Test manuel** :
1. Générer le ZIP portable
2. Extraire et lancer Voyl depuis un dossier temporaire
3. Tester la désinstallation
4. Tester le mode sans échec
5. Vérifier que %APPDATA%\Voyl est propre après désinstallation

**Critères de validation** :
- [ ] ZIP portable ≤ 10 Mo
- [ ] Désinstallation propre (pas de fichiers résiduels)
- [ ] Mode sans échec fonctionnel
- [ ] Documentation complète

**Sous-étapes (1-2h chacune)** :
1. [ ] Créer le script de build pour le ZIP
2. [ ] Implémenter le script de désinstallation
3. [ ] Ajouter le mode sans échec
4. [ ] Rédiger la documentation utilisateur
5. [ ] Tester l'ensemble du packaging

---

## Option : Étude du remplacement du shell (explorer.exe)

**Objectif** : Étudier la faisabilité du remplacement de explorer.exe par Voyl en tant que shell par défaut.

**Livrables** :
- Documentation des risques et des étapes nécessaires
- Script pour sauvegarder/restaurer explorer.exe
- **À tester uniquement en machine virtuelle**

**Test manuel** :
1. **En VM uniquement** : Configurer Voyl comme shell par défaut
2. Vérifier que le système reste stable
3. Vérifier que explorer.exe peut être restauré

**Critères de validation** :
- [ ] Documentation complète des risques
- [ ] Script de sauvegarde/restauration fonctionnel
- [ ] Tests effectués en VM

**Sous-étapes (1-2h chacune)** :
1. [ ] Documenter les risques et les étapes
2. [ ] Créer le script de sauvegarde/restauration
3. [ ] Tester en VM
