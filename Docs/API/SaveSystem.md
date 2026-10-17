# SaveSystem

## Ownership

`LostCity.Meta.Core` owns Unity-independent DTOs and sanitization; its
`noEngineReferences` boundary is unchanged. `LostCity.Meta.Persistence` references
Core and UnityEngine and contains `LostCity.Meta.Persistence.SaveGameManager`.
`GameRuntime` initializes this manager before the registry.

Default path: `Application.persistentDataPath/lost-city-save.json`.
`Initialize(string explicitPath = null)` is idempotent; later calls do not
change paths or reload. Explicit paths isolate EditMode tests.

## Version 1 Fields

JSON uses the exact public DTO field names (PascalCase):

- `Version`, currently `SaveGameData.CurrentVersion` (`1`).
- `CompletedCaseIds`, `PermanentUnlockIds`, `DiscoveredClueIds`.
- `EncounteredEnemyIds`, `EncounteredBossIds`.
- `PlayerProgress`: `Level`, `CurrentExperience`, `ExperienceToNextLevel`, `PendingUpgradeChoices`,
  `AttackMultiplier`, `MaxHpMultiplier`, `FireRateMultiplier`, `CritChanceBonus`,
  `DodgeChanceBonus`, `DroneProjectileBonus`.

Defaults: level 1, XP threshold 5, multipliers 1, additive values 0.
Current HP, spawned entities, pending UI, and derived case/chapter state are not
saved. Unknown legacy IDs remain even when their definitions are absent.

## Public API

```csharp
string SavePath { get; }
bool IsReadOnly { get; }
bool HasUnsavedChanges { get; }
string LastError { get; }
event Action DataChanged;

IReadOnlyList<string> CompletedCaseIds { get; }
IReadOnlyList<string> PermanentUnlockIds { get; }
IReadOnlyList<string> DiscoveredClueIds { get; }
IReadOnlyList<string> EncounteredEnemyIds { get; }
IReadOnlyList<string> EncounteredBossIds { get; }

void Initialize(string explicitPath = null);
bool IsCaseCompleted(string caseId);
PlayerProgressData GetPlayerProgress();
SaveGameData GetSnapshot();
bool CompleteCase(string caseId);
bool DiscoverClue(string clueId);
bool EncounterEnemy(string enemyId);
bool EncounterBoss(string bossId);
bool AddPermanentUnlock(string unlockId);
void SetPlayerProgress(PlayerProgressData progress);
void SetPlayerProgressAndUnlocks(PlayerProgressData progress, IEnumerable<string> unlockIds);
void SaveNow();
void Reload();
```

Initialize before persistence operations. Collection getters return copied
arrays; progress and snapshots are deep copies. Input progress is also copied.
Pass changed progress back through `SetPlayerProgress`.

## Mutation And Autosave

IDs are trimmed and behave as sets. Blank/duplicate mutations return false
without saving or notifying. Equal sanitized player progress is also a no-op.
Accepted changes mark dirty, attempt `SaveNow`, then raise `DataChanged` once.

Mutation success reports an in-memory change, not durable IO success. Check
`HasUnsavedChanges` and `LastError`. Successful retries clear both.
The runtime root retries dirty data on application pause/quit; force termination
cannot guarantee these callbacks execute. Hub should show storage diagnostics.

## Atomic Writes And IO Failures

1. Recheck an existing file for an unsupported future version.
2. Sanitize and write UTF-8 JSON to `<save>.tmp`, flushing the file to disk.
3. Use `File.Replace` for existing saves, retaining the previous version in
   `<save>.bak`; use same-directory `File.Move` for the first save.

There is no delete-then-move fallback. Unsupported atomic replacement fails
diagnostically, preserving the old destination and dirty in-memory progress.
Temporary files are cleaned where possible. Retry through `SaveNow`.
The backup is for manual recovery; no automatic rollback is performed.

`Reload` first flushes dirty supported data and refuses to discard it when
that fails. Read/backup failures enter read-only mode so an unreadable original
is not overwritten. Resolve the external IO problem and explicitly reload.
Multiple concurrent writers to the same save path are unsupported.

## Corruption And Versions

Missing files create defaults and attempt an initial save. Malformed JSON is
moved to a unique `<save>.corrupt-<UTC>-<GUID>.json` before defaults are created.
If preservation fails, the original stays and the manager becomes read-only.

Sparse valid objects are sanitized: null collections, blank/duplicate IDs, and
invalid values are repaired. IDs are trimmed. Invalid XP threshold restores 5;
nonfinite/nonpositive multipliers restore 1; invalid additive bonuses restore 0.

`Migrate` is the persistence migration entry point. Version 0/missing fields
currently use v1 defaults and Core sanitization. Add explicit migrations there
when adding supported schemas.

Future versions are detected before sanitization. The file stays in place,
with no backup/move/write. `IsReadOnly` and `LastError` identify the incompatible
version and path; mutations are rejected and `SaveNow` cannot bypass this state.
Explicit `Reload` can resolve it only after a supported file is provided.
Core independently throws `NotSupportedException` before modifying a future DTO.
This is not a fallback that silently autosaves defaults over newer data.

## Usage

```csharp
var save = LostCity.Meta.GameRuntime.Instance.SaveGame;
save.DiscoverClue("case_a_clue");
var progress = save.GetPlayerProgress();
progress.Level = 2;
save.SetPlayerProgress(progress);
```

## Gameplay Integration

PlayerProgressPersistence restores once in Start after Damageable/PlayerStats
Awake, then subscribes only to permanent stats, experience and drone changes.
It captures all approved player fields; damage/healing never writes the save.
RecordReward captures progression and registers unlocks through focused methods.

GameFlowManager marks completion only after a selected reward has been applied
once and captured durably. Failed writes/read-only state cancel the success
countdown and retain a retryable scene. Retry flushes/captures/registers without
applying the reward again. Death reload also refuses to discard dirty progress.

PendingUpgradeChoices is an additive v1 field; missing old fields default to 0,
and negative values sanitize to 0. XP/level changes save outstanding choice debt.
Player initialization resumes it only after all Awake methods and subscriptions.
During a choice, Begin/EndProgressMutation suppresses stat/XP intermediate writes;
after decrement, SetPlayerProgressAndUnlocks commits stats, choice count and
unlock IDs in one root mutation, write and DataChanged event. A failed write
retains that consistent dirty snapshot for retry. Old saves cannot recover debt
that a previous build never recorded. Boss reward selection/retry remains scene
state, so force termination during failed IO is still a durability limitation.

## Verification

`SaveGameDataTests` covers pure sanitization. `SaveGameManagerTests` uses
temporary files and native JsonUtility to cover all fields, copies, idempotency,
sparse JSON, corruption, future-version protection, and IO retry.
Manager tests require native EditMode execution; compilation alone does not
verify serialization or filesystem behavior inside Unity.
