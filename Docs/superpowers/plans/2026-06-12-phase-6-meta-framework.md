# Phase 6 Meta Framework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a data-driven Hub, case registry, progression, save, archive, bestiary, chapter, and automatic return flow around the existing Room 304 investigation loop.

**Architecture:** Use one shared `CombatSandbox` investigation scene selected through a persistent runtime session. Keep save data and progression rules in a tested `LostCity.Meta.Core` assembly, use ScriptableObjects for content definitions, generate a runtime `GameContentCatalog`, and keep Unity-facing managers thin.

**Tech Stack:** Unity 2022.3.32f1, C#, Unity Test Framework 1.1.33, Unity UI, Unity Input System, ScriptableObjects, JSON files under `Application.persistentDataPath`.

---

## File Structure

### Pure Meta Core

```text
Assets/_Project/Code/Meta/Core/
|-- LostCity.Meta.Core.asmdef
|-- CaseState.cs
|-- PlayerProgressData.cs
|-- SaveGameData.cs
|-- SaveDataSanitizer.cs
|-- CaseProgressionInput.cs
|-- CaseProgressionRules.cs
|-- CaseStartBlockReason.cs
`-- CaseStartRules.cs
```

Purpose: Unity-independent save data, sanitization, set semantics, and case/chapter state rules.

### Runtime Meta Framework

```text
Assets/_Project/Code/Meta/Runtime/
|-- RuntimeBootstrap.cs
|-- GameRuntime.cs
|-- SaveGameManager.cs
|-- CaseRegistry.cs
|-- CaseRegistryEntry.cs
|-- CaseSessionManager.cs
|-- SceneFlowManager.cs
|-- PlayerProgressPersistence.cs
`-- MetaProgressionRecorder.cs
```

Purpose: persistent services, scene flow, content lookup, and Unity integration.

### Content Definitions

```text
Assets/_Project/Code/Meta/Definitions/
|-- ChapterDefinition.cs
`-- GameContentCatalog.cs
```

Existing definitions modified:

```text
Assets/_Project/Code/CombatSandbox/Investigation/CaseDefinition.cs
Assets/_Project/Code/CombatSandbox/Enemies/EnemyDefinition.cs
Assets/_Project/Code/CombatSandbox/Enemies/BossDefinition.cs
```

### Hub And Generic UI

```text
Assets/_Project/Code/Meta/UI/
|-- HubController.cs
|-- HubCaseRow.cs
|-- InvestigationArchiveUI.cs
`-- BestiaryUI.cs
```

Existing Room 304-named classes renamed:

```text
Room304RewardSelectionUI.cs -> RewardSelectionUI.cs
Room304CompletionUI.cs -> CaseCompletionUI.cs
Room304DebugTools.cs -> PrototypeDebugTools.cs
```

Legacy files removed:

```text
Assets/_Project/Code/CombatSandbox/Core/Room304GameStateController.cs
Assets/_Project/Code/CombatSandbox/Progression/Room304RewardType.cs
```

### Editor Automation

```text
Assets/_Project/Code/CombatSandbox/Editor/
|-- CombatSandboxCreator.cs
|-- GameContentCatalogBuilder.cs
`-- GameContentCatalogPostprocessor.cs
```

### Tests

```text
Assets/_Project/Tests/EditMode/Meta/
|-- LostCity.Meta.Core.Tests.asmdef
|-- SaveGameDataTests.cs
|-- CaseProgressionRulesTests.cs
`-- SaveGameManagerTests.cs
```

### Documentation

Create:

```text
Docs/API/SaveSystem.md
Docs/API/CaseRegistry.md
Docs/API/ArchiveSystem.md
Docs/API/BestiarySystem.md
```

Update:

```text
README.md
Docs/README.md
Docs/Architecture.md
Docs/EventFlow.md
Docs/GameplayLoop.md
Docs/FolderStructure.md
Docs/DeveloperOnboarding.md
Docs/Roadmap.md
Docs/PhaseReport.md
Docs/API/CaseSystem.md
Docs/API/ChapterSystem.md
Docs/API/GameFlowManager.md
Docs/API/InvestigationSystem.md
Docs/API/EnemySystem.md
Docs/API/BossSystem.md
Docs/API/RewardSystem.md
Docs/API/UISystem.md
```

## Verification Command

Use this command for EditMode tests:

```bash
"/Applications/Unity/Hub/Editor/2022.3.32f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -nographics \
  -projectPath "/Users/vaema/Documents/Lost City" \
  -runTests \
  -testPlatform EditMode \
  -testResults "/tmp/lost-city-phase6-editmode.xml" \
  -logFile "/tmp/lost-city-phase6-editmode.log" \
  -quit
```

If Unity licensing prevents batch mode, compile with the current Unity response files and report EditMode execution as manually pending. Do not use Computer Use.

---

### Task 1: Create Versioned Save Data And Sanitization

**Files:**
- Create: `Assets/_Project/Code/Meta/Core/LostCity.Meta.Core.asmdef`
- Create: `Assets/_Project/Code/Meta/Core/CaseState.cs`
- Create: `Assets/_Project/Code/Meta/Core/PlayerProgressData.cs`
- Create: `Assets/_Project/Code/Meta/Core/SaveGameData.cs`
- Create: `Assets/_Project/Code/Meta/Core/SaveDataSanitizer.cs`
- Create: `Assets/_Project/Tests/EditMode/Meta/LostCity.Meta.Core.Tests.asmdef`
- Create: `Assets/_Project/Tests/EditMode/Meta/SaveGameDataTests.cs`
- Create: `Docs/API/SaveSystem.md`

- [ ] **Step 1: Add the production and test assembly definitions**

`LostCity.Meta.Core.asmdef`:

```json
{
  "name": "LostCity.Meta.Core",
  "rootNamespace": "LostCity.Meta.Core",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": true
}
```

`LostCity.Meta.Core.Tests.asmdef`:

```json
{
  "name": "LostCity.Meta.Core.Tests",
  "rootNamespace": "LostCity.Meta.Core.Tests",
  "references": [
    "LostCity.Meta.Core"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false,
  "optionalUnityReferences": [
    "TestAssemblies"
  ]
}
```

- [ ] **Step 2: Write failing tests for default and sanitized save data**

