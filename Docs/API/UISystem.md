# UISystem

## Purpose

The UI system provides graybox feedback and controls for the current prototype.

## Responsibilities

- Show player HUD stats.
- Show world-space health bars.
- Show minimap markers.
- Show collected evidence.
- Show deduction board.
- Show upgrade choices.
- Show Room 304 rewards.
- Show chapter completion.

## Public Fields

No public fields. UI references are serialized.

## Public Properties

- `UpgradeSelectionController.IsShowing`

## Public Methods

- `EvidenceJournal.Hide()`
- `DeductionBoard.SetAvailable(bool available)`
- `DeductionBoard.Hide()`
- `Room304RewardSelectionUI.Show(PlayerStats playerStats)`
- `Room304RewardSelectionUI.Hide()`
- `Room304CompletionUI.ShowChapterComplete()`
- `Room304CompletionUI.ShowNextChapterPlaceholder()`
- `Room304CompletionUI.Hide()`

## Dependencies

- Unity UI
- Unity Input System UI module
- `EventSystem`
- `GraphicRaycaster` on interactive canvases

## Events

- `Room304RewardSelectionUI.RewardSelected`
- `Room304CompletionUI.ContinueRequested`

## Usage Example

```csharp
deductionBoard.SetAvailable(false);
rewardSelectionUI.Show(playerStats);
```

## Common Pitfalls

- Interactive UI needs an `EventSystem`.
- Interactive Canvas objects need a `GraphicRaycaster`.
- Time scale pauses can affect gameplay but Unity UI should still receive pointer events.
- Keep visible text in Simplified Chinese for the current prototype.
