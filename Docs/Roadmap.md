# Roadmap

## Current Phase

**Phase 6, Meta Framework + independent Room 304 spatial sample.** Phase 6 automated
validation is complete; manual gameplay acceptance is pending. The authorized 2.5D
sample reuses supplied UI art and original clues, not final environment production.
No new bosses, chapters, weapons or story are in scope.

## Completed Systems

Phase 5 established movement, manual/automatic weapons, four enemy archetypes, The Warden, XP/upgrades, HUD/minimap, Room 304 investigation/deduction, data-driven rewards, and editor generation. These form the existing baseline, not evidence that new Phase 6 assets were generated.

Phase 6 includes core DTO/rules, catalog discovery, registry, atomic storage, session/scene services, Hub/archive/bestiary, player progression persistence, discovery recording, and automatic completion flow. Integrated into the original project with authored-layout preservation and passive legacy-scene compatibility. Native EditMode tests pass 89/89; migrated scenes and data contracts pass. See Phase6Integration.md.

Approved protagonist follow-up: editable 25-bone Generic rig, blended skin,
procedural gait/attention/turn/reach, scoped prefab integration and success-only
clue presentation. Latest native suite **176/176** passes, including 45 character
checks. Imported geometry, saved references, three reachable clue contacts and
actual offline motion rendering pass. See CharacterMotion.md and its preview index.

## In Progress

- Independent Room304Greybox manual acceptance: implementation, saved-scene/static
  validation, three offline renders and pre-character EditMode suite (131/131) pass.
  Actual walking, pickup, journal input and Hub return remain unplayed. See
  Room30425DSample.md for evidence and manual gates.
- User-owned normal gameplay flow, actual scene transitions, input and visual verification.
- Reproducing any reported runtime issues after automated acceptance.
- User-owned character feel acceptance: actual-speed stride, start/stop, mouse-facing
  turns, strafing/backpedaling, arm/bag clearance and E-triggered table reach. No
  automated Play Mode or gameplay input was performed.

## Blocked Systems

- New playable content: wait for the full Hub-to-case-to-Hub acceptance chain.
- Final narrative/art: wait for validated gameplay architecture.

## Technical Debt

- Large legacy sandbox generator; only the new Hub builder is separated.
- Prototype CJK font fallback, not a packaged localization/font pipeline.
- Simple deduction accepts configured clue sets, not natural-language reasoning.
- Single-writer local JSON; no cloud, profiles, or concurrent writers.
- Generated resources need refresh after serialized-field changes.
- Manual gameplay and balance verification remain necessary.
- Sample pickup range uses a single player overlap flag; keep triggers separate.
- Sample art retains source pixels, including small nonzero alpha residue.
- First-pass character has simplified finger groups, linear skinning and separate
  garment meshes; no corrective shapes, facial rig, clip retargeting or cloth simulation.
- Pocket/collar/strap attachment is coarse; 94 readable/offscreen-updated skin
  renderers prioritize inspection, not shipping performance. Gait assumes a flat plane.
- The independent sample is not yet the shared case scene; integrating its spatial
  layout with combat/deduction requires a separately scoped follow-up.

## Future Systems

After Phase 6 acceptance: stronger authoring validation, more investigation interactions, migration tests for actual save schema additions, then carefully scoped content. No Chapter 2 is authorized now.

## Recommended Next Phase

Stability, not content: test Room 304 normally and with shortcuts, confirm real quit/relaunch persistence and replay, and report input/lifecycle regressions. The license issue is resolved and native automation has run. Do not delete unrelated local assets such as `Assets/Room.unity`.
