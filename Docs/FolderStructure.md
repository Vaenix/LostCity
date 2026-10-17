# Folder Structure

## Root

| Path | Responsibility |
| --- | --- |
| README.md | Overview, controls, run instructions, roadmap, known issues |
| Docs/ | Permanent design, API, event, onboarding, phase evidence |
| Tools/Verification/ | Reproducible managed compilation/core tests; native command guide |
| Assets/ | Unity source, content, prefabs, scenes and metadata |
| Packages/ | Package manifest and lock file |
| ProjectSettings/ | Unity project settings, including scene build order |

## Meta Code

All paths below are under `Assets/_Project/Code/Meta`.

| Folder | Responsibility | Key types |
| --- | --- | --- |
| Core | Unity-independent DTOs, sanitization, progression/start rules | SaveGameData, PlayerProgressData, CaseProgressionRules, CaseStartRules |
| Persistence | Unity JSON and filesystem adapter; separate assembly | SaveGameManager |
| Definitions | Chapter/catalog ScriptableObjects and ID validation | ChapterDefinition, GameContentCatalog, ContentDefinitionIndex |
| Runtime | Persistent services and player/discovery integration | GameRuntime, CaseRegistry, CaseSessionManager, SceneFlowManager, PlayerProgressPersistence |
| UI | Hub selection, archive, bestiary and shared view helper | HubController, HubCaseRow, InvestigationArchiveUI, BestiaryUI |

Core has no UnityEngine references. Persistence depends on Core. Runtime/UI remain in
Assembly-CSharp; Editor utilities stay in Assembly-CSharp-Editor. EditMode tests use
reflection for predefined assemblies instead of illegal asmdef references.

## Combat Sandbox Code

All paths below are under `Assets/_Project/Code/CombatSandbox`.

| Folder | Responsibility | Key types |
| --- | --- | --- |
| Camera | Top-down following | SimpleTopDownCameraFollow |
| Core | Damage, teams and local flow | Damageable, GameFlowManager, GameFlowState |
| Debug | Prototype shortcuts | PrototypeDebugTools |
| Editor | Asset/scene creation, catalog indexing and validation | CombatSandboxCreator, HubSceneBuilder, GameContentCatalogBuilder, Phase6EditorValidation |
| Enemies | Definitions and behavior | EnemyDefinition, MemoryFragmentEnemy, BossDefinition, WardenBoss |
| Feedback | Combat feedback | HitFlash, WorldHealthBar |
| Investigation | Case/clue data, pickups and transient clue state | CaseDefinition, ClueDefinition, CluePickup, CaseClueLayout, InvestigationProgress |
| Pickups | XP drops and collection | XpDropper, XpOrb |
| Player | Input, movement, aiming, health/death and stats | PlayerInputReader, PlayerMotor, PlayerAim, PlayerStats |
| Progression | Experience and data-driven rewards | PlayerExperience, CombatUpgradeStats, RewardDefinition |
| Spawning | Weighted enemies and case boss | EnemySpawner, BossSpawnController |
| UI | HUD/minimap, journal, deduction, prompts and rewards | PlayerHud, DeductionBoard, RewardSelectionUI, CaseCompletionUI |
| Weapons | Manual/automatic weapon and projectile logic | PistolWeapon, MemoryOrbWeapon, Projectile |

## Content And Generated Assets

| Path under Assets/_Project | Contents |
| --- | --- |
| Art/CombatSandbox/Generated | Existing graybox circle/square sprites |
| Prefabs/CombatSandbox | Player, projectiles, XP orb, four enemies, Warden |
| Scenes | Hub.unity and shared CombatSandbox.unity |
| ScriptableObjects/CombatSandbox | Existing case/clues, boss/enemies, weapons, projectiles, rewards |
| ScriptableObjects/Meta | Chapter_01 and metadata-only Case_Placeholder |
| Resources | Generated GameContentCatalog.asset for runtime loading |
| Settings/Input | CombatSandbox.inputactions plus persistent action references |
| Tests/EditMode/Meta | Pure core tests and Unity-native save/content/UI tests |

Generation status is in PhaseReport; paths describe intended generator output, not a claim
that ungenerated scenes already exist. Add custom content outside seeded paths because
Create Combat Sandbox overwrites its seed values.

## Documentation

Docs/API covers the system contracts. Architecture describes ownership and dependencies;
EventFlow describes triggers and results; GameplayLoop describes player-facing flow.
DeveloperOnboarding is the extension guide, Roadmap tracks remaining scope, PhaseReport
records actual verification and blockers. Historical plans under Docs/superpowers are design
records; current API documentation and source take precedence after documented adjustments.
