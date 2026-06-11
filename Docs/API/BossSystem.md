# BossSystem

## Purpose

The boss system validates post-deduction combat and boss death transitions.

## Responsibilities

- Spawn The Warden through `BossSpawnController`.
- Assign player target to boss behavior.
- Execute simple boss attacks through `WardenBoss`.
- Report boss death through `Damageable.Died`.
- Allow `GameFlowManager` to transition to reward flow.

## Public Fields

No public fields. Boss prefab and spawn references are serialized.

## Public Properties

- `BossSpawnController.ActiveBoss`

## Public Methods

- `BossSpawnController.SpawnBoss()`

## Dependencies

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
- Boss death should stop combat and clear enemies.
- Do not add final boss art or VFX before the flow is validated.
