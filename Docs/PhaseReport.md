# Phase 6 Report: Meta Framework

## Status And Objectives

Implementation and automated acceptance are complete; manual Play Mode acceptance
remains user-owned and has not been performed. The prototype now has a data-driven
Hub-to-case-to-Hub framework around existing Room 304 content. No Chapter 2, new
boss, new weapon, final story, art, VFX pass, or localization framework was added.

Phase 6 was developed on `codex/phase-6-meta-framework`. On 2026-10-17 the user
requested integration into the original Lost City project on `master`, preserving
local asset edits and the untracked Room scene. Integration is complete: 89 native
EditMode tests pass, scene references and meta contracts pass, and a second migration
leaves asset hashes unchanged. See [Phase6Integration](Phase6Integration.md) for evidence.

### Main Project Integration Record

- Affected systems: saved scene/prefab wiring, UI Input Actions, case metadata and
  the existing Phase 6 services; no content or combat redesign.
- Preserve the original local asset IDs, tuning, scene hierarchy and Room files.
  Add only missing Phase 6 components/references; do not regenerate seed assets.
- The pre-integration snapshot is `228b5ad0c15d078b18c022a04033ca03a40950a7`, retained
  by `refs/backup/lost-city/pre-phase6-20261017` and a named stash. Its third parent
  stores the originally untracked Room and UI input files. This backup stays local.
- Keep the user's asset edits unstaged/uncommitted. Push Phase 6 plus integration
  tooling/documentation, not the user's unpublished scene or unrelated Design files.
- Updated README, onboarding, verification instructions and this report with the
  original-project entry point, migration precautions and actual test evidence.
- Validated native EditMode tests and saved references in the original project;
  compared Room hashes and original asset data against the backup. No Play Mode.

## Completed Features

- Hub with Chinese case selection, chapter progress, completed/locked states and replay.
- Automatic content catalog and validated case registry, including unassigned content.
- Ordered chapter progression and a non-playable next-case placeholder.
- Versioned local JSON saves with corruption preservation, future-schema protection,
  atomic replacement, explicit errors, and recovery controls.
- Persistent levels, partial XP, modifiers, drone count, pending upgrades and unlocks.
- Investigation archive and encounter-driven enemy/boss bestiary.
- Shared-scene clue generation from each case's complete clue list.
- Generic reward/completion/debug components; the legacy Room304 state machine is
  removed. A passive serialization shell preserves untouched legacy scenes during
  main-project integration; it does not participate in Phase 6 gameplay.
- Boss cleanup, XP/upgrade settlement, durable reward completion and automatic Hub return.
- Editor generation and saved-scene reference validation.

## Architecture Changes

GameRuntime owns persistent save, registry, session and scene services.
GameFlowManager remains the local five-state controller. Definitions contain content;
save DTOs contain stable IDs and player modifiers; spawned entities remain scene-local.
Case state is derived, not redundantly persisted.

```mermaid
flowchart LR
    Hub["大厅 / 注册表"] --> Session["选定案件会话"]
    Session --> Investigation["共享调查场景"]
    Investigation --> Deduction["推理"]
    Deduction --> Boss["首领战"]
    Boss --> Queue["经验 / 待选升级"]
    Queue --> Reward["案件奖励"]
    Reward --> Save["保存与案件完成"]
    Save --> Hub
    Save --> Archive["档案 / 图鉴 / 后续案件解锁"]
```

## New Systems And Dependencies

Core DTO/rules, ChapterDefinition, GameContentCatalog and its builder/postprocessor,
SaveGameManager, GameRuntime/RuntimeBootstrap, CaseRegistry/Entry, CaseSessionManager,
SceneFlowManager, PlayerProgressPersistence, MetaProgressionRecorder, CaseClueLayout,
HubController/HubCaseRow, InvestigationArchiveUI, BestiaryUI and validation utilities.

No new Unity package was installed. New assembly boundaries are LostCity.Meta.Core
(no engine dependency) and LostCity.Meta.Persistence (Unity JSON/storage adapter).
Runtime UI remains on existing Unity UI and Input System.

## Behavior And Event Changes

- The selected case overrides the scene's debug fallback before clue layout starts.
- New discoveries autosave IDs and update archive/bestiary views.
- Boss death clears combat objects and pays boss XP directly once, then resolves
  pending level choices before displaying the case reward.
- Upgrade effects, consumed choice debt and unlocks use one save snapshot.
- Completion requires reward application and durable save before the success screen.
- Save retry does not reapply the reward. Death reload retains the selected case.
- The completion screen returns after three unscaled seconds, with optional Space skip.
- Active flow controllers contain no Room304-specific names or story strings.
  Only the deprecated serialization shell retains its historical class name/GUID.

## Verification Results

Native Unity version: **2022.3.32f1**. Date: **2026-10-17**.
No Computer Use, Play Mode, manual clicks, or game balance tests were performed.

