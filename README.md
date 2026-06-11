<div align="center">

# Lost City

**Mystery Puzzle + Roguelike Action**

Lost City is an indie Unity project about exploring unstable memory spaces, solving layered mysteries, and surviving lightweight top-down combat encounters.

Current prototype: **Combat Sandbox Phase 2**

```text
Status: Playable combat prototype
Engine: Unity 2022 LTS
Target: PC first, mobile later
Repository: Unity + C# + GitHub
```

</div>

---

## Game Vision

Lost City is a roguelike mystery game that combines exploration, environmental storytelling, deduction, and light combat.

Players enter procedurally generated locations such as abandoned cities, ancient ruins, futuristic facilities, and forgotten settlements. Each run reshuffles the physical experience while preserving long-term player knowledge and progression.

The intended experience is not only about defeating enemies. The player investigates fragmented stories, collects clues, reconstructs hidden truths, and gradually discovers that the deeper mystery is personal.

### Core Pillars

**1. Mystery & Deduction**

- Inspired by lateral-thinking puzzles, including Sea Turtle Soup-style deduction.
- Players collect clues and reconstruct hidden stories.
- Scenarios can contain multiple layers of truth.

**2. Lightweight Roguelike Combat**

- Hades-style top-down perspective.
- Lower mechanical difficulty than traditional action games.
- One manually controlled weapon.
- One or more automatic companion/drone weapons.
- Upgrade choices during runs.

**3. Emotional Narrative**

- Surface goal: escape, survive, defeat a boss, recover memories.
- Hidden goal: personal growth, emotional healing, self-discovery.

---

## Core Gameplay Loop

```text
Enter run
   |
   v
Explore location
   |
   +--> Fight enemies
   |       |
   |       +--> Gain XP
   |       +--> Level up
   |       +--> Choose upgrades
   |
   +--> Discover clues
   |       |
   |       +--> Reconstruct story
   |       +--> Form deductions
   |
   v
Unlock deeper area or encounter
   |
   v
Face boss / reveal truth
   |
   v
Carry knowledge and progression forward
```

The current prototype validates only the combat and upgrade loop. Mystery, exploration rooms, clue logic, story progression, and save systems are planned but not yet implemented.

---

## Current Progress

### Completed

- Unity project setup.
- Top-down player movement.
- Mouse aiming.
- Manual pistol weapon.
- Automatic drone weapon.
- Enemy spawning.
- Enemy chasing AI.
- Enemy health system.
- World-space health bars.
- XP drops.
- XP collection.
- Player leveling.
- Upgrade selection UI.
- Fire Rate upgrade.
- Damage upgrade.
- Drone Projectile upgrade.

### Current Prototype

**Combat Sandbox Phase 2**

The current scene is a graybox arena designed to test the basic feel of:

- Movement.
- Manual shooting.
- Automatic companion fire.
- Enemy pressure.
- XP collection.
- Level-up upgrade choices.

This prototype does not include final art, narrative content, exploration rooms, clues, save data, or boss encounters.

---

## Repository Structure

