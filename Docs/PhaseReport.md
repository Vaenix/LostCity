# Phase 6 Report: Meta Framework

## Walking Observation Regression

User report: the player translates but has no walking animation; mouse-facing works.
The first-delivery driver consumed Rigidbody2D.velocity, whereas the unchanged
PlayerMotor uses MovePosition. A native EditMode physics probe on 2026-10-17 measured
.064 m actual displacement per .02 s step with velocity remaining zero, both with
and without interpolation. This explains the stationary locomotion state.

The prior 176 tests verified bone posing and collection integration, but injected
movement velocity and missed the native movement-to-pose connection. This follow-up
is scoped to movement observation and its regression coverage, not a controller,
animation-library or asset redesign. No gameplay UI is automated.

### Correction And Commit Scope

- Runtime file: `Assets/_Project/Code/Characters/Runtime/ProtagonistVisualMotion.cs`.
  FixedUpdate samples completed Rigidbody2D.position displacement; LateUpdate
  consumes the cached velocity. Enable/disable/direct reparent reset the baseline.
  Pickup gating also checks observed motion before locomotion blend catches up.
- Test files: `Assets/_Project/Tests/EditMode/Meta/ProtagonistMovementObservationTests.cs`
  and its Unity `.meta`. Ten cases use actual MovePosition/native simulation and
  production observation/pose methods, not injected Advance velocity.
- Documentation: README, CharacterMotion, CharacterVisual and PlayerControl APIs,
  Architecture, EventFlow, Roadmap, Room30425DSample, PhaseReport and
  Tools/Verification/README.
- Architecture change: completed physical displacement replaces velocity-property
  readback at the visual boundary. No public API, GameFlow state, package, input,
  progression or save-schema change.
- Gameplay change: movement now drives the existing in-place gait; stopped or
  blocked motion settles idle. Mouse-facing remains independent. No controller,
  aim, pose-curve, model, prefab or scene changes; no generator was run.
- Event flow: Motor -> native physics -> next fixed observation -> cached speed
  -> late visual pose. Collection/save events remain unchanged and authoritative.
- Debt/known issues: one fixed-step observation lag (20 ms at current settings),
  existing flat-plane gait/skin limitations and unverified actual-input feel.
  No new blocker found in native tests. Optional local Git graph-hook dependency
  remains missing as documented below; it is not used as verification evidence.
- Next steps: user tests the saved Room304Greybox directly for WASD/arrows,
  stop/wall idle, mouse-only turns, movement plus aim and existing E/J behavior.
  No scene regeneration, new chapter or content expansion is needed.

Commit hash is supplied by the final Git report; the commit's file list is the
authoritative scope. User-authored dirty assets, Room scene and Design files stay
unstaged and are not included in this fix.

### Walking Regression Results

| Gate | Actual result |
| --- | --- |
| New native movement-observation fixture | 10/10 passed: interpolation on/off, stop, aim-only turns, wall blocking, pause, two render cadences, lifecycle/reparent and pickup guards |
| Full native EditMode suite in original project | 186/186 passed, 0 failed/skipped; 2026-10-17 08:32:26 UTC; original 176 cases retained |
| Managed verification | Core 24/24 passed; 80 runtime, 14 Editor and 12 test sources compile; five existing unused-field warnings in passive Room304GameStateController |
| Physics isolation | Disposable Editor preview scenes verified non-default before simulation; no user scene simulation or scene save |
| Existing movement/aim/assets | PlayerMotor, PlayerAim, Room304Greybox, character prefab/art unchanged; all 54 protected local-file hashes unchanged |
| Manual gameplay | Not run; no Play Mode, UI input, upgrade selection or gameplay automation |

The first new-fixture run failed in setup because SceneManager.CreateScene is
PlayMode-only. The fixture was corrected to EditorSceneManager.NewPreviewScene,
retaining non-default-physics and all behavioral assertions. The subsequent native
run passed; no gameplay code was changed to accommodate this fixture error.

