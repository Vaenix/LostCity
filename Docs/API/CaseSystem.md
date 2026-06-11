# CaseSystem

## Purpose

The case system makes each investigation case data-driven through `CaseDefinition`.

## Responsibilities

- Store the active case id, name, and description.
- Provide the clue list for journal and deduction UI.
- Provide the deduction question and correct answer text.
- Provide the boss definition for combat.
- Provide the reward pool after boss death.
- Provide chapter completion text.

## Public Fields

No public fields. Case data is configured through serialized ScriptableObject fields.

## Public Properties

`CaseDefinition` exposes:

- `CaseId`
- `CaseName`
- `Description`
- `Clues`
- `DeductionQuestion`
- `CorrectAnswer`
- `BossDefinition`
- `RewardPool`
- `CompletionText`

## Public Methods

No public methods. Runtime systems read immutable case data through properties.

## Dependencies

- `ClueDefinition`
- `BossDefinition`
- `RewardDefinition`
- `InvestigationProgress`
- `CombatSandboxCreator`

## Events

No direct events. `InvestigationProgress` raises clue and case progress events based on the active case data.

## Usage Example

Create a new case by making a `CaseDefinition`, assigning clue assets, assigning a boss definition, assigning a reward pool, and then assigning the case to `InvestigationProgress`.

## Common Pitfalls

- Do not hardcode case text in UI scripts.
- Do not generate mystery logic procedurally.
- A case without clues cannot enter deduction.
- A case without a boss definition cannot spawn a data-driven boss.
