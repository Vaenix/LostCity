# Evidence Presentation

## Purpose And Boundaries

Optional illustrated presentation for the existing EvidenceJournal, used only by
Room304Greybox. It does not own collection, deduction, inventory, save data or case
completion. Text and stable IDs remain in the original ClueDefinition assets.
No public mutable fields or new gameplay events are introduced.

## Runtime API

| Class | Responsibilities / public surface |
| --- | --- |
| EvidenceIllustrationCatalog | ScriptableObject ID-to-Sprite mapping; `Entries` returns a copy; `GetIllustration(string)` returns null for blank, missing or ambiguous IDs |
| EvidenceIllustrationCatalog.Entry | Read-only `ClueId` and `Illustration`; private serialized fields |
| IllustratedEvidenceJournal | `Refresh(IEnumerable<ClueDefinition>)` filters null/blank/duplicate IDs, retains a collected selection or chooses first; `SelectClue(string)` rejects unavailable IDs; `ApplyChineseFont()` applies runtime fallback; `VisibleClues`, `SelectedClue`, `SelectedClueId` expose state |
| EvidenceThumbnailRow | `Bind(ClueDefinition, Sprite, Action<string>)`, `SetSelected(bool)`; owns only its named button callback, not unrelated listeners |
| EvidenceJournal | Existing collection event subscriber; public `Toggle()` and `Hide()`; optional `illustratedView` redirects rendering, null retains original text behavior |

Dependencies: Unity UI, ClueDefinition, InvestigationProgress, existing CJK font
helper. Catalog has no scene references. View serialized references are `journal`,
`catalog`, `listRoot`, `rowTemplate`, `detailsRoot`, `emptyText`, `illustration`,
`illustrationPlaceholder`, `clueTitleText`, `categoryText`, `bodyText`, `closeButton`,
`bodyScroll`. Row references are `button`, `background`, `thumbnail`, `titleText`,
`categoryText`, `normalSprite`, `selectedSprite`.

Selection updates image/title/category/body together. Images preserve aspect;
missing illustrations show a Chinese placeholder. Long body text scrolls. The
inactive row template must stay under the Canvas font-fallback root. Disable and
rebind remove only owned listeners. Refresh occurs on collection/opening, not every frame.
`ApplyChineseFont()` may be used for disposable editor previews; never save its
dynamic runtime Font reference into authored scene assets.

## Editor API

`EvidenceArtImporter.ImportAssets()` copies the supplied kit when available, or
uses committed image copies when the untracked Design directory is absent. It
returns the persistent EvidenceIllustrationCatalog, preserving its GUID.
Public constants: `ArtRoot`, `CatalogPath`, `SourceRoot`; `UiAssetIds` names the
required UI subset. It does not modify ClueDefinition assets or source PNG pixels.
Single/FullRect sprites use sRGB/alpha, no mipmaps/compression, 2048 max size,
Clamp/Bilinear and the supplied nine-slice borders.

```csharp
EvidenceJournal journal =
    LostCity.EvidencePresentation.EditorTools.EvidencePresentationBuilder
        .CreateJournal(existingOverlayCanvas.transform, investigationProgress);
```

The factory requires an existing Overlay Canvas and progress component, adds a
GraphicRaycaster if needed, creates the hidden panel and wires references. It
does not create EventSystem or a second Canvas. Sample generator owns input setup.

## Usage And Pitfalls

At runtime pass only `InvestigationProgress.CollectedClues` to the view. Supplying
the complete case definition would reveal uncollected evidence. Never infer IDs
from filenames or translated labels. Catalog duplicates deliberately fail closed.
UI illustrations are not world models, UV textures or a playable background.
Do not add imported copies to GameContentCatalog: they belong to this separate
presentation catalog. Opening the panel does not pause input or consume E globally.
The old text journal remains compatible and is not silently replaced elsewhere.

## Verification

Tests cover catalog ambiguity, collection filtering, selection retention, image/text
pairing and legacy fallback. Saved scene checks cover persistent references and
import settings. Runtime clicking, scrolling and font appearance remain manual
acceptance; see [Room30425DSample](../Room30425DSample.md).
