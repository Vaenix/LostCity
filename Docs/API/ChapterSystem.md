# ChapterSystem

## Definition

`LostCity.Meta.ChapterDefinition` is a ScriptableObject with `ChapterId`,
`ChapterName`, `Description`, ordered `CaseDefinition[] Cases`, and
`string[] RequiredCompletedCaseIds`. Empty display names fall back to the asset
name. Array getters return copies and tolerate null serialized fields.

The generated catalog discovers chapters automatically. Chapter order follows
ordinal asset path, then stable sub-asset local ID. Case order follows the
chapter array, independent of case filenames and IDs.

## Progression

`LostCity.Meta.Core.CaseProgressionInput` holds copied ordered IDs and
prerequisites. Core rules derive case state:

1. Completed cases stay completed and may replay when content is valid.
2. Incomplete chapter prerequisites lock incomplete cases.
3. The first case in an unlocked chapter is available.
4. Later cases require their immediately preceding case to be completed.

Metadata-only cases participate in progression and can become available, but
`CaseRegistry.CanStart` always rejects them. Truly unassigned catalog cases
remain visible outside chapters, with playability validated separately.

## Current Chapter

```csharp
int CaseProgressionRules.GetCurrentChapterIndex(
    IReadOnlyList<CaseProgressionInput> chapters,
    ISet<string> completedCaseIds);

ChapterDefinition CaseRegistry.GetCurrentChapter();
ChapterDefinition CaseRegistry.GetChapterForCase(CaseDefinition definition);
```

Selection returns the first unlocked nonempty chapter with incomplete cases.
When all unlocked chapters are complete, it returns the latest unlocked
nonempty chapter. Null/empty chapters are skipped; no match returns -1/null.
Availability and current chapter are computed, not persisted.

## Validation And Ownership

IDs must be stable, nonblank, and trimmed. Duplicate chapter IDs exclude every
ambiguous chapter. Null/missing/ambiguous cases, duplicate references, and
cross-chapter membership block affected cases instead of treating them as
unlocked unassigned content. Invalid progression chapters are not current.

`GameRuntime` owns the registry. Definitions do not own mutable player state.
Save completion and `DataChanged` drive fresh views. Shared scene names belong to
SceneFlowManager, not chapter definitions or individual case branches.

## Extending Content

Create a valid CaseDefinition with a new stable ID. Add it at the intended
position in one chapter's Cases array, or leave it unassigned. Use prerequisite
case IDs for chapter gates. The postprocessor rebuilds the catalog; the explicit
menu is `Tools/Lost City/Rebuild Content Catalog`. No case-specific registry or
Hub code is needed. Seeding belongs to the Editor generator; this phase does not
add a second playable chapter.

## Tests

Core tests cover predecessor gates, prerequisites, completed states, current
chapter, and metadata start rejection. `MetaRuntimeTests` covers Unity-facing
chapter order, unassigned discovery, placeholder unlocking, and ambiguous
membership. Native EditMode execution remains separate from compilation.
