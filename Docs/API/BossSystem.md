# BossSystem

## Purpose

The boss system validates post-deduction combat and boss death transitions using `BossDefinition`.

## Responsibilities

- Spawn the configured boss prefab through `BossSpawnController`.
- Apply boss health, move speed, XP reward, skills, and reward pool from `BossDefinition`.
- Assign player target to boss behavior.
- Execute simple boss attacks through `WardenBoss`.
- Report boss death through `Damageable.Died`.
- Allow `GameFlowManager` to transition to reward flow.

## Public Fields

No public fields. Boss prefab and spawn references are serialized.

## Public Properties

- `BossSpawnController.ActiveBoss`
- `BossDefinition.Name`
- `BossDefinition.BossId`
- `BossDefinition.BestiaryDescription`
- `BossDefinition.Health`
- `BossDefinition.MoveSpeed`
- `BossDefinition.Skills`
- `BossDefinition.XpReward`
- `BossDefinition.RewardPool`

## Public Methods

- `BossSpawnController.SpawnBoss()`
- `BossSpawnController.SetBossDefinition(BossDefinition definition)`

## Dependencies

- `BossDefinition`
- Boss prefab with `WardenBoss`, `Damageable`, `TeamMember`, and `Rigidbody2D`.
- `GameFlowManager`
- `EnemySpawner` for summoned enemies and remaining enemy cleanup.

## Events

- `BossSpawnController.BossSpawned`
- `Damageable.Died`

SpawnBoss records the configured BossId through MetaProgressionRecorder after
applying its definition. Repeated spawn requests return the active instance and
do not duplicate encounters. Setting a null definition clears stale selection.

GameFlowManager disables the tracked boss's XpDropper and awards XpReward directly
once on death, before closing XP intake. This intentionally replaces the old
late boss-orb pickup behavior. Enemies, projectiles and remaining XP orbs are
cleared, but every existing/boss-earned level choice is drained before showing
the case reward. GameplayMetaIntegrationTests verifies this ordering.

## Usage Example

```csharp
GameObject boss = bossSpawnController.SpawnBoss();
```

## Common Pitfalls

- The boss must have `Damageable` or reward flow will not trigger.
- The boss definition must reference a prefab.
- Boss death should stop combat and clear enemies.
- Do not add final boss art or VFX before the flow is validated.
