# CombatSystem

## Purpose

The combat system validates lightweight top-down combat for the mystery loop.

## Responsibilities

- Resolve health and damage through `Damageable`.
- Separate player and enemy teams through `TeamMember`.
- Move and aim the player.
- Fire manual and automatic weapons.
- Spawn enemies and boss encounters.
- Award XP and upgrades.

## Public Fields

No single combat facade exists. Combat is composed from focused MonoBehaviours.

## Public Properties

Key properties:

- `Damageable.CurrentHealth`
- `Damageable.MaxHealth`
- `Damageable.IsAlive`
- `TeamMember.Team`

## Public Methods

- `Damageable.ApplyDamage(DamageInfo damageInfo)`
- `Damageable.SetMaxHealth(float value, bool resetCurrentHealth)`
- `Damageable.ResetHealth()`

## Dependencies

- Unity 2D physics.
- Unity Input System.
- `PlayerStats` for damage modifiers.
- `EnemySpawner` and `BossSpawnController` for encounter setup.

## Events

- `Damageable.Damaged`
- `Damageable.Died`
- `Damageable.HealthChanged`

## Usage Example

```csharp
damageable.ApplyDamage(new DamageInfo(10f, CombatTeam.Player, source, hitPoint));
```

## Common Pitfalls

- Do not use 3D physics types in this project.
- Combat should not start before deduction except through debug tools.
- Keep combat simpler than Hades. Investigation remains a core pillar.
