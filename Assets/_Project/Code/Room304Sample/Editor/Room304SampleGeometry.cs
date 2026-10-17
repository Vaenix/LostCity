using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LostCity.Room304Sample.EditorTools
{
    // All mesh data is conventional Y-up. Only visual roots rotate into the XY game plane.
    internal sealed class Room304SampleGeometry
    {
        internal const string ArtRoot = "Assets/_Project/Art/Room304Sample";
        private readonly Mesh box;
        private readonly Mesh cylinder;
        private readonly Mesh bracelet;
        private readonly Mesh foldedPaper;

        internal Room304SampleGeometry()
        {
            EnsureFolder(ArtRoot + "/Meshes");
            EnsureFolder(ArtRoot + "/Materials");
            box = SaveMesh("Box", BoxMesh());
            cylinder = SaveMesh("Cylinder", CylinderMesh(24));
            bracelet = SaveMesh("BraceletBand", BraceletMesh(40));
            foldedPaper = SaveMesh("FoldedPaper", FoldedPaperMesh());
        }

        internal Material Material(string name, Color color, float metallic = 0f)
        {
            string path = ArtRoot + "/Materials/" + name + ".mat";
            Shader shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Built-in Standard shader is required.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", metallic > 0f ? 0.36f : 0.12f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        internal static Transform VisualRoot(Transform parent)
        {
            Transform root = new GameObject("Visual_YUp").transform;
            root.SetParent(parent, false);
            root.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            return root;
        }

        internal GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material material)
            => Part(name, parent, box, center, size, material);

        internal GameObject Cylinder(string name, Transform parent, Vector3 center, Vector3 size, Material material)
            => Part(name, parent, cylinder, center, size, material);

        internal GameObject Bracelet(Transform parent, Vector3 center, Material material)
            => Part("BraceletBand", parent, bracelet, center, Vector3.one, material);

        internal GameObject FoldedPaper(Transform parent, Vector3 center, Material material)
            => Part("FoldedApologyPaper", parent, foldedPaper, center, Vector3.one, material);

        private static GameObject Part(string name, Transform parent, Mesh mesh, Vector3 center, Vector3 size, Material material)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.transform.localPosition = center;
            part.transform.localScale = size;
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        private static Mesh SaveMesh(string name, Mesh generated)
        {
            string path = ArtRoot + "/Meshes/" + name + ".asset";
            generated.name = name;
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }
            // Preserve mesh GUIDs on regeneration, including references from the previous sample scene.
            EditorUtility.CopySerialized(generated, existing);
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            return existing;
        }

        private static Mesh BoxMesh()
        {
            var data = new MeshData();
            Vector3 a = new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3 b = new Vector3(0.5f, -0.5f, -0.5f);
            Vector3 c = new Vector3(0.5f, 0.5f, -0.5f);
            Vector3 d = new Vector3(-0.5f, 0.5f, -0.5f);
            Vector3 e = new Vector3(-0.5f, -0.5f, 0.5f);
            Vector3 f = new Vector3(0.5f, -0.5f, 0.5f);
            Vector3 g = new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 h = new Vector3(-0.5f, 0.5f, 0.5f);
            data.Quad(a, d, c, b);
            data.Quad(e, f, g, h);
            data.Quad(a, e, h, d);
            data.Quad(b, c, g, f);
            data.Quad(d, h, g, c);
            data.Quad(a, b, f, e);
            return data.Build();
        }

        private static Mesh CylinderMesh(int segments)
        {
            var data = new MeshData();
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float b = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 loA = new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f);
                Vector3 loB = new Vector3(Mathf.Cos(b) * 0.5f, -0.5f, Mathf.Sin(b) * 0.5f);
                Vector3 hiA = loA + Vector3.up;
                Vector3 hiB = loB + Vector3.up;
                data.Quad(loA, hiA, hiB, loB);
                data.Triangle(Vector3.up * 0.5f, hiB, hiA);
                data.Triangle(Vector3.down * 0.5f, loA, loB);
            }
            return data.Build();
        }

        private static Mesh BraceletMesh(int segments)
        {
            var data = new MeshData();
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float b = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 oa = RingPoint(a, 0.40f, 0.30f, 0f);
                Vector3 ob = RingPoint(b, 0.40f, 0.30f, 0f);
                Vector3 ia = RingPoint(a, 0.32f, 0.22f, 0f);
                Vector3 ib = RingPoint(b, 0.32f, 0.22f, 0f);
                Vector3 lift = Vector3.up * 0.14f;
                data.Quad(oa, oa + lift, ob + lift, ob);
                data.Quad(ib, ib + lift, ia + lift, ia);
                data.Quad(oa + lift, ia + lift, ib + lift, ob + lift);
                data.Quad(ob, ib, ia, oa);
            }
            return data.Build();
        }

        private static Vector3 RingPoint(float angle, float xRadius, float zRadius, float height)
            => new Vector3(Mathf.Cos(angle) * xRadius, height, Mathf.Sin(angle) * zRadius);

        private static Mesh FoldedPaperMesh()
        {
            var data = new MeshData();
            // Uneven ridges and a lifted corner make this a thick folded sheet, not a flat icon.
            float[,] height = { { 0.02f, 0.10f, 0.04f }, { 0.03f, 0.15f, 0.02f }, { 0.12f, 0.07f, 0.03f } };
            var points = new Vector3[3, 3];
            for (int z = 0; z < 3; z++)
                for (int x = 0; x < 3; x++)
                    points[x, z] = new Vector3((x - 1) * 0.46f, height[x, z], (z - 1) * 0.40f);
            Vector3 thickness = Vector3.down * 0.025f;
            for (int z = 0; z < 2; z++)
                for (int x = 0; x < 2; x++)
                {
                    Vector3 a = points[x, z];
                    Vector3 b = points[x, z + 1];
                    Vector3 c = points[x + 1, z + 1];
                    Vector3 d = points[x + 1, z];
                    data.Quad(a, b, c, d);
                    data.Quad(d + thickness, c + thickness, b + thickness, a + thickness);
                }
            for (int i = 0; i < 2; i++)
            {
                Edge(data, points[i, 0], points[i + 1, 0], thickness);
                Edge(data, points[i + 1, 2], points[i, 2], thickness);
                Edge(data, points[0, i + 1], points[0, i], thickness);
                Edge(data, points[2, i], points[2, i + 1], thickness);
            }
            return data.Build();
        }

        private static void Edge(MeshData data, Vector3 a, Vector3 b, Vector3 thickness)
            => data.Quad(a, b, b + thickness, a + thickness);

        private sealed class MeshData
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<int> indices = new List<int>();

            internal void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                indices.Add(start);
                indices.Add(start + 1);
                indices.Add(start + 2);
            }

            internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                Triangle(a, b, c);
                Triangle(a, c, d);
            }

            internal Mesh Build()
            {
                var mesh = new Mesh();
                mesh.SetVertices(vertices);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