```csharp
using System.Collections.Generic;
using NUnit.Framework;

namespace LostCity.Meta.Core.Tests
{
    public sealed class SaveGameDataTests
    {
        [Test]
        public void CreateDefault_InitializesEveryRequiredCollectionAndPlayerValue()
        {
            SaveGameData data = SaveGameData.CreateDefault();

            Assert.That(data.Version, Is.EqualTo(SaveGameData.CurrentVersion));
            Assert.That(data.CompletedCaseIds, Is.Empty);
            Assert.That(data.PermanentUnlockIds, Is.Empty);
            Assert.That(data.DiscoveredClueIds, Is.Empty);
            Assert.That(data.EncounteredEnemyIds, Is.Empty);
            Assert.That(data.EncounteredBossIds, Is.Empty);
            Assert.That(data.PlayerProgress.Level, Is.EqualTo(1));
            Assert.That(data.PlayerProgress.AttackMultiplier, Is.EqualTo(1f));
            Assert.That(data.PlayerProgress.MaxHpMultiplier, Is.EqualTo(1f));
            Assert.That(data.PlayerProgress.FireRateMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Sanitize_RemovesBlankAndDuplicateIdsAndRepairsInvalidProgress()
        {
            SaveGameData data = SaveGameData.CreateDefault();
            data.CompletedCaseIds = new List<string> { "room_304", "", "room_304", " " };
            data.PlayerProgress.Level = 0;
            data.PlayerProgress.AttackMultiplier = -2f;

            SaveDataSanitizer.Sanitize(data);

            Assert.That(data.CompletedCaseIds, Is.EqualTo(new[] { "room_304" }));
            Assert.That(data.PlayerProgress.Level, Is.EqualTo(1));
            Assert.That(data.PlayerProgress.AttackMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void AddUnique_ReturnsFalseForExistingId()
        {
            List<string> ids = new List<string>();

            Assert.That(SaveDataSanitizer.AddUnique(ids, "room_304"), Is.True);
            Assert.That(SaveDataSanitizer.AddUnique(ids, "room_304"), Is.False);
            Assert.That(ids, Is.EqualTo(new[] { "room_304" }));
        }
    }
}
```

- [ ] **Step 3: Run EditMode tests and verify RED**

Expected: compile fails because `SaveGameData`, `PlayerProgressData`, and `SaveDataSanitizer` do not exist.

- [ ] **Step 4: Implement minimal core data**

```csharp
using System;

namespace LostCity.Meta.Core
{
    public enum CaseState
    {
        Locked,
        Available,
        Completed
    }

    [Serializable]
    public sealed class PlayerProgressData
    {
        public int Level = 1;
        public int CurrentExperience;
        public int ExperienceToNextLevel = 5;
        public float AttackMultiplier = 1f;
        public float MaxHpMultiplier = 1f;
        public float FireRateMultiplier = 1f;
        public float CritChanceBonus;
        public float DodgeChanceBonus;
        public int DroneProjectileBonus;
    }
}
```

```csharp
using System;
using System.Collections.Generic;

namespace LostCity.Meta.Core
{
    [Serializable]
    public sealed class SaveGameData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public List<string> CompletedCaseIds = new List<string>();
        public List<string> PermanentUnlockIds = new List<string>();
        public List<string> DiscoveredClueIds = new List<string>();
        public List<string> EncounteredEnemyIds = new List<string>();
        public List<string> EncounteredBossIds = new List<string>();
        public PlayerProgressData PlayerProgress = new PlayerProgressData();

        public static SaveGameData CreateDefault()
        {
            return new SaveGameData();
        }
    }
}
```

```csharp
using System;
using System.Collections.Generic;

namespace LostCity.Meta.Core
{
    public static class SaveDataSanitizer
    {
        public static SaveGameData Sanitize(SaveGameData data)
        {
            data ??= SaveGameData.CreateDefault();
            data.Version = SaveGameData.CurrentVersion;
            data.CompletedCaseIds = SanitizeIds(data.CompletedCaseIds);
            data.PermanentUnlockIds = SanitizeIds(data.PermanentUnlockIds);
            data.DiscoveredClueIds = SanitizeIds(data.DiscoveredClueIds);
            data.EncounteredEnemyIds = SanitizeIds(data.EncounteredEnemyIds);
            data.EncounteredBossIds = SanitizeIds(data.EncounteredBossIds);
            data.PlayerProgress ??= new PlayerProgressData();
            data.PlayerProgress.Level = Math.Max(1, data.PlayerProgress.Level);
            data.PlayerProgress.ExperienceToNextLevel = Math.Max(1, data.PlayerProgress.ExperienceToNextLevel);
            data.PlayerProgress.CurrentExperience = Math.Max(0, data.PlayerProgress.CurrentExperience);
            data.PlayerProgress.AttackMultiplier = PositiveOrOne(data.PlayerProgress.AttackMultiplier);
            data.PlayerProgress.MaxHpMultiplier = PositiveOrOne(data.PlayerProgress.MaxHpMultiplier);
            data.PlayerProgress.FireRateMultiplier = PositiveOrOne(data.PlayerProgress.FireRateMultiplier);
            data.PlayerProgress.CritChanceBonus = Math.Max(0f, data.PlayerProgress.CritChanceBonus);
            data.PlayerProgress.DodgeChanceBonus = Math.Max(0f, data.PlayerProgress.DodgeChanceBonus);
            data.PlayerProgress.DroneProjectileBonus = Math.Max(0, data.PlayerProgress.DroneProjectileBonus);
            return data;
        }

        public static bool AddUnique(List<string> values, string id)
        {
            if (values == null || string.IsNullOrWhiteSpace(id) || values.Contains(id))
            {
                return false;
            }

            values.Add(id);
            return true;
        }

        private static List<string> SanitizeIds(List<string> source)
        {
            List<string> result = new List<string>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                AddUnique(result, source[i]?.Trim());
            }

            return result;
        }

        private static float PositiveOrOne(float value)
        {
            return value > 0f ? value : 1f;
        }
    }
}
```

- [ ] **Step 5: Run EditMode tests and verify GREEN**

Expected: all `SaveGameDataTests` pass.

- [ ] **Step 6: Document the save data contract**

Create `Docs/API/SaveSystem.md` with purpose, version 1 fields, mutation rules, autosave behavior, dependencies, usage, corrupt-save fallback, and the fact that current HP is not persisted.

- [ ] **Step 7: Commit the tested save model and documentation**

```bash
git add Assets/_Project/Code/Meta/Core Assets/_Project/Tests/EditMode/Meta Docs/API/SaveSystem.md
git commit -m "feat: add versioned meta save data"
```

---

### Task 2: Implement And Test Case And Chapter Progression Rules

**Files:**
- Create: `Assets/_Project/Code/Meta/Core/CaseProgressionInput.cs`
- Create: `Assets/_Project/Code/Meta/Core/CaseProgressionRules.cs`
- Create: `Assets/_Project/Code/Meta/Core/CaseStartBlockReason.cs`
- Create: `Assets/_Project/Code/Meta/Core/CaseStartRules.cs`
- Create: `Assets/_Project/Tests/EditMode/Meta/CaseProgressionRulesTests.cs`
- Update: `Docs/API/CaseSystem.md`
- Update: `Docs/API/ChapterSystem.md`

- [ ] **Step 1: Write failing progression tests**

