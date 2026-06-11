# Roadmap

## Current Phase

**Room 304 gameplay loop validation**

The current objective is to validate the complete chapter loop:

```text
探索 -> 收集线索 -> 推理成功 -> Boss生成 -> Boss战 -> Boss死亡 -> 奖励选择 -> 章节完成
```

## Completed Systems

- Unity project setup.
- Combat sandbox generation tool.
- Top-down player movement.
- Mouse aiming.
- Manual pistol weapon.
- Automatic memory orb weapon.
- Player health and death.
- Player stats.
- Enemy spawning.
- Enemy archetypes.
- The Warden boss prototype.
- Combat HUD.
- Minimap.
- World-space health bars.
- XP drops and collection.
- Level-up upgrade choices.
- Room 304 clue pickups.
- Evidence journal.
- Deduction board.
- Game flow state machine.
- Boss death to reward transition.
- Chapter complete screen.
- Placeholder next chapter state.

## In Progress Systems

- Room 304 end-to-end Play Mode validation.
- Generated scene migration to the latest `GameFlowManager` flow.
- Documentation baseline.

## Future Systems

- Stronger generated-scene validation.
- Save and persistent progression.
- Multi-room exploration.
- More clue interaction types.
- More enemy tuning.
- Elite enemy encounters.
- Real chapter data model.
- Chapter 2 content after Room 304 validation.

## Blocked Systems

- Chapter 2 content is blocked until Room 304 is manually validated.
- Persistent progression is blocked until in-session progression is stable.
- Final story content is blocked until the investigation and deduction systems prove usable.
- Art polish is blocked until gameplay architecture is stable.

## Technical Debt

- `Room304GameStateController` is legacy.
- Some generated Unity assets in the working tree may be stale until the generator is rerun.
- Unity batchmode generation may fail on local licensing IPC, requiring editor menu generation.
- There are no automated Play Mode tests for the generated Room 304 chain.

## Recommended Next Phase

1. Regenerate the sandbox from the Unity Editor menu.
2. Manually test the full Room 304 chain.
3. Commit the regenerated scene, prefabs, ScriptableObjects, and input assets after validation.
4. Remove or archive stale `Assets/Room.unity` if confirmed unused.
5. Add reference validation to `CombatSandboxCreator`.
