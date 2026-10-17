using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LostCity.Characters.EditorTools
{
    public static class ProtagonistMotionPreview
    {
        [MenuItem("Tools/Lost City/Render Protagonist Motion Previews")]
        public static void RenderSequences()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Editor sampling only.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save authored scenes first.");
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            var generated = new System.Collections.Generic.List<Object>();
            string output = Path.Combine(Path.GetTempPath(), "lost-city-motion-preview");
            try
            {
                Scene stage = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProtagonistPrefabBuilder.PrefabPath);
                if (prefab == null) throw new InvalidOperationException("Create skinned protagonist first.");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage);
                var motion = root.GetComponent<ProtagonistVisualMotion>();
                var snapshots = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(skin =>
                {
                    var mesh = new Mesh { name = skin.name + "_PosedSnapshot" };
                    generated.Add(mesh);
                    var go = new GameObject("OfflineSkinSnapshot", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(skin.transform, false);
                    go.GetComponent<MeshFilter>().sharedMesh = mesh;
                    go.GetComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                    skin.enabled = false;
                    return (skin, mesh);
                }).ToArray();
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.55f, .55f, .55f);
                RenderSettings.fog = false;
                Light key = new GameObject("PreviewKey").AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1f;
                key.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
                Light fill = new GameObject("PreviewFill").AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = .4f;
                fill.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
                Material floor = Material("PreviewFloor", new Color(.36f, .39f, .40f), generated);
                Material tableMaterial = Material("PreviewTable", new Color(.40f, .44f, .42f), generated);
                Material paperMaterial = Material("PreviewPaper", new Color(.86f, .86f, .81f), generated);
                Box("Ground", new Vector3(0f, -.025f, 0f), new Vector3(4f, .04f, 4f), floor, generated);
                GameObject table = Box("ContactSurface", new Vector3(-.3f, .95f, .55f), new Vector3(.55f, .04f, .42f), tableMaterial, generated);
                GameObject paper = Box("VisualOnlyPaper", new Vector3(-.3f, .976f, .43f), new Vector3(.18f, .008f, .16f), paperMaterial, generated);
                Vector3 contact = new Vector3(-.3f, .98f, .36f);
                Vector3 paperRest = paper.transform.position;
                Camera camera = new GameObject("MotionPreviewCamera").AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 1.04f;
                camera.nearClipPlane = .02f;
                camera.farClipPlane = 20f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.68f, .71f, .72f);
                camera.transform.position = new Vector3(2.8f, 1.9f, 4.8f);
                camera.transform.LookAt(new Vector3(0f, .87f, 0f), Vector3.up);
                Directory.CreateDirectory(output);
                foreach (string action in new[] { "Idle", "Walk", "Turn", "Pickup" })
                {
                    int frames = action == "Idle" ? 64 : action == "Pickup" ? 36 : 32;
                    Directory.CreateDirectory(Path.Combine(output, action));
                    table.SetActive(action == "Pickup");
                    paper.SetActive(action == "Pickup");
                    Texture2D sheet = new Texture2D(320 * 4, 480 * 2, TextureFormat.RGB24, false);
                    generated.Add(sheet);
                    var frameHashes = new System.Collections.Generic.HashSet<int>();
                    for (int frame = 0; frame < frames; frame++)
                    {
                        float p = frame / (float)frames;
                        if (action == "Idle") motion.SampleEditorPose(0f, 0f, p * 3.2f);
                        if (action == "Walk") motion.SampleEditorPose(p, 1f, 0f);
                        if (action == "Turn") motion.SampleEditorPose(0f, 0f, 0f, -90f * (1f - p), 1f - p, p);
                        if (action == "Pickup")
                        {
                            motion.SampleEditorPose(0f, 0f, 0f, pickupProgress: p, pickupTarget: contact);
                            paper.transform.position = paperRest + (p >= .65f / 1.2f ? motion.VisualGripPoint - contact : Vector3.zero);
                            paper.SetActive(p < .95f);
                        }
                        // Camera.Render does not advance the Editor's skinning player loop.
                        // Bake each actual posed skin rather than accidentally reusing a cached GPU pose.
                        foreach (var snapshot in snapshots) snapshot.skin.BakeMesh(snapshot.mesh);
                        Texture2D image = Render(camera, 320, 480);
                        try
                        {
                            int hash = 17;
                            foreach (Color32 pixel in image.GetPixels32()) unchecked { hash = hash * 31 + pixel.r + (pixel.g << 8) + (pixel.b << 16); }
                            frameHashes.Add(hash);
                            File.WriteAllBytes(Path.Combine(output, action, frame.ToString("D3") + ".png"), image.EncodeToPNG());
                            for (int sample = 0; sample < 8; sample++)
                                if (frame == sample * frames / 8) sheet.SetPixels(sample % 4 * 320, (1 - sample / 4) * 480, 320, 480, image.GetPixels());
                        }
                        finally { Object.DestroyImmediate(image); }
                    }
                    sheet.Apply();
                    if (frameHashes.Count < (action == "Idle" ? 3 : 8))
                        throw new InvalidOperationException("Motion render reused a static/cached pose: " + action);
                    File.WriteAllBytes(Path.Combine(output, action + "-Sequence.png"), sheet.EncodeToPNG());
                }
                motion.RestoreEditorPose();
                Debug.Log("Motion offline sequences rendered: " + output + ". Procedural skinning; no Play Mode or input automation.");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (Object item in generated) if (item != null) Object.DestroyImmediate(item);
                if (setup.All(item => !string.IsNullOrEmpty(item.path)) && setup.Any(item => item.isLoaded && item.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
        private static Material Material(string name, Color color, System.Collections.Generic.List<Object> generated)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Glossiness", .1f);
            generated.Add(material);
            return material;
        }
        private static GameObject Box(string name, Vector3 center, Vector3 size, Material material, System.Collections.Generic.List<Object> generated)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { new Vector3(-1,-1,-1), new Vector3(1,-1,-1), new Vector3(1,1,-1), new Vector3(-1,1,-1),
                new Vector3(-1,-1,1), new Vector3(1,-1,1), new Vector3(1,1,1), new Vector3(-1,1,1) }.Select(p => Vector3.Scale(p, size) * .5f).ToArray();
            mesh.triangles = new[] { 0,2,1,0,3,2,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,3,7,6,3,6,2,0,1,5,0,5,4 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); generated.Add(mesh);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.transform.position = center;
            return go;
        }
        private static Texture2D Render(Camera camera, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                Color32[] pixels = image.GetPixels32();
                if (pixels.Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count() < 100
                    || pixels.Count(p => p.r > 230 && p.b > 230 && p.g < 25) > pixels.Length / 1000)
                    throw new InvalidOperationException("Blank preview or missing shader.");
                return image;
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); }
        }
    }
}
