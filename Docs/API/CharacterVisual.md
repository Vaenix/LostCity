# Character Visual API

## Purpose And Boundaries

`LostCity.Characters` supplies optional visual-only protagonist motion and clue
presentation. It uses a real 25-bone skinned Generic rig with deterministic
programmatic two-bone IK, not authored AnimationClips or root motion. Gameplay
movement, 2D physics, aim rules, clue transactions, saves and case progression
remain with their existing owners. All configuration fields are private serialized
fields; there are no new collection or persistence events.

Runtime files are under `Assets/_Project/Code/Characters/Runtime/`. The persistent
prefab is `Assets/_Project/Prefabs/Characters/ProtagonistVisual.prefab`. Canonical
model axes are +X anatomical right, +Y up, +Z front, in meters, with soles at Y=0.
The existing sample's `CharacterVisual` adapter maps that space into the unchanged
XY 2D gameplay plane. See [Rigging](../../SourceArt/Characters/Protagonist/Rigging.md)
for exact bone heads, hierarchy, export settings and deformation limitations.

## ProtagonistVisualMotion

`[DefaultExecutionOrder(150), DisallowMultipleComponent]`. Initialization captures
rest positions and rotations of descendant `Rig_` transforms once. It requires
all 25 exact names; missing or duplicate names leave `IsReady` false. A complete
rig must exist before initialization; replacing the hierarchy later is unsupported.

### Serialized Configuration

| Private field | Default | Meaning |
| --- | --- | --- |
| `movementBody` | null | Existing parent Rigidbody2D; auto-resolved when unset; completed-step position changes drive gait |
| `cycleDistance` | 1.25 m | Full two-step distance; minimum .5, gait phase advances from actual speed |
| `blendSeconds` | .18 s | Starting/stopping blend; minimum .05 |
| `idleSeconds` | 3.2 s | Breathing loop; minimum .1 |
| `pickupSeconds` | 1.2 s | Entire left-hand table-height reach/contact/return; minimum .1 |
| `headYawLimit` | 25 degrees | Relative gaze yaw, inspector range 0..45 |
| `headPitchLimit` | 12 degrees | Relative gaze pitch, inspector range 0..25 |
| `armedPose` | false | Uses restrained weapon-ready hands and rejects empty-hand pickup presentation |

Pickup phase landmarks use the design's 1.2-second normalized timeline. Retain the
default duration when matching the published .15/.50/.65/.95-second landmarks.

### Public Methods

| Method | Contract |
| --- | --- |
| `InitializeRig()` | Idempotent one-time rest capture and required-bone validation; called by Awake/OnEnable |
| `Advance(Vector3 worldVelocity, float deltaTime)` | Deterministic visual step, normally called by LateUpdate; does not set Rigidbody velocity, move the gameplay root, change time scale or write progress |
| `SetAttentionTarget(Transform target)` | Set optional gaze target; null or inactive target returns gaze toward forward; pickup attention takes priority |
| `TryPresentPickup(Vector3 worldPoint, UnityEngine.Object targetLifetime = null)` | Returns true only when a new stationary, unarmed, reachable table-height left-hand presentation starts; false means skip visual, not failed collection |
| `CancelPickup()` | Immediately ends pickup ownership and clears lifetime; captures current pose for approximately .18-second recovery on subsequent Advance calls |
| `FootTrajectory(float phase, float distance, out float travel, out float lift, out float roll)` | Static normalized in-place gait trajectory; repeats phase into [0,1), 55% stance; output meters/degrees |

`Advance` ignores non-finite or non-positive time steps, preserving pose during
pause; positive steps are capped at .1 s. Invalid velocity becomes zero. Movement
above .12 m/s, a facing jump above 15 degrees, or a destroyed tracked lifetime
cancels pickup on the next positive step. Hiding a collected source is deliberately
not destruction and does not cancel the reach. OnDisable cancels, restores rest
bones and resets locomotion/look/recovery clocks.

