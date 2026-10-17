using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LostCity.EvidencePresentation.EditorTools
{
    public static class EvidenceArtImporter
    {
        public const string ArtRoot = "Assets/_Project/Art/EvidencePresentation";
        public const string CatalogPath = "Assets/_Project/Resources/EvidenceIllustrationCatalog.asset";
        public const string SourceRoot = "Design/2026-10-17-production-kit-v1";
        public static readonly string[] UiAssetIds =
        {
            "panel-paper", "evidence-card-normal", "evidence-card-selected", "button-secondary-normal",
            "button-secondary-hover", "button-secondary-pressed", "button-secondary-disabled", "icon-close"
        };
        private static readonly string[] ClueIds = { "room304_patient_bracelet", "room304_visitor_log", "room304_apology_note" };
        private static readonly string[] EvidenceNames = { "patient-bracelet", "visitor-register", "apology-note" };

        [Serializable] private sealed class Manifest { public AssetInfo[] assets; }
        [Serializable] private sealed class AssetInfo { public string id; public string png; public float[] border; }

        public static EvidenceIllustrationCatalog ImportAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请退出运行模式后导入证据图片。");
            string project = Path.GetDirectoryName(Application.dataPath);
            string source = Path.Combine(project, SourceRoot);
            string manifestPath = Path.Combine(source, "ui/manifest.json");
            string manifestJson = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : null;
            EnsureFolder(ArtRoot + "/Evidence");
            EnsureFolder(ArtRoot + "/UI");
            var illustrations = new Sprite[EvidenceNames.Length];
            for (int i = 0; i < EvidenceNames.Length; i++)
                illustrations[i] = CopyAndImport(Path.Combine(source, "evidence", EvidenceNames[i] + ".png"),
                    ArtRoot + "/Evidence/" + EvidenceNames[i] + ".png", Vector4.zero);
            foreach (string id in UiAssetIds)
            {
                CopyAndImport(Path.Combine(source, "ui/png/" + id + ".png"), ArtRoot + "/UI/" + id + ".png",
                    ResolveBorder(id, manifestJson));
            }

            EnsureFolder("Assets/_Project/Resources");
            EvidenceIllustrationCatalog catalog = AssetDatabase.LoadAssetAtPath<EvidenceIllustrationCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<EvidenceIllustrationCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = ClueIds.Length;
            for (int i = 0; i < ClueIds.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("clueId").stringValue = ClueIds[i];
                entry.FindPropertyRelative("illustration").objectReferenceValue = illustrations[i];
            }
            if (serialized.ApplyModifiedPropertiesWithoutUndo()) EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        private static Sprite CopyAndImport(string source, string destination, Vector4 border)
        {
            string physicalPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), destination);
            byte[] bytes = File.Exists(source) ? File.ReadAllBytes(source) : null;
            if (bytes == null && !File.Exists(physicalPath))
                throw new FileNotFoundException("证据原图与已导入副本均不存在，无法再生：" + destination, source);
            bool copied = bytes != null && (!File.Exists(physicalPath) || !File.ReadAllBytes(physicalPath).SequenceEqual(bytes));
            if (copied) File.Copy(source, physicalPath, true);
            if (copied || AssetImporter.GetAtPath(destination) == null)
                AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(destination) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("无法导入证据图片：" + destination);
            string previous = EditorJsonUtility.ToJson(importer);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);
            TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
            platform.maxTextureSize = 2048;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            platform.crunchedCompression = false;
            platform.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(platform);
            if (previous != EditorJsonUtility.ToJson(importer)) importer.SaveAndReimport();
            if (bytes != null && !File.ReadAllBytes(physicalPath).SequenceEqual(bytes))
                throw new InvalidOperationException("证据图片与原图字节不一致：" + destination);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(destination);
            if (sprite == null) throw new InvalidOperationException("图片未生成单精灵资源：" + destination);
            return sprite;
        }

        private static Vector4 ResolveBorder(string id, string manifestJson)
        {
            if (!UiAssetIds.Contains(id)) throw new InvalidOperationException("非本档案使用的界面素材：" + id);
            if (manifestJson == null)
            {
                // Exact production-kit-v1 subset, in manifest order: left, bottom, right, top.
                // Enables fresh-clone regeneration from committed PNG copies without the private Design folder.
                float border = id == "icon-close" ? 0f : id.StartsWith("button-", StringComparison.Ordinal) ? 12f : 16f;
                return new Vector4(border, border, border, border);
            }
            Manifest manifest = JsonUtility.FromJson<Manifest>(manifestJson);
            if (manifest?.assets == null) throw new InvalidOperationException("证据界面素材清单无效。");
            AssetInfo entry = manifest.assets.Single(item => item != null && item.id == id);
            if (entry.png != "png/" + id + ".png" || entry.border == null || entry.border.Length != 4
                || entry.border.Any(value => value < 0f || float.IsNaN(value) || float.IsInfinity(value)))
                throw new InvalidOperationException("界面素材路径或边框无效：" + id);
            return new Vector4(entry.border[0], entry.border[1], entry.border[2], entry.border[3]);
        }

        internal static Sprite UiSprite(string id) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/UI/" + id + ".png")
            ?? throw new InvalidOperationException("未找到已导入界面精灵：" + id);
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
