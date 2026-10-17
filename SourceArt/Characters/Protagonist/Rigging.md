# Protagonist Minimal Skin Rig

## Regenerate

Run from the repository root with Blender 4.2.1 LTS:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python Tools/Art/GenerateProtagonist.py
```

On this machine Blender requires normal, approved execution outside the filesystem
sandbox because its Metal initialization crashes in the sandbox. No Unity process
is started by this generator. The generator saves the editable `.blend`, exports
the same `Protagonist.fbx`, then imports that FBX into a clean Blender scene for
roundtrip checks. It does not modify the existing FBX `.meta` or GUID
`1534a8d1ecd59417fac0f11daa94fa54`.

`Tools/Art/GenerateProtagonist.py` owns the original procedural shapes/materials.
`Tools/Art/ProtagonistRig.py` owns the skeleton, deterministic weights and checks.
Bevel/solidify geometry is baked before skinning in the editable blend; the Python
source retains all original procedural parameters. Regeneration overwrites the
generated source/export, so hand edits to the blend are not generator inputs.

## Coordinate Contract

- Canonical coordinates are meters: +X anatomical RIGHT, +Y up, +Z front.
- Blender authoring is `(canonical x, canonical z, canonical y)`: +X anatomical
  right, +Z up, +Y front. The inverse is also `(x,z,y)`.
- This basis has determinant -1. Mesh face winding is reversed when constructed;
  closed surfaces have normals recalculated. Eye slits explicitly face front.
  Axial rotation vectors in the Blender test helper include the reflection sign.
- FBX export uses `axis_forward='-Z'`, `axis_up='Y'`, unit scale 1, no negative
  object scales, no space-transform baking. Do not mirror the model at runtime.
- Blender roundtrip is not proof of Unity's native axis conversion. The main task
  must independently verify +Z front and anatomical sides after actual import.

Static rest bounds in canonical meters:
`(-0.412, 0, -0.132)` to `(0.412, 1.769819, 0.195)`.
Soles are at Y=0 within floating-point error, not centered vertically.

## Bone Contract

There is one `ProtagonistArmature` under `ProtagonistModel`, with exactly 25
deforming bones. Root has no direct vertex weights, but moves the whole skeleton.
All other bones have weighted vertices. No leaf bones, actions, clips, constraints,
IK controls, or authored animation are exported. The saved pose is the rest pose.

Bone heads below are canonical world-space positions. Parent names omit `Rig_`
only for compactness; actual bone names always include the prefix.

| Bone | Parent | Head (x,y,z) |
| --- | --- | --- |
| Rig_Root | none | (0,0,0) |
| Rig_Pelvis | Root | (0,.91,0) |
| Rig_Spine | Pelvis | (0,1.08,0) |
| Rig_Chest | Spine | (0,1.27,0) |
| Rig_Neck | Chest | (0,1.43,0) |
| Rig_Head | Neck | (0,1.52,0) |
| Rig_LeftUpperArm | Chest | (-.207,1.425,0) |
| Rig_LeftForearm | LeftUpperArm | (-.296,1.185,.011) |
| Rig_LeftHand | LeftForearm | (-.358,.915,.023) |
| Rig_LeftFingers | LeftHand | (-.374,.835,.03) |
| Rig_LeftThumb | LeftHand | (-.337,.875,.026) |
| Rig_LeftThigh | Pelvis | (-.10,.91,0) |
| Rig_LeftShin | LeftThigh | (-.117,.48,.018) |
| Rig_LeftFoot | LeftShin | (-.12,.12,-.009) |
| Rig_LeftToe | LeftFoot | (-.12,.045,.13) |
| Rig_RightUpperArm | Chest | (.207,1.425,0) |
| Rig_RightForearm | RightUpperArm | (.296,1.185,.011) |
| Rig_RightHand | RightForearm | (.358,.915,.023) |
| Rig_RightFingers | RightHand | (.374,.835,.03) |
| Rig_RightThumb | RightHand | (.337,.875,.026) |
| Rig_RightThigh | Pelvis | (.10,.91,0) |
| Rig_RightShin | RightThigh | (.117,.48,.018) |
| Rig_RightFoot | RightShin | (.12,.12,-.009) |
| Rig_RightToe | RightFoot | (.12,.045,.13) |
| Rig_Bag | Pelvis | (.267,.95,.005) |

No agreed joint origin was changed. Local rest axes/rolls are NOT a driver
contract: derive directions from world-space joint pairs/rest transforms. This is
a Generic procedural rig, not a mapped Unity Humanoid avatar. Disable Animator
and root motion when the procedural driver owns the pose.

The original whole-limb `*Pivot` and `MotionRoot` empties are removed. Semantic
group nodes `Head`, `Hair`, `LeftBoot`, `RightBoot`, `RightHipBag` are retained under
the armature, with identity transforms; they are inspection groups, NOT animation
controls. Named meshes including `Jacket`, `Shirt`, `LeftWristband`, `LeftChestTab`,
`StrapFront`, `StrapBack`, `Nose` remain intact. All 94 mesh parts have real vertex
groups and an Armature modifier, including facial, hair, boot and bag details.

## Skin And Topology

- Weights are deterministic smoothstep bands, normalized per vertex, with a hard
  maximum of four influences (actual generated maximum: three). Weights below
  .001 are pruned before normalization to match the existing import threshold.
- Elbow band: Y=1.12..1.25; knee: Y=.415...545. Sleeves and trouser legs remain
  single connected surfaces through each joint, with added longitudinal rings at
  no more than .025 m spacing. No separate rigid elbow/knee pieces are used.
- Garment shoulders blend chest/upper arm spatially around the armhole. The seat,
  upper trouser legs and jacket hem blend pelvis/thigh across Y=.83...97. Torso
  rings are spaced at most .035 m; neck/wrist rings at most .02 m.
- Neck blends chest/neck/head. Head, facial details and all hair follow Rig_Head.
  Palms/finger roots blend into the shared four-finger group; thumbs use independent
  thumb bones. Wrist skin blends forearm/hand.
- Anatomical LEFT wristband is rigidly skinned to Rig_LeftForearm; LEFT chest tag
  is rigidly skinned to Rig_Chest. The bag is on anatomical RIGHT and rigidly
  skinned to Rig_Bag. Strap front/back blend torso bones to Rig_Bag over the lower
  section, and the shoulder bridge follows chest. Six-degree bag sway is checked.
- Boots blend shin/foot at the ankle; their forefoot/sole blends foot/toe for roll.
- Linear blend skinning is used, not Blender dual-quaternion preserve-volume,
  to match the intended Unity SkinnedMeshRenderer behavior.

## Executed Checks

The final generator run completed on Blender 4.2.1 LTS. Assertions and metrics:

- 25 exact bone names, parents and head positions; no obsolete pivot nodes;
  every required semantic node survives source creation and FBX roundtrip.
- 94 skinned meshes; 7,051 weighted vertices, including 2,936 genuinely blended
  vertices; 13,692 evaluated triangles (previous static model: 8,024). The increase
  is joint/garment/strap subdivision, not a redesign.
- Every vertex has 1..4 positive valid-bone weights summing to 1; measured maximum
  is three influences and maximum sum error is 3.15e-8. Accessory side/bone
  ownership and connected elbow/knee surfaces are asserted.
- Eleven isolated deformation cases: elbow 90, knee 70, shoulder 45, hip 30,
  neck yaw 25, hand 25, fingers 30, thumb 20, toe 20, bag 6, spine 15 degrees.
  Each required mesh moves beyond a minimum threshold and every evaluated vertex
  matches independent linear blend evaluation within 1e-5 m. Actual worst error
  is below 2.8e-7 m. Reset returns exactly to the captured rest pose.
- Actual exported FBX imported in a clean Blender scene: one armature, 25 bones,
  94 skinned meshes, 7,051 vertices and 13,692 triangles; positive object scales;
  maximum joint-head error below 3.1e-7 m and bounds error below 1.5e-7 m.

`model-metrics.json` contains evaluated source bounds, material/part counts, full
bone heads/tails, per-mesh weight statistics and deformation checks.
`fbx-roundtrip-metrics.json` contains actual FBX roundtrip measurements.
`rig-rest-front.png`, `rig-rest-back.png`, `rig-bend-front.png`, `rig-bend-side.png`
are Blender workbench evidence. The combined bend image is a topology stress pose,
not a finished walk or pickup pose, and is never saved as animation/rest pose.

## Native Acceptance And Limitations

The model implementer ran the Blender checks above; the main task subsequently
ran native Unity Generic import, actual baked-skin bounds, anatomical axes/side,
saved-reference and procedural motion acceptance in the original project. Unity
reports 8,707 split vertices, 13,584 triangles, 94 skins and 3,048 blended vertices.
See [CharacterMotion](../../../Docs/CharacterMotion.md) for the integrated evidence,
actual motion previews and remaining manual gameplay checks.

Separate garment/accessory meshes deliberately remain separate overlapping
surfaces, not one watertight skin. Elbow/knee topology is continuous; seams at
garment boundaries still need in-engine inspection under the actual driver.
Linear skinning can lose volume at extreme angles and does not provide corrective
shapes. This minimal rig has no clavicle, per-finger chains, facial rig or cloth
simulation. A shared finger group plus thumb supports approximate relaxed/reach/
pinch shapes, not precise individual-finger contact. Foot grounding, anti-slip,
collision avoidance, pickup reach limits and interruptions remain driver work.
Isolated toe stress rotation can move the sole below ground without compensation;
runtime grounding must handle this. No motion-design source files were modified.