Production movement observation uses `FixedUpdate`, not raw Rigidbody2D.velocity:
the current controller uses MovePosition, which can move while reporting zero
velocity. Each fixed tick samples the previous completed step's body.position
delta divided by fixedDeltaTime. LateUpdate consumes that cached planar velocity
after the existing PlayerAim update. There is one physics-step observation latency;
render frames without a new physics tick keep the last sample instead of flickering
to idle. Blocked movement contributes no displacement and therefore settles idle.
OnEnable/OnDisable reset the baseline; directly reparenting the visual rebinds to
its new parent body and resets the baseline, avoiding a cross-parent position jump.
No movement command, input value or visual-bone displacement is used as gait speed.

`TryPresentPickup` rejects incomplete rig, active pickup, armed pose, invalid point,
locomotion blend above .12, observed velocity squared above .01, or direct
Rigidbody2D velocity squared above .01 (retained as an additional guard). In canonical
local coordinates it accepts only Y=.72..1.14 and Z=.05...65, then checks geometric
reach against the unchanged arm lengths and bent-torso pose. It never stretches
an arm to make a distant interaction look successful.

### Public State

| Property / field | Meaning |
| --- | --- |
| `IsReady` | Read-only externally; complete unique required-bone set captured |
| `IsPickingUp` | True during visual pickup; false immediately on cancel, even while recovery continues |
| `PickupProgress` | Clamped normalized pickup phase, otherwise zero |
| `PickupContact` | Active pickup has reached .50/1.20 normalized phase; this is visual state, not permission to record a clue |
| `LeftHand` | `Rig_LeftHand` transform, or null if missing |
| `LocomotionBlend` | Current 0..1 moving-pose blend |
| `VisualGripPoint` | World contact position inferred from hand and visual pose basis; use instead of wrist origin for a proxy |
| `PickupWristOffset` | Static canonical `(0,.02,-.105)` wrist-to-contact offset |
| `RequiredBones` | Static readonly array of 25 unprefixed keys; do not mutate its elements |

Under `UNITY_EDITOR`, `SampleEditorPose(phase, amount, clock, bodyYaw=0, turn=0,
turnPhase=0, pickupProgress=-1, pickupTarget=default)` samples the same evaluator
without input or Play Mode and throws for an incomplete rig. It changes visual
pickup state; do not mix it with runtime collection. `RestoreEditorPose()` cancels
and restores bones after disposable previews.

## Clue Presentation And Anchors

`ProtagonistPickupAnchor` has private serialized `Transform contactPoint` and
`Transform visualRoot`, both null by default, exposed through read-only
`ContactPoint` and `VisualRoot`. It adds no input, event, collection or save logic.
Attach it to the same GameObject as the existing CluePickup. Contact is the authored
table-height world point; visual root contains the renderable source only.

`ProtagonistCluePresentation` is `[DefaultExecutionOrder(200),
DisallowMultipleComponent]`. Its private serialized fields are
`InvestigationProgress investigationProgress`, `ProtagonistVisualMotion motion`
and `CluePickup[] pickups`, all unset by default. It has no public methods,
properties or events. Configure references before enabling it. It does not search
for progress or pickups and changing references while subscribed is unsupported.

The dependent event is **InvestigationProgress.ClueCollected**, not an event owned
by these visual components. The existing collection flow remains:

1. CluePickup requests `InvestigationProgress.TryCollectClue`.
2. Progress validates membership/stable-ID duplication/case state, records the
   clue through the original recorder, then invokes ClueCollected synchronously.
3. The visual subscriber finds a matching active configured pickup and valid
   anchor. If motion accepts the point, it creates a temporary render-only proxy.
4. The existing pickup hides itself after successful collection. The visual copy
   remains independent; hiding is allowed, destroying the source cancels motion.

Null/missing anchor references, unreachable points, movement, armed pose or a busy
reach skip presentation without rolling back the already successful collection.
No retry queue or persistent hand-held-item gameplay is introduced. A second valid
clue during a reach is still recorded normally but cannot replace the active reach.

The proxy copies enabled active MeshFilter/MeshRenderer data, shares meshes and
materials, and never clones CluePickup, collider, physics or save components. It
stays at the original visual position through contact and follows `VisualGripPoint`
during return starting at .65/1.20 normalized phase; it retains original rotation.
LateUpdate removes it when motion is absent, disabled or no longer picking up.
OnDisable unsubscribes from progress, cancels the motion and removes the proxy.
Destroy is deferred in Play Mode and immediate in Edit Mode.

