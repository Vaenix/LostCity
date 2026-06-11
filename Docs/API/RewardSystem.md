# RewardSystem

## Purpose

The reward system validates post-boss and level-up progression through `RewardDefinition`.

## Responsibilities

- Show the Room 304 reward panel.
- Offer temporary rewards from a reward pool.
- Apply the selected reward to `PlayerStats` or `CombatUpgradeStats`.
- Fire `RewardSelected` for the game flow.

## Public Fields

No public fields. UI references are serialized.

## Public Properties

None.

## Public Methods

- `Show(PlayerStats playerStats)`
- `Show(PlayerStats playerStats, RewardDefinition[] rewards, CombatUpgradeStats upgradeStats)`
- `Hide()`

## Dependencies

- `PlayerStats`
- `CombatUpgradeStats`
- `RewardDefinition`
- Unity UI buttons and text
- `GameFlowManager`
- `Room304RewardType` only for legacy fallback data.

## Events

- `RewardSelected`

## Usage Example

```csharp
rewardSelectionUI.Show(playerStats);
```

## Common Pitfalls

- Rewards are current-session only.
- Reward buttons must have listeners assigned in `Awake`.
- Do not hardcode new reward behavior in UI; add or extend `RewardDefinition`.
- Reward UI needs a `GraphicRaycaster` on the Canvas.
