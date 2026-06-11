# Folder Structure

## Root

| Path | Purpose |
| --- | --- |
| `README.md` | Project overview and quick start. |
| `Docs/` | Permanent project documentation. |
| `Assets/` | Unity assets and project code. |
| `Packages/` | Unity package manifest and lock files. |
| `ProjectSettings/` | Unity project settings. |

## Documentation

| Path | Purpose |
| --- | --- |
| `Docs/README.md` | Documentation index and development rules. |
| `Docs/Architecture.md` | System architecture and diagrams. |
| `Docs/GameplayLoop.md` | Current gameplay loop and phase flow. |
| `Docs/EventFlow.md` | Trigger to system to result event chains. |
| `Docs/DeveloperOnboarding.md` | How to extend the project safely. |
| `Docs/Roadmap.md` | Current phase, completed systems, blockers, and next steps. |
| `Docs/API/` | Major gameplay system API notes. |

## Unity Project

| Path | Purpose |
| --- | --- |
| `Assets/_Project/Art/CombatSandbox/Generated` | Generated graybox sprites. |
| `Assets/_Project/Code/CombatSandbox` | Runtime and editor C# scripts for the prototype. |
| `Assets/_Project/Prefabs/CombatSandbox` | Generated prefabs. |
| `Assets/_Project/Scenes` | Generated playable scene. |
| `Assets/_Project/ScriptableObjects/CombatSandbox` | ScriptableObject definitions for cases, clues, bosses, rewards, weapons, enemies, and projectiles. |
| `Assets/_Project/Settings/Input` | Generated Input System assets and action references. |

## Code Folders

| Path | Purpose | Contains |
| --- | --- | --- |
| `Camera` | Camera follow behavior. | `SimpleTopDownCameraFollow` |
| `Core` | Shared combat and flow primitives. | `Damageable`, `DamageInfo`, `TeamMember`, `GameFlowManager`, `GameFlowState` |
| `Debug` | Temporary prototype debug tools. | `Room304DebugTools` |
| `Editor` | Scene and asset generation. | `CombatSandboxCreator` |
| `Enemies` | Enemy data and behavior. | `EnemyDefinition`, `MemoryFragmentEnemy`, `WardenBoss` |
| `Feedback` | Runtime feedback components. | `HitFlash`, `WorldHealthBar` |
| `Investigation` | Case, clue, and case progress systems. | `CaseDefinition`, `ClueDefinition`, `ClueType`, `CluePickup`, `InvestigationProgress` |
| `Pickups` | XP pickup flow. | `XpDropper`, `XpOrb` |
| `Player` | Player input, movement, aim, death, and stats. | `PlayerInputReader`, `PlayerMotor`, `PlayerAim`, `PlayerDeathHandler`, `PlayerStats` |
| `Progression` | XP, levels, upgrades, rewards. | `PlayerExperience`, `CombatUpgradeStats`, `RewardDefinition`, `RewardType`, `RewardStatType` |
| `Spawning` | Enemy and boss spawning. | `EnemySpawner`, `BossSpawnController`, `EnemySpawnEntry` |
| `UI` | HUD, prompts, journal, deduction, reward, completion, minimap. | `GamePromptManager`, `PromptType`, `PlayerHud`, `EvidenceJournal`, `DeductionBoard`, `Room304RewardSelectionUI`, `Room304CompletionUI` |
| `Weapons` | Manual and automatic weapon logic. | `PistolWeapon`, `MemoryOrbWeapon`, `Projectile`, `WeaponDefinition`, `ProjectileDefinition` |