```csharp
using System.Collections.Generic;
using NUnit.Framework;

namespace LostCity.Meta.Core.Tests
{
    public sealed class CaseProgressionRulesTests
    {
        [Test]
        public void FirstCaseInUnlockedChapter_IsAvailable()
        {
            CaseProgressionInput input = Chapter("room_304", "placeholder");

            CaseState state = CaseProgressionRules.GetCaseState("room_304", input, new HashSet<string>());

            Assert.That(state, Is.EqualTo(CaseState.Available));
        }

        [Test]
        public void LaterCase_IsLockedUntilPreviousCaseCompletes()
        {
            CaseProgressionInput input = Chapter("room_304", "placeholder");

            Assert.That(
                CaseProgressionRules.GetCaseState("placeholder", input, new HashSet<string>()),
                Is.EqualTo(CaseState.Locked));

            Assert.That(
                CaseProgressionRules.GetCaseState("placeholder", input, new HashSet<string> { "room_304" }),
                Is.EqualTo(CaseState.Available));
        }

        [Test]
        public void CompletedCase_RemainsCompleted()
        {
            CaseProgressionInput input = Chapter("room_304", "placeholder");

            CaseState state = CaseProgressionRules.GetCaseState(
                "room_304",
                input,
                new HashSet<string> { "room_304" });

            Assert.That(state, Is.EqualTo(CaseState.Completed));
        }

        [Test]
        public void ChapterPrerequisites_LockEveryCase()
        {
            CaseProgressionInput input = new CaseProgressionInput(
                new[] { "case_a" },
                new[] { "required_case" });

            CaseState state = CaseProgressionRules.GetCaseState("case_a", input, new HashSet<string>());

            Assert.That(state, Is.EqualTo(CaseState.Locked));
        }

        [Test]
        public void CurrentChapter_IsFirstUnlockedChapterWithIncompleteCases()
        {
            CaseProgressionInput[] chapters =
            {
                new CaseProgressionInput(new[] { "room_304" }, new string[0]),
                new CaseProgressionInput(new[] { "future_case" }, new[] { "room_304" })
            };

            Assert.That(
                CaseProgressionRules.GetCurrentChapterIndex(chapters, new HashSet<string>()),
                Is.EqualTo(0));

            Assert.That(
                CaseProgressionRules.GetCurrentChapterIndex(chapters, new HashSet<string> { "room_304" }),
                Is.EqualTo(1));
        }

        [Test]
        public void MetadataOnlyAvailableCase_CannotStart()
        {
            bool canStart = CaseStartRules.CanStart(
                CaseState.Available,
                isPlayable: false,
                hasRequiredContent: false,
                out CaseStartBlockReason reason);

            Assert.That(canStart, Is.False);
            Assert.That(reason, Is.EqualTo(CaseStartBlockReason.NotPlayable));
        }

        private static CaseProgressionInput Chapter(params string[] caseIds)
        {
            return new CaseProgressionInput(caseIds, new string[0]);
        }
    }
}
```

- [ ] **Step 2: Run tests and verify RED**

Expected: compile fails because progression input and rules do not exist.

- [ ] **Step 3: Implement the minimal progression evaluator**

```csharp
namespace LostCity.Meta.Core
{
    public sealed class CaseProgressionInput
    {
        public CaseProgressionInput(string[] orderedCaseIds, string[] requiredCompletedCaseIds)
        {
            OrderedCaseIds = orderedCaseIds ?? new string[0];
            RequiredCompletedCaseIds = requiredCompletedCaseIds ?? new string[0];
        }

        public string[] OrderedCaseIds { get; }
        public string[] RequiredCompletedCaseIds { get; }
    }
}
```

```csharp
using System;
using System.Collections.Generic;

namespace LostCity.Meta.Core
{
    public static class CaseProgressionRules
    {
        public static CaseState GetCaseState(
            string caseId,
            CaseProgressionInput chapter,
            ISet<string> completedCaseIds)
        {
            if (string.IsNullOrWhiteSpace(caseId) || chapter == null)
            {
                return CaseState.Locked;
            }

            completedCaseIds ??= new HashSet<string>();
            if (completedCaseIds.Contains(caseId))
            {
                return CaseState.Completed;
            }

            if (!AreAllCompleted(chapter.RequiredCompletedCaseIds, completedCaseIds))
            {
                return CaseState.Locked;
            }

            int index = Array.IndexOf(chapter.OrderedCaseIds, caseId);
            if (index < 0)
            {
                return CaseState.Locked;
            }

            return index == 0 || completedCaseIds.Contains(chapter.OrderedCaseIds[index - 1])
                ? CaseState.Available
                : CaseState.Locked;
        }

        private static bool AreAllCompleted(string[] requiredIds, ISet<string> completedCaseIds)
        {
            for (int i = 0; i < requiredIds.Length; i++)
            {
                if (!completedCaseIds.Contains(requiredIds[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static int GetCurrentChapterIndex(
            IReadOnlyList<CaseProgressionInput> chapters,
            ISet<string> completedCaseIds)
        {
            completedCaseIds ??= new HashSet<string>();
            int latestUnlocked = -1;

            for (int i = 0; i < chapters.Count; i++)
            {
                CaseProgressionInput chapter = chapters[i];
                if (!AreAllCompleted(chapter.RequiredCompletedCaseIds, completedCaseIds))
                {
                    continue;
                }

                latestUnlocked = i;
                if (!AreAllCompleted(chapter.OrderedCaseIds, completedCaseIds))
                {
                    return i;
                }
            }

            return latestUnlocked;
        }
    }
}
```

- [ ] **Step 4: Implement start validation rules**

```csharp
namespace LostCity.Meta.Core
{
    public enum CaseStartBlockReason
    {
        None,
        Locked,
        NotPlayable,
        MissingContent
    }

    public static class CaseStartRules
    {
        public static bool CanStart(
            CaseState state,
            bool isPlayable,
            bool hasRequiredContent,
            out CaseStartBlockReason reason)
        {
            if (state == CaseState.Locked)
            {
                reason = CaseStartBlockReason.Locked;
                return false;
            }

            if (!isPlayable)
            {
                reason = CaseStartBlockReason.NotPlayable;
                return false;
            }

            if (!hasRequiredContent)
            {
                reason = CaseStartBlockReason.MissingContent;
                return false;
            }

            reason = CaseStartBlockReason.None;
            return true;
        }
    }
}
```

- [ ] **Step 5: Run tests and verify GREEN**

Expected: all progression tests pass.

- [ ] **Step 6: Update case and chapter API documentation**

Document `CaseState`, sequential unlocking, chapter prerequisites, replay behavior, and metadata-only non-playable cases.

- [ ] **Step 7: Commit rules, tests, and documentation**

```bash
git add Assets/_Project/Code/Meta/Core Assets/_Project/Tests/EditMode/Meta Docs/API/CaseSystem.md Docs/API/ChapterSystem.md
git commit -m "feat: add data-driven case progression rules"
```

---

### Task 3: Extend Content Definitions And Build The Generated Catalog

**Files:**
- Update: `Assets/_Project/Code/CombatSandbox/Investigation/CaseDefinition.cs`
- Update: `Assets/_Project/Code/CombatSandbox/Enemies/EnemyDefinition.cs`
- Update: `Assets/_Project/Code/CombatSandbox/Enemies/BossDefinition.cs`
- Create: `Assets/_Project/Code/Meta/Definitions/ChapterDefinition.cs`
- Create: `Assets/_Project/Code/Meta/Definitions/GameContentCatalog.cs`
- Create: `Assets/_Project/Code/CombatSandbox/Editor/GameContentCatalogBuilder.cs`
- Create: `Assets/_Project/Code/CombatSandbox/Editor/GameContentCatalogPostprocessor.cs`
- Update: `Docs/Architecture.md`
- Update: `Docs/FolderStructure.md`

