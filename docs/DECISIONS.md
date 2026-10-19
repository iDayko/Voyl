# Décisions Techniques - Voyl

> Documentant les choix techniques, les alternatives écartées et les risques associés.

---

## Stack Technique Verrouillée

### Langage et Runtime
- **Choix** : C# avec .NET 8
- **Raison** :
  - Performance native avec AOT (Ahead-of-Time compilation)
  - Accès direct à Win32 via P/Invoke
  - Gestion mémoire fiable (GC)
  - Écosystème mature pour les applications Windows

### Compilation
- **Choix** : Native AOT
- **Raison** :
  - Exécutable autonome sans dépendance au runtime .NET
  - Temps de démarrage réduit
  - Taille de l'exécutable optimisée
  - Moins de surfaces d'attaque (pas de JIT)

### Interop Win32
- **Choix** : CsWin32 (source generator)
- **Raison** :
  - Génération de bindings Win32 à la compilation
  - Pas de dépendance runtime supplémentaire
  - Typage fort et sécurisé
  - Maintenance simplifiée (pas de code manuel P/Invoke)

### UI
- **Choix** : Win32 pur (HWND, GDI)
- **Raison** :
  - Aucune dépendance à WPF, WinUI ou WinForms
  - Contrôle total sur les performances et la mémoire
  - Intégration native avec le bureau Windows
  - Pas de surcoût lié à un framework UI

### Configuration
- **Choix** : JSON avec System.Text.Json
- **Raison** :
  - Format lisible et éditable manuellement
  - Pas de dépendance externe (System.Text.Json est intégré à .NET)
  - Performances de sérialisation/désérialisation suffisantes

