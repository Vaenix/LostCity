# Room 304 2.5D Graybox Sample

## Authorized Scope

The user approved this work in the UI/materials chat and explicitly delegated it to
this development task on 2026-10-17. Phase 6 integration is already pushed on master
at `9c237a7`; this follow-up stays in the original project on
`codex/room304-25d-graybox`. Preserve all unpublished authoring assets and Room files.

Build one hospital room and a short corridor using simple 3D geometry, with fixed
oblique overhead camera, traversable space, readable clue objects and illustrated
journal. Reuse the existing bracelet, visitor register and apology clue IDs/content.
No new case, story, boss, complete UI redesign, final art or physics-engine migration.
The sample is not a replacement for the full Hub/CombatSandbox investigation loop.

## Coordinate And Physics Contract

- One unit approximates one meter. Gameplay remains in world XY at Z=0.
- Rigidbody2D, Collider2D, player movement, trigger pickup and all existing combat
  calculations stay in this plane. Solid obstacles use BoxCollider2D footprints;
  floors do not block XY movement. No Rigidbody/Collider/Physics 3D APIs or packages.
- 3D visual height extends along negative world Z. Conventional Y-up mesh coordinates
  map to gameplay via a visual-only root rotation of (-90,0,0): `(x,height,depth)`
  becomes `(x,depth,-height)`. Never rotate the physics root or game scene globally.
- The orthographic camera is fixed, looking from negative Y/negative Z toward the
  room. Frame the room and corridor without following the player. Low front walls
  and open door gaps provide cutaway visibility without a new occlusion framework.
- PlayerAim must intersect ScreenPointToRay with the XY plane at the aim root's Z.
  It must also retain correct behavior for the existing untilted sandbox camera.
- Every clue has a grounded 2D trigger, separate raised 3D visual and the original
  ClueDefinition. Pickup is E through the existing CluePickup/InvestigationProgress
  chain; archive recording retains the current stable-ID/idempotency contract.

## Scene And Asset Contract

- New scene: `Assets/_Project/Scenes/Room304Greybox.unity`.
- New code root: `Assets/_Project/Code/Room304Sample`.
- New sample mesh/material assets: `Assets/_Project/Art/Room304Sample`.
- Own input assets under `Assets/_Project/Settings/Input/Room304Sample*`; do not bind
  new scene YAML to locally regenerated sub-asset IDs from the user's input file.
- Generator: `LostCity.Room304Sample.EditorTools.Room304SampleCreator.CreateSample`;
  menu `Tools/Lost City/Create Room 304 2.5D Sample`. Touch only owned sample assets;
  do not invoke CombatSandboxCreator or reset seed definitions, player prefabs or Hub.
- Build a minimal player from existing PlayerInputReader/PlayerMotor/PlayerAim and
  TeamMember plus Rigidbody2D/Collider2D. No weapons, damage, XP, upgrade selection or
  PlayerProgressPersistence are needed in this investigation-only sample.
- Include the existing case as InvestigationProgress's direct-scene fallback, three
  placed CluePickup objects, journal, prompt UI and a return-to-Hub button. Do not
  add GameFlowManager or a boss/deduction flow to this isolated spatial sample.
- Append the sample to Build Settings without changing Hub/CombatSandbox ordering.

## Shared Interfaces And Ownership

1. Scene generation owns `Room304Sample/Editor/Room304SampleCreator.cs`, geometry helpers
   in that Editor folder and their metadata. It builds world, camera, lighting,
   minimal player, colliders, clue visuals, Input Actions, EventSystem and Canvas.
   Its cross-module calls use the two interfaces below. Run native generation only
   after all modules compile, with no concurrent Editor operating on the project.
2. Player interaction owns PlayerAim's plane projection, its tests, and
   `LostCity.Room304Sample.Room304SampleController` under `Room304Sample/Runtime`.
   Public `ReturnToHub()` saves dirty discovery progress before calling
   GameRuntime.SceneFlow.LoadHub; errors display through serialized `statusText`.
   No combat, movement or save-schema redesign. It reviews trigger/footprint contracts.
