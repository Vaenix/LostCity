# Lost City Development Constitution

This document defines permanent development rules for Lost City.

## Primary Goal

The project must remain:

- Understandable.
- Maintainable.
- Extensible.
- Team-friendly.

Future developers must be able to join without reading the entire codebase. The project must never depend on AI memory. Documentation is a first-class deliverable.

## Development Priorities

1. Gameplay architecture.
2. System stability.
3. Documentation.
4. Gameplay content.
5. Art.
6. Visual polish.
7. Optimization.

Do not prioritize art, VFX, animation, or polish before gameplay systems are validated.

## Documentation Requirement

- Every feature must include documentation.
- Every behavior-changing bug fix must update documentation.
- Every commit must update documentation.
- A task is not complete until documentation is updated.

## Required Documentation Structure

```text
Docs/
|-- README.md
|-- Architecture.md
|-- GameplayLoop.md
|-- FolderStructure.md
|-- EventFlow.md
|-- DeveloperOnboarding.md
|-- Roadmap.md
|-- PhaseReport.md
`-- API/
    |-- PlayerStats.md
    |-- GameFlowManager.md
    |-- CombatSystem.md
    |-- CaseSystem.md
    |-- InvestigationSystem.md
    |-- DeductionSystem.md
    |-- BossSystem.md
    |-- RewardSystem.md
    |-- ChapterSystem.md
    |-- EnemySystem.md
    |-- WeaponSystem.md
    `-- UISystem.md
```

## README Requirement

The root README must always contain:

- Project overview.
- Current development phase.
- Current playable features.
- Controls.
- How to run.
- Folder structure summary.
- Current roadmap.
- Known issues.
- Latest architecture diagram.

## Architecture Requirement

`Docs/Architecture.md` must contain:

- System overview.
- Manager overview.
- State machines.
- Scene flow.
- Data flow.
- Dependencies.
- Subsystem relationships.

Use Mermaid diagrams whenever architecture changes.

## Event Flow Requirement

`Docs/EventFlow.md` must document:

- Clue pickup.
- Journal update.
- Deduction submission.
- Truth reconstruction.
- Boss spawn.
- Boss death.
- Reward selection.
- Level up.
- Scene transition.
- Chapter completion.

Each event chain should use:

```text
Trigger
↓
System
↓
Result
```

## API Documentation Requirement

Every major gameplay system requires documentation covering:

- Purpose.
- Responsibilities.
- Public fields.
- Public properties.
- Public methods.
- Dependencies.
- Events.
- Usage examples.
- Common pitfalls.

## Commit Report Requirement

Every commit report must contain:

1. Commit hash.
2. Files changed.
3. Documentation files updated.
4. Architecture changes.
5. Gameplay changes.
6. Event flow changes.
7. Technical debt.
8. Known issues.
9. Recommended next steps.

## Phase Completion Requirement

At the end of every development phase, update `Docs/PhaseReport.md` with:

- Objectives.
- Completed features.
- Architecture changes.
- New systems.
- New dependencies.
- Known issues.
- Verification results.
- Next phase recommendations.

## AI Development Rule

Before writing code:

1. Identify affected systems.
2. Identify affected documentation.
3. Update documentation plan.
4. Implement code.
5. Update documentation.
6. Generate commit summary.

Documentation and code must remain synchronized.

## Project Philosophy

Lost City is built as:

```text
Investigation + Deduction + Roguelike Combat
```

Validate gameplay architecture first. Do not over-invest in art, animation, VFX, or story content before the gameplay loop is fully validated.