- [ ] **Step 1: Add stable IDs and archive/bestiary metadata**

Add to `CaseDefinition`:

```csharp
[SerializeField, TextArea] private string caseSummary;
[SerializeField] private bool isPlayable = true;

public string CaseSummary => caseSummary;
public bool IsPlayable => isPlayable;
```

Add to `EnemyDefinition`:

```csharp
[SerializeField] private string enemyId;
[SerializeField, TextArea] private string bestiaryDescription;

public string EnemyId => enemyId;
public string BestiaryDescription => bestiaryDescription;
```

Add to `BossDefinition`:

```csharp
[SerializeField] private string bossId;
[SerializeField, TextArea] private string bestiaryDescription;

public string BossId => bossId;
public string BestiaryDescription => bestiaryDescription;
```

- [ ] **Step 2: Create chapter and catalog definitions**

`ChapterDefinition` exposes:

```csharp
public string ChapterId { get; }
public string ChapterName { get; }
public string Description { get; }
public CaseDefinition[] Cases { get; }
public string[] RequiredCompletedCaseIds { get; }
```

`GameContentCatalog` exposes read-only arrays for cases, chapters, clues, enemies, and bosses.

- [ ] **Step 3: Implement the Editor catalog builder**

`GameContentCatalogBuilder.Rebuild()` must:

1. Find all assets using `AssetDatabase.FindAssets("t:CaseDefinition")` and equivalent filters.
2. Load and sort assets deterministically by asset path.
3. Create or update `Assets/_Project/Resources/GameContentCatalog.asset`.
4. Assign all arrays with `SerializedObject`.
5. Save assets and return the catalog.

Expose menu item:

```text
Tools/Lost City/Rebuild Content Catalog
```

- [ ] **Step 4: Implement automatic rebuilding**

`GameContentCatalogPostprocessor.OnPostprocessAllAssets(...)` checks whether imported, deleted, moved, or moved-from paths contain `.asset`. Schedule one delayed rebuild with `EditorApplication.delayCall` and prevent recursive rebuilds.

- [ ] **Step 5: Add validation**

The builder logs errors for:

- Empty IDs.
- Duplicate case IDs.
- Duplicate chapter IDs.
- Duplicate clue IDs.
- Duplicate enemy IDs.
- Duplicate boss IDs.
- A chapter referencing the same case more than once.

Catalog generation remains successful but excludes ambiguous duplicate IDs at runtime.

- [ ] **Step 6: Update architecture and folder documentation**

Add Mermaid data flow:

```text
ScriptableObject assets
-> GameContentCatalogBuilder
-> GameContentCatalog
-> CaseRegistry
-> Hub / Archive / Bestiary
```

- [ ] **Step 7: Compile and commit**

Run Unity compile verification. Then:

```bash
git add Assets/_Project/Code/Meta/Definitions Assets/_Project/Code/CombatSandbox/Investigation/CaseDefinition.cs Assets/_Project/Code/CombatSandbox/Enemies Assets/_Project/Code/CombatSandbox/Editor/GameContentCatalogBuilder.cs Assets/_Project/Code/CombatSandbox/Editor/GameContentCatalogPostprocessor.cs Docs/Architecture.md Docs/FolderStructure.md
git commit -m "feat: add generated meta content catalog"
```

---

### Task 4: Implement And Test SaveGameManager File Persistence

**Files:**
- Create: `Assets/_Project/Code/Meta/Persistence/LostCity.Meta.Persistence.asmdef`
- Create: `Assets/_Project/Code/Meta/Persistence/SaveGameManager.cs`
- Create: `Assets/_Project/Tests/EditMode/Meta/SaveGameManagerTests.cs`
- Update: `Docs/API/SaveSystem.md`

- [ ] **Step 1: Add persistence assembly**

The assembly references:

```json
[
  "LostCity.Meta.Core"
]
```

It keeps Unity engine references enabled so it can use `MonoBehaviour` and `JsonUtility`.

Also update `LostCity.Meta.Core.Tests.asmdef` to reference `LostCity.Meta.Persistence`.

- [ ] **Step 2: Write failing temporary-file tests**

Tests create a unique directory under `Path.GetTempPath()`, initialize `SaveGameManager` with an explicit test path, mutate it, construct a fresh manager, and assert:

- Completed case survives reload.
- Discovered clue survives reload.
- Enemy and boss encounters survive reload.
- Permanent unlock survives reload.
- Player progress survives reload.
- Duplicate mutation returns false and does not duplicate data.
- Corrupt JSON is backed up and default data loads.

Use `Object.DestroyImmediate` in teardown.

- [ ] **Step 3: Run tests and verify RED**

Expected: compile fails because `SaveGameManager` does not exist.

- [ ] **Step 4: Implement SaveGameManager**

Required public API:

```csharp
public string SavePath { get; }
public event Action DataChanged;
public IReadOnlyList<string> CompletedCaseIds { get; }
public IReadOnlyList<string> PermanentUnlockIds { get; }
public IReadOnlyList<string> DiscoveredClueIds { get; }
public IReadOnlyList<string> EncounteredEnemyIds { get; }
public IReadOnlyList<string> EncounteredBossIds { get; }

public void Initialize(string explicitPath = null);
public bool IsCaseCompleted(string caseId);
public PlayerProgressData GetPlayerProgress();
public SaveGameData GetSnapshot();
public bool CompleteCase(string caseId);
public bool DiscoverClue(string clueId);
public bool EncounterEnemy(string enemyId);
public bool EncounterBoss(string bossId);
public bool AddPermanentUnlock(string unlockId);
public void SetPlayerProgress(PlayerProgressData progress);
public void SaveNow();
public void Reload();
```

Implementation requirements:

- Default path is `Application.persistentDataPath/lost-city-save.json`.
- `Initialize()` is idempotent.
- Every successful mutation calls `SaveNow()` and `DataChanged`.
- Write JSON to `<save>.tmp`, then replace the target.
- On corrupt JSON, move it to `<save>.corrupt-<UTC timestamp>.json`.
- Sanitize after load and before save.
- Copy `PlayerProgressData` values instead of retaining caller references.
- Return cloned progress and snapshot objects so callers cannot mutate authoritative save state directly.

- [ ] **Step 5: Run tests and verify GREEN**

Expected: all save manager tests pass and temporary files are removed in teardown.

- [ ] **Step 6: Update SaveSystem documentation**

