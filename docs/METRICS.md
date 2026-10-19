# Mesures des Performances - Voyl

> Méthodes pour mesurer chaque budget avec les outils Windows standard.

---

## Outils Recommandés

| Outil | Usage | Précision | Remarques |
|-------|-------|-----------|-----------|
| **Gestionnaire des tâches** | Mesure rapide de la RAM et du CPU | Moyenne | Intégré à Windows, facile d'accès |
| **Process Explorer** (Sysinternals) | Analyse détaillée des processus (handles, DLLs, threads) | Élevée | [Téléchargement](https://learn.microsoft.com/fr-fr/sysinternals/downloads/process-explorer) |
| **PerfMon** (Performance Monitor) | Mesure précise des compteurs système | Élevée | Intégré à Windows (`perfmon.exe`) |
| **Stopwatch** (C#) | Mesure de latence dans le code | Très élevée | Utiliser `System.Diagnostics.Stopwatch` |
| **Resource Monitor** | Surveillance temps réel des ressources | Moyenne | Intégré à Windows (`resmon.exe`) |
| **VMMap** (Sysinternals) | Analyse de la mémoire virtuelle | Élevée | [Téléchargement](https://learn.microsoft.com/fr-fr/sysinternals/downloads/vmmap) |

---

## Préparation des Mesures

### Conditions de Test
1. **Machine propre** :
   - Redémarrer la machine avant les tests.
   - Désactiver toutes les applications tierces au démarrage (via `msconfig` ou Gestionnaire des tâches > Onglet Démarrage).
   - Fermer tous les programmes non essentiels (navigateur, IDE, etc.).

2. **Stabilisation** :
   - Attendre **5 minutes** après le démarrage avant de mesurer (pour laisser le système se stabiliser).

3. **Outils** :
   - Ouvrir **Process Explorer** et **PerfMon** avant de lancer Voyl pour capturer les données dès le démarrage.

---

## Mesure de la RAM (Private Working Set)

### Définition
- **Private Working Set** : Mémoire physique utilisée par le processus, **non partagée** avec d'autres processus.
- **Cible** : ≤ 30 Mo pour Voyl en idle.

### Méthodes

#### 1. Gestionnaire des tâches
1. Ouvrir le Gestionnaire des tâches (`Ctrl+Shift+Esc`).
2. Onglet **Détails**.
3. Cliquer droit sur les colonnes > **Sélectionner des colonnes** > cocher **Set de travail privé**.
4. Trouver le processus `Voyl.exe` ou `explorer.exe`.
5. Noter la valeur dans la colonne **Set de travail privé**.

⚠️ **Limite** : Arrondi à 1 Mo près. Pas assez précis pour des mesures < 10 Mo.

#### 2. Process Explorer
1. Lancer **Process Explorer** (en tant qu'administrateur pour voir tous les processus).
2. Trouver `Voyl.exe` dans la liste.
3. Colonne **Private Bytes** = mémoire virtuelle privée.
4. Colonne **Working Set** = mémoire physique utilisée.
5. **Private Working Set** = `Working Set` - mémoire partagée (non directement visible, mais **Private Bytes** est un bon proxy).

📌 **À utiliser pour Voyl** : **Private Bytes** est la métrique la plus proche de notre cible.

#### 3. PerfMon
1. Lancer `perfmon.exe`.
2. Ajouter un compteur :
   - **Objet** : `Process`
   - **Compteur** : `Private Bytes`
   - **Instance** : `Voyl`
3. Lancer la mesure et noter la valeur stable après 5 min.

#### 4. VMMap (pour analyse détaillée)
1. Lancer **VMMap** (en tant qu'administrateur).
2. Sélectionner le processus `Voyl.exe`.
3. Vérifier la section **Private Data** pour voir la mémoire privée utilisée.

---

## Mesure du CPU (Utilisation)

### Définition
- **CPU idle** : Pourcentage d'utilisation du CPU par le processus **lorsqu'il ne fait rien** (pas d'interaction utilisateur).
- **Cible** : < 1 % pour Voyl en idle.

### Méthodes

#### 1. Gestionnaire des tâches
1. Onglet **Détails**.
2. Colonne **CPU**.
3. Noter la valeur pour `Voyl.exe` après 5 min d'inactivité.

⚠️ **Limite** : Moyenne sur 1 seconde, peut fluctuer.

#### 2. Process Explorer
1. Colonne **CPU** (affichée par défaut).
2. Noter la valeur moyenne sur 10 secondes.

#### 3. PerfMon
1. Ajouter un compteur :
   - **Objet** : `Process`
   - **Compteur** : `% Processor Time`
   - **Instance** : `Voyl`
2. Mesurer la moyenne sur 30 secondes.

---

## Mesure de la Taille de l'Exécutable

### Définition
- **Taille de Voyl.exe** : Taille du fichier binaire publié en mode Release AOT.
- **Cible** : ≤ 10 Mo.

### Méthode
1. Compiler en mode **Release** avec Native AOT :
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained true /p:PublishAot=true
   ```
2. Trouver le fichier `Voyl.exe` dans `bin/Release/net8.0/win-x64/publish/`.
3. Vérifier sa taille avec l'Explorateur de fichiers ou :
   ```powershell
   Get-Item .\bin\Release\net8.0\win-x64\publish\Voyl.exe | Select-Object Length
   ```
   (La valeur `Length` est en octets. Diviser par 1MB pour obtenir la taille en Mo.)

---

## Mesure de la Latence de Changement de Page

### Définition
- **Latence** : Temps entre le déclenchement du changement de page (ex: raccourci clavier) et l'affichage complet de la nouvelle page.
- **Cible** : < 100 ms.

### Méthodes

#### 1. Stopwatch dans le Code (Recommandé)
Ajouter dans `PageManager.cs` :
```csharp
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// ... code de changement de page ...
stopwatch.Stop();
Console.WriteLine($