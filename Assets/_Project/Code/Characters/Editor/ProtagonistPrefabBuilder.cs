using System;
using System.IO;
using System.Linq;
using LostCity.CombatSandbox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LostCity.Characters.EditorTools
{
    public static class ProtagonistPrefabBuilder
    {
        public const string ModelPath = "Assets/_Project/Art/Characters/Protagonist/Protagonist.fbx";
        public const string PrefabPath = "Assets/_Project/Prefabs/Characters/ProtagonistVisual.prefab";
        private const string MaterialRoot = "Assets/_Project/Art/Characters/Protagonist/Materials";
        private const string PalettePath = "SourceArt/Characters/Protagonist/model-metrics.json";
        private const string SamplePath = "Assets/_Project/Scenes/Room304Greybox.unity";
        [Serializable] private sealed class Palette { public string colorSpace; public Entry[] materials; }
        [Serializable] private sealed class Entry { public string name; public float[] color; }

        [MenuItem("Tools/Lost City/Create Protagonist Visual")]
        public static void CreatePrefab() { BuildPrefab(); }

        public static GameObject BuildPrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before rebuilding character art.");
            if (!File.Exists(ModelPath)) throw new FileNotFoundException("Generate the Blender source/FBX first.", ModelPath);
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            string before = EditorJsonUtility.ToJson(importer);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.addCollider = false;
            importer.isReadable = true;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            if (before != EditorJsonUtility.ToJson(importer)) importer.SaveAndReimport();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new InvalidOperationException("FBX import failed.");
            EnsureFolder(MaterialRoot);
            EnsureFolder("Assets/_Project/Prefabs/Characters");
            Palette palette = File.Exists(PalettePath) ? JsonUtility.FromJson<Palette>(File.ReadAllText(PalettePath)) : null;
            if (palette != null && (palette.colorSpace != "srgb" || palette.materials == null))
                throw new InvalidOperationException("Character palette must contain sRGB material colors.");
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = new GameObject("ProtagonistVisual");
                SceneManager.MoveGameObjectToScene(root, preview);
                GameObject imported = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                imported.transform.SetParent(root.transform, false);
                foreach (Renderer renderer in imported.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(material => PersistentMaterial(material.name, palette)).ToArray();
                foreach (Animator animator in imported.GetComponentsInChildren<Animator>(true))
                {
                    animator.applyRootMotion = false;
                    animator.enabled = false;
                }
                foreach (SkinnedMeshRenderer renderer in imported.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.updateWhenOffscreen = true;
                    renderer.localBounds = new Bounds(new Vector3(0f, .9f, 0f), new Vector3(2.5f, 2.5f, 2.5f));
                }
                ProtagonistVisualMotion motion = root.AddComponent<ProtagonistVisualMotion>();
                motion.InitializeRig();
                if (!motion.IsReady) throw new InvalidOperationException("Complete Rig_ skeleton must be imported before creating the visual prefab.");
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success || prefab == null) throw new InvalidOperationException("Character prefab save failed.");
                Debug.Log("Protagonist visual prefab created: " + PrefabPath);
                return prefab;
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        public static GameObject AttachToAimRoot(Transform aimRoot)
        {
            if (aimRoot == null) throw new ArgumentNullException(nameof(aimRoot));
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = BuildPrefab();
            Transform legacy = aimRoot.Find("Visual_YUp");
            if (legacy != null)
            {
                string[] expected = { "PlayerBody", "PlayerHead", "FacingMarker", "LeftFoot", "RightFoot" };
                string[] actual = legacy.Cast<Transform>().Select(item => item.name).ToArray();
                if (actual.Length != expected.Length || expected.Except(actual).Any()
                    || legacy.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Refusing to replace a customized sample placeholder.");
            }
            Transform existing = aimRoot.Find("CharacterVisual");
            if (existing != null && (existing.childCount != 1
                || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(existing.GetChild(0).gameObject) != PrefabPath))
                throw new InvalidOperationException("Refusing to replace an unrecognized character visual.");
            if (legacy != null) Object.DestroyImmediate(legacy.gameObject);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var adapter = new GameObject("CharacterVisual");
            adapter.transform.SetParent(aimRoot, false);
            adapter.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, aimRoot.gameObject.scene);
            instance.transform.SetParent(adapter.transform, false);
            return instance;
        }

        [MenuItem("Tools/Lost City/Install Protagonist In Room 304")]
        public static void InstallIntoSample()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before installing character art.");
            BuildPrefab();
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(SamplePath, OpenSceneMode.Single);
                PlayerMotor player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerMotor>(true)).Single();
                Transform aimRoot = player.transform.Find("AimRoot");
                GameObject visual = AttachToAimRoot(aimRoot);
                WireSamplePresentation(scene, player, visual.GetComponent<ProtagonistVisualMotion>());
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Sample scene save failed.");
                EditorSceneManager.OpenScene(SamplePath, OpenSceneMode.Single);
                Debug.Log("Protagonist installed into Room304Greybox; character visual and owned pickup presentation only. No Play Mode.");
            }
            finally
            {
                if (setup.All(item => !string.IsNullOrEmpty(item.path)) && setup.Any(item => item.isLoaded && item.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        public static void WireSamplePresentation(Scene scene, PlayerMotor player, ProtagonistVisualMotion motion)
        {
            InvestigationProgress progress = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<InvestigationProgress>(true)).Single();
            CluePickup[] pickups = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CluePickup>(true)).ToArray();
            foreach (CluePickup pickup in pickups)
            {
                Transform visual = pickup.transform.Find("Visual_YUp");
                if (visual == null) throw new InvalidOperationException("Missing sample clue visual: " + pickup.name);
                Vector3 offset, point;
                switch (pickup.Definition.Id)
                {
                    case "room304_patient_bracelet": offset = new Vector3(0f, -.12f, 0f); point = new Vector3(0f, .97f, -.26f); break;
                    case "room304_visitor_log": offset = new Vector3(0f, -.07f, 0f); point = new Vector3(.03f, 1.02f, -.32f); break;
                    case "room304_apology_note": offset = new Vector3(0f, -.25f, 0f); point = new Vector3(-.15f, .93f, -.33f); break;
                    default: throw new InvalidOperationException("No authored pickup pose for " + pickup.Definition.Id);
                }
                visual.localPosition = offset;
                Transform contact = visual.Find("MotionContact");
                if (contact == null) { contact = new GameObject("MotionContact").transform; contact.SetParent(visual, false); }
                contact.localPosition = point;
                var anchor = pickup.GetComponent<ProtagonistPickupAnchor>() ?? pickup.gameObject.AddComponent<ProtagonistPickupAnchor>();
                var data = new SerializedObject(anchor);
                data.FindProperty("contactPoint").objectReferenceValue = contact;
                data.FindProperty("visualRoot").objectReferenceValue = visual;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var bridge = player.GetComponent<ProtagonistCluePresentation>() ?? player.gameObject.AddComponent<ProtagonistCluePresentation>();
            var serialized = new SerializedObject(bridge);
            serialized.FindProperty("motion").objectReferenceValue = motion;
            serialized.FindProperty("investigationProgress").objectReferenceValue = progress;
            SerializedProperty array = serialized.FindProperty("pickups");
            array.arraySize = pickups.Length;
            for (int i = 0; i < pickups.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = pickups[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material PersistentMaterial(string name, Palette palette)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidOperationException("Invalid model material name.");
            string path = MaterialRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Entry[] entries = palette?.materials.Where(item => item != null && item.name == name).ToArray();
            if (palette == null)
                return material != null ? material : throw new InvalidOperationException("Missing committed palette/material: " + name);
            if (entries.Length != 1 || entries[0].color == null || entries[0].color.Length != 4
                || entries[0].color.Any(value => float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f))
                throw new InvalidOperationException("Invalid or ambiguous character palette entry: " + name);
            Shader shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Built-in Standard shader is required.");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            float[] c = entries[0].color;
            material.shader = shader;
            material.color = new Color(c[0], c[1], c[2], c[3]);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", .12f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
