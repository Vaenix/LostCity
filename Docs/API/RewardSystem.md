# RewardSystem

## Purpose

The reward system validates post-boss player progression.

## Responsibilities

- Show the Room 304 reward panel.
- Offer temporary rewards.
- Apply the selected reward to `PlayerStats`.
- Fire `RewardSelected` for the game flow.

## Public Fields

No public fields. UI references are serialized.

## Public Properties

None.

## Public Methods

- `Show(PlayerStats playerStats)`
- `Hide()`

## Dependencies

- `PlayerStats`
- Unity UI buttons and text
- `GameFlowManager`
- `Room304RewardType`

## Events

- `RewardSelected`

## Usage Example

```csharp
rewardSelectionUI.Show(playerStats);
```

## Common Pitfalls

- Rewards are current-session only.
- Reward buttons must have listeners assigned in `Awake`.
- Reward UI needs a `GraphicRaycaster` on the Canvas.
