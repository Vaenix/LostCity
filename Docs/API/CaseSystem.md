# CaseSystem

## Purpose

The case system combines data-driven investigation content with deterministic Phase 6 meta progression rules.

`CaseDefinition` continues to provide investigation content. `LostCity.Meta.Core` derives case state and validates whether a selected case may start without depending on `UnityEngine`.

## Responsibilities

- Represent case content through `CaseDefinition`.
- Derive `Locked`, `Available`, or `Completed` from chapter order, chapter prerequisites, and completed case IDs.
- Prevent invalid, locked, metadata-only, or incomplete cases from starting.
- Allow a completed playable case with all required content to be replayed.
- Keep derived availability out of save data.

## Public Types

### `CaseState`

- `Locked`: The case cannot currently be selected.
- `Available`: The case is unlocked and not yet completed.
- `Completed`: The case ID is present in the completion set.

### `CaseProgressionInput`

Pure C# chapter input used by the progression evaluator:

- `OrderedCaseIds`: Case IDs in sequential unlock order.
- `RequiredCompletedCaseIds`: Case IDs that must all be completed before the chapter unlocks.

Null arrays are normalized to empty arrays.

### `CaseStartBlockReason`

- `None`: The case may start.
- `Locked`: Progression state blocks the case.
- `NotPlayable`: The entry is metadata-only or otherwise marked non-playable.
- `MissingContent`: The playable entry lacks required case content.

## Public Properties

`CaseDefinition` currently exposes:

- `CaseId`
- `CaseName`
- `Description`
- `Clues`
- `DeductionQuestion`
- `CorrectAnswer`
- `BossDefinition`
- `RewardPool`
- `CompletionText`
- `CaseSummary`
- `IsPlayable`

CaseDefinition.GetCompletionRewards() returns applicable case rewards, or the
filtered boss pool when none remain. CaseRegistry validates this same result;
GameFlow and RewardSelectionUI must not consume unfiltered reward slots.

InvestigationProgress resolves the selected CaseSession definition in Awake,
before graybox clue layout in Start. A serialized definition remains available
for direct-scene testing. Completion is recorded only after a real reward is
applied and progression captured; failed storage remains retryable.

## Public Methods

### `CaseProgressionRules.GetCaseState`

```csharp
CaseState GetCaseState(
    string caseId,
    CaseProgressionInput chapter,
    ISet<string> completedCaseIds)
```

Evaluation order:

1. Blank IDs, null chapters, and IDs not present in `OrderedCaseIds` are `Locked`.
2. A valid chapter case already in the completion set is `Completed`.
3. Missing chapter prerequisites make the case `Locked`.
4. The first case is `Available`.
5. Each later case is `Available` only after the immediately preceding case completes.

A null completion set is treated as empty.

### `CaseStartRules.CanStart`

```csharp
bool CanStart(
    CaseState state,
    bool isPlayable,
    bool hasRequiredContent,
    out CaseStartBlockReason reason)
```

Block reasons use this priority:

1. `Locked`
2. `NotPlayable`
3. `MissingContent`

Both `Available` and `Completed` cases may start when playable and complete, which enables replay.

## Dependencies

- `System`
- `System.Collections.Generic`
- Existing investigation content definitions

The production Meta Core assembly has `noEngineReferences: true` and does not reference `UnityEngine`.

## Events

The pure progression and start rules raise no events. Runtime services and `InvestigationProgress` own state-change notifications.

## Usage Example

```csharp
CaseProgressionInput chapter = new CaseProgressionInput(
    new[] { "room_304", "placeholder" },
    new string[0]);

HashSet<string> completedCases = new HashSet<string> { "room_304" };
CaseState state = CaseProgressionRules.GetCaseState(
    "placeholder",
    chapter,
    completedCases);

bool canStart = CaseStartRules.CanStart(
    state,
    isPlayable: false,
    hasRequiredContent: false,
    out CaseStartBlockReason reason);
```

The placeholder is `Available` after Room 304 completes, but `CanStart` returns `false` with `NotPlayable`.

## Common Pitfalls

- Do not persist `CaseState`; derive it from stable IDs and chapter definitions.
- Do not report a completed ID as completed unless that ID belongs to the evaluated chapter.
- Do not treat `Available` as sufficient to start; playability and required content are separate gates.
- Do not block replay solely because a case is `Completed`.
- Do not hardcode case text or Room 304-specific branches in runtime UI.
- A case without clues or a boss cannot enter the complete investigation flow.
