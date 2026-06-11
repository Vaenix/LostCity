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
- `MemoryFragmentEnemy`
- `Damageable`
- `TeamMember`
- `XpDropper`
- Unity 2D physics

## Events

- Enemy death uses `Damageable.Died`.

## Usage Example

```csharp
enemySpawner.StopAndClearEnemies();
```

## Common Pitfalls

- Keep enemy AI simple.
- Use weighted spawning for variety instead of complex encounter systems.
- Enemies must be on `CombatTeam.Enemy`.
