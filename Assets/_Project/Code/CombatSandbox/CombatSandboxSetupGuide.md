# Combat Sandbox Setup Guide

This phase is a single-scene Unity graybox. It intentionally avoids final art, narrative, UI polish, save data, procedural rooms, and upgrades.

## Packages

Install these through Unity Package Manager:

- Input System
- Cinemachine, optional but preferred

When Unity asks to enable the new Input System backend, accept and restart the editor.

## Scene

Create:

```text
Assets/_Project/Scenes/CombatSandbox.unity
```

Use a 2D scene on the XY plane.

## ScriptableObjects

Create these in `Assets/_Project/ScriptableObjects/CombatSandbox/`:

### PistolProjectileDefinition

- Type: `Lost City/Combat Sandbox/Projectile Definition`
- Damage: `18`
- Speed: `22`
- Lifetime Seconds: `2`

### OrbProjectileDefinition

- Type: `Lost City/Combat Sandbox/Projectile Definition`
- Damage: `10`
- Speed: `16`
- Lifetime Seconds: `2.5`

### PistolWeaponDefinition

- Type: `Lost City/Combat Sandbox/Weapon Definition`
- Display Name: `Pistol`
- Projectile Prefab: `Projectile_Player`
- Projectile Definition: `PistolProjectileDefinition`
- Cooldown Seconds: `0.25`
- Range: `14`

### OrbWeaponDefinition

- Type: `Lost City/Combat Sandbox/Weapon Definition`
- Display Name: `Memory Orb`
- Projectile Prefab: `Projectile_Orb`
- Projectile Definition: `OrbProjectileDefinition`
- Cooldown Seconds: `0.8`
- Range: `12`

### MemoryFragmentEnemyDefinition

- Type: `Lost City/Combat Sandbox/Enemy Definition`
- Max Health: `45`
- Move Speed: `3.5`
- Stopping Distance: `0.75`
- Contact Damage: `12`
- Contact Cooldown Seconds: `0.75`

## GameObjects

### CombatGameManager

Create an empty GameObject named `CombatGameManager`.

Add:

- `CombatGameManager`

### ArenaFloor

Create a 2D sprite or tilemap named `ArenaFloor`.

Recommended sprite setup:

- Position: `(0, 0, 0)`
- Scale: `(40, 40, 1)`
- Add a `BoxCollider2D` only if you need arena bounds
- Assign a neutral gray material

### Player

Create a 2D sprite or capsule sprite named `Player`.

Recommended setup:

- Position: `(0, 0, 0)`
- Tag: optional
- `Rigidbody2D`
  - Gravity Scale: `0`
  - Body Type: `Dynamic`
  - Constraints: freeze rotation
- `CapsuleCollider2D` or `CircleCollider2D`
- `TeamMember`
  - Team: `Player`
- `Damageable`
  - Max Health: `100`
  - Destroy On Death: `false`
- `PlayerInputReader`
  - Leave input references empty for runtime WASD/left-click fallback
- `PlayerMotor`
  - Move Speed: `7`
- `PlayerAim`
  - Yaw Root: assign child `AimRoot`
  - Aim Camera: assign `Main Camera`
- `PlayerDeathHandler`
  - Restart Delay Seconds: `1.25`
  - Disable On Death: assign `PlayerMotor`, `PistolWeapon`, `MemoryOrbWeapon`

Create children:

```text
Player
  AimRoot
    Muzzle
  MemoryOrb
```

`AimRoot`:

- Position: `(0, 0, 0)`

`Muzzle`:

- Position: `(0.8, 0, 0)` relative to `AimRoot`

`MemoryOrb`:

- Position: `(1.25, 1.25, 0)`
- Add `MemoryOrbWeapon`
- Weapon Definition: `OrbWeaponDefinition`

Add `PistolWeapon` to `AimRoot` or `Player`:

- Input Reader: `Player`
- Player Aim: `Player`
- Muzzle: `Muzzle`
- Weapon Definition: `PistolWeaponDefinition`

### Projectile_Player

Create a small 2D circle sprite prefab.

- Scale: `(0.25, 0.25, 1)`
- `Rigidbody2D`
  - Gravity Scale: `0`
  - Body Type: `Dynamic`
- `CircleCollider2D`
  - Is Trigger: `true`
- `Projectile`
  - Default Definition: `PistolProjectileDefinition`

### Projectile_Orb

Duplicate `Projectile_Player`.

- Scale: `(0.2, 0.2, 1)`
- `Projectile`
  - Default Definition: `OrbProjectileDefinition`

### MemoryFragmentEnemy

Create a 2D sprite or capsule sprite prefab.

- Position: any
- `Rigidbody2D`
  - Gravity Scale: `0`
  - Body Type: `Dynamic`
  - Constraints: freeze rotation
- `CapsuleCollider2D` or `CircleCollider2D`
- `TeamMember`
  - Team: `Enemy`
- `Damageable`
  - Max Health: `45`
  - Destroy On Death: `false`
- `MemoryFragmentEnemy`
  - Definition: `MemoryFragmentEnemyDefinition`

### EnemySpawner

Create an empty GameObject named `EnemySpawner`.

Add:

- `EnemySpawner`

Recommended values:

- Enemy Prefab: `MemoryFragmentEnemy`
- Player: `Player`
- Arena Center: empty object at `(0, 0, 0)` or leave as spawner transform
- Spawn Interval Seconds: `1.25`
- Initial Spawn Count: `4`
- Max Alive Enemies: `12`
- Spawn Radius: `18`
- Minimum Distance From Player: `8`

### Camera

Option A, preferred:

- Install Cinemachine.
- Add a Cinemachine Virtual Camera.
- Follow: `Player`
- Look At: `Player`
- Set top-down offset/framing.

Option B, fallback:

- Add `SimpleTopDownCameraFollow` to `Main Camera`.
- Target: `Player`
- Offset: `(0, 0, -10)`
- Follow Sharpness: `10`

## Playtest Checklist

- WASD moves player on XY plane.
- Mouse rotates player aim root.
- Left click fires pistol projectile.
- Memory orb shoots nearest enemy without input.
- Enemies chase player.
- Projectiles damage enemies.
- Contact damages player.
- Dead enemies disappear.
- More enemies spawn over time.
- Player death reloads the scene.
