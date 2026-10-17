# InvestigationSystem

## Phase 6 Clue Layout

`CaseClueLayout` owns only graybox pickup placement. Its serialized inputs are
`investigationProgress`, an inactive `pickupTemplate`, optional `authoredPickups`,
`center` and `radius`. `Start()` calls `Rebuild()`, which replaces only previously
generated instances. Matching authored pickups are reused by clue ID without moving
or reparenting them; missing required clues are generated from the template.
`SpawnedPickups` exposes the resulting authored-plus-generated set as a read-only view.

`CluePickup.Initialize(ClueDefinition, InvestigationProgress)` assigns runtime
data before a clone is activated; `Definition` exposes the assigned clue.
The layout has no events. Collection continues through `TryCollectClue`.

Usage: the Editor generator creates one disabled pickup template, assigns it and
the scene investigation progress to the layout, and leaves spawning to `Start`.
Existing authored scenes register their fixed pickups in `authoredPickups`; do not
leave extra, unmanaged duplicates active. Unused authored pickups are hidden at runtime,
never destroyed. An empty authored array keeps the generator's original behavior.
Invalid or duplicate clue IDs are skipped defensively; the registry should reject
invalid playable cases before entering the scene. The template must be distinct from
the authored objects. Rebuild is a run setup operation, not a collection-event handler.

## Purpose

The investigation system tracks clues collected for the active `CaseDefinition`.

In Awake it prefers GameRuntime.CaseSession.ActiveCase, preserving the serialized
caseDefinition as a direct-scene fallback. This happens before CaseClueLayout.Start
places pickups. Missing both sources logs an error and requests Hub return.
Generic fallback text contains no case-specific constants.

Accepted clues must belong to the selected required set, have a nonblank ID, and
not already be collected. Collection calls MetaProgressionRecorder.RecordClue
before notifications. Required/collected arrays are copied for external readers.
MarkCaseSolved requires every required clue; deduction still uses the exact set.
RewardPool exposes the shared filtered GetCompletionRewards result.

## Responsibilities

- Read case title, description, clue list, deduction question, correct answer, boss, rewards, and completion text from `CaseDefinition`.
- Track required clues from case data.
- Track collected clues.
- Notify UI when clues are collected.
- Notify flow when all required clues are collected.
- Validate deduction clue sets.

## Public Fields

No public fields. Case data is serialized.

## Public Properties

- `CaseTitle`
- `CaseDescription`
- `MysteryQuestion`
- `CorrectAnswer`
- `CompletionText`
- `BossDefinition`
- `RewardPool`
- `RequiredClues`
- `CollectedClues`
- `IsCaseSolved`
- `HasCollectedAllRequiredClues`

## Public Methods

- `TryCollectClue(ClueDefinition clue)`
- `HasClue(ClueDefinition clue)`
- `IsCorrectDeduction(IReadOnlyList<ClueDefinition> selectedClues)`
- `MarkCaseSolved()`
- `CollectAllRequiredClues()`

## Dependencies

- `ClueDefinition`
- `CaseDefinition`
- `CluePickup`
- `EvidenceJournal`
- `DeductionBoard`
- `GameFlowManager`

## Events

- `ClueCollected`
- `AllRequiredCluesCollected`
- `CaseSolved`

## Usage Example

```csharp
if (investigationProgress.TryCollectClue(clue))
{
    // UI refreshes through ClueCollected.
}
```

## Common Pitfalls

- Mystery logic must remain handcrafted.
- Do not generate clue relationships procedurally.
- Current deduction expects the exact case clue set.
