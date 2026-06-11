# InvestigationSystem

## Purpose

The investigation system tracks clues collected for the active `CaseDefinition`.

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