## Safe Usage And Pitfalls

- Keep the Animator disabled and root motion off when the procedural driver owns
  bones. Do not animate old semantic Head/Hair/Boot groups; drive `Rig_` bones.
- Use unit character scale, canonical joint origins and the supplied sample
  adapter; this is not a general retargeter or nonuniformly scaled IK solver.
- Let FixedUpdate observe the body and LateUpdate advance the pose once per frame;
  do not also call Advance manually while the component's LateUpdate is running.
- Collection/save decisions must never depend on PickupContact or animation
  success. Do not call TryCollectClue, DiscoverClue or SaveNow from visual phases.
- Keep source pickup objects alive but hidden after collection. Destroying a
  tracked source means cancellation, not successful persistent hand-held state.
- Author contact positions, not arbitrary collider centers. Ground-height pickup
  is unsupported and must not be made reachable by stretching or teleporting.
- Visual proxies currently support static mesh renderers only, not skinned clues,
  particle systems, SpriteRenderers, cloth or collider interaction.
- A scaled source visual must retain its world geometry when copied. Regression
  coverage includes this contract; do not accept duplicated root/child scale.
- No face rig, per-finger physics, cloth simulation, attack/death/dodge clips or
  changes to aiming/combat rules are implied by this API.

## Verification

`ProtagonistCluePresentationTests` uses the actual prefab in the existing active
scene, temporary clue/case assets, inactive runtime/prompt stubs and a unique
temporary save. It tracks and destroys only its own fixture/prefab roots, generated
proxy roots and temporary assets. Existing scene roots are excluded from proxy
counts and cleanup. It never creates, switches, saves or closes scenes, including
an unsaved untitled user scene. It preserves/restores the runtime singleton and
time scale, never calls GameRuntime.Awake and never initializes the real user save.
Tests exercise the
actual progress event, render-only copy, contact/return timing, duplicate/failed
collection, stationary/reach gates, missing anchors, concurrent collection,
movement/destruction/disable cleanup, unsubscribe and source-scale preservation.
Save mutation counts and disk contents ensure visual ticks do not re-record clues.
`ProtagonistMovementObservationTests` adds ten native cases using disposable
Editor preview scenes with non-default 2D physics. MovePosition, the fixed-step
observer, native simulation and the late-frame pose path are exercised together,
without injecting velocity into Advance or simulating the user's scene. Coverage
includes interpolation on/off, stopped/blocked movement, aim-only turning, pause,
render/fixed cadence, enable/reparent baselines and pickup gating/cancellation.

The main task executed the full native suite after the walking fix: **186/186 pass**,
including these ten cases, 18 collection-presentation and 27 skeletal-pose cases.
Managed compilation alone is not execution of native integration tests. Imported
skin/axes, saved references and actual rendered sequences were inspected in the
preceding character delivery; this correction changes no asset or pose evaluator.
See [CharacterMotion](../CharacterMotion.md) for manual checks and limitations.

## Usage Examples

The committed prefab and Room304Greybox already contain the normal wiring. For a
new supported visual instance, keep the rig at unit scale and parent its adapter
under the existing aim root; leave the physics root unchanged. Its normal
OnEnable discovers the existing parent Rigidbody2D automatically; no scene rebuild
or manual Rigidbody reference assignment is needed for this walking fix.

```csharp
// Optional gaze only; this does not change gameplay aim.
visualMotion.SetAttentionTarget(cluePresentationTransform);
// Presentation can be skipped; never use this result as collection success.
bool shown = visualMotion.TryPresentPickup(contact.position, cluePickup);
visualMotion.CancelPickup();
```

For authored clues, configure ProtagonistPickupAnchor with a visible table contact
and render-only visual subtree, then include the original pickup in the bridge's
serialized array. Configure progress and motion before enabling the bridge; it
automatically listens to the existing success event. Do not also call the example
TryPresentPickup in that path, since the bridge already requests it once.
