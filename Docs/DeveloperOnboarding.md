# Developer Onboarding

## Start Here

1. Read README, Architecture, GameplayLoop, and the current PhaseReport.
2. Open Unity 2022.3.32f1 with an active license. Import packages.
3. Open the included Hub scene. Do not regenerate already-authored sandbox assets.
4. Run `Validate Meta Framework` and `Validate Meta Contracts`.
5. Open Hub and manually verify the flow in PhaseReport.
6. Run the EditMode test suite. Managed fallback commands live in Tools/Verification.

Do not equate a successful compiler check with generated-scene or Play Mode verification.
The user performs gameplay interactions; automation operates on code, assets, and tests.
For an older local scene retained during a Phase 6 merge, see
[Phase6Integration](Phase6Integration.md). Back up local changes before migration;
`Create Combat Sandbox` is a destructive seed rebuild, not a normal startup step.

## Architecture In One Page

`RuntimeBootstrap` creates one persistent `GameRuntime` before the scene starts.
It loads SaveGameManager, catalog/registry, session, and scene-flow services in that order.
Hub lists registry entries. Selecting one starts a session; the shared investigation scene
reads its CaseDefinition. GameFlowManager owns local Investigation/Deduction/Combat/Reward/
ChapterComplete states. Meta services own persistence and scene transitions.

ScriptableObjects describe content, save DTOs store IDs and player modifiers, and scene
components own transient objects. Never store scene object references in JSON. See
[CaseRegistry](API/CaseRegistry.md) and [SaveSystem](API/SaveSystem.md).

## Adding A Case Or Clue

1. Create assets outside the generator's seeded asset paths.
2. Create each ClueDefinition with a unique, nonblank, trimmed ID and Chinese visible text.
3. Create CaseDefinition with unique CaseId, name, summary, required clues, question,
   answer, BossDefinition, and at least one applicable reward.
4. Set IsPlayable only when this content is complete. Metadata-only cases remain visible
   but cannot be started.
5. The asset postprocessor rebuilds the catalog automatically. Use
   `Tools > Lost City > Rebuild Content Catalog` if needed.
6. Add the case to exactly one ChapterDefinition in progression order; unassigned valid
   cases appear under 未归档案件 and are available without a chapter predecessor.
7. Do not edit Hub or GameFlowManager for another case. CaseClueLayout reuses optional
   authored pickups by clue ID and generates remaining clues from its disabled template.
   Register fixed pickups in the layout instead of leaving unmanaged duplicates active.

Duplicate IDs exclude all ambiguous definitions. A missing predecessor or duplicate
chapter membership blocks starting, rather than silently unlocking content. Do not
rename stable IDs after players have saves; asset filenames may change independently.
Saved old IDs are preserved even if their definitions disappear.

Deduction relationships remain handcrafted. This framework does not generate mystery truth.

## Adding A Chapter

Real Chapter 2 content is not yet authorized. When authorized, create ChapterDefinition,
assign its stable ID/name and ordered case list, and set RequiredCompletedCaseIds.
A later case depends on its immediate predecessor. Chapters are catalog-ordered by asset
path, not by a numeric chapter-name parser. Keep ordering intentional.
The current generator creates Chapter_01 with Room 304 plus one non-playable placeholder.

## Adding Enemies Or Bosses

Enemies need an EnemyDefinition with stable EnemyId, Chinese display name, and bestiary
description, a compatible prefab, and a weighted spawn entry. Reuse MemoryFragmentEnemy
for existing archetypes. A new behavior warrants a focused component, not a new framework.

Bosses need a BossDefinition with stable BossId, description, prefab, stats, skills, and
rewards. The prefab must contain root Damageable, TeamMember, and appropriate behavior.
Assign it to the case. Catalog inclusion and encounter recording are separate: content
appears as unknown until actually encountered. No new boss is part of Phase 6.

## Adding Weapons Or Rewards

Reuse ProjectileDefinition and WeaponDefinition for data-only changes; add a runtime
component only for genuinely different behavior. Keep manual and auto weapon input separate.

Create RewardDefinition and add it to an upgrade, case, or boss pool. Use existing stat
types for attack, health, fire rate, crit, dodge, and drone count. Unlock rewards persist
their UnlockId; a weapon reward is not a general inventory/unlock implementation.
Case completion uses GetCompletionRewards: valid case rewards first, boss fallback only
when no applicable case reward remains. Keep UI and start validation on this shared resolver.

## Adding UI Panels

Use focused controllers and serialized references. HubSceneBuilder owns Hub generation;
CombatSandboxCreator owns the sandbox. Interactive Canvases need GraphicRaycaster and the
scene needs one EventSystem with InputSystemUIInputModule and persisted action references.
Subscribe/unsubscribe listeners symmetrically. Paused gameplay must not block UI input.
Visible strings are Chinese; identifiers and asset names remain English. This is not a
localization framework.

## Saves And Tests

Production data lives at Application.persistentDataPath/lost-city-save.json.
Never run destructive tests on that path. Tests and Meta Contracts use a unique temporary
directory. Do not delete a real save to hide a regression. Future schema files stay read-only;
corrupt files are preserved before defaults are created. Back up real data before recovery.

When changing schema, add a version/migration and tests for sparse, corrupt, and future data.
When changing flow, test normal completion, replay, interruption, repeated callbacks, and
save failure. Health damage is not a permanent-progress save trigger.

## Collaboration And Handoff

Preserve other work, keep .meta GUIDs with moved scripts, and avoid generated-asset churn.
The generator resets seeded definitions but preserves asset GUIDs; it is not an authoring
tool for custom content. Document deviations and test gaps.

Each commit updates relevant documentation, including Architecture, EventFlow, GameplayLoop,
and PhaseReport when required by the active phase. A handoff lists commit hash, changed files,
documentation, architecture/gameplay/event changes, technical debt, known issues, and next steps.
