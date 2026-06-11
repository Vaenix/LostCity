# InvestigationSystem

## Purpose

The investigation system tracks clues collected for a handcrafted case.

## Responsibilities

- Store case title and mystery question.
- Track required clues.
- Track collected clues.
- Notify UI when clues are collected.
- Notify flow when all required clues are collected.
- Validate deduction clue sets.

## Public Fields

No public fields. Case data is serialized.

## Public Properties

- `CaseTitle`
- `MysteryQuestion`
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
- Current deduction expects the exact required clue set.
