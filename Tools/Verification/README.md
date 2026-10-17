# Phase 6 Verification

## Managed Checks Without Unity Activation

```sh
node Tools/Verification/verify-phase6.mjs "/path/to/an/imported/Lost City"
```

The optional argument supplies a read-only reference project whose `Library/Bee`
response files and package DLLs already exist. Sources always come from the
checkout containing this script. `LOST_CITY_UNITY_CONTENTS` can override the Unity
2022.3.32f1 installation path. Node.js, Unity's bundled Roslyn and Mono are used;
no packages are downloaded. Temporary outputs and NUnit XML are placed in the
OS temporary directory printed by the command.
Project assemblies named by local `.asmdef` files are excluded from cached
references and rebuilt from source, so an already-imported project cannot silently
substitute an old Core/Persistence DLL for the current code.

The script executes the production core NUnit tests with NUnit's assembly runner,
then compiles runtime, persistence, Editor and test sources. This is not a Unity
runtime emulator. Native `JsonUtility`, filesystem persistence tests using
MonoBehaviour, scene serialization, lifecycle and UI behavior still require Unity.

## Unity Checks

Use a licensed Unity Editor with the project closed in other Editor instances:

```sh
"/path/to/Unity" -batchmode -nographics -projectPath "/path/to/Lost City" \
  -runTests -testPlatform EditMode -testResults /tmp/lost-city-editmode.xml \
  -logFile /tmp/lost-city-editmode.log
```

Do not add `-quit` to a test run: the test runner exits after recording results.
Scene generation is a separate, destructive seed-rebuild command. Do not run it
to test an existing project with authored assets or uncommitted changes:

```sh
"/path/to/Unity" -batchmode -nographics -projectPath "/path/to/Lost City" \
  -executeMethod LostCity.CombatSandbox.EditorTools.CombatSandboxCreator.CreateCombatSandbox \
  -logFile /tmp/lost-city-generate.log -quit
```

After generation, run `Tools > Lost City > Validate Meta Framework` or use
`-executeMethod LostCity.CombatSandbox.EditorTools.Phase6EditorValidation.ValidateGeneratedAssets`.
It checks build order, scenes/catalog, missing scripts, camera, EventSystem input
bindings, player Move/Fire actions, raycasters, scrolling panels, weapon definitions,
projectile 2D physics and required component references. Interactive validation
requires a saved open scene; batch mode may start with no scene setup. It
does not enter Play Mode or click UI, and is not proof of the complete gameplay loop.

`Tools > Lost City > Validate Meta Contracts` runs session/unlock/replay/native JSON
reload checks against a unique temporary save and verifies one-clue and five-clue
layouts. Batch entry point:
`LostCity.CombatSandbox.EditorTools.Phase6EditorValidation.ValidateMetaContracts`.
The temporary objects/files are removed in `finally`; the player's save is untouched.

For a single batch command that generates, reopens and validates both scenes, then
runs the temporary-save contracts, use
`-executeMethod LostCity.CombatSandbox.EditorTools.Phase6EditorValidation.GenerateAndValidate`
with `-quit`. It never enters Play Mode. A zero exit code plus both validation
success messages is required; merely finding `.unity` files is not sufficient.

Unity-generated YAML retains Unity's own whitespace formatting. Run the source
whitespace check separately with
`git diff --check -- '*.cs' '*.md' '*.mjs' '*.asmdef'` rather than rewriting
serialized assets only to remove native trailing spaces.

## Original Project Integration

The original project is `/Users/vaema/Documents/Lost City`; no temporary worktree
is required for gameplay testing. Use the included Hub scene and validation menus.
For explicitly migrating an older retained sandbox, the additive batch entry point is
`LostCity.CombatSandbox.EditorTools.Phase6ProjectMigration.MigrateExistingProject`.
Use `-executeMethod` with `-quit`, not `-runTests`; back up authoring work first.
It is already applied to this local project and is not required before each test.
The integration backup, local asset ownership and actual checks are documented in
[Phase6Integration](../../Docs/Phase6Integration.md). `.meta` files are also Unity
YAML and can contain native trailing spaces; preserve their GUIDs rather than
rewriting them to satisfy the source whitespace check.

