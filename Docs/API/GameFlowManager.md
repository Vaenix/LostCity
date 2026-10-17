# GameFlowManager

## Legacy Scene Compatibility

`Room304GameStateController` is deprecated and hidden from Add Component. Its sole
purpose is retaining the old script GUID and serialized `investigationProgress`,
`deductionBoard`, `enemySpawner`, `bossSpawnController` and `memoryFragmentPanel`
references in untouched legacy scenes. It has no public API, events or active flow
logic, and must not be added to new scenes. It warns on Play when the scene lacks a
GameFlowManager. Use Hub and GameFlowManager for Phase 6; keeping this shell is not
a conversion of the user's Room scene into a complete gameplay scene.

## Flow And Ownership

The scene manager uses the selected CaseDefinition, without specific case IDs.
InvestigationProgress resolves session data in Awake; GameFlowManager begins in
Start, after investigation and player component Awake initialization.

`Investigation -> Deduction -> Combat -> Reward -> ChapterComplete -> Hub`

ChapterComplete is the existing enum name for a successfully saved **case**
completion; it does not create a new chapter or a terminal placeholder screen.

## Public API

Properties: `CurrentState`, `CompletionPersisted`.
Methods: `UnlockAllClues()`, `DebugSpawnBoss()`, `DebugKillBoss()`,
`RetryCompletion()`.
Event: `StateChanged`.

Existing serialized references remain (using generic renamed UI types).
Additional optional auto-resolved references: `playerExperience`,
`playerProgressPersistence`, `upgradeSelectionController`.

## Boss Death And Upgrades

The tracked boss's XpDropper is disabled. On death, GameFlowManager synchronously
pays BossDefinition.XpReward once, closes XP intake, disables player weapons and
spawning, and immediately deactivates/removes enemies, projectiles and XP orbs.
Both enemy and friendly projectiles are cleared to avoid late damage callbacks.

Existing and boss-earned upgrade choices remain queued. Reward state waits for
PlayerExperience.PendingUpgradeChoices to reach zero and the upgrade panel to
close. No per-level choice is discarded to display the boss reward. Reward UI
receives only CaseDefinition.GetCompletionRewards().

## Durable Completion And Retry

RewardSelectionUI is a selector. GameFlowManager latches a single selection,
applies it once, captures permanent stats/unlocks, then calls CompleteCase.
Only writable, clean saved data allows CompletionPersisted and ChapterComplete.
Save failures/read-only state show a diagnostic and retain the scene/selection.
RetryCompletion retries storage/capture, never the reward effect.

CaseCompletionUI retains `panelRoot`, `titleText`, `bodyText`, and adds optional
`retryButton`. `ShowChapterComplete(caseName, completionText)` displays 案件完成
and 正在返回大厅; an unscaled three-second countdown or Space emits ReturnRequested
once. `ShowError(message)` cancels auto-return and enables RetryRequested via
Space or retryButton. Show/Hide resets the waiting mode.

Saved completion returns through SceneFlowManager.LoadHub, which ends the
session. Failed scene loading leaves the current scene and permits return retry.
Events are removed/re-added idempotently on component enable/disable.

## Failed Runs

PlayerDeathHandler stops XP intake and restores timeScale. CombatGameManager
captures progression before a real-time delayed reload; it preserves the active
CaseSession so InvestigationProgress chooses the same case again. Storage failure
keeps the current scene and offers a retry instead of discarding dirty progress.
Reload exceptions log the target and do not intentionally end the session.

## Debug And Compatibility

PrototypeDebugTools replaces the specific-case debug class with its original
script GUID preserved. RewardSelectionUI and CaseCompletionUI also preserve
their old GUIDs. Unused legacy controller/enum were removed after reference and
GUID searches. Serialized caseDefinition remains a direct-scene fallback.

## Verification And Known Gap

GameplayMetaIntegrationTests covers boss XP/queue ordering, cleanup, readonly and
write failures, one-time rewards, and unscaled completion. Full scene transitions
and generated references additionally require native Unity verification.

The v1 save now includes backward-compatible PendingUpgradeChoices. Restoration
resumes outstanding choices after player/UI Awake. Choice effects, pending count
decrement and unlock IDs are captured atomically, not as separate intermediate
snapshots. Old saves missing the field have no recoverable historical choice debt.
Boss reward selection and an in-scene completion retry are not themselves saved;
forced termination during failed IO still cannot guarantee a durable completion.