Local evidence: `/private/tmp/lost-city-walk-physics-probe.log` (root-cause probe),
`/private/tmp/lost-city-walk-regression.xml` / `.log` (initial fixture failure), and
`/private/tmp/lost-city-walk-regression-final.xml` / `.log` (186 passing). Reproduction
instructions are in [the verification guide](../Tools/Verification/README.md).

## Approved Character Motion Follow-Up

The first-pass protagonist and motion brief were separately approved and delegated
on 2026-10-17. Source/model and Unity presentation changes are scoped to the independent
Room304Greybox sample. No Phase 6 progression, save schema, old Player prefab or Room
scene redesign is authorized. [CharacterMotion](CharacterMotion.md) records the
implemented motion and acceptance. That delivery expanded the 131-test spatial
baseline below to **176/176 native EditMode tests passing**; the subsequent walking
correction and current 186-test result are recorded above.

New systems: 25-bone Generic skin rig with reproducible Blender source,
ProtagonistVisualMotion procedural posing, ProtagonistCluePresentation success-event
bridge, authored ProtagonistPickupAnchor and EditMode sampling tools. No new Unity
package dependency. The Blender authoring tool is not required to open Unity.

Data event flow stays `TryCollectClue -> RecordClue -> ClueCollected`; optional visual
contact/recovery occurs afterward and cannot grant another clue or write saves.
The user still owns normal Play Mode and game-feel verification.

### Character Acceptance Results

| Gate | Actual result |
| --- | --- |
| Editable source and FBX roundtrip | Blender 4.2.1 passes 25-bone/weight assertions and 11 deformation cases |
| Unity import and accessories | 25 bones, 94 skins, 8,707 split vertices, 13,584 triangles; anatomical LEFT band/tab and RIGHT bag, positive scales, canonical axes pass |
| Saved prefab/scene | Connected prefab, all script/material/bone references and unchanged 2D physics contract pass |
| Pose and interruption tests | 27 cases pass: alternating gait, support, ground clearance, cycle continuity, head limits/stability, constant limb lengths, pause, reach and cancellation |
| Collection/persistence integration | 18 cases pass: actual event chain, duplicate/failed collection, gates, missing anchors, concurrent clues, cleanup, unsubscribe and scale preservation |
| Full native regression | 176/176 passed, 0 skipped, 2026-10-17 07:36:56 UTC; original 131 tests retained |
| Managed verification | 24 Core tests pass; runtime, persistence, Editor and test sources compile |
| Actual clue contact | All three sample anchors have collision-clear approaches and sampled left-hand contact within 2 cm |
| Offline motion | Four sequences rendered from actual baked posed skin, nonblank/no-magenta/distinct-frame checks pass; sheets visually reviewed |
| Saved sample camera | Updated skinned player rendered and inspected at 1280x720, 1024x768 and 1680x720; references/routes/framing pass; Overlay UI excluded |
| Source/local edits | 54 protected file hashes unchanged; 615 existing scene records inspected, only seven permitted presentation changes plus visual-subtree replacement |
| Manual gameplay | Not run; user owns Play Mode, input, game feel and actual quit/relaunch testing |

The first new collection-fixture run failed before assertions because Unity refused
an additive scene beside an unsaved untitled scene. The fixture now tracks only
its own objects and temporary save, without creating/switching/saving/closing scenes.
The subsequent full native run passes. A real proxy double-scale defect was also
fixed: the proxy root stays unit scale while each mesh preserves source world TRS.
Walking hand height now follows pelvis descent, avoiding artificially raised elbows.

### Character Commit Scope

- Files: `Code/Characters`, character FBX/materials/prefab and metadata; source Blend,
  generators/rig evidence; Room304Sample creator/validator and owned Room304Greybox;
  two character test fixtures; `.gitignore` for local Blender backup files only.
