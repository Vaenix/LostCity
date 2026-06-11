# WeaponSystem

## Purpose

The weapon system validates the dual-weapon combat design.

## Responsibilities

- Fire manual pistol projectiles.
- Fire automatic memory orb projectiles.
- Resolve projectile damage.
- Apply player attack and crit modifiers.
- Support fire rate, damage, and drone projectile upgrades.

## Public Fields

No public fields. Weapon definitions and prefab references are serialized.

## Public Properties

Key data lives on:

- `WeaponDefinition`
- `ProjectileDefinition`
- `CombatUpgradeStats`

## Public Methods

Weapons are controlled by input or update loops. Projectiles are initialized by weapon scripts.

## Dependencies

- `PlayerInputReader`
- `PlayerAim`
- `WeaponDefinition`
- `ProjectileDefinition`
- `Projectile`
- `PlayerStats`
- `CombatUpgradeStats`

## Events

No public weapon events currently exist.

## Usage Example

Create and assign weapons through `CombatSandboxCreator`, not manual scene setup.

## Common Pitfalls

- Do not leave weapon references null in prefabs.
- Projectiles use 2D physics and trigger colliders.
- Combat is a support pillar, not the whole game.
