# RewardSystem

## Data And Resolution

RewardDefinition owns attribute/passive effects, drone weapon effects, and unlock
metadata. `IsApplicable` rejects unsupported enums, missing effects/unlock IDs and
nonfinite stat values. Apply ignores invalid entries.

`CaseDefinition.GetCompletionRewards()` filters the case pool by IsApplicable,
then falls back to the filtered boss pool only if no case reward remains.
CaseRegistry start validation and GameFlowManager consume this same array.
RewardSelectionUI displays up to the first three resolved entries; null slots
or a valid entry after slot three in an original pool cannot hide every button.

## Presentation API

```csharp
void RewardSelectionUI.Show(PlayerStats playerStats, string completionText,
    RewardDefinition[] rewards, CombatUpgradeStats upgradeStats);
void RewardSelectionUI.Hide();
bool RewardSelectionUI.IsShowing { get; }
event Action<RewardDefinition> RewardSelectionUI.RewardSelected;
```

UI copies the supplied array, hides on selection, and emits the selected
definition once. It does not apply the effect or persist data. GameFlowManager
owns application, capture, unlock registration and case completion.

## Level-Up Choices

UpgradeSelectionController uses valid configured reward definitions or its
existing fire-rate/attack/drone fallback choices. It clears the old callback and
active stats before invoking completion, so synchronous Show for the next level
cannot be overwritten. Each earned level queues one choice.

Choice application runs inside PlayerProgressPersistence.Begin/EndProgressMutation.
Automatic capture is deferred until the callback decrements the pending count.
Stat effects, consumed choice count and unlocks then share one root save/event.

CombatUpgradeStats.ApplyReward applies the reward and calls
PlayerProgressPersistence.RecordReward; unlock IDs therefore use the same save
boundary as boss rewards. Health changes do not trigger save writes.

## Boss And Storage Ordering

Boss death stops enemies, projectiles, weapons and further XP intake. Boss XP is
paid directly once (its orb dropper is disabled while tracking), before intake
closes. All queued level choices finish before the case reward is shown.

The case reward is applied at most once per completion attempt. Writable captured
progress and unlocks precede CompleteCase. Write failure shows a retry error,
without the success countdown. Retrying flushes/captures/registers completion;
it never multiplies the selected reward again. A future-version read-only save
blocks application until an explicit retry loads a compatible file.

## Limitations And Tests

Outstanding level-up choices are saved as PendingUpgradeChoices and resumed
after new-player initialization. Old v1 saves without the field default to zero.
Boss reward selection and pending completion retry still live in the current scene;
forced termination during failed IO cannot guarantee persistence.

GameplayMetaIntegrationTests covers synchronous multi-level choices, boss XP
ordering, completion write retry without duplicate effects, readonly protection,
and unique unlock recording.