Document atomic writes, corruption backup, automatic mutation saves, test path injection, and version migration entry points.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Code/Meta/Persistence Assets/_Project/Tests/EditMode/Meta Docs/API/SaveSystem.md
git commit -m "feat: persist meta progression to versioned json"
```

---

### Task 5: Add Runtime Bootstrap, Registry, Session, And Scene Flow

**Files:**
- Create: `Assets/_Project/Code/Meta/Runtime/GameRuntime.cs`
- Create: `Assets/_Project/Code/Meta/Runtime/RuntimeBootstrap.cs`
- Create: `Assets/_Project/Code/Meta/Runtime/CaseRegistryEntry.cs`
- Create: `Assets/_Project/Code/Meta/Runtime/CaseRegistry.cs`
- Create: `Assets/_Project/Code/Meta/Runtime/CaseSessionManager.cs`
- Create: `Assets/_Project/Code/Meta/Runtime/SceneFlowManager.cs`
- Update: `Docs/API/CaseRegistry.md`
- Update: `Docs/API/ChapterSystem.md`
- Update: `Docs/Architecture.md`

- [ ] **Step 1: Create the persistent runtime root**

`RuntimeBootstrap` uses:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void Initialize()
{
    if (GameRuntime.Instance != null)
    {
        return;
    }

    GameObject root = new GameObject("LostCityRuntime");
    UnityEngine.Object.DontDestroyOnLoad(root);
    root.AddComponent<GameRuntime>();
}
```

`GameRuntime.Awake()` creates or resolves:

- `SaveGameManager`
- `CaseRegistry`
- `CaseSessionManager`
- `SceneFlowManager`

It loads `Resources.Load<GameContentCatalog>("GameContentCatalog")`, initializes save first, then registry.

Expose:

```csharp
public static GameRuntime Instance { get; private set; }
public SaveGameManager SaveGame { get; private set; }
public CaseRegistry CaseRegistry { get; private set; }
public CaseSessionManager CaseSession { get; private set; }
public SceneFlowManager SceneFlow { get; private set; }
```

`OnDestroy()` clears `Instance` only when the destroyed object is the active runtime.

- [ ] **Step 2: Implement CaseRegistry indexing**

`CaseRegistry` exposes:

```csharp
public IReadOnlyList<ChapterDefinition> Chapters { get; }
public IReadOnlyList<CaseDefinition> Cases { get; }
public CaseDefinition GetCase(string caseId);
public CaseState GetCaseState(CaseDefinition definition);
public ChapterDefinition GetChapterForCase(CaseDefinition definition);
public ChapterDefinition GetCurrentChapter();
public bool CanStart(CaseDefinition definition, out string reason);
```

It uses `CaseProgressionRules`, `CaseStartRules`, and the completed IDs from `SaveGameManager`.

`CanStart` rejects:

- Null definitions.
- Locked cases.
- `IsPlayable == false`.
- Missing clues.
- Missing boss definition or boss prefab.

Use Chinese reasons such as `案件尚未解锁`, `案件仍在开发中`, and `案件配置不完整`.

- [ ] **Step 3: Implement CaseSessionManager**

```csharp
public string ActiveCaseId { get; }
public CaseDefinition ActiveCase { get; }
public bool HasActiveCase { get; }
public bool BeginCase(CaseDefinition definition);
public void EndCase();
```

`BeginCase` validates through registry and stores only one active case.

- [ ] **Step 4: Implement SceneFlowManager**

Serialized/default scene names:

```csharp
private string hubSceneName = "Hub";
private string investigationSceneName = "CombatSandbox";
```

Public API:

```csharp
public void LoadHub();
public bool StartCase(CaseDefinition definition);
```

`StartCase` begins session, checks `Application.CanStreamedLevelBeLoaded`, then loads investigation. If loading is impossible, end the session and log the target scene.

- [ ] **Step 5: Document registry and runtime ownership**

`Docs/API/CaseRegistry.md` documents lookup, states, validation, current chapter, and no Room 304 constants.

Update Architecture Mermaid diagram with persistent runtime and two scenes.

- [ ] **Step 6: Compile and commit**

```bash
git add Assets/_Project/Code/Meta/Runtime Docs/API/CaseRegistry.md Docs/API/ChapterSystem.md Docs/Architecture.md
git commit -m "feat: add persistent case runtime services"
```

---

### Task 6: Persist Player Level, XP, Permanent Stats, And Unlock Rewards

**Files:**
- Update: `Assets/_Project/Code/CombatSandbox/Player/PlayerStats.cs`
- Update: `Assets/_Project/Code/CombatSandbox/Progression/PlayerExperience.cs`
- Update: `Assets/_Project/Code/CombatSandbox/Progression/CombatUpgradeStats.cs`
- Update: `Assets/_Project/Code/CombatSandbox/Progression/RewardDefinition.cs`
- Update: `Assets/_Project/Code/CombatSandbox/UI/UpgradeSelectionController.cs`
- Create: `Assets/_Project/Code/Meta/Runtime/PlayerProgressPersistence.cs`
- Update: `Docs/API/PlayerStats.md`
- Update: `Docs/API/RewardSystem.md`
- Update: `Docs/API/SaveSystem.md`

- [ ] **Step 1: Separate baseline values from permanent modifiers**

In `PlayerStats.Awake()`, capture:

```csharp
baseMaxHp = maxHp;
baseAttack = attack;
baseFireRate = fireRate;
baseCritChance = critChance;
baseDodgeChance = dodgeChance;
```

Add:

```csharp
public event Action PermanentProgressChanged;
public float AttackMultiplier => Attack / Mathf.Max(0.01f, baseAttack);
public float MaxHpMultiplier => MaxHp / Mathf.Max(1f, baseMaxHp);
public float FireRateMultiplier => FireRate / Mathf.Max(0.01f, baseFireRate);
public float CritChanceBonus => Mathf.Max(0f, CritChance - baseCritChance);
public float DodgeChanceBonus => Mathf.Max(0f, DodgeChance - baseDodgeChance);
public void ApplyPermanentProgress(PlayerProgressData progress);
```

Permanent mutation methods raise both `StatsChanged` and `PermanentProgressChanged`. Health changes raise only `StatsChanged`.

- [ ] **Step 2: Add experience import/export**

`PlayerExperience` adds:

```csharp
public event Action ProgressChanged;
public void ApplySavedProgress(PlayerProgressData progress);
public void WriteProgressTo(PlayerProgressData progress);
```

Raise `ProgressChanged` after XP or level changes.

- [ ] **Step 3: Add drone import/export**

`CombatUpgradeStats` adds:

```csharp
public event Action PermanentProgressChanged;
public void SetExtraDroneProjectiles(int amount);
public void WriteProgressTo(PlayerProgressData progress);
```

- [ ] **Step 4: Implement PlayerProgressPersistence**

On `Start()`:

1. Resolve player components and `SaveGameManager`.
2. Apply saved `PlayerProgressData`.
3. Subscribe to permanent stat, experience, and drone events.

On mutation, capture:

```csharp
PlayerProgressData data = new PlayerProgressData();
playerExperience.WriteProgressTo(data);
data.AttackMultiplier = playerStats.AttackMultiplier;
data.MaxHpMultiplier = playerStats.MaxHpMultiplier;
data.FireRateMultiplier = playerStats.FireRateMultiplier;
data.CritChanceBonus = playerStats.CritChanceBonus;
data.DodgeChanceBonus = playerStats.DodgeChanceBonus;
combatUpgradeStats.WriteProgressTo(data);
saveGameManager.SetPlayerProgress(data);
```

- [ ] **Step 5: Persist unlock rewards**

