# Player Control And 2.5D Compatibility

## PlayerAim

Purpose: convert mouse screen position into a direction in the XY gameplay plane.
Responsibilities: resolve camera/yaw root, project onto the root's Z plane and turn
the yaw root around world Z at the configured angular speed. It does not move the
player, perform hit tests or change projectile physics.

Public fields: none. Serialized inputs remain `yawRoot`, `aimCamera` and
`turnSpeedDegrees`. Public property: `AimDirection` returns the yaw root's right
vector, falling back to the component transform. No events.

Public method:

```csharp
bool PlayerAim.TryGetAimPoint(Camera camera, Vector2 screenPosition,
    float planeZ, out Vector3 point);
```

It uses Camera.ScreenPointToRay and Unity's Plane.Raycast. Missing/destroyed camera,
near-parallel rays and a plane behind the ray return false with a zero output. Callers
must check the boolean, not treat zero as an aiming target. Existing straight-down
orthographic cameras retain their result; oblique cameras no longer use an incorrect
world-Z difference as camera-space depth. Movement and weapons remain unchanged.

Dependencies: Unity Camera/Ray/Plane and the existing Input System Mouse API.
Usage: PlayerAim.LateUpdate calls TryGetAimPoint at yawRoot.position.z, then computes
Atan2 in XY and MoveTowardsAngle. A 3D visual child must not rotate the physics root.
Do not replace this with Physics.Raycast: 3D rendering is not a physics migration.

### Character Motion Boundary

Room304Greybox alone has a connected `ProtagonistVisual` below the canonical-axis
adapter. FixedUpdate observes completed Rigidbody2D.position displacement and caches
planar velocity; LateUpdate poses bones after PlayerAim updates. This supports the
existing MovePosition controller even when its velocity property is zero, with one
physics-step observation lag. Visual body-turn lag and limited head attention remain.
It never rotates the gameplay root/AimRoot, changes speed, blocks input or enables
root motion. Moving cancels an
optional pickup presentation; collection success is already recorded independently.
The old combat Player prefab is not replaced. See [CharacterVisual](CharacterVisual.md).

## Room304SampleController

Purpose: safely leave the isolated spatial sample for the existing Hub.
Responsibilities: resolve GameRuntime services, retry dirty discovery saves, refuse
leaving on read-only/failed saves, request LoadHub and show Chinese diagnostics.

Public fields/properties/events: none. Serialized input: `statusText` (Unity UI Text).
Public method: `ReturnToHub()`; bind the sample return button to it in the Editor.
Dependencies: GameRuntime, SaveGameManager, SceneFlowManager, Unity UI. No new save
format or progression owner. It does not complete a case or grant rewards.

Pitfalls: the sample is entered directly from its scene, not by changing Hub's case
selection target. InvestigationProgress still honors an existing active session over
its fallback; do not load this sample additively into an active investigation. A failed
return keeps the error visible and can be retried. User tests actual scene loading.

## Movement And Pickup Contract

PlayerMotor still moves Rigidbody2D in XY with zero gravity and frozen Z rotation.
The sample uses one player collider and TeamMember(Player). CluePickup uses a grounded
2D trigger and E, then InvestigationProgress.TryCollectClue rejects unrelated or
duplicate stable IDs and MetaProgressionRecorder writes discovery IDs once. Do not
add visual-mesh colliders or overlapping pickup regions. Native tests of these APIs
do not prove actual input delivery or the feel of walking through the sample.
