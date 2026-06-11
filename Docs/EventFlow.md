# Event Flow

## Clue Pickup

```text
Trigger
Player presses E inside a clue trigger.

System
CluePickup calls InvestigationProgress.TryCollectClue().

Result
Clue is added, ClueCollected fires, journal and deduction UI can refresh.
```

```mermaid
flowchart TD
    Input["E pressed near clue"] --> Pickup["CluePickup"]
    Pickup --> Progress["InvestigationProgress.TryCollectClue"]
    Progress --> Event["ClueCollected"]
    Event --> Journal["EvidenceJournal refresh"]
    Event --> Board["DeductionBoard refresh if open"]
```

## Journal Update

```text
Trigger
InvestigationProgress.ClueCollected.

System
EvidenceJournal rebuilds text from collected clues.

Result
The journal shows collected clue titles, categories, and descriptions.
```

## Deduction Submission

```text
Trigger
Player clicks 提交推理.

System
DeductionBoard checks InvestigationProgress.IsCorrectDeduction().

Result
Correct deduction displays 真相重现 and marks the case solved.
Incorrect deduction displays 证据还无法成立。
```

## Truth Reconstruction

```text
Trigger
DeductionBoard accepts the selected clues.

System
InvestigationProgress.MarkCaseSolved() raises CaseSolved.

Result
GameFlowManager enters Combat.
```

## Boss Spawn

```text
Trigger
GameFlowManager enters Combat.

System
EnemySpawner is enabled and BossSpawnController.SpawnBoss() is called.

Result
The Warden appears and GameFlowManager tracks its Damageable.Died event.
```

## Boss Death

```text
Trigger
The Warden Damageable reaches 0 health.

System
GameFlowManager receives Damageable.Died.

Result
Combat stops, remaining enemies are destroyed, and reward UI appears.
```

## Reward Selection

```text
Trigger
Player clicks one reward button.

System
Room304RewardSelectionUI applies the reward to PlayerStats.

Result
RewardSelected fires and GameFlowManager enters ChapterComplete.
```

## Level Up

```text
Trigger
PlayerExperience receives enough XP.

System
UpgradeSelectionController shows upgrade choices.

Result
CombatUpgradeStats applies fire rate, projectile damage, or extra drone projectile upgrades.
```

## Scene Transition

```text
Trigger
Currently none.

System
ChapterComplete remains in the same scene.

Result
Placeholder text displays 下一章节开发中.
```

## Chapter Completion

```text
Trigger
Reward selected, then Space pressed on the chapter complete screen.

System
Room304CompletionUI raises ContinueRequested.

Result
GameFlowManager logs Room304 Completed and shows the next chapter placeholder.
```