| Check | Result |
| --- | --- |
| Unity EditMode suite | 83 passed, 0 failed, 0 skipped |
| CaseProgressionRulesTests | 14 passed |
| SaveGameDataTests | 10 passed |
| SaveGameManagerTests | 10 passed |
| MetaRuntimeTests | 25 passed |
| MetaHubUITests | 12 passed |
| GameplayMetaIntegrationTests | 12 passed |
| GenerateAndValidate | Exit 0; both scenes generated and reopened successfully |
| Generated references | Cameras, missing scripts, UI/player actions, raycasters, scroll panels, flow references and projectile 2D components passed |
| Temporary save contracts | Session, unlock, replay, native JSON reload, one/five-clue layouts passed |
| Runtime hardcoding audit | Active Phase 6 flow is generic; a passive legacy shell preserves the user's Room scene |
| Source/docs whitespace | Passed; native Unity YAML formatting retained |
| Main checkout preservation | Existing user changes remain untouched |

Reproduction commands and their limits are in [Tools/Verification](../Tools/Verification/README.md).
The successful test run recorded 83 cases at 03:36:04 UTC. The first run was 66/67:
a whitespace-ID fixture also generated invalid child IDs. The fixture was corrected
to isolate its intended invalid-case condition without weakening production validation.
These are the feature-branch baseline results. The merged original-project results
and authored-resource preservation checks are recorded separately in
[Phase6Integration](Phase6Integration.md).

Initial batch startup reported no valid Editor license. After the user's Hub license
confirmation, the same Editor resolved entitlements and all native checks ran. License
availability is no longer a blocker.

### Generated Input Regression Found And Fixed

The first native generation saved both UI input modules with null action references
despite in-memory property assignment. Both builders now assign the serialized action
fields through SerializedObject and mark the component dirty. Saving/reopening then
retained the asset and action references. Hub generation additionally rejects a saved
scene if any of its six required UI actions is absent or points to a different asset.
Actual pointer interaction is still a manual check.

Standalone batch validation also handles an initially empty scene setup; it must
not call RestoreSceneManagerSetup without a loaded active scene. Interactive
validation requires a saved scene and still refuses unsaved changes.

## Gameplay State Checklist

| State or transition | Automated / inspected evidence | Manual Play Mode |
| --- | --- | --- |
| Hub case selection and lock display | UI handler tests; generated references | Pending |
| Selected case -> shared investigation | Session/definition override tests and source inspection | Pending |
| Clue collection -> journal/archive | Collection idempotency and archive visibility tests; layout contracts | Pending |
| Deduction -> truth -> Combat | Existing call path reviewed; not driven through real input | Pending |
| Boss spawn / encounter recording | Runtime hook tests | Pending |
| Boss death / combat cleanup | Controller integration tests | Pending |
| Pending level choices -> case reward | Queue/reentrancy and XP settlement tests | Pending |
| Reward application -> save completion | Success, duplicate callback, readonly and IO retry tests | Pending |
| Three-second completion / Space skip | Unscaled one-shot controller tests | Pending |
| Actual scene load -> Hub refresh | References/build order and source review; real runtime transition not exercised | Pending |
| Replay and quit/relaunch progression | Native JSON reload, new-player restore and registry replay tests | Pending |

Tests call component methods in EditMode; they do not prove runtime animation,
rendering, real input delivery, scene lifecycle timing, or a complete played run.

## Manual Acceptance

1. Open Hub, verify Room 304 is available and the placeholder is locked.
2. Start Room 304; collect clues normally, inspect J journal, submit Tab deduction.
3. Fight the boss; confirm remaining combat objects are removed on death.
4. Resolve every earned level choice, then choose one case reward.
5. Confirm stats change once, completion appears, and Hub returns automatically.
6. Confirm archive/bestiary updates and the placeholder becomes unlocked but non-playable.
7. Replay: permanent stats/level remain; run clues and current HP restart correctly.
8. Quit after gaining a level but before selecting; relaunch and check the unspent choice.
9. Verify manual/automatic fire, UI clicks, health bars, fonts and minimap remain usable.

F1/F2/F3 can shorten a smoke test, but a shortcut run is not a substitute for normal
clue collection and deduction acceptance.

## Known Issues And Technical Debt

- Actual runtime scene transitions, rendering and the normal played loop still require user testing.
- Current content remains one graybox case; deduction validates a configured clue set.
- The generator resets seed values, so custom content must use separate asset paths.
- Hub uses an installed Chinese font fallback; shipping font coverage is not guaranteed.
- One local save writer is supported. Cloud, profiles and concurrent processes are unsupported.
- Forced termination during unresolved IO failure can lose dirty in-memory data.
- Old saves cannot recover upgrade choices that an earlier build never recorded.
- Boss reward retry state is scene-local, not a suspend/resume checkpoint.
- The legacy Room scene is preserved for authoring, not promoted to the Phase 6
  gameplay entry point. Its compatibility shell can be removed only after that
  scene is deliberately migrated by its owner.
- Unity-generated YAML retains its native trailing-space formatting.
- The local Git code-review-graph hook cannot refresh its index because its Python
  environment lacks `tree_sitter_c_sharp`. Git commits still succeed; this does not
  affect Unity compilation or the native test results. The global environment was
  left unchanged.

## Next Phase Recommendations

Complete the manual checklist before content expansion. Fix observed lifecycle/input
issues first, then prove another metadata definition can use the shared scene without
runtime case-specific edits. No Chapter 2 or art production is recommended yet.
