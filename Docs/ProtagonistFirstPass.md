# Protagonist 3D First Pass

## Authorized Brief

Approved on 2026-10-17 in the visual-design chat and explicitly sent to this
development task for implementation. Use the four-view sheet in
`Design/2026-10-17-character-v1/protagonist-four-views.png` as a shape/color reference,
not exact engineering measurements. Preserve the source kit and all local user edits.

Build a recognizable low-to-mid-poly adult character: short dark hair, shaped face,
light sage-gray hip-length jacket, dark inner shirt/trousers, brown ankle boots.
Anatomical LEFT wrist has one pale hospital band; LEFT chest has a faded red tab;
RIGHT hip has an ochre canvas bag with strap from LEFT shoulder to RIGHT hip on
front and back. No armor, long coat, weapon kit, new identity or story.

This is a first-pass procedural mesh/material model, not a production sculpt,
hand-painted texture pass, facial rig or final likeness. No reference-plane billboard
or reused capsule may be presented as a finished character.

## Deliverables And Ownership

- Blender source/generator: `Tools/Art/GenerateProtagonist.py` and
  `SourceArt/Characters/Protagonist/Protagonist.blend`.
- Unity model: `Assets/_Project/Art/Characters/Protagonist/Protagonist.fbx`;
  material palette and model metrics beside the model or in SourceArt.
- Independent visual prefab:
  `Assets/_Project/Prefabs/Characters/ProtagonistVisual.prefab`.
- Integration: replace only the known sample player's placeholder visual subtree
  in Room304Greybox; update its scoped generator so recreation retains the new model.
- Runtime motion and editor builder under `Code/Characters/Runtime` and `Editor`;
  no changes to old Player.prefab, Room.unity, player movement, input or saves.
- Parent owns independent native acceptance, offline four-view/sample render tools,
  documentation and Git. Model and Unity implementation have disjoint file ownership.

Blender 4.2.1 LTS is installed. Use its native FBX exporter and editable object meshes;
keep `.blend` outside Assets so opening Unity does not require Blender integration.
No package install or cloud modeling service. Do not commit Design wholesale.

## Coordinate And Hierarchy Contract

Canonical imported Unity model: meters, soles at Y=0, approximately 1.76 m total
height, +Y up, +Z forward, +X anatomical right. No negative scale or mirrored
accessories. Export conversion must be checked against actual imported landmarks,
not assumed from exporter option names.

Required named semantic parts include Hair, Head, Jacket, Shirt, LeftBoot,
RightBoot, LeftWristband, LeftChestTab, RightHipBag, StrapFront and StrapBack.
The approved motion follow-up replaces the initial whole-limb pivots with 25
`Rig_` bones and genuine blended skinning. Semantic part groups remain inspection
nodes, not animation controls. See [CharacterMotion](CharacterMotion.md) and the
[rig contract](../SourceArt/Characters/Protagonist/Rigging.md). The rig is Generic,
not a mapped Humanoid avatar. Source topology is 13,692 triangles after joint
subdivision; the earlier static 8,024-triangle export is superseded.

The sample remains XY gameplay at Z=0. PlayerAim points local +X in that plane.
Under AimRoot, CharacterVisual applies
`Quaternion.Euler(-90,0,0) * Quaternion.Euler(0,90,0)` to the canonical model:
model forward becomes +world X and model height becomes -world Z. Physics root,
collider radius, movement speed, clue triggers and all existing services stay intact.
Use a real connected prefab instance below that adapter. Never rotate or scale
the player's Rigidbody2D root to accommodate an art export.

## Minimal Motion Contract

The follow-up uses procedural bone posing, two-segment limb IK, measured Rigidbody2D
velocity, constrained head attention and a left-hand reach. Animator is disabled and
root motion is off. Physics, input, combat, save state and Time.timeScale remain owned
by existing systems. This is not an AnimationClip library, final gait, facial rig,
cloth simulation, or complete combat animation set.

## Acceptance Plan

1. Inspect the actual source and FBX: nonempty volumetric meshes, finite normals,
   reasonable triangle budget, named material slots, meter-scale bounds, positive
   transforms, expected anatomical accessory side and front/back placement.
2. Render imported Unity prefab from front/back/left/right with neutral lighting,
   plus fixed sample-camera views. Inspect actual exported geometry, not source art.
3. Save/reopen prefab/scene and verify model/material/bone references, 2D physics
   settings, original clue IDs and existing journal/input wiring.
4. Run affected compilation/EditMode tests plus existing regression suite; verify
   visual motion never changes physics root or stats/save state.
5. Compare protected user asset/source hashes. Commit/push only owned changes and docs.

No Computer Use, Play Mode or automated gameplay/UI interaction. The user checks
walking/facing, motion feel, clipping and clue/journal interaction in the Editor.
Native import, canonical axes, anatomical accessories, skin weights, saved scene
references and reachable clue contacts pass. Four-view and motion renders are
available in [the preview index](Previews/CharacterMotion/README.md). The updated
driver and original regressions are covered by the native suite recorded in
[PhaseReport](PhaseReport.md); [CharacterMotion](CharacterMotion.md) separates actual
automated evidence from manual gameplay and remaining geometry refinements.