3. Evidence presentation owns `Code/EvidencePresentation`, art under
   `Art/EvidencePresentation`, catalog under `Resources/EvidenceIllustrationCatalog.asset`,
   focused tests, and an optional presentation hook in EvidenceJournal (legacy text
   fallback unchanged). Public Editor factory:
   `LostCity.EvidencePresentation.EditorTools.EvidencePresentationBuilder.CreateJournal(Transform parent, InvestigationProgress progress)`
   returns EvidenceJournal with illustrated panel wired. EvidenceJournal.Toggle is
   public for a sample toolbar button; J continues to work. The factory imports art
   and builds its own child hierarchy below the supplied overlay Canvas, not a new
   EventSystem or a global UI redesign.
4. Editor acceptance owns the separate validation/render utility. Integration must
   synchronize architecture/event/API/onboarding/phase documentation and native evidence.

Use the existing stable-ID mapping for illustration lookup without editing/cloning
the user's three ClueDefinition assets. Keep scene geometry and UI illustration assets
separate. The sample journal shows only actually collected clues, with independent
image/title/body and selection state; the original persistent Hub archive remains intact.

## Material Import

Source: `Design/2026-10-17-production-kit-v1`. Copy original evidence PNGs unchanged;
they are UI illustrations, not meshes or UV maps. Import Single/Full Rect sprites,
sRGB/alpha transparency, no compression/mipmaps, Clamp/Bilinear, max size at least2048.
Use only necessary paper/button/card/icon PNGs and manifest borders for journal UI.
Keep generated textures byte-identical to source. Low nonzero alpha residue is known;
do not silently edit pixels. Do not import menu background as the playable level.

## Acceptance Plan

- Compile and run EditMode tests, including old top-down/oblique aim projection and
  existing Phase 6 regression cases. No Play Mode or automated game/UI interaction.
- Generate only the independent sample; save/reopen and validate required scripts,
  references, 2D physics settings, trigger sizes, stable IDs, bindings and art import.
- Use static editor geometry/Physics2D queries to check corridor/door clearance and
  approachable clue triggers. Inspect camera projection and an offline Editor camera
  render at representative aspect ratios; this is not gameplay verification.
- Confirm repeat pickup does not duplicate archive IDs via existing production APIs.
- Recheck Room and existing gameplay asset preservation against the integration baseline.
- User manually checks movement, wall blocking, visibility, aiming, E pickup, J/click
  journal selection, image/text pairing and return to Hub. Report all unverified behavior.

## Generated Hierarchy

```text
Room304Sample
  World
    Floors / Walls / WallFixtures
    Door_OpenLeaf / DoorJamb_West / DoorJamb_East
    HospitalBed / BedsideCabinet / WritingTable / VisitorRegisterStand
  MainCamera / Daylight
  InvestigationProgress
  Player (Rigidbody2D, CircleCollider2D, TeamMember, input, motor, aim)
    AimRoot / CharacterVisual / ProtagonistVisual (connected skinned prefab)
    ProtagonistCluePresentation (successful pickup visuals only)
  Clues (bracelet, register, note; original grounded trigger + raised visual)
    Visual_YUp / MotionContact (authored tabletop contact)
    ProtagonistPickupAnchor (presentation references)
  EventSystem (InputSystemUIInputModule)
  Room304Canvas (CanvasScaler, GraphicRaycaster, Room304SampleController)
    Toolbar / StatusText / GamePromptManager / InteractionPrompt
    IllustratedEvidenceJournal / Panel
      Header / CloseButton / ClueList / EmptyState / ClueDetails
```

Room floor XY is `[-5,5] x [0,8]`; corridor is `[-7,7] x [-3,0]`.
Player spawns at `(0,-1.6,0)`, radius `0.35`, speed `3.2`, dynamic zero-gravity,
frozen rotation, interpolated continuous 2D collision. Door opening is `2.2` wide.
Clue centers are bracelet `(-1.5,5.8)`, register `(-3.9,-1)`, note `(3,4.3)`;
each trigger radius is `1.1`. All physics roots remain at Z=0.
The camera is fixed orthographic, automatically fitted at generation to all mesh
bounds for at least 4:3. No camera-follow script or 3D physics component exists.

