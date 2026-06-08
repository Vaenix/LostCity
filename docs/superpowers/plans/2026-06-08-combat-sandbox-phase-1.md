# Combat Sandbox Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Unity graybox combat sandbox that validates top-down movement, mouse aiming, pistol shooting, an automatic memory orb, enemy chase/contact damage, continuous spawning, player death, and scene restart.

**Architecture:** Use a small 2D top-down sandbox on the XY plane. Keep combat data in ScriptableObjects where it helps tuning, and use MonoBehaviours for scene behavior. Avoid room systems, narrative, save data, inventory, and UI.

**Tech Stack:** Unity, C#, Unity Input System, optional Cinemachine for camera follow.

---

## Phase Folder Structure

```text
Assets/
  _Project/
    Code/
      CombatSandbox/
        Camera/
        Core/
        Enemies/
        Player/
        Spawning/
        Weapons/
    Prefabs/
      CombatSandbox/
    Scenes/
    ScriptableObjects/
      CombatSandbox/
docs/
  superpowers/
    plans/
```

## Required GameObjects

- `CombatGameManager`
  - Components: `CombatGameManager`
- `Main Camera`
  - Components: `Camera`, either `SimpleTopDownCameraFollow` or Cinemachine Brain + Virtual Camera setup
- `Player`
  - Components: `Rigidbody2D`, `CapsuleCollider2D` or `CircleCollider2D`, `TeamMember`, `Damageable`, `PlayerInputReader`, `PlayerMotor`, `PlayerAim`, `PlayerDeathHandler`
  - Children: `AimRoot`, `Muzzle`, `MemoryOrb`
- `Pistol`
  - Can be on `AimRoot` or `Player`
  - Components: `PistolWeapon`
- `MemoryOrb`
  - Components: `MemoryOrbWeapon`
- `Projectile_Player`
  - Components: `Rigidbody2D`, `CircleCollider2D` with `Is Trigger`, `Projectile`
- `Projectile_Orb`
  - Components: `Rigidbody2D`, `CircleCollider2D` with `Is Trigger`, `Projectile`
- `MemoryFragmentEnemy`
  - Components: `Rigidbody2D`, `CapsuleCollider2D` or `CircleCollider2D`, `TeamMember`, `Damageable`, `MemoryFragmentEnemy`
- `EnemySpawner`
  - Components: `EnemySpawner`
- `ArenaFloor`
  - Components: `SpriteRenderer` or `TilemapRenderer`, optional `BoxCollider2D` for arena bounds

## Required ScriptableObjects

- `PistolWeaponDefinition`
  - `WeaponDefinition`
- `OrbWeaponDefinition`
  - `WeaponDefinition`
- `PistolProjectileDefinition`
  - `ProjectileDefinition`
- `OrbProjectileDefinition`
  - `ProjectileDefinition`
- `MemoryFragmentEnemyDefinition`
  - `EnemyDefinition`

## Script Dependencies

- `Damageable` uses `DamageInfo`
- `Projectile`, `MemoryFragmentEnemy`, and `PlayerDeathHandler` use `Damageable`
- `Projectile` and damage sources use `CombatTeam`
- `TeamMember` identifies player/enemy allegiance
- `PlayerMotor` and `PistolWeapon` read from `PlayerInputReader`
- `PlayerAim` reads mouse position from Unity Input System
- `PistolWeapon` and `MemoryOrbWeapon` use `WeaponDefinition` and spawn `Projectile`
- `MemoryFragmentEnemy` uses `EnemyDefinition`
- `EnemySpawner` spawns the enemy prefab and tracks alive enemies
- `PlayerDeathHandler` calls `CombatGameManager`

## Step-by-Step Implementation Order

- [x] Create `Assets/_Project` combat sandbox folders.
- [x] Add shared combat primitives: team, damage info, health, and game manager.
- [x] Add player input, movement, aiming, and death restart scripts.
- [x] Add projectile and weapon ScriptableObject definitions.
- [x] Add manual pistol weapon.
- [x] Add automatic memory orb weapon.
- [x] Add memory fragment enemy chase/contact damage.
- [x] Add continuous enemy spawner.
- [x] Add simple top-down camera follow fallback for teams not using Cinemachine yet.
- [x] Add editor setup guide with required GameObjects, components, and tuning values.

## Minimum Playable Editor Setup

1. Install/enable Unity Input System.
2. Create a new scene at `Assets/_Project/Scenes/CombatSandbox.unity`.
3. Create a large plane or cube floor at world origin.
4. Create the player prefab at `(0, 0, 0)`.
5. Create projectile and enemy prefabs.
6. Create the ScriptableObject definitions listed above.
7. Assign projectile definitions and prefabs to pistol/orb weapon definitions.
8. Assign the enemy prefab to `EnemySpawner`.
9. Set camera to top-down follow.
10. Press Play.

## Recommended Tuning Defaults

- Player move speed: `7`
- Player health: `100`
- Pistol cooldown: `0.25`
- Pistol projectile damage: `18`
- Pistol projectile speed: `22`
- Orb cooldown: `0.8`
- Orb range: `12`
- Orb projectile damage: `10`
- Orb projectile speed: `16`
- Enemy health: `45`
- Enemy move speed: `3.5`
- Enemy contact damage: `12`
- Enemy contact cooldown: `0.75`
- Spawner interval: `1.25`
- Max alive enemies: `12`
- Spawn radius: `18`
- Minimum spawn distance from player: `8`

## Deferred From Phase 1

- Ranged enemy
- Health UI
- Score/kill counter
- Weapon upgrades
- Room transitions
- Save system
- Narrative/mystery content
- Mobile controls
