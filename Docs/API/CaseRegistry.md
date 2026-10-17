# CaseRegistry

## Ownership

`GameRuntime` is the persistent `DontDestroyOnLoad` singleton. It initializes save,
loads `Resources/GameContentCatalog`, then initializes registry, session, and
scene flow. Duplicate roots destroy themselves without clearing the active root.
Services use namespace `LostCity.Meta`; SaveGameManager uses
`LostCity.Meta.Persistence` in its separate Unity-enabled assembly.

## Catalog

`LostCity.CombatSandbox.EditorTools.GameContentCatalogBuilder.Rebuild()` discovers
every Case/Chapter/Clue/Enemy/Boss definition with AssetDatabase, including
sub-assets. References sort by ordinal asset path, then local file ID. It writes
`Assets/_Project/Resources/GameContentCatalog.asset` only when references change.
Menu: `Tools/Lost City/Rebuild Content Catalog`.

The postprocessor coalesces imported/deleted/moved `.asset` changes into delayed
rebuilds; catalog-only updates are ignored to prevent recursion. Runtime never
uses AssetDatabase. The catalog retains all discovered references so invalid
relationships are diagnosable; the registry excludes ambiguous definitions from
every exposed index and view.

IDs must be nonblank and trimmed. `ContentDefinitionIndex` logs every duplicate
and removes **all** definitions sharing that ID, not an arbitrary first entry.
This applies independently to all five categories.

## Public API

```csharp
void Initialize(GameContentCatalog catalog, SaveGameManager saveGame);
IReadOnlyList<ChapterDefinition> Chapters { get; }
IReadOnlyList<CaseDefinition> Cases { get; }
IReadOnlyList<ClueDefinition> Clues { get; }
IReadOnlyList<EnemyDefinition> Enemies { get; }
IReadOnlyList<BossDefinition> Bosses { get; }
IReadOnlyList<CaseRegistryEntry> Entries { get; }
IReadOnlyList<CaseRegistryEntry> GetEntries();
CaseDefinition GetCase(string caseId);
ClueDefinition GetClue(string clueId);
EnemyDefinition GetEnemy(string enemyId);
BossDefinition GetBoss(string bossId);
CaseState GetCaseState(CaseDefinition definition);
ChapterDefinition GetChapterForCase(CaseDefinition definition);
ChapterDefinition GetCurrentChapter();
bool CanStart(CaseDefinition definition, out string reason);
```

Missing/ambiguous lookups return null. Collections return copied arrays of
definition references. Entries are fresh save-derived views; refresh after
`SaveGameManager.DataChanged`. Each immutable `CaseRegistryEntry` exposes
`Definition`, `Chapter`, `State`, `CanStart`, and `StartBlockReason`.

## Progression

Chapters follow catalog order; their cases follow chapter array order. Valid
unassigned cases are appended in catalog order and remain visible/available,
with playability validated separately. There are no case-specific constants.
Core derives states from completion, immediate predecessors, and prerequisites.
Completed valid cases may replay.

Null/missing/ambiguous or repeated chapter references block affected content.
Cross-chapter membership also blocks starting. Broken relationships never
silently unlock cases as unassigned content. Invalid progression chapters are
excluded from current-chapter selection.

## Start Validation And Rewards

`CanStart` rejects unknown/foreign definitions, invalid membership, locked cases,
metadata-only cases, and incomplete data. Required content:

- Nonblank deduction question and answer.
- Nonempty clue array with no null elements; clue IDs are nonblank, unique, and
  resolve to the exact registered definitions.
- A registered boss with a prefab containing root `Damageable`.
- At least one applicable reward from `CaseDefinition.GetCompletionRewards()`.

Chinese reasons: `案件尚未解锁`, `案件仍在开发中`, `案件配置不完整`.

`GetCompletionRewards()` filters null/unsupported entries from the case pool;
only if none remain does it use the filtered boss pool. It recognizes the
stat/weapon effects implemented by RewardDefinition.Apply and nonblank unlocks.
It returns a new array. **GameFlow and RewardUI must consume this same result**,
not raw pools or their first three unfiltered slots.

## Session And Scenes

CaseSessionManager exposes `Initialize(CaseRegistry)`, `BeginCase(CaseDefinition)`,
`EndCase()`, `ActiveCaseId`, `ActiveCase`, and `HasActiveCase`. Begin validates
through the registry and rejects replacing an already active run.

SceneFlowManager exposes `Initialize(CaseSessionManager)`, `StartCase(CaseDefinition)`,
`LoadHub()`, `LastError`, and `IsLoading`. Start begins a session and loads the
configured investigation scene; Hub ends the session. Defaults are generic
`CombatSandbox` and `Hub`. Failed availability/load checks log the target, clear
session, restore timeScale to 1, and leave the current scene active.

## Errors And Verification

Missing catalog logs an error and initializes an empty registry. Saved IDs without
definitions remain in persistence for legacy display; views do not erase them.

MetaRuntimeTests covers order/prerequisites, unassigned cases, placeholders,
duplicate IDs in all categories, invalid starts, shared reward resolution,
ambiguous membership, scene failure, null-safe arrays, and catalog discovery.
Tests access predefined Unity assemblies through reflection, not illegal asmdef
references to Assembly-CSharp. Native execution requires licensed EditMode.
