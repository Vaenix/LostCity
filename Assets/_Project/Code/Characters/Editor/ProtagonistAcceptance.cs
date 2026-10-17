using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LostCity.CombatSandbox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LostCity.Characters.EditorTools
{
    public static class ProtagonistAcceptance
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Characters/ProtagonistVisual.prefab";
        private const string ModelPath = "Assets/_Project/Art/Characters/Protagonist/Protagonist.fbx";
        private const string SamplePath = "Assets/_Project/Scenes/Room304Greybox.unity";
        private static readonly string[] Parts = { "Head", "Hair", "Jacket", "Shirt", "LeftBoot", "RightBoot",
            "LeftWristband", "LeftChestTab", "RightHipBag", "StrapFront", "StrapBack" };

        [MenuItem("Tools/Lost City/Validate Protagonist First Pass")]
        public static void Validate()
        {
            WithSavedSetup(() =>
            {
                Scene stage = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Require(prefab != null && AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) != null, "Missing protagonist prefab/model.");
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage);
                ValidateModel(instance);
                ValidateSample(EditorSceneManager.OpenScene(SamplePath, OpenSceneMode.Single), prefab);
            });
            Debug.Log("Protagonist acceptance passed: imported geometry, anatomical accessories, canonical axes, materials and saved sample references. No Play Mode.");
        }

        public static void RenderPreviews()
        {
            WithSavedSetup(() =>
            {
                Scene stage = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Require(prefab != null, "Create protagonist prefab first.");
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage);
                Require(model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0, "No skinned protagonist geometry.");
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.56f, .56f, .56f);
                RenderSettings.fog = false;
                CreateLight("Key", new Vector3(-.6f, -1f, -.8f), .9f);
                CreateLight("Fill", new Vector3(.5f, -.6f, .8f), .55f);
                Camera camera = new GameObject("PreviewCamera").AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 1.04f;
                camera.nearClipPlane = .02f;
                camera.farClipPlane = 20f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.77f, .79f, .8f);
                string output = Path.Combine(Path.GetTempPath(), "lost-city-protagonist-preview");
                Directory.CreateDirectory(output);
                const int width = 512;
                const int height = 896;
                Texture2D sheet = new Texture2D(width * 4, height, TextureFormat.RGBA32, false);
                try
                {
                    Vector3 center = new Vector3(0f, .9f, 0f);
                    Vector3[] positions = { center + Vector3.forward * 4f, center + Vector3.back * 4f,
                        center + Vector3.left * 4f, center + Vector3.right * 4f };
                    string[] labels = { "Front", "Back", "Left", "Right" };
                    for (int i = 0; i < positions.Length; i++)
                    {
                        camera.transform.position = positions[i];
                        camera.transform.LookAt(center, Vector3.up);
                        Texture2D frame = Render(camera, width, height);
                        try
                        {
                            File.WriteAllBytes(Path.Combine(output, "Protagonist-" + labels[i] + ".png"), frame.EncodeToPNG());
                            sheet.SetPixels(i * width, 0, width, height, frame.GetPixels());
                        }
                        finally { Object.DestroyImmediate(frame); }
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(output, "Protagonist-FourViews.png"), sheet.EncodeToPNG());
                }
                finally { Object.DestroyImmediate(sheet); }

                Bounds head = PartBounds(model, "Head");
                camera.orthographicSize = .23f;
                Vector3 faceCenter = head.center + Vector3.up * .015f;
                camera.transform.position = faceCenter + new Vector3(.28f, .04f, 1.2f);
                camera.transform.LookAt(faceCenter, Vector3.up);
                Texture2D closeup = Render(camera, 768, 768);
                try { File.WriteAllBytes(Path.Combine(output, "Protagonist-Head.png"), closeup.EncodeToPNG()); }
                finally { Object.DestroyImmediate(closeup); }
                Debug.Log("Imported FBX offline previews: " + output + ". FourViews order: Front, Back, anatomical Left, anatomical Right. Not Play Mode.");
            });
        }

        private static void ValidateModel(GameObject root)
        {
            Require(root.transform.position == Vector3.zero && root.transform.rotation == Quaternion.identity
                && root.transform.localScale == Vector3.one, "Canonical prefab root must have identity transform.");
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                Require(item.localScale.x > 0f && item.localScale.y > 0f && item.localScale.z > 0f, "Negative/zero scale: " + item.name);
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) == 0, "Missing script: " + item.name);
                foreach (Component component in item.GetComponents<Component>())
                    for (Type type = component.GetType(); type != null; type = type.BaseType)
                        Require(type.FullName != "UnityEngine.Collider" && type.FullName != "UnityEngine.Rigidbody"
                            && type.FullName != "UnityEngine.Collider2D" && type.FullName != "UnityEngine.Rigidbody2D",
                            "Visual prefab must not own physics: " + item.name);
            }
            SkinnedMeshRenderer[] filters = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(filters.Length >= 10, "Expected skinned volumetric character parts, not a rigid placeholder.");
            long triangles = 0;
            long vertices = 0;
            var materialNames = new HashSet<string>();
            int blendedVertices = 0;
            foreach (SkinnedMeshRenderer filter in filters)
            {
                Mesh mesh = filter.sharedMesh;
                Renderer renderer = filter;
                Require(mesh != null && renderer != null && renderer.enabled, "Missing model mesh/renderer.");
                Require(mesh.vertexCount >= 3 && mesh.normals.Length == mesh.vertexCount, "Missing mesh normals: " + filter.name);
                Require(mesh.vertices.All(Finite) && mesh.normals.All(Finite), "Nonfinite vertex/normal: " + filter.name);
                Require(mesh.triangles.Length > 0 && mesh.triangles.All(index => index >= 0 && index < mesh.vertexCount),
                    "Invalid mesh indices: " + filter.name);
                triangles += mesh.triangles.Length / 3;
                vertices += mesh.vertexCount;
                Require(mesh.boneWeights.Length == mesh.vertexCount && filter.bones.Length > 0
                    && filter.bones.All(bone => bone != null && bone.IsChildOf(root.transform)), "Missing skin/bones: " + filter.name);
                Require(mesh.bindposes.Length == filter.bones.Length, "Skin bindpose count mismatch: " + filter.name);
                foreach (BoneWeight weight in mesh.boneWeights)
                {
                    Require(Mathf.Abs(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3 - 1f) < .001f,
                        "Non-normalized skin weight: " + filter.name);
                    if (weight.weight1 > .01f) blendedVertices++;
                }
                Require(renderer.sharedMaterials.Length == mesh.subMeshCount, "Missing material slot: " + filter.name);
                foreach (Material material in renderer.sharedMaterials)
                {
                    Require(material != null && material.shader != null && material.shader.name == "Standard"
                        && AssetDatabase.Contains(material), "Missing persistent built-in material: " + filter.name);
                    materialNames.Add(material.name);
                }
            }
            Require(triangles >= 1500 && triangles <= 40000, "First-pass model outside expected low/mid-poly budget: " + triangles);
            Require(materialNames.Count >= 6, "Missing character color/material separation.");
            Bounds bounds = CombinedBounds(root);
            Require(bounds.size.y >= 1.65f && bounds.size.y <= 1.9f && Mathf.Abs(bounds.min.y) < .025f,
                "Imported scale or soles origin is wrong: " + bounds);
            Require(bounds.size.x >= .45f && bounds.size.x <= 1.15f && bounds.size.z >= .2f && bounds.size.z <= .8f,
                "Unexpected adult silhouette depth/width: " + bounds);
            foreach (string part in Parts)
            {
                Bounds partBounds = PartBounds(root, part);
                Require(partBounds.size.sqrMagnitude > .00001f, "Part has no geometry: " + part);
            }
            Require(blendedVertices >= 100, "Expected genuine blended skin at joints, not rigid whole-limb parenting.");
            foreach (string bone in ProtagonistVisualMotion.RequiredBones) Named(root, "Rig_" + bone);
            Require(Named(root, "Rig_LeftUpperArm").position.x < 0 && Named(root, "Rig_RightUpperArm").position.x > 0
                && Named(root, "Rig_LeftThigh").position.x < 0 && Named(root, "Rig_RightThigh").position.x > 0,
                "Anatomical bones are mirrored.");
            Require(PartBounds(root, "Nose").center.z > .04f, "Model must face canonical +Z.");
            Bounds band = PartBounds(root, "LeftWristband");
            Bounds tab = PartBounds(root, "LeftChestTab");
            Bounds bag = PartBounds(root, "RightHipBag");
            Require(band.center.x < -.15f && band.center.y > .7f && band.center.y < 1.2f, "Hospital band must be on anatomical left wrist.");
            Require(tab.center.x < -.015f && tab.center.z > .03f && tab.center.y > 1.1f, "Red tab must be on left FRONT chest.");
            Require(bag.center.x > .15f && bag.center.y > .65f && bag.center.y < 1.15f, "Bag must hang at anatomical right hip.");
            Require(PartBounds(root, "Hair").center.y > PartBounds(root, "Head").center.y, "Hair must cap the head.");
            ValidateStrap(root, "StrapFront", 1f);
            ValidateStrap(root, "StrapBack", -1f);
            Require(root.GetComponentsInChildren<ProtagonistVisualMotion>(true).Length == 1, "Missing/duplicate visual motion component.");
            foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
                Require(!animator.enabled && !animator.applyRootMotion, "Procedural rig must not compete with Animator/root motion.");
            Debug.Log("Protagonist mesh audit: " + filters.Length + " meshes, " + vertices + " vertices, " + triangles
                + " triangles, " + materialNames.Count + " materials, " + blendedVertices + " blended vertices; canonical bounds=" + bounds);
        }

        private static void ValidateStrap(GameObject root, string name, float frontSign)
        {
            Transform strap = Named(root, name);
            Vector3[] points = GeometryPoints(strap.gameObject).ToArray();
            Require(points.Length > 0, "Missing strap mesh.");
            float min = points.Min(point => point.y);
            float max = points.Max(point => point.y);
            Require(max - min > .35f, "Strap must span shoulder to hip.");
            Vector3[] top = points.Where(point => point.y > Mathf.Lerp(min, max, .8f)).ToArray();
            Vector3[] bottom = points.Where(point => point.y < Mathf.Lerp(min, max, .2f)).ToArray();
            Require(top.Average(point => point.x) < 0f && bottom.Average(point => point.x) > 0f,
                "Strap must run from anatomical left shoulder to right hip: " + name);
            Require(points.Average(point => point.z) * frontSign > .035f, "Strap is on wrong face: " + name);
        }

        private static void ValidateSample(Scene scene, GameObject prefab)
        {
            PlayerMotor player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerMotor>(true)).Single();
            Transform aim = player.transform.Find("AimRoot");
            Transform adapter = aim != null ? aim.Find("CharacterVisual") : null;
            Require(adapter != null && aim.Find("Visual_YUp") == null, "Sample still uses the old placeholder.");
            GameObject instance = adapter.GetComponentsInChildren<ProtagonistVisualMotion>(true).Single().gameObject;
            Require(PrefabUtility.GetCorrespondingObjectFromSource(instance) != null, "Model visual must remain a connected prefab instance.");
            GameObject nearest = PrefabUtility.GetNearestPrefabInstanceRoot(instance);
            Require(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(nearest) == PrefabPath, "Wrong character prefab.");
            Require(Vector3.Dot(adapter.TransformDirection(Vector3.up), Vector3.back) > .999f
                && Vector3.Dot(adapter.TransformDirection(Vector3.forward), aim.right) > .999f,
                "Character visual axes disagree with gameplay XY / PlayerAim.");
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            CircleCollider2D collider = player.GetComponent<CircleCollider2D>();
            Require(body != null && body.bodyType == RigidbodyType2D.Dynamic && body.gravityScale == 0f
                && body.constraints == RigidbodyConstraints2D.FreezeRotation && collider != null && !collider.isTrigger
                && Mathf.Abs(collider.radius - .35f) < .0001f, "Character integration changed physics footprint.");
            Require(player.transform.localScale == Vector3.one && Mathf.Abs(player.transform.position.z) < .0001f,
                "Character integration changed gameplay root scale/plane.");
            ProtagonistVisualMotion motion = instance.GetComponent<ProtagonistVisualMotion>();
            motion.InitializeRig();
            Require(motion.IsReady && player.GetComponent<ProtagonistCluePresentation>() != null, "Missing sample motion/collection bridge.");
            foreach (CluePickup pickup in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CluePickup>(true)))
            {
                var anchor = pickup.GetComponent<ProtagonistPickupAnchor>();
                Require(anchor != null && anchor.ContactPoint != null && anchor.VisualRoot != null, "Missing saved clue presentation anchor.");
                GameObject reachFixture = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                try
                {
                    Vector3 contact = anchor.ContactPoint.position;
                    Vector2 approach = new Vector2(contact.x + .30f, contact.y - .44f);
                    Physics2D.SyncTransforms();
                    Require(!Physics2D.OverlapCircleAll(approach, .35f).Any(hit => !hit.isTrigger
                        && !hit.transform.IsChildOf(player.transform)), "Authored pickup approach is blocked: " + pickup.name);
                    reachFixture.transform.SetPositionAndRotation(new Vector3(approach.x, approach.y, 0f),
                        Quaternion.Euler(-90f, 0f, 0f));
                    var reachMotion = reachFixture.GetComponent<ProtagonistVisualMotion>();
                    Require(reachMotion.TryPresentPickup(contact, pickup), "Authored tabletop contact cannot be reached: " + pickup.name);
                    reachMotion.SampleEditorPose(0f, 0f, 0f, pickupProgress: .5f, pickupTarget: contact);
                    Require(Vector3.Distance(reachMotion.VisualGripPoint, contact) < .02f,
                        "Left hand did not reach the actual clue contact: " + pickup.name);
                }
                finally { Object.DestroyImmediate(reachFixture); }
            }
            foreach (Transform item in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) == 0, "Missing saved sample script.");
            Debug.Log("Protagonist saved scene reference and visual-only XY integration passed.");
        }

        private static Transform Named(GameObject root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
            Require(matches.Length == 1, "Missing/ambiguous semantic node: " + name);
            return matches[0];
        }

        private static Bounds PartBounds(GameObject root, string name) => CombinedBounds(Named(root, name).gameObject);
        private static Bounds CombinedBounds(GameObject root)
        {
            Vector3[] points = GeometryPoints(root).ToArray();
            Require(points.Length > 0, "No geometry: " + root.name);
            Bounds bounds = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 point in points.Skip(1)) bounds.Encapsulate(point);
            return bounds;
        }

        public static IEnumerable<Vector3> GeometryPoints(GameObject root)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                foreach (Vector3 vertex in filter.sharedMesh.vertices) yield return filter.transform.TransformPoint(vertex);
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = new Mesh();
                try
                {
                    renderer.BakeMesh(mesh);
                    foreach (Vector3 vertex in mesh.vertices) yield return renderer.transform.TransformPoint(vertex);
                }
                finally { Object.DestroyImmediate(mesh); }
            }
        }

        private static void CreateLight(string name, Vector3 direction, float intensity)
        {
            Light light = new GameObject(name).AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.LookRotation(direction);
            light.intensity = intensity;
        }

        private static Texture2D Render(Camera camera, int width, int height)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.aspect = (float)width / height;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                result.Apply();
                Color32[] pixels = result.GetPixels32();
                Require(pixels.Select(color => (color.r << 16) | (color.g << 8) | color.b).Distinct().Count() > 100,
                    "Blank or incomplete character render.");
                Require(pixels.Count(color => color.r > 220 && color.b > 220 && color.g < 40) < pixels.Length / 1000,
                    "Missing-shader pixels in character render.");
                return result;
            }
            catch { Object.DestroyImmediate(result); throw; }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static bool Finite(Vector3 value) => !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z)
            && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
        private static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        private static void WithSavedSetup(Action action)
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Exit Play Mode before art validation.");
            for (int i = 0; i < SceneManager.sceneCount; i++) Require(!SceneManager.GetSceneAt(i).isDirty, "Save open scenes before art validation.");
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try { action(); }
            finally
            {
                if (setup.All(item => !string.IsNullOrEmpty(item.path)) && setup.Any(item => item.isLoaded && item.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
