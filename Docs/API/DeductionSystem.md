# DeductionSystem

## Purpose

The deduction system lets the player select collected clues and submit a hypothesis.

## Responsibilities

- Display the current mystery question.
- Display collected clue buttons.
- Track selected clues.
- Submit selected clues to `InvestigationProgress`.
- Show success or failure feedback.
- Mark the case solved after a short success delay.

## Public Fields

No public fields. UI references are serialized and generated.

## Public Properties

None.

## Public Methods

- `SetAvailable(bool available)`
- `Hide()`

## Dependencies

- `InvestigationProgress`
- Unity UI `Button` and `Text`
- Unity Input System keyboard access for Tab toggle

## Events

No public events. Deduction success calls `InvestigationProgress.MarkCaseSolved()`.

## Usage Example

```csharp
deductionBoard.SetAvailable(false);
```

## Common Pitfalls

- The deduction board is not a general puzzle framework.
- Correct deduction currently means selecting all required clues exactly.
- Do not add branching narrative logic here yet.