## Spatial Sample Baseline Verification

Executed in `/Users/vaema/Documents/Lost City`, Unity **2022.3.32f1**, 2026-10-17.
No Play Mode, Computer Use, game clicking or real player-save modification occurred.

| Gate | Actual result |
| --- | --- |
| Managed C# compile + pure core tests | Passed; 24 core tests, all runtime/editor/test sources compile |
| CreateSample | Exit 0; scene, meshes/materials, input actions/references, 11 PNG copies and illustration catalog saved |
| ValidateSample after reopening | Exit 0; no missing scripts/references; input/2D physics/UI wiring valid |
| Static route clearance | 1,149 reachable grid cells; all three triggers approachable at player width |
| Native EditMode suite | 131 passed, 0 failed, 0 skipped; original 89 + 21 interaction + 21 presentation tests |
| Stable-ID/archive behavior | Original clue set, duplicate collection, replay and temporary JSON reload pass; no case completion |
| Projection regression | Tilted and old top-down cameras pass; invalid/parallel/behind-plane rays rejected |
| Offline world camera render | 1280x720, 1024x768, 1680x720; all nonblank, zero shader-error magenta pixels; inspected room/door/player/clue visibility |
| External serialized references | 59 project .unity/.prefab/.asset files scanned; no missing external GUID/fileID |
| Original-resource protection | All 51 baseline file hashes unchanged, including Room, old scenes/prefabs/input/definitions and physics settings |
| Source/docs whitespace | Passed; native Unity YAML whitespace retained |

Logs: `/private/tmp/lost-city-room304-create.log`, `lost-city-room304-validate.log`,
`lost-city-room304-render.log`, `lost-city-room304-editmode-final.log` and matching
`lost-city-room304-editmode-final.xml`. Final test time: 04:23:23 UTC / 12:23:23 CST.
Offline render PNGs are in the OS temp folder `lost-city-room304-sample`.
These renders do not include the Overlay UI and are not gameplay screenshots.
Evidence logs are local diagnostics, not committed build assets.

The approved protagonist follow-up adds a skinned visual prefab and a collection
presentation bridge. It preserves all physics roots/footprints and IDs, and shifts
only three owned clue visuals toward their table edges for reachable contact.
The 131-test results above predate this follow-up; current motion evidence is recorded
separately in [CharacterMotion](CharacterMotion.md), not inferred from that baseline.
The combined native suite now passes 176/176; saved sample reference/route/framing
checks and all three offline camera renders were rerun with the skinned character.

The first suite was 130/131: one new fixture incorrectly required clue array order.
It now checks the original ID set without ordering. Count, duplicate, replay,
unrelated-clue rejection and persistence assertions remain; authored assets were not changed.

## Manual Acceptance Checklist

All items below are **pending user verification**, not claims of a completed play test.

- Open Room304Greybox directly, enter Play: room/corridor and Chinese toolbar visible.
- WASD/arrows move; walls, bed and tables block; door and all clue approaches are traversable.
- Mouse facing remains correct at 4:3, 16:9 and ultrawide; no firing is expected here.
- Approach each of the three items: Chinese prompt appears; E collects exactly once.
- J/toolbar opens journal; only collected clues appear; thumbnails select the matching
  image/title/body; scrolling and close work; opening again retains selection.
- Return to Hub: archive shows discoveries; no level/stat/case-completion reset or grant.
- Restart/re-enter: durable archive retains IDs while this run's pickups are fresh.
- Open the existing Hub/CombatSandbox and regress mouse aim, pistol/orb, old text journal
  and full case flow. The original Room scene remains an untouched authoring asset.

## Known Limits And Next Step

This is an isolated spatial sample, not another chapter or completed case scene.
No combat, boss, deduction, reward UI, inventory, occlusion framework, VFX or final
environment art was added. Journal does not pause movement; maintain separate trigger
ranges. CJK relies on installed fonts. Original image alpha residue is retained.
The next step is user acceptance of walking/pickup/journal and the Phase 6 full loop,
then a separate decision about integrating this layout into the shared case scene.
