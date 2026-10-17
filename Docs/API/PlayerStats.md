# PlayerStats

## Responsibilities

PlayerStats synchronizes HP with Damageable and supplies attack, defense, movement,
fire rate, critical/dodge chances, XP multiplier and pickup radius. Serialized
baseline combat values are captured once in Awake.

## Public API

Combat properties:

- `MaxHp`, `CurrentHp`: maximum/current HP synchronized with Damageable.
- `Attack`, `Defense`, `MoveSpeed`, `FireRate`: combat and movement values.
- `CritChance`, `CritDamage`, `DodgeChance`: critical and evasion values.
- `XpMultiplier`, `PickupRadius`: XP gain and pickup range.

Combat methods:

- `MultiplyAttack(float)`, `MultiplyMaxHp(float)`, `MultiplyFireRate(float)`.
- `AddCritChance(float)`, `AddDodgeChance(float)`.
- `RollOutgoingDamageMultiplier()`: attack multiplier including the critical roll.
- `ModifyIncomingDamage(DamageInfo)`: enemy damage after dodge/defense.

Permanent progression adds:

- `AttackMultiplier`, `MaxHpMultiplier`, `FireRateMultiplier`.
- `CritChanceBonus`, `DodgeChanceBonus`.
- `ApplyPermanentProgress(PlayerProgressData)`.
- `PermanentProgressChanged`, separate from `StatsChanged`.

ApplyPermanentProgress derives values from the captured baseline, not current
values, so applying the same save cannot compound multipliers. It also restores
full health for a new run. Normal maximum-HP rewards retain their existing heal
behavior. Current HP is never a persisted value.

Permanent mutation methods notify both events. Damage/healing only notifies
StatsChanged and never writes progression. Use public methods for rewards rather
than editing private fields.

## Dependencies And Events

PlayerStats requires Damageable. Weapons use outgoing multipliers; Damageable
uses ModifyIncomingDamage; PlayerHud consumes StatsChanged; RewardDefinition and
CombatUpgradeStats use the mutation methods. PlayerProgressPersistence depends
on Meta Core DTOs and the separate Meta Persistence save service.

`StatsChanged` updates presentation for health and combat changes.
`PermanentProgressChanged` signals only permanent stat mutations to persistence.

## Usage And Pitfalls

```csharp
playerStats.MultiplyAttack(1.2f);
playerStats.AddCritChance(0.05f);
```

These methods notify presentation and autosave permanent progression when the
persistence component is initialized. Do not subscribe save writes to
StatsChanged, or every health change will write. MultiplyMaxHp heals to the new
maximum. ApplyPermanentProgress is for restoring baseline-relative values;
calling it mid-run also restores full health. Do not apply saved multipliers by
calling Multiply methods, which would compound previously restored values.

## Persistence Lifecycle

PlayerProgressPersistence is on the player and requires PlayerStats,
PlayerExperience and CombatUpgradeStats. Its serialized references have those
same lower-camel names. Start restores a save once after every Awake has run;
this prevents Damageable.Awake from resetting restored maximum HP. It then
subscribes to permanent-stat, XP and drone events. Disable/enable only removes
and restores subscriptions; it does not reapply the save.

Public methods: `Initialize(SaveGameManager)`, `Capture()`,
`RecordReward(RewardDefinition)`, `BeginProgressMutation()`, `EndProgressMutation()`.
Properties: `IsInitialized`, `CanPersist`,
`SaveGame`. Capture returns whether the current progression is durable and
writable, not merely whether a DTO was constructed.

PlayerExperience exports/imports through `WriteProgressTo` and
`ApplySavedProgress`, exposes `ProgressChanged`, `UpgradeChoicesChanged`,
`PendingUpgradeChoices` and `AcceptsExperience`, and controls intake with
`SetAcceptsExperience` and `ResumePendingChoices`. CombatUpgradeStats exposes `PermanentProgressChanged`,
`SetExtraDroneProjectiles` and `WriteProgressTo`.

## Boundaries

Saved: level, XP/threshold, pending upgrade choices, permanent attack/HP/fire-rate multipliers, critical
and dodge bonuses, drone bonus, unlock IDs. Defense, movement, current HP and
other run-only fields are not part of the approved v1 schema.

PendingUpgradeChoices is a backward-compatible v1 field (missing in old saves
means zero). Initialize restores the count and starts the queue only after all
Awake methods and restoration/subscriptions have finished. Upgrade application
suppresses intermediate captures; the stat effect, decremented choice count and
unlock IDs are saved in one logical root mutation. Quit/death reload therefore
cannot replay an already saved effect or permanently lose an outstanding choice.

GameplayMetaIntegrationTests covers restored stats, health-only notifications,
subscription idempotency, XP export, multiple synchronous choices, and save retry.