The user owns Play Mode interaction and gameplay feel validation. Do not use
desktop automation to click upgrades or play the game.

## Independent Room 304 2.5D Sample

Use the same licensed Unity command and original project path, with one of these
`-executeMethod` values and `-quit` (not concurrently with another Editor):

```text
LostCity.Room304Sample.EditorTools.Room304SampleCreator.CreateSample
LostCity.Room304Sample.EditorTools.Room304SampleValidation.ValidateSample
LostCity.Room304Sample.EditorTools.Room304SampleValidation.RenderSample
```

Creation writes only the owned Room304Greybox scene, art and input assets, then
appends Build Settings. It does not run the CombatSandbox generator. It requires
saved open scenes; protect manual changes to the sample before regenerating it.

Validation reopens the sample and checks references, matching UI actions, minimal
player, original clue IDs, 2D footprints, static player-width routes to all three
clues, imported image settings and 4:3/16:9/21:9 framing. It does not drive input.
RenderSample needs a graphics device: omit `-nographics`, optionally use
`-force-metal` on macOS. It calls Camera.Render in Edit Mode and writes images to
the OS temp folder `lost-city-room304-sample`; it checks nonblank pixels and missing
shader magenta. Overlay UI is not included in those world renders.

The regular EditMode suite includes Room304SampleInteractionTests and
EvidencePresentationTests. Tests use temporary save data, not the real player save.
Static clearance and camera renders do not replace user walking, pointer input,
scrolling, font, save/relaunch or return-to-Hub Play Mode checks.

## Protagonist Skin And Motion

Regenerate source/FBX with Blender 4.2.1 using
`--background --factory-startup --python Tools/Art/GenerateProtagonist.py`.
This also performs source weight/deformation and FBX roundtrip assertions. Preserve
the FBX `.meta`; changing Blender source does not automatically install the prefab.

With the original project saved and closed in other Unity instances, use these
existing `-executeMethod` entry points and `-quit`:

```text
LostCity.Characters.EditorTools.ProtagonistPrefabBuilder.InstallIntoSample
LostCity.Characters.EditorTools.ProtagonistAcceptance.Validate
LostCity.Characters.EditorTools.ProtagonistAcceptance.RenderPreviews
LostCity.Characters.EditorTools.ProtagonistMotionPreview.RenderSequences
```

Install updates the recognized character visual subtree and authored clue display
anchors/bridge only. It never invokes full sandbox/sample regeneration. Validation
bakes actual skin geometry, checks bone/weight/axis/material contracts and reopens
the sample. Renderer bounding boxes alone are not mesh-shape evidence.

Rendering requires a graphics device: omit `-nographics`, use `-force-metal` here.
Static previews go to OS temp `lost-city-protagonist-preview`; sequence frames and
eight-panel sheets go to `lost-city-motion-preview`. These are EditMode Camera.Render
samples of the real procedural bone driver, not gameplay recordings or AI concept art.
Regular native EditMode tests include skin, gait, pause, reach and interruption checks.
No test should load or mutate the real player's save.

Implementation notes: Unity's [BakeMesh](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html)
produces a posed mesh snapshot; [applyRootMotion](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Animator-applyRootMotion.html)
must remain off. Runtime movement remains the existing Rigidbody2D controller.

## Optional Local Code-Graph Hook

On this machine the Git `code-review-graph` hook reports
`ModuleNotFoundError: No module named 'tree_sitter_c_sharp'`. The commit succeeds,
but its automatic graph refresh does not. This is a separate local Python toolchain
issue, not a Unity compile/test failure. The character acceptance used actual source,
native tests and imported assets, not a claimed updated graph. Do not silently
disable hooks or install dependencies as part of gameplay work; repair the optional
tool separately before relying on its index.