After any reward choice, if:

```csharp
rewardDefinition.RewardType == RewardType.Unlock
```

call:

```csharp
GameRuntime.Instance?.SaveGame.AddPermanentUnlock(rewardDefinition.UnlockId);
```

Do this through `PlayerProgressPersistence.RecordReward(RewardDefinition)` so UI does not own save logic.

- [ ] **Step 6: Compile and update documentation**

Document persisted versus run-only values and why current HP is excluded.

- [ ] **Step 7: Commit**

```bash
git add Assets/_Project/Code/CombatSandbox/Player Assets/_Project/Code/CombatSandbox/Progression Assets/_Project/Code/CombatSandbox/UI/UpgradeSelectionController.cs Assets/_Project/Code/Meta/Runtime/PlayerProgressPersistence.cs Docs/API/PlayerStats.md Docs/API/RewardSystem.md Docs/API/SaveSystem.md
git commit -m "feat: persist player meta progression"
```

---

### Task 7: Generalize Investigation, Reward, Completion, And Return Flow

**Files:**
- Update: `Assets/_Project/Code/CombatSandbox/Investigation/InvestigationProgress.cs`
- Update: `Assets/_Project/Code/CombatSandbox/Core/GameFlowManager.cs`
- Move: `Assets/_Project/Code/CombatSandbox/UI/Room304RewardSelectionUI.cs` to `Assets/_Project/Code/CombatSandbox/UI/RewardSelectionUI.cs`
- Move: `Assets/_Project/Code/CombatSandbox/UI/Room304CompletionUI.cs` to `Assets/_Project/Code/CombatSandbox/UI/CaseCompletionUI.cs`
- Move: `Assets/_Project/Code/CombatSandbox/Debug/Room304DebugTools.cs` to `Assets/_Project/Code/CombatSandbox/Debug/PrototypeDebugTools.cs`
- Delete: `Assets/_Project/Code/CombatSandbox/Core/Room304GameStateController.cs`
- Delete: `Assets/_Project/Code/CombatSandbox/Progression/Room304RewardType.cs`
- Update: `Docs/API/GameFlowManager.md`
- Update: `Docs/API/InvestigationSystem.md`
- Update: `Docs/API/RewardSystem.md`
- Update: `Docs/EventFlow.md`

- [ ] **Step 1: Resolve the selected case before investigation starts**

`InvestigationProgress.Awake()`:

```csharp
if (GameRuntime.Instance != null && GameRuntime.Instance.CaseSession.HasActiveCase)
{
    caseDefinition = GameRuntime.Instance.CaseSession.ActiveCase;
}

if (caseDefinition == null)
{
    Debug.LogError("未找到当前案件配置。", this);
    GameRuntime.Instance?.SceneFlow.LoadHub();
}
```

Keep the serialized definition as a direct-scene fallback.

- [ ] **Step 2: Rename Room 304-specific UI and debug classes**

Use `apply_patch` moves so existing `.meta` GUIDs move with the scripts.

Rename class identifiers and all references:

```text
Room304RewardSelectionUI -> RewardSelectionUI
Room304CompletionUI -> CaseCompletionUI
Room304DebugTools -> PrototypeDebugTools
```

Delete the unused legacy controller and enum with their `.meta` files.

- [ ] **Step 3: Make reward UI case-driven**

Change the reward UI body from a Room 304 string to:

```csharp
public void Show(
    PlayerStats playerStats,
    string completionText,
    RewardDefinition[] rewards,
    CombatUpgradeStats upgradeStats)
```

Visible text:

```text
真相已揭开
{CaseDefinition.CompletionText}
选择一项奖励
```

- [ ] **Step 4: Make completion UI return automatically**

`CaseCompletionUI`:

- Uses title `案件完成`.
- Shows case completion text.
- Shows `正在返回大厅`.
- Starts an unscaled-time three-second countdown.
- Emits `ReturnRequested` when countdown ends.
- Space emits it immediately.

Remove `ShowNextChapterPlaceholder`.

- [ ] **Step 5: Replace GameFlowManager completion behavior**

On reward selection:

1. Record unlock and capture permanent player progress.
2. Call `SaveGameManager.CompleteCase(activeCase.CaseId)`.
3. Enter `ChapterComplete`.
4. Show `CaseCompletionUI`.

On return request:

1. Log generic case completion with ID.
2. End active session.
3. Load Hub.

Replace logs:

```text
Case Started: {id}
Case Deduction Success: {id}
Boss Spawned: {bossId}
Boss Defeated: {bossId}
Reward Selected: {rewardId}
Case Completed: {id}
```

No runtime log contains a Room 304 constant.

- [ ] **Step 6: Update event-flow and API documentation**

Document the new case completion and scene transition chains.

- [ ] **Step 7: Compile and commit**

```bash
git add Assets/_Project/Code/CombatSandbox/Core Assets/_Project/Code/CombatSandbox/Debug Assets/_Project/Code/CombatSandbox/Investigation Assets/_Project/Code/CombatSandbox/Progression Assets/_Project/Code/CombatSandbox/UI Docs/API/GameFlowManager.md Docs/API/InvestigationSystem.md Docs/API/RewardSystem.md Docs/EventFlow.md
git commit -m "feat: return completed cases to the hub"
```

---

### Task 8: Record Discovered Clues And Bestiary Encounters

**Files:**
- Update: `Assets/_Project/Code/CombatSandbox/Investigation/InvestigationProgress.cs`
- Update: `Assets/_Project/Code/CombatSandbox/Enemies/MemoryFragmentEnemy.cs`
- Update: `Assets/_Project/Code/CombatSandbox/Spawning/BossSpawnController.cs`
- Create: `Assets/_Project/Code/Meta/Runtime/MetaProgressionRecorder.cs`
- Update: `Docs/API/InvestigationSystem.md`
- Update: `Docs/API/EnemySystem.md`
- Update: `Docs/API/BossSystem.md`
- Update: `Docs/EventFlow.md`

- [ ] **Step 1: Create a small recording facade**

```csharp
public static class MetaProgressionRecorder
{
    public static void RecordClue(ClueDefinition clue);
    public static void RecordEnemy(EnemyDefinition enemy);
    public static void RecordBoss(BossDefinition boss);
}
```

Each method:

- Ignores null or blank IDs.
- Resolves `GameRuntime.Instance?.SaveGame`.
- Calls the focused save mutation method.

- [ ] **Step 2: Record clue discovery**

In `InvestigationProgress.TryCollectClue`, after adding the clue:

```csharp
MetaProgressionRecorder.RecordClue(clue);
```

- [ ] **Step 3: Record enemy encounters**

In `MemoryFragmentEnemy.OnEnable`, after dependencies exist:

```csharp
MetaProgressionRecorder.RecordEnemy(definition);
```

- [ ] **Step 4: Record boss encounters**

In `BossSpawnController.SpawnBoss`, after applying the boss definition:

```csharp
MetaProgressionRecorder.RecordBoss(bossDefinition);
```

- [ ] **Step 5: Update event-flow documentation**

Add save mutation results to clue pickup, enemy encounter, boss spawn, archive, and bestiary event chains.