- Documentation: README, documentation index, Architecture, EventFlow, FolderStructure,
  DeveloperOnboarding, Roadmap, PhaseReport, Room30425DSample, ProtagonistFirstPass,
  CharacterMotion, PlayerControl/CharacterVisual APIs, verification guide, rig contract
  and actual preview index/assets.
- Architecture: a visual-only Generic skeleton/IK layer; no new GameFlow state,
  package dependency, controller, progression transaction or save schema.
- Gameplay: existing collection remains authoritative; valid stationary collection
  may show left-hand reach. Movement never locks for animation.
- Event flow: existing successful clue event feeds presentation only. Rendering,
  contact, cancellation and cleanup cannot grant clues or trigger saves.
- Debt/issues: coarse garment attachment/fingers, linear skinning, flat-plane gait,
  94 skin renderers, no retargetable clips or combat animation library. Actual-speed
  turn/strafe feel and artifact clipping remain manual acceptance items.
- Local tooling: the optional Git code-review-graph hook lacks `tree_sitter_c_sharp`;
  index refresh failed but the commit succeeded. No hook was disabled and no package
  installed. Native/managed validation evidence is independent of that index.
- Next: review [actual previews](Previews/CharacterMotion/README.md), test the saved
  sample directly, then scope only observed motion/interaction defects. No Chapter 2.

Commit hash is reported by Git after committing this report; use `git show --stat`
on that commit for the exact file list. Existing user assets and Design source kits
are intentionally left unstaged and unpublished by this change.

Local evidence: `/private/tmp/lost-city-motion-final-editmode.xml` and `.log`,
`lost-city-motion-final-acceptance.log`, `lost-city-motion-sample-validation.log`,
`lost-city-motion-render-final.log`, `lost-city-character-final-views.log`, and
`lost-city-motion-final-room-render.log`. Reproducible command entry points are in
`Tools/Verification/README.md`; preview outputs are committed, machine logs are not.

## Room 304 Spatial Sample Follow-Up

The separately approved 2026-10-17 follow-up builds an independent hospital room
and short corridor in the original project. It is not Chapter 2 and does not replace
the Phase 6 gameplay loop. Detailed acceptance and command evidence are maintained
in [Room30425DSample](Room30425DSample.md).

- Objectives: validate fixed oblique overhead framing, true 3D graybox geometry,
  solid XY 2D physics, original clue pickup and illustrated collected-evidence UI.
- New systems: Room304SampleCreator/Geometry/Validation, Room304SampleController,
  EvidenceIllustrationCatalog, IllustratedEvidenceJournal, EvidenceThumbnailRow,
  EvidenceArtImporter and EvidencePresentationBuilder.
- Shared changes: PlayerAim now ray-intersects the gameplay plane; EvidenceJournal
  has an optional illustrated renderer, preserving its original text fallback.
- Dependencies: existing Unity UI/Input System/2D physics/built-in renderer only.
  No new Unity package, save schema, case, story, boss or physics migration.
- Assets: owned Room304Greybox scene, meshes/materials, input actions and unchanged
  copies of three evidence illustrations plus necessary UI sprites. Existing
  authored scenes/prefabs/definitions are outside this generator's ownership.
- Event changes: collection refreshes the optional illustrated view; sample return
  flushes dirty discoveries before using the existing Hub scene service. No local
  deduction/combat/completion state machine is added.
- Known issues/debt: sample not integrated with combat; installed CJK font fallback;
  one overlap flag per clue; source alpha residue retained; actual game interaction
  remains the user's responsibility. No Computer Use or Play Mode automation.
- Next recommendation: manually validate this spatial sample and the existing
  Phase 6 full loop before combining their layouts or expanding content.
- Actual verification: native generation/reopen/reference/static-route checks pass;
  full EditMode suite **131/131** passes (89 baseline + 42 new); three offline world
  renders are nonblank and have no missing-shader pixels. All 51 protected original
  asset hashes remain unchanged. UI interaction and gameplay are not verified.

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