### Logging
- **Choix** : Fichiers texte dans `%APPDATA%\Voyl\logs\`
- **Raison** :
  - Pas de dépendance à des librairies de logging (ex: Serilog, NLog)
  - Simple et fiable
  - Facile à consulter et à archiver

---

## Alternatives Écartées

### Langage
| Alternative | Raison de l'écart |
|-------------|-------------------|
| C++ | Complexité de gestion mémoire manuelle, risque de fuites, temps de développement plus long |
| Rust | Courbe d'apprentissage raide, interop Win32 moins mature que pour C# |
| Go | Taille de l'exécutable trop importante, GC moins optimisé pour les apps UI |
| Python | Performance insuffisante, dépendance à l'interpréteur, taille trop importante |
| JavaScript/TypeScript (Electron) | **INTERDIT** : Taille et RAM bien au-delà des budgets (100+ Mo RAM idle) |

### UI Framework
| Alternative | Raison de l'écart |
|-------------|-------------------|
| WPF | **INTERDIT** : Taille et RAM trop importantes, dépendance à .NET runtime |
| WinUI 3 | **INTERDIT** : Dépendance à WinRT, taille et RAM trop importantes |
| WinForms | **INTERDIT** : Dépendance à System.Windows.Forms, taille trop importante |
| Electron | **INTERDIT** : Chromium + Node.js = 100+ Mo RAM idle, taille exécutable énorme |
| WebView | **INTERDIT** : Dépendance à Edge/Chromium, RAM trop importante |
| Avalonia | Dépendance externe, taille et RAM non garanties dans les budgets |
| MAUI | Dépendance externe, ciblé mobile/cross-platform, pas optimisé pour Windows desktop |

### Compilation
| Alternative | Raison de l'écart |
|-------------|-------------------|
| JIT (classique) | Temps de démarrage plus long, dépendance au runtime .NET |
| CoreRT (ancien) | Abandonné au profit de Native AOT dans .NET 8 |
| IL2CPP (Unity) | Spécifique à Unity, pas adapté pour une app Windows native |

### Interop Win32
| Alternative | Raison de l'écart |
|-------------|-------------------|
| P/Invoke manuel | Maintenance lourde, risque d'erreurs, moins sécurisé |
| Windows API Code Pack | Déprécié, pas maintenu, dépendance externe |
| Vanara | Dépendance NuGet externe, pas autorisé sans accord explicite |

### Configuration
| Alternative | Raison de l'écart |
|-------------|-------------------|
| XML | Plus verbeux que JSON, parsing plus lent |
| INI | Format limité, pas de hiérarchie, parsing manuel nécessaire |
| YAML | Dépendance externe (ex: YamlDotNet), pas autorisé sans accord |
| SQLite | Surkill pour une config simple, dépendance externe |

### Logging
| Alternative | Raison de l'écart |
|-------------|-------------------|
| Serilog | Dépendance NuGet externe, pas autorisé sans accord |
| NLog | Dépendance NuGet externe, pas autorisé sans accord |
| ETW (Event Tracing for Windows) | Complexité inutile pour une app simple, difficile à consulter |

---

## Risques Identifiés

### Performances
| Risque | Impact | Mitigation |
|--------|--------|------------|
| Dépassement de la RAM idle (30 Mo) | Bloquant | Utiliser Native AOT, éviter les allocations inutiles, mesurer régulièrement |
| Dépassement de la taille de l'exécutable (10 Mo) | Bloquant | Limiter les dépendances, utiliser Native AOT avec compression |
| Latence de changement de page > 100 ms | Bloquant | Chargement paresseux, une seule page active en mémoire |
| Surcoût de lancement d'app > 50 ms | Bloquant | Utiliser CreateProcess directement, éviter les wrappers |

### Stabilité
| Risque | Impact | Mitigation |
|--------|--------|------------|
| Crash de Voyl → perte du bureau | Critique | Implémenter une touche de secours (Ctrl+Alt+Shift+Q) pour quitter |
| Conflit avec explorer.exe | Critique | Ne jamais killer explorer.exe, utiliser HWND_BOTTOM pour rester derrière |
| Fuites de mémoire | Majeur | Utiliser le GC de .NET, éviter les handles non libérés, tester avec Process Explorer |
| Fuites de handles Win32 | Majeur | Toujours appeler CloseHandle, utiliser `using` pour les objets IDisposable |

### Compatibilité
| Risque | Impact | Mitigation |
|--------|--------|------------|
| Incompatibilité avec Windows 10 | Majeur | Tester sur Windows 10 et 11, utiliser des APIs disponibles sur les deux |
| Incompatibilité avec les DPI non-standard | Mineur | Gérer WM_DPICHANGED, tester avec différents DPI |
| Incompatibilité avec les configurations multi-écrans | Mineur | Tester avec plusieurs écrans, gérer HMONITOR |

### Sécurité
| Risque | Impact | Mitigation |
|--------|--------|------------|
| Injection de code via config JSON | Majeur | Valider strictement la config, ne jamais exécuter de code dynamique |
| Élévation de privilèges involontaire | Critique | Ne jamais demander de droits admin, éviter les opérations système sensibles |
| Accès non autorisé aux fichiers | Mineur | Lire uniquement les raccourcis du menu Démarrer, pas d'écriture sans consentement |

### Maintenance
| Risque | Impact | Mitigation |
|--------|--------|------------|
| Dépendance à .NET 8 | Mineur | .NET 8 est LTS (support jusqu'en novembre 2026) |
| Changements dans les APIs Win32 | Mineur | Utiliser CsWin32 pour générer les bindings à jour |
| Complexité de la gestion des pages | Mineur | Garder le code simple, éviter les abstractions inutiles |

---

## Contraintes Non-Négociables

1. **Aucune dépendance NuGet sans accord explicite** : Tout package externe doit être validé.
2. **Pas de WPF/WinUI/WinForms/Electron/WebView** : Interdit par la stack verrouillée.
3. **Namespace `Voyl`** : Tout le code doit être dans ce namespace.
4. **Exécutable `Voyl.exe`** : Nom de l'exécutable final.
5. **Configuration dans `%APPDATA%\Voyl`** : Emplacement fixe pour les fichiers de config et logs.
6. **Structure de dossiers** : `src/`, `docs/`, `tests/` à la racine.
7. **Budgets stricts** :
   - Exécutable ≤ 10 Mo
   - RAM idle ≤ 30 Mo
   - CPU idle < 1 %
   - Changement de page < 100 ms
   - Surcoût de lancement < 50 ms

---

## Décisions Ouvertes (à valider plus tard)

- **Gestion des tuiles dynamiques** : Faut-il supporter les tuiles dynamiques (ex: météo, actualités) ?
  - Impact : Complexité et RAM supplémentaires.
- **Plugins** : Faut-il permettre des extensions via plugins ?
  - Impact : Sécurité, stabilité, RAM.
- **Multi-langues** : Faut-il supporter plusieurs langues pour l'UI ?
  - Impact : Taille de l'exécutable, complexité.
- **Thèmes personnalisés** : Faut-il permettre aux utilisateurs de créer leurs propres thèmes ?
  - Impact : Complexité, sécurité (chargement de fichiers externes).
