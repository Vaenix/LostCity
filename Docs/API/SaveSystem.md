# SaveSystem

## Purpose

The Phase 6 save contract defines versioned, JSON-compatible meta progression data without owning file storage.

## Responsibilities

- Store completed case, permanent unlock, discovered clue, encountered enemy, and encountered boss IDs.
- Store permanent player progression separately from run-only combat state.
- Provide stable defaults for a new save.
- Repair null collections, blank IDs, duplicate IDs, and invalid progression values.
- Upgrade recognized older DTO data to the current schema version while preserving valid fields.
- Keep the data assembly independent from `UnityEngine`.

File IO, save paths, backup policy, and automatic saving are not part of Task 1. A later runtime save manager owns those responsibilities and must sanitize data after JSON deserialization.

## Public Fields

### `SaveGameData`

- `Version`: Serialized schema version. The current version is `SaveGameData.CurrentVersion`.
- `CompletedCaseIds`: Stable IDs for completed cases.
- `PermanentUnlockIds`: Stable IDs for permanent reward unlocks.
- `DiscoveredClueIds`: Stable IDs for clues discovered in any case.
- `EncounteredEnemyIds`: Stable IDs for encountered standard enemies.
- `EncounteredBossIds`: Stable IDs for encountered bosses.
- `PlayerProgress`: Permanent player progression values.

### `PlayerProgressData`

- `Level`: Permanent player level. Default `1`.
- `CurrentExperience`: Experience accumulated toward the next level. Default `0`.
- `ExperienceToNextLevel`: Current level threshold. Default `5`.
- `AttackMultiplier`: Permanent attack multiplier. Default `1`.
- `MaxHpMultiplier`: Permanent maximum HP multiplier. Default `1`.
- `FireRateMultiplier`: Permanent fire-rate multiplier. Default `1`.
- `CritChanceBonus`: Permanent critical-hit chance bonus. Default `0`.
- `DodgeChanceBonus`: Permanent dodge chance bonus. Default `0`.
- `DroneProjectileBonus`: Permanent additional drone projectile count. Default `0`.

`CaseState` defines `Locked`, `Available`, and `Completed` enum values for later case progression rules. It is JSON-compatible when used as a field on a serializable DTO, but Task 1 does not persist derived case-state records.

## Public Properties

No public properties. The DTO uses public fields because Unity `JsonUtility` serializes fields rather than general C# properties.

## Public Methods

### `SaveGameData`

- `CreateDefault()`: Creates a version 1 save with initialized collections and default player progression.

### `SaveDataSanitizer`

- `Sanitize(SaveGameData data)`: Returns a usable save, initializes missing data, removes invalid IDs, normalizes invalid progression values, and sets recognized data to the current version.
- `AddUnique(List<string> values, string id)`: Adds a non-blank ID once and reports whether the list changed.

## Dependencies

- `System`
- `System.Collections.Generic`
- NUnit for EditMode tests

The production assembly has `noEngineReferences: true` and does not depend on `UnityEngine`, PlayerPrefs, third-party packages, or file APIs.

## Events

No events. Runtime save services introduced by later tasks will own change notifications and autosave triggers.

## Usage Example

```csharp
SaveGameData data = SaveGameData.CreateDefault();

SaveDataSanitizer.AddUnique(data.DiscoveredClueIds, "room_304_chart");
data.PlayerProgress.Level = 2;

data = SaveDataSanitizer.Sanitize(data);
```

A Unity-facing persistence service can pass this DTO to `JsonUtility.ToJson` and must call `SaveDataSanitizer.Sanitize` after `JsonUtility.FromJson`.

## JSON Collection Design

Phase 6 uses `List<string>` DTO fields instead of `Dictionary` or `HashSet` because Unity `JsonUtility` does not serialize those collection types. The sanitizer restores set semantics by removing blank and duplicate IDs while preserving the first valid occurrence and list order.

This keeps the JSON stable and inspectable while allowing runtime systems to build temporary lookup sets when faster membership checks are needed.

## Versioning And Corrupt Data

- Version 1 is the first schema.
- Null save data becomes a new default save.
- Null collections become empty lists.
- IDs are trimmed; null, blank, and duplicate entries are removed.
- Levels and thresholds have a minimum of `1`.
- Experience and additive bonuses have a minimum of `0`.
- Multipliers must be finite and greater than `0`; otherwise they reset to `1`.
- Valid recognized fields are retained when older data is normalized.

Task 1 does not implement raw JSON backup or forward-schema rejection. The later file persistence layer must preserve the original file before replacing corrupt or unsupported future-version data.

## Autosave Behavior

There is no autosave behavior in the data contract. Later runtime code is responsible for deciding when mutations trigger serialization and file writes. This separation keeps DTO sanitization deterministic and testable.

## Common Pitfalls

- Do not replace the lists with `Dictionary`, `HashSet`, interfaces, or properties; `JsonUtility` will not preserve them as this contract expects.
- Do not persist current HP. Only the permanent maximum HP multiplier belongs in meta progression.
- Do not persist derived case availability. Task 2 computes `CaseState` from completed IDs and content definitions.
- Call `Sanitize` after deserialization before reading or mutating save data.
- Pass normalized stable IDs to `AddUnique`; load-time sanitization trims legacy entries.
- ID matching is case-sensitive. Changing an ID after release requires an explicit migration.
- Do not add PlayerPrefs or file IO to the core assembly.