```text
Lost City/
|-- Assets/
|   `-- _Project/
|       |-- Art/
|       |   `-- CombatSandbox/
|       |-- Code/
|       |   `-- CombatSandbox/
|       |       |-- Camera/
|       |       |-- Core/
|       |       |-- Editor/
|       |       |-- Enemies/
|       |       |-- Feedback/
|       |       |-- Pickups/
|       |       |-- Player/
|       |       |-- Progression/
|       |       |-- Spawning/
|       |       |-- UI/
|       |       `-- Weapons/
|       |-- Prefabs/
|       |   `-- CombatSandbox/
|       |-- Scenes/
|       |-- ScriptableObjects/
|       |   `-- CombatSandbox/
|       `-- Settings/
|           `-- Input/
|-- Packages/
|-- ProjectSettings/
`-- README.md
```

### Key Runtime Areas

- `Camera/` - simple top-down camera follow behavior.
- `Core/` - combat teams, damage data, health/damage handling.
- `Enemies/` - current Memory Fragment enemy definition and behavior.
- `Feedback/` - hit flash and world-space health bars.
- `Pickups/` - XP orb drop and collection logic.
- `Player/` - input reading, movement, aiming, and death handling.
- `Progression/` - XP, levels, and combat upgrade stats.
- `Spawning/` - enemy wave spawning for the sandbox arena.
- `UI/` - upgrade selection UI.
- `Weapons/` - pistol, memory orb, projectile, and weapon definitions.
- `Editor/` - automation for generating the Combat Sandbox scene and assets.

---

## Getting Started

### Requirements

- Unity **2022.3.32f1** or compatible Unity 2022 LTS version.
- Git.
- A local clone of this repository.

### Unity Packages

The project currently uses:

- Unity Input System.
- Unity 2D feature set.
- Unity UI.
- Cinemachine.
- TextMeshPro.
- Unity Test Framework.

Unity should restore packages automatically from `Packages/manifest.json` when the project opens.

---

## How To Run

1. Clone the repository.

   ```bash
   git clone git@github.com:Vaenix/LostCity.git
   ```

2. Open the project folder in Unity Hub.

3. Use Unity **2022.3.32f1** or another compatible Unity 2022 LTS editor.

4. Open the sandbox scene:

   ```text
   Assets/_Project/Scenes/CombatSandbox.unity
   ```

5. Press **Play**.

### Regenerating The Sandbox

The project includes an editor automation tool for rebuilding the combat sandbox assets and scene:

```text
Tools > Lost City > Create Combat Sandbox
```

After running the tool, the generated `CombatSandbox` scene should be playable immediately.

---

## Collaboration Workflow

This project is currently structured for a solo developer, but the repository is organized so collaborators can safely join later.

Recommended workflow:

1. Pull the latest `master`.
2. Create a feature branch.
3. Keep changes focused on one feature, bug fix, or content slice.
4. Test in Unity before opening a pull request.
5. Include a short summary of what changed and how it was verified.

Before merging, verify:

- Unity compiles without console errors.
- `CombatSandbox` still enters Play Mode.
- Player movement, pistol fire, drone fire, enemy spawning, XP collection, and upgrade selection still work.

---

## Git Branch Strategy

```text
master
  |
  +-- feature/combat-elite-enemies
  |
  +-- feature/boss-prototype
  |
  +-- feature/clue-system
  |
  +-- fix/ui-upgrade-clicks
```

### Branch Guidelines

- `master` should remain playable.
- Use `feature/<name>` for new systems.
- Use `fix/<name>` for bug fixes.
- Use `prototype/<name>` for experiments that may be thrown away.
- Avoid large mixed commits that combine gameplay, art, scenes, and unrelated refactors.

---

## Roadmap

### Current Milestone

**Combat Sandbox Phase 2**

Goal: validate whether the dual-weapon combat loop feels good enough to support the future mystery/exploration game.

### Planned Next Milestones

- Multiple enemy archetypes.
- Elite enemies.
- Boss encounters.
- Exploration rooms.
- Interactive clue system.
- Story fragments.
- Deduction board.
- Procedural mystery generation.
- Save/progression system.

### Roadmap Shape

```text
Phase 1 - Combat Foundation
    Done: movement, aiming, pistol, drone, enemies, spawning

Phase 2 - Combat Feedback & Progression
    Done: HP bars, XP, leveling, upgrade choices

Phase 3 - Enemy Variety
    Planned: new enemy archetypes, elites, encounter tuning

Phase 4 - Boss Prototype
    Planned: one boss encounter with readable attack patterns

Phase 5 - Exploration Prototype
    Planned: connected rooms, interactables, clue placement

Phase 6 - Mystery Prototype
    Planned: clue collection, deduction board, story fragments

Phase 7 - Progression Prototype
    Planned: persistent progression and run-to-run structure
```

---

## Team Roles

Current expected structure:

```text
Solo Developer
  |-- Gameplay programming
  |-- Systems architecture
  |-- Graybox level design
  |-- Prototype UI
  `-- Build/test ownership
```

Potential future collaborators:

- **Gameplay Programmer** - combat feel, enemy behavior, boss implementation.
- **Narrative Designer** - mysteries, clue chains, emotional story structure.
- **Level Designer** - exploration spaces, encounter layout, pacing.
- **Technical Artist / VFX Artist** - combat feedback, readability, atmosphere.
- **UI/UX Designer** - deduction board, clue organization, upgrade presentation.
- **Composer / Sound Designer** - mood, combat feedback, environmental audio.

---

## Future Vision

Lost City is intended to become a replayable mystery-action game where every run provides tension, discovery, and emotional context.

The long-term goal is to combine:

- Roguelike replayability.
- Layered mystery and clue systems.
- Environmental storytelling.
- Accessible top-down combat.
- Emotional narrative progression that persists beyond individual runs.

The current repository is intentionally small and prototype-focused. The priority is to prove the core game feel first, then expand into mystery, exploration, and story systems only after the combat foundation is stable.

---

## License

License placeholder.

No final license has been selected yet. All rights are reserved until a license is added.
