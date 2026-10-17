# Lost City Documentation

For testing Phase 6 in the original project while preserving local authoring work,
see [Phase 6 Integration](Phase6Integration.md).

This folder is the source of truth for Lost City development. Code is not considered complete unless the relevant documentation is updated in the same change.

## Current Phase

Lost City has implemented **Phase 6, Meta Framework**. Automated acceptance passes;
manual Play Mode acceptance remains pending. See [PhaseReport](PhaseReport.md).

The separately authorized [Room 304 2.5D sample](Room30425DSample.md) validates
spatial investigation and supplied evidence illustrations without replacing the
Phase 6 loop or adding a chapter.

- [Approved design](superpowers/specs/2026-06-12-phase-6-meta-framework-design.md)
- [Implementation plan](superpowers/plans/2026-06-12-phase-6-meta-framework.md)

The active prototype validates this chain:

```text
大厅 -> 选择案件 -> 探索 -> 收集线索 -> 推理 -> Boss -> 奖励 -> 案件完成 -> 大厅
```

Phase 6 adds Hub, registry, progression, persistence, archive, bestiary, chapter, and return-flow architecture without adding final story, art, VFX, Chapter 2, or new playable content.

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
- [PlayerControl](API/PlayerControl.md)
- [EvidencePresentation](API/EvidencePresentation.md)
- [GameFlowManager](API/GameFlowManager.md)
- [CombatSystem](API/CombatSystem.md)
- [CaseSystem](API/CaseSystem.md)
- [InvestigationSystem](API/InvestigationSystem.md)
- [DeductionSystem](API/DeductionSystem.md)
- [BossSystem](API/BossSystem.md)
- [RewardSystem](API/RewardSystem.md)
- [ChapterSystem](API/ChapterSystem.md)
- [EnemySystem](API/EnemySystem.md)
- [WeaponSystem](API/WeaponSystem.md)
- [UISystem](API/UISystem.md)
- [SaveSystem](API/SaveSystem.md)
- [CaseRegistry](API/CaseRegistry.md)
- [ArchiveSystem](API/ArchiveSystem.md)
- [BestiarySystem](API/BestiarySystem.md)
- [Verification commands](../Tools/Verification/README.md)

## Development Rules

- Gameplay architecture comes before content and polish.
- Documentation must be updated with every feature and every behavior-changing bug fix.
- Mermaid diagrams must be updated whenever architecture, state flow, or event flow changes.
- New systems need API documentation before the task is considered complete.
- New cases, clues, bosses, and rewards should be added as ScriptableObject data and prefabs before core code is changed.
- Do not rely on AI memory for project knowledge. Put durable knowledge in this folder.