- [ ] **Step 6: Compile and commit**

```bash
git add Assets/_Project/Code/CombatSandbox/Investigation Assets/_Project/Code/CombatSandbox/Enemies Assets/_Project/Code/CombatSandbox/Spawning Assets/_Project/Code/Meta/Runtime/MetaProgressionRecorder.cs Docs/API/InvestigationSystem.md Docs/API/EnemySystem.md Docs/API/BossSystem.md Docs/EventFlow.md
git commit -m "feat: record investigation and bestiary discoveries"
```

---

### Task 9: Build The Data-Driven Hub, Archive, And Bestiary UI

**Files:**
- Create: `Assets/_Project/Code/Meta/UI/HubCaseRow.cs`
- Create: `Assets/_Project/Code/Meta/UI/HubController.cs`
- Create: `Assets/_Project/Code/Meta/UI/InvestigationArchiveUI.cs`
- Create: `Assets/_Project/Code/Meta/UI/BestiaryUI.cs`
- Create: `Docs/API/ArchiveSystem.md`
- Create: `Docs/API/BestiarySystem.md`
- Update: `Docs/API/UISystem.md`
- Update: `Docs/GameplayLoop.md`

- [ ] **Step 1: Implement reusable HubCaseRow**

Serialized fields:

```csharp
Text caseNameText;
Text descriptionText;
Text stateText;
Button startButton;
Text startButtonText;
```

`Bind(CaseRegistryEntry entry, Action<CaseDefinition> selected)`:

- Shows `已完成`, `已解锁`, or `未解锁`.
- Uses `重新调查` for completed playable cases.
- Uses `进入案件` for available playable cases.
- Uses `开发中` for metadata-only cases.
- Disables start button for locked or non-playable entries.
- Clears previous button listeners before binding.

- [ ] **Step 2: Implement HubController**

Serialized fields:

```csharp
Text currentChapterText;
Text statusText;
Transform caseListRoot;
HubCaseRow rowTemplate;
Button archiveButton;
Button bestiaryButton;
Button casesButton;
GameObject casesPanel;
InvestigationArchiveUI archiveUI;
BestiaryUI bestiaryUI;
```

On start and `SaveGameManager.DataChanged`:

- Resolve registry.
- Display `章节进度：{chapter name}`.
- Rebuild rows from registry chapter order.
- Display unassigned cases in `未归档案件`.

Starting a case calls `SceneFlowManager.StartCase`.

- [ ] **Step 3: Implement InvestigationArchiveUI**

Build one text document from registry and save data:

```text
案件档案

【304号病房】
{CaseSummary}

已发现线索
・{Clue.Name}｜{Clue.Category}
  {Clue.JournalText}
```

Only completed cases appear. Only discovered clue IDs appear.

- [ ] **Step 4: Implement BestiaryUI**

Build:

```text
怪物图鉴

敌人
・{EnemyDefinition.DisplayName}
  {BestiaryDescription}

Boss
・{BossDefinition.Name}
  {BestiaryDescription}
```

Undiscovered catalog entries appear as `・未知记录`.

- [ ] **Step 5: Document UI and archive/bestiary APIs**

Include responsibilities, dependencies, data sources, refresh events, and common pitfalls.

- [ ] **Step 6: Compile and commit**

```bash
git add Assets/_Project/Code/Meta/UI Docs/API/ArchiveSystem.md Docs/API/BestiarySystem.md Docs/API/UISystem.md Docs/GameplayLoop.md
git commit -m "feat: add data-driven hub archive and bestiary ui"
```

---

### Task 10: Generate Chapter 1, Placeholder Case, Hub Scene, And Catalog

**Files:**
- Update: `Assets/_Project/Code/CombatSandbox/Editor/CombatSandboxCreator.cs`
- Update: `Assets/_Project/Code/CombatSandbox/Editor/GameContentCatalogBuilder.cs`
- Create/update generated assets:
  - `Assets/_Project/ScriptableObjects/Meta/Chapter_01.asset`
  - `Assets/_Project/ScriptableObjects/Meta/Case_Placeholder.asset`
  - `Assets/_Project/Resources/GameContentCatalog.asset`
  - `Assets/_Project/Scenes/Hub.unity`
  - `Assets/_Project/Scenes/CombatSandbox.unity`
- Update: `ProjectSettings/EditorBuildSettings.asset`
- Update: `Docs/FolderStructure.md`
- Update: `Docs/DeveloperOnboarding.md`

- [ ] **Step 1: Extend the generator data**

Add constants for:

```text
Assets/_Project/Scenes/Hub.unity
Assets/_Project/ScriptableObjects/Meta/Chapter_01.asset
Assets/_Project/ScriptableObjects/Meta/Case_Placeholder.asset
```

Update generated Room 304:

- `caseSummary`: generic summary of the completed prototype case.
- `isPlayable`: true.

Update generated enemy and boss definitions with stable IDs and short graybox bestiary descriptions.

- [ ] **Step 2: Create placeholder case data**

Exact visible data:

```text
CaseId: placeholder_next_case
CaseName: 下一案件开发中
Description: 用于验证案件解锁流程。
CaseSummary: 空
IsPlayable: false
Clues: empty
BossDefinition: null
RewardPool: empty
CompletionText: 空
```

- [ ] **Step 3: Create Chapter 1 data**

```text
ChapterId: chapter_01
ChapterName: 第一章
Description: 当前章节框架验证。
Cases:
  1. Room304_CaseDefinition
  2. Case_Placeholder
RequiredCompletedCaseIds: empty
```

- [ ] **Step 4: Generate Hub scene**

Create a graybox Canvas with:

- `GraphicRaycaster`.
- `EventSystem` using Input System.
- Header `迷城大厅`.
- Current chapter label.
- Cases, archive, and bestiary tabs.
- Scrollable or vertically sized case list root.
- Disabled `HubCaseRow` template.
- Archive text panel.
- Bestiary text panel.
- Status text.

Do not add new art assets. Reuse the generated square sprite and Unity UI.

- [ ] **Step 5: Update investigation scene generation**

- Do not serialize Room 304 as authoritative active case.
- Keep Room 304 as fallback for direct testing.
- Add `PlayerProgressPersistence`.
- Use generic reward, completion, and debug types.
- Ensure completion returns to Hub.

- [ ] **Step 6: Rebuild catalog and build settings**

Build order:

```text
0: Assets/_Project/Scenes/Hub.unity
1: Assets/_Project/Scenes/CombatSandbox.unity
```

- [ ] **Step 7: Regenerate using the Editor menu**

Run:

```text
Tools > Lost City > Create Combat Sandbox
```

The user performs this Editor action if batch generation is unavailable.

- [ ] **Step 8: Update onboarding and folder documentation**

Document how to add cases, chapters, IDs, summaries, bestiary descriptions, and how automatic catalog rebuild works.

- [ ] **Step 9: Commit validated generated assets**

Only after checking the generated references and excluding unrelated stale assets:

