using System;
using LostCity.Meta.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.Meta
{
    // Each visible view owns its subscription; enabling a previously hidden view always refreshes it.
    internal sealed class HubViewSupport : IDisposable
    {
        private readonly Action changed;
        private string lastError;
        private bool readOnly;
        private bool unsaved;
        private bool first = true;
        private static Font chineseFont;
        private static bool fontWarningIssued;

        public SaveGameManager Save { get; private set; }
        public CaseRegistry Registry { get; private set; }
        public bool Ready => Save != null && Registry != null;
        public bool HasStorageError => Save != null && (Save.IsReadOnly || Save.HasUnsavedChanges
            || !string.IsNullOrEmpty(Save.LastError));
        public bool CanSave => Ready && !HasStorageError;
        public string StorageStatus => !Ready ? "大厅服务尚未就绪，暂时无法进入案件。"
            : Save.IsReadOnly ? "存档当前为只读，无法保存调查进度，暂时无法进入案件。"
            : !CanSave ? "存档保存异常，暂时无法进入案件。请检查存储空间与文件权限。"
            : string.Empty;

        public HubViewSupport(Action changed) { this.changed = changed; }

        public void Tick()
        {
            GameRuntime runtime = GameRuntime.Instance;
            SaveGameManager nextSave = runtime != null ? runtime.SaveGame : null;
            CaseRegistry nextRegistry = runtime != null ? runtime.CaseRegistry : null;
            bool rebind = Save != nextSave || Registry != nextRegistry;
            if (rebind)
            {
                Dispose();
                Save = nextSave;
                Registry = nextRegistry;
                if (Save != null) Save.DataChanged += OnDataChanged;
            }
            string error = Save != null ? Save.LastError : null;
            bool nextReadOnly = Save != null && Save.IsReadOnly;
            bool nextUnsaved = Save != null && Save.HasUnsavedChanges;
            bool refresh = first || rebind || error != lastError || nextReadOnly != readOnly || nextUnsaved != unsaved;
            first = false;
            lastError = error;
            readOnly = nextReadOnly;
            unsaved = nextUnsaved;
            if (refresh) changed();
        }

        private void OnDataChanged() { changed(); }

        public void Dispose()
        {
            if (Save != null) Save.DataChanged -= OnDataChanged;
        }

        public static void ApplyChineseFont(Transform root)
        {
            if (chineseFont == null)
            {
                string[] installed = Font.GetOSInstalledFontNames();
                string[] preferred = { "PingFang SC", "Microsoft YaHei", "Noto Sans CJK SC", "Source Han Sans SC", "SimHei" };
                foreach (string candidate in preferred)
                {
                    if (!Array.Exists(installed, name => string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase))) continue;
                    chineseFont = Font.CreateDynamicFontFromOSFont(candidate, 20);
                    if (chineseFont != null) break;
                }
                if (chineseFont == null)
                {
                    if (!fontWarningIssued)
                    {
                        Debug.LogWarning("未找到可用的系统中文字体，使用内置字体；部分中文字形可能无法显示。");
                        fontWarningIssued = true;
                    }
                    chineseFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }
            foreach (Text text in root.GetComponentsInChildren<Text>(true)) text.font = chineseFont;
        }
    }
}
