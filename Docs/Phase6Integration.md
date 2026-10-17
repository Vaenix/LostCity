# Phase 6 Integration Into The Original Project

## Scope And Ownership

Date: 2026-10-17. Target: `/Users/vaema/Documents/Lost City`, branch `master`.
Phase 6 commit `32fff2c03f4b3b9e613ce24d8ef9e4a1b8f22284` was fast-forward merged.
No temporary project is required for the user's gameplay testing.

The existing modified scenes, prefabs, definitions and Input Actions remain local
uncommitted work. `Assets/Room.unity` and its meta remain untracked. Unrelated `Design/`
files are untouched. Only integration tooling, tests and documentation are committed
on top of Phase 6; the user's unpublished asset work is not silently included.

## Backup

Before integration, tracked and untracked files were saved in Git stash snapshot:

`228b5ad0c15d078b18c022a04033ca03a40950a7`

Persistent local reference: `refs/backup/lost-city/pre-phase6-20261017`.
Stash message: `lost-city-pre-phase6-integration-20261017`.
The snapshot's first parent is the old base; its own tree contains tracked edits;
its third parent contains originally untracked files. No backup reference is pushed.
Retain it until manual acceptance is complete. Do not blindly apply the old stash
again over the migrated project; inspect individual files before restoring anything.

Original Room blob IDs, for preservation checks:

- `Assets/Room.unity`: `6b8d9c32de89bc0667e02fd7429dcc6b558e11ac`.
- `Assets/Room.unity.meta`: `8f2fa1d44db795eb80c6dd938e208a72a5fe9942`.

## Conflict Policy

The original project's regenerated prefabs use different internal file IDs from the
Phase 6 checkout. Its Input Actions also have different action IDs and UI reference
GUIDs. A textual merge alone cannot prove these references work.

- Retain original prefab objects, serialized values, file IDs and matching references.
- Add the new catalog IDs/descriptions and case flags without changing old fields.
- Upgrade scene/prefab wiring in place through Unity Editor APIs, not seed regeneration.
- Rebind the new Hub to the retained local UI input assets.
- Leave Room files unchanged. Phase 6 starts from Hub, not the user's Room scene.
- Keep the user's asset edits unstaged after resolving Git's conflict index.

## Verification

Native Unity 2022.3.32f1 verification completed in the original project on 2026-10-17.
No Play Mode, Computer Use or manual UI interaction was performed.

| Check | Result |
| --- | --- |
| Full EditMode suite | 89 passed, 0 failed, 0 skipped; includes six migration regressions |
| Incremental migration | Exit 0; existing scene/prefab references validated after save/reopen |
| Repeated migration | Exit 0; Player prefab, CombatSandbox, Hub and both Room file hashes unchanged |
| Native meta contracts | Session, unlock, replay, temporary JSON reload and variable clue layouts passed |
| External serialized references | 45 project assets checked; no missing external GUID/fileID references |
| Original authoring state | 53 byte-identical files, six additive-only definitions, two deliberately migrated files; no missing files |
| Room preservation | Both blob IDs above unchanged |
| Git conflict index | No unresolved entries; user assets remain unstaged |

Test output: `/private/tmp/lost-city-main-editmode.xml` (2026-10-17 12:01:36 CST).
Native logs: `/private/tmp/lost-city-main-migration-second.log` and
`/private/tmp/lost-city-main-contracts.log`. Logs are local evidence, not repository assets.

The first import exposed a 33-character GUID in the newly added test metadata. It was
replaced with a valid fresh GUID before the successful native suite; all six new tests
were then discovered and passed. Managed compilation alone did not detect that issue.

The initial restored snapshot audit found 55 byte-identical files and six definitions
with only additive Phase 6 fields. No preexisting field values or files were lost.
After migration, the two changed originals are Player.prefab and CombatSandbox.unity:
existing object IDs, transforms, tuning and references remain; additions are persistence,
authored clue wiring, a disabled pickup template, retry UI and its raycaster. Null UI
action references are repaired. Unity removed one obsolete empty UI rewardPool field;
the active rewards still come from the unchanged case data. Hub references now resolve
against the retained local input assets instead of branch-specific action sub-assets.

## Migration API And Limits

`LostCity.CombatSandbox.EditorTools.Phase6ProjectMigration.MigrateExistingProject()`
is an explicit Editor-only entry point, also available under
`Tools > Lost City > Migrate Existing Project To Phase 6`. It is already applied here.
It has no public fields, properties or events. Dependencies are Unity Editor scene/
prefab/serialization APIs, the existing Phase 6 components and persistent Input Actions.

For another retained pre-Phase-6 sandbox, back up all authoring work, finish importing
Phase 6, save open scenes, then invoke this menu. It requires existing Hub/CombatSandbox
scenes, player components and valid definitions; it refuses ambiguous/missing objects.
It fills missing references and repairs required UI bindings, preserving optional
bindings. It never runs the full generator, changes InputActions/Room, or resets tuning.
Save/reopen validation runs before it reports success. On failure, inspect the error
and any unsaved scene changes; do not rerun the full generator as a substitute.

The optional authored-pickup layout reuses matching objects and generates only missing
clues. The legacy Room controller is a passive serialized compatibility shell, not
another state machine. These are the only runtime compatibility changes; combat,
progression, save schema and the case-completion event chain remain unchanged.

## Remaining Debt

- Normal input, rendering, scene transitions and the complete played loop require user testing.
- Room remains a legacy authoring scene, not the Phase 6 entry point.
- Keep the local backup until acceptance; the user's asset edits are not published.
- The preexisting local Git indexing hook lacks `tree_sitter_c_sharp`; it does not
  block commits or Unity tests and no global environment packages were changed.

## Manual Acceptance

Open Hub in the original project, start Room 304, collect clues, finish deduction,
defeat the boss, resolve upgrades/reward and return to Hub. Verify archive/bestiary
updates and persisted upgrades after relaunch. Inspect the original Room scene and
your local authoring changes separately. No Computer Use or Play Mode automation is used.
