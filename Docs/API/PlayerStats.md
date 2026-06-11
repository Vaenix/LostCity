# PlayerStats

## Purpose

`PlayerStats` stores current-session player combat and progression stats.

## Responsibilities

- Max HP and current HP sync with `Damageable`.
- Attack multiplier for outgoing damage.
- Defense and dodge for incoming enemy damage.
- Move speed, fire rate, crit chance, crit damage, XP multiplier, and pickup radius.
- Notify UI and systems when stats change.

## Public Fields

No public fields. Serialized private fields are assigned by generated prefabs.

## Public Properties

- `MaxHp`
- `CurrentHp`
- `Attack`
- `Defense`
- `MoveSpeed`
- `FireRate`
- `CritChance`
- `CritDamage`
- `DodgeChance`
- `XpMultiplier`
- `PickupRadius`

## Public Methods

- `MultiplyAttack(float multiplier)`
- `MultiplyMaxHp(float multiplier)`
- `MultiplyFireRate(float multiplier)`
- `AddCritChance(float amount)`
- `AddDodgeChance(float amount)`
- `RollOutgoingDamageMultiplier()`
- `ModifyIncomingDamage(DamageInfo damageInfo)`

## Dependencies

- Requires `Damageable`.
- Used by `PlayerHud`, weapons, rewards, and damage handling.

## Events

- `StatsChanged`

## Usage Example

```csharp
playerStats.MultiplyAttack(1.1f);
playerStats.AddCritChance(0.05f);
```

## Common Pitfalls

- `PlayerStats` is not saved between sessions.
- `MultiplyMaxHp` resets current health to the new max.
- Use public methods for upgrades so UI receives `StatsChanged`.
