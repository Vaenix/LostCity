# Developer Onboarding

## Start Here

1. Read `README.md`.
2. Read `Docs/Architecture.md`.
3. Read `Docs/GameplayLoop.md`.
4. Run `Tools > Lost City > Create Combat Sandbox`.
5. Open `Assets/_Project/Scenes/CombatSandbox.unity`.
6. Enter Play Mode and verify Room 304.

## Project Architecture

The current architecture is generated-scene first. `CombatSandboxCreator` creates the scene, assets, and serialized references. Most runtime systems should not require manual scene wiring.

`GameFlowManager` owns the chapter state flow:

```text
Investigation -> Deduction -> Combat -> Reward -> ChapterComplete
```

## How To Add Weapons

1. Add or reuse a `ProjectileDefinition`.
2. Add a `WeaponDefinition`.
3. Add a runtime weapon component if behavior differs from `PistolWeapon` or `MemoryOrbWeapon`.
4. Update `CombatSandboxCreator` to generate and assign references.
5. Update `Docs/API/WeaponSystem.md`.

## How To Add Enemies

1. Add or reuse an `EnemyDefinition`.
2. Add behavior to `MemoryFragmentEnemy` only if the archetype fits the existing simple model.
3. Create a generated prefab in `CombatSandboxCreator`.
4. Add a weighted spawn entry.
5. Update `Docs/API/EnemySystem.md`.

## How To Add Bosses

1. Create a dedicated boss behavior class when attacks are boss-specific.
2. Create or duplicate a boss prefab.
3. Create a `BossDefinition` asset and assign the prefab, health, speed, skills, XP, and reward pool.
4. Assign the `BossDefinition` to the active `CaseDefinition`.
5. Ensure the prefab has `Damageable` and `TeamMember`.
6. Only update `CombatSandboxCreator` if the default generated sandbox needs to include the new boss.
7. Update `Docs/API/BossSystem.md`.

## How To Add Clues

1. Add a `ClueDefinition` asset through the Unity Editor.
2. Add the clue to the active `CaseDefinition.Clues`.
3. Add a `CluePickup` in the scene or scene generator so the clue can be collected.
4. Update `Docs/API/InvestigationSystem.md` and `Docs/API/DeductionSystem.md`.

Do not procedurally generate mystery logic. Clue relationships are handcrafted.

## How To Add Chapters

1. Do not add real Chapter 2 content until Room 304 is validated.
2. Create a `CaseDefinition` for the chapter's first playable case.
3. Assign clue, boss, and reward definitions from data assets.
4. Keep `GameFlowManager` readable. Do not build a large abstract framework before two chapters exist.
5. Update `Docs/API/ChapterSystem.md`.

## How To Add UI Panels

1. Create a focused UI controller script.
2. Generate the Canvas, panel, text, buttons, and references from `CombatSandboxCreator`.
3. Ensure buttons have `Button` components, target graphics, and listeners.
4. Ensure Canvas has `GraphicRaycaster` when interaction is required.
5. Update `Docs/API/UISystem.md`.

## How To Add Rewards

1. Add a `RewardDefinition`.
2. Choose `RewardType` and `RewardStatType`.
3. Add it to a case, boss, or upgrade reward pool.
4. Keep reward effects visible in HUD where possible.
5. Do not add code for simple stat rewards; use the existing reward stat types.
6. Update `Docs/API/RewardSystem.md`.

## Common Rules

- Keep systems small and explicit.
- Do not add save data until session flow is stable.
- Do not add final art or VFX before architecture validation.
- Do not leave generated references for manual setup unless unavoidable.
- Every behavior change needs documentation.
