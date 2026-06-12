# ChapterSystem

## Purpose

The Phase 6 chapter system derives chapter and case progression from ordered case IDs, chapter prerequisites, and completed case IDs.

The existing Room 304 completion UI remains in place. Task 2 adds the pure selection rules that later Hub and catalog tasks will consume.

## Responsibilities

- Define ordered cases for each chapter.
- Require all chapter prerequisite case IDs before unlocking that chapter.
- Select the first unlocked chapter that still has incomplete cases.
- Fall back to the latest unlocked non-empty chapter when all unlocked chapters are complete.
- Ignore null and empty chapter definitions.
- Preserve the existing chapter-complete UI flow until later runtime integration.

## Public Types

`CaseProgressionInput` is the chapter-level input:

- `OrderedCaseIds`: Sequential case order within the chapter.
- `RequiredCompletedCaseIds`: Completion prerequisites for the entire chapter.

It is a pure C# type in `LostCity.Meta.Core`.

## Public Properties

`GameFlowManager.CurrentState` continues to report `ChapterComplete` after reward selection.

## Public Methods

### `CaseProgressionRules.GetCurrentChapterIndex`

```csharp
int GetCurrentChapterIndex(
    IReadOnlyList<CaseProgressionInput> chapters,
    ISet<string> completedCaseIds)
```

The method:

1. Returns `-1` for a null list, an empty list, or no unlocked non-empty chapter.
2. Skips null chapters and chapters with no case IDs.
3. Skips chapters whose prerequisite IDs are not all completed.
4. Returns the first unlocked chapter containing any incomplete case.
5. Returns the latest unlocked non-empty chapter when every unlocked case is complete.

A null completion set is treated as empty.

### Existing Completion UI

- `Room304CompletionUI.ShowChapterComplete()`
- `Room304CompletionUI.ShowChapterComplete(string caseName, string completionText)`
- `Room304CompletionUI.ShowNextChapterPlaceholder()`
- `Room304CompletionUI.Hide()`

## Sequential Unlocking

Chapter prerequisites and case order are separate gates:

- Chapter prerequisites lock every case in the chapter until all prerequisite IDs complete.
- After the chapter unlocks, its first case is available.
- Each later case requires the immediately preceding case to complete.
- A completed case remains completed and does not regress to available.

## Dependencies

Meta Core:

- `System.Collections.Generic`
- No `UnityEngine` dependency

Existing completion UI:

- `GameFlowManager`
- `CaseDefinition`
- Unity Input System
- Unity UI text

## Events

- `Room304CompletionUI.ContinueRequested`
- `GameFlowManager.StateChanged`

The pure chapter progression rules raise no events.

## Usage Example

```csharp
CaseProgressionInput[] chapters =
{
    new CaseProgressionInput(new[] { "room_304" }, new string[0]),
    new CaseProgressionInput(new[] { "future_case" }, new[] { "room_304" })
};

int currentChapter = CaseProgressionRules.GetCurrentChapterIndex(
    chapters,
    new HashSet<string> { "room_304" });
```

`currentChapter` is `1` because the first chapter is complete and its completion unlocks the second chapter.

## Common Pitfalls

- Do not select a locked chapter merely because it appears later in the list.
- Do not select null or empty chapter definitions as the current chapter.
- Keep chapter prerequisites as stable case IDs, not scene names or display text.
- Do not persist the current chapter index; recompute it from definitions and completion data.
- Do not add Chapter 2 content as part of Task 2.