```bash
git add Assets/_Project/Code/CombatSandbox/Editor Assets/_Project/Scenes/Hub.unity Assets/_Project/Scenes/Hub.unity.meta Assets/_Project/Scenes/CombatSandbox.unity Assets/_Project/ScriptableObjects/Meta Assets/_Project/Resources ProjectSettings/EditorBuildSettings.asset Docs/FolderStructure.md Docs/DeveloperOnboarding.md
git commit -m "feat: generate the meta hub and chapter framework"
```

---

### Task 11: Remove Remaining Runtime Room304 Hardcoding

**Files:**
- Inspect and update: all `Assets/_Project/Code/**/*.cs`
- Update: `README.md`
- Update: `Docs/Roadmap.md`
- Update: relevant API docs

- [ ] **Step 1: Search for runtime hardcoding**

Run:

```bash
rg -n "Room304|Room 304|304号病房|下一章节开发中" Assets/_Project/Code --glob '*.cs'
```

Allowed matches:

- Editor generator seed constants and default content creation.
- Migration comments that explicitly identify legacy data.

Disallowed matches:

- Runtime manager branches.
- Runtime UI fallback text.
- Scene transition logic.
- Save keys embedded outside data assets.

- [ ] **Step 2: Replace remaining runtime fallbacks**

Use neutral text:

```text
当前案件
案件配置不完整
未找到当前案件配置
```

- [ ] **Step 3: Search UI text for English**

Run:

```bash
rg -n '"[^"]*(Level|Case|Archive|Bestiary|Completed|Locked|Unlocked|Return|Hub)[^"]*"' Assets/_Project/Code --glob '*.cs'
```

Convert player-visible strings to Simplified Chinese while leaving identifiers unchanged.

- [ ] **Step 4: Update roadmap and README**

Reflect Phase 6 Hub-first startup, save path, controls, current features, architecture diagram, and known manual verification requirements.

- [ ] **Step 5: Commit cleanup with documentation**

```bash
git add Assets/_Project/Code README.md Docs/Roadmap.md Docs/API
git commit -m "refactor: remove room-specific meta flow coupling"
```

---

### Task 12: Complete Documentation And Phase Report

**Files:**
- Update: `Docs/Architecture.md`
- Update: `Docs/EventFlow.md`
- Update: `Docs/GameplayLoop.md`
- Update: `Docs/PhaseReport.md`
- Update: `Docs/README.md`
- Update: `Docs/DevelopmentConstitution.md`

- [ ] **Step 1: Update Architecture.md**

Include:

- Persistent runtime services.
- Hub and Investigation scene responsibilities.
- Case state machine.
- Chapter/case/catalog relationships.
- Save data flow.
- Archive and bestiary data flow.
- Dependencies and known debt.

- [ ] **Step 2: Update EventFlow.md**

Document trigger/system/result chains for:

- Runtime bootstrap.
- Hub case selection.
- Scene transition to investigation.
- Clue discovery save.
- Enemy and boss encounter save.
- Reward persistence.
- Case completion.
- Automatic Hub return.
- Archive refresh.
- Bestiary refresh.
- Save load and corruption fallback.

- [ ] **Step 3: Update GameplayLoop.md**

Primary loop:

```text
大厅 -> 案件选择 -> 调查 -> 推理 -> 战斗 -> 奖励 -> 案件完成 -> 大厅
```

Describe persistent progression, replay, locked placeholder behavior, failure state, and victory state.

- [ ] **Step 4: Replace PhaseReport.md with Phase 6 report**

Include objectives, completed systems, architecture changes, dependencies, known issues, verification results, and recommended next phase.

- [ ] **Step 5: Update documentation index and Constitution API list**

Add:

- SaveSystem
- CaseRegistry
- ArchiveSystem
- BestiarySystem

- [ ] **Step 6: Run documentation checks**

```bash
rg -n "Phase 5|placeholder next chapter|下一章节开发中" README.md Docs
git diff --check -- README.md Docs
```

Review each match and retain only historical design context where appropriate.

- [ ] **Step 7: Commit documentation**

```bash
git add README.md Docs
git commit -m "docs: document phase 6 meta framework"
```

---

### Task 13: Full Verification, Completion Audit, Commit, And Push

**Files:**
- Verify all Phase 6 files and generated assets.

- [ ] **Step 1: Run all EditMode tests**

Run the Unity test command from the Verification Command section.

Expected:

- Save tests pass.
- Progression tests pass.
- No failed EditMode tests.

- [ ] **Step 2: Run compile verification**

Open Unity or use batch mode and confirm:

- Runtime assembly compiles.
- Editor assembly compiles.
- Test assemblies compile.
- No new Console errors.

If response-file compilation is used, append newly added source files not yet imported by Unity and report any duplicate-type warnings as response-file artifacts rather than project errors.

- [ ] **Step 3: Run static checks**

```bash
git diff --check
rg -n "Room304|Room 304|304号病房" Assets/_Project/Code --glob '*.cs'
rg -n "Room304RewardSelectionUI|Room304CompletionUI|Room304DebugTools|Room304GameStateController|Room304RewardType" Assets Docs
```

Expected:

- No whitespace errors.
- No runtime Room 304 coupling.
- No references to deleted or renamed types.

- [ ] **Step 4: Audit every success criterion**

Confirm from code/assets:

- Hub scene exists and is build index 0.
- Case selection is registry-driven.
- Completed, locked, and current chapter states display.
- Catalog includes all case definitions automatically.
- Case states use data and save completion.
- Save persists all requested categories.
- Archive reads completed cases and clues.
- Bestiary reads encountered enemies and bosses.
- Chapter 1 contains Room 304 and placeholder.
- Completion returns to Hub.
- All new visible text is Chinese.
- No new boss, chapter, story, weapon, or art was added.

- [ ] **Step 5: Prepare user manual verification checklist**

The user manually verifies:

1. Enter Play Mode and land in `迷城大厅`.
2. Start `304号病房`.
3. Complete clues, deduction, boss, and reward.
4. Wait for automatic return or press Space.
5. Confirm Room 304 is `已完成`.
6. Confirm `下一案件开发中` is `已解锁 · 开发中` and disabled.
7. Open `案件档案` and confirm case summary and collected clues.
8. Open `怪物图鉴` and confirm encountered entries.
9. Restart Play Mode and confirm progress reloads.

- [ ] **Step 6: Stage only Phase 6 files**

Do not stage unrelated pre-existing dirty generated assets. Inspect:

```bash
git status --short
git diff --cached --name-only
git diff --cached --check
```

- [ ] **Step 7: Create final implementation commit if verification fixes remain**

```bash
git commit -m "feat: complete phase 6 meta framework"
```

Every final code fix must include its corresponding documentation update.

- [ ] **Step 8: Push**

```bash
git push
```

- [ ] **Step 9: Return the constitution commit report**

Report:

1. Commit hash.
2. Files changed.
3. Documentation files updated.
4. Architecture changes.
5. Gameplay changes.
6. Event flow changes.
7. Technical debt.
8. Known issues.
9. Recommended next steps.

Also return the requested Phase 6 summary:

1. New systems added.
2. Architecture changes.
3. Save system structure.
4. Future content workflow.
5. Commit hash.
