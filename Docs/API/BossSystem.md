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

## Usage Example

```csharp
GameObject boss = bossSpawnController.SpawnBoss();
```

## Common Pitfalls

- The boss must have `Damageable` or reward flow will not trigger.
- The boss definition must reference a prefab.
- Boss death should stop combat and clear enemies.
- Do not add final boss art or VFX before the flow is validated.
