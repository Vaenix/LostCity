# ChapterSystem

## Purpose

The chapter system is currently a lightweight flow validation layer.

## Responsibilities

- Represent active case completion.
- Allow the player to continue with Space.
- Show placeholder next chapter text.
- Avoid creating real Chapter 2 content before Room 304 is validated.

## Public Fields

No public fields. UI references are serialized.

## Public Properties

`GameFlowManager.CurrentState` reports `ChapterComplete` after reward selection.

## Public Methods

- `Room304CompletionUI.ShowChapterComplete()`
- `Room304CompletionUI.ShowChapterComplete(string caseName, string completionText)`
- `Room304CompletionUI.ShowNextChapterPlaceholder()`
- `Room304CompletionUI.Hide()`

## Dependencies

- `GameFlowManager`
- `CaseDefinition`
- Unity Input System for Space key
- Unity UI text

## Events

- `Room304CompletionUI.ContinueRequested`
- `GameFlowManager.StateChanged`

## Usage Example

```csharp
completionUI.ShowChapterComplete();
```

## Common Pitfalls

- This is not a full chapter framework yet.
- Completion text should come from `CaseDefinition`.
- Do not add Chapter 2 content while Room 304 is unverified.
- No scene transition exists yet.
