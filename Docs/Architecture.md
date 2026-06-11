# Architecture

## System Overview

Lost City currently runs as a generated Unity combat sandbox scene with Room 304 investigation and chapter flow layered on top.

The prototype is intentionally simple:

- One generated scene.
- One playable player.
- One investigation case.
- One deduction board.
- One boss encounter.
- One reward screen.
- One placeholder next chapter state.

## Manager Overview

| Manager or Controller | Purpose |
| --- | --- |
| `GameFlowManager` | Owns Room 304 state transitions. |
| `InvestigationProgress` | Tracks collected clues and deduction success. |
| `DeductionBoard` | Lets the player choose clues and submit the deduction. |
| `EnemySpawner` | Spawns weighted enemy archetypes during combat. |
| `BossSpawnController` | Spawns The Warden and exposes the active boss. |
| `PlayerStats` | Stores player combat and progression stats for the current session. |
| `Room304RewardSelectionUI` | Applies one post-boss reward. |
| `Room304CompletionUI` | Shows chapter completion and placeholder next chapter text. |
| `CombatSandboxCreator` | Editor automation that generates scene, prefabs, ScriptableObjects, input actions, and references. |

## State Machine

```mermaid
stateDiagram-v2
    [*] --> Investigation
    Investigation --> Deduction: all required clues collected
    Deduction --> Combat: correct deduction submitted
    Combat --> Reward: boss dies
    Reward --> ChapterComplete: reward selected
    ChapterComplete --> ChapterComplete: Space shows placeholder next chapter
```

## Scene Flow

```mermaid
flowchart TD
    Create["Tools/Lost City/Create Combat Sandbox"]
    Scene["CombatSandbox.unity"]
    Play["Enter Play Mode"]
    Explore["Investigation"]
    Deduce["Deduction"]
    Fight["Combat"]
    Reward["Reward"]
    Complete["ChapterComplete"]

    Create --> Scene
    Scene --> Play
    Play --> Explore
    Explore --> Deduce
    Deduce --> Fight
    Fight --> Reward
    Reward --> Complete
```

## Data Flow

```mermaid
flowchart LR
    Clue["ClueDefinition"]
    Pickup["CluePickup"]
    Progress["InvestigationProgress"]
    Board["DeductionBoard"]
    Flow["GameFlowManager"]
    Boss["BossSpawnController"]
    Reward["Room304RewardSelectionUI"]
    Stats["PlayerStats"]

    Clue --> Pickup
    Pickup --> Progress
    Progress --> Board
    Board --> Progress
    Progress --> Flow
    Flow --> Boss
    Boss --> Flow
    Flow --> Reward
    Reward --> Stats
```

## Dependencies

- Runtime input uses Unity Input System.
- UI uses Unity UI.
- Combat physics uses Unity 2D physics.
- Generated assets are graybox placeholders.
- `CombatSandboxCreator` depends on UnityEditor APIs and only runs in the Editor.

## Subsystem Relationships

- Investigation and deduction are handcrafted. Mystery logic is not procedural.
- Combat starts only after deduction success or debug spawn.
- Rewards modify `PlayerStats` directly and persist until the scene or session resets.
- Chapter progression currently ends at a placeholder state. No Chapter 2 content exists.

## Known Architecture Debt

- `Room304GameStateController` is legacy and should be retired after generated scenes fully migrate to `GameFlowManager`.
- Scene generation still needs a stronger automated validation pass for all serialized references.
- Session persistence exists only in-memory through scene objects. There is no save system.
