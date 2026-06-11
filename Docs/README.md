# Lost City Documentation

This folder is the source of truth for Lost City development. Code is not considered complete unless the relevant documentation is updated in the same change.

## Current Phase

Lost City is in **Room 304 gameplay loop validation**.

The active prototype validates this chain:

```text
探索 -> 收集线索 -> 推理成功 -> Boss生成 -> Boss战 -> Boss死亡 -> 奖励选择 -> 章节完成
```

No final story, final art, VFX polish, localization framework, or Chapter 2 content should be added until Room 304 is validated end to end.

## Required Reading Order

1. [Architecture.md](Architecture.md)
2. [GameplayLoop.md](GameplayLoop.md)
3. [EventFlow.md](EventFlow.md)
4. [FolderStructure.md](FolderStructure.md)
5. [DeveloperOnboarding.md](DeveloperOnboarding.md)
6. [Roadmap.md](Roadmap.md)
7. [DevelopmentConstitution.md](DevelopmentConstitution.md)

## API Documentation

- [PlayerStats](API/PlayerStats.md)
- [GameFlowManager](API/GameFlowManager.md)
- [CombatSystem](API/CombatSystem.md)
- [InvestigationSystem](API/InvestigationSystem.md)
- [DeductionSystem](API/DeductionSystem.md)
- [BossSystem](API/BossSystem.md)
- [RewardSystem](API/RewardSystem.md)
- [ChapterSystem](API/ChapterSystem.md)
- [EnemySystem](API/EnemySystem.md)
- [WeaponSystem](API/WeaponSystem.md)
- [UISystem](API/UISystem.md)

## Development Rules

- Gameplay architecture comes before content and polish.
- Documentation must be updated with every feature and every behavior-changing bug fix.
- Mermaid diagrams must be updated whenever architecture, state flow, or event flow changes.
- New systems need API documentation before the task is considered complete.
- Do not rely on AI memory for project knowledge. Put durable knowledge in this folder.
