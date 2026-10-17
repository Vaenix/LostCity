# EnemySystem

## Purpose

The enemy system validates lightweight combat pressure.

## Responsibilities

- Define enemy archetype stats through `EnemyDefinition`.
- Spawn weighted enemies through `EnemySpawner`.
- Chase, charge, shoot, or tank depending on archetype.
- Drop XP through `XpDropper`.
- Show health bars and hit flash feedback.

## Public Fields

No public fields. Runtime data is serialized.

## Public Properties

Important data lives on `EnemyDefinition`:

- `DisplayName`
- `EnemyId`
- `BestiaryDescription`
- `Archetype`
- `MaxHealth`
- `MoveSpeed`
- `ContactDamage`
- `ShootRange`
- `ShootCooldownSeconds`

## Public Methods

- `EnemySpawner.StopAndClearEnemies()`

## Dependencies

- `EnemyDefinition`
- `BossDefinition` for boss configuration.
- `MemoryFragmentEnemy`
- `Damageable`
- `TeamMember`
- `XpDropper`
- Unity 2D physics

## Events

- Enemy death uses `Damageable.Died`.
- MemoryFragmentEnemy.OnEnable records EnemyId through MetaProgressionRecorder.
  Re-enabling removes/re-adds the death handler and save set mutations are
  idempotent, so repeated encounters do not write duplicates.

GameFlowManager deactivates/removes remaining enemy objects and projectiles when
the boss dies. New XP intake stops after direct boss XP settlement; pending
level-up choices still finish before the case reward appears.

## Usage Example

```csharp
enemySpawner.StopAndClearEnemies();
```

## Common Pitfalls

- Keep enemy AI simple.
- Use weighted spawning for variety instead of complex encounter systems.
- Enemies must be on `CombatTeam.Enemy`.
