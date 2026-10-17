using System.Collections.Generic;
using LostCity.CombatSandbox;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.Meta
{
    public sealed class HubController : MonoBehaviour
    {
        [SerializeField] private Text currentChapterText;
        [SerializeField] private Text statusText;
        [SerializeField] private Transform caseListRoot;
        [SerializeField] private HubCaseRow rowTemplate;
        [SerializeField] private Button archiveButton;
        [SerializeField] private Button bestiaryButton;
        [SerializeField] private Button casesButton;
        [SerializeField] private GameObject casesPanel;
        [SerializeField] private InvestigationArchiveUI archiveUI;
        [SerializeField] private BestiaryUI bestiaryUI;
        [SerializeField] private Button retrySaveButton;

        private readonly List<HubCaseRow> rows = new List<HubCaseRow>();
        private HubViewSupport view;
        private bool starting;

        private void Awake()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            HubViewSupport.ApplyChineseFont(canvas != null ? canvas.transform : transform);
        }

        private void OnEnable()
        {
            if (casesButton != null) casesButton.onClick.AddListener(ShowCases);
            if (archiveButton != null) archiveButton.onClick.AddListener(ShowArchive);
            if (bestiaryButton != null) bestiaryButton.onClick.AddListener(ShowBestiary);
            if (retrySaveButton != null) retrySaveButton.onClick.AddListener(RetrySave);
            view = new HubViewSupport(Refresh);
            starting = false;
            ShowCases();
            view.Tick();
        }

        private void Start() { Refresh(); }
        private void Update() { view?.Tick(); }
        private void OnDisable()
        {
            if (casesButton != null) casesButton.onClick.RemoveListener(ShowCases);
            if (archiveButton != null) archiveButton.onClick.RemoveListener(ShowArchive);
            if (bestiaryButton != null) bestiaryButton.onClick.RemoveListener(ShowBestiary);
            if (retrySaveButton != null) retrySaveButton.onClick.RemoveListener(RetrySave);
            view?.Dispose();
            view = null;
        }

        public void Refresh()
        {
            if (retrySaveButton != null)
            {
                retrySaveButton.gameObject.SetActive(view != null && view.HasStorageError);
                retrySaveButton.interactable = !starting;
            }
            ClearRows();
            if (view == null || !view.Ready)
            {
                SetStatus("大厅服务尚未就绪，暂时无法进入案件。");
                if (currentChapterText != null) currentChapterText.text = "章节进度：暂无章节";
                return;
            }
            CaseRegistry registry = view.Registry;
            ChapterDefinition current = registry.GetCurrentChapter();
            if (currentChapterText != null)
            {
                int completed = 0;
                CaseDefinition[] definitions = current != null ? current.Cases : new CaseDefinition[0];
                foreach (CaseDefinition definition in definitions)
                    if (definition != null && view.Save.IsCaseCompleted(definition.CaseId)) completed++;
                currentChapterText.text = current == null ? "章节进度：暂无章节"
                    : $"章节进度：{current.ChapterName}　已完成 {completed}/{definitions.Length}";
            }
            SetStatus(view.StorageStatus);
            if (rowTemplate == null || caseListRoot == null)
            {
                SetStatus("大厅界面配置不完整，暂时无法进入案件。");
                return;
            }
            rowTemplate.gameObject.SetActive(false);
            IReadOnlyList<CaseRegistryEntry> entries = registry.GetEntries();
            ChapterDefinition previousChapter = null;
            bool first = true;
            foreach (CaseRegistryEntry entry in entries)
            {
                if (entry == null || entry.Definition == null) continue;
                if (first || previousChapter != entry.Chapter)
                {
                    HubCaseRow heading = CreateRow();
                    heading.BindHeading(entry.Chapter != null ? entry.Chapter.ChapterName : "未归档案件");
                    heading.gameObject.SetActive(true);
                    previousChapter = entry.Chapter;
                    first = false;
                }
                HubCaseRow row = CreateRow();
                row.Bind(entry, StartCase);
                row.SetSaveAllowed(view.CanSave && !starting);
                row.gameObject.SetActive(true);
            }
            if (first && view.CanSave) SetStatus("暂无案件配置。");
        }

        private HubCaseRow CreateRow()
        {
            HubCaseRow row = Instantiate(rowTemplate, caseListRoot);
            rows.Add(row);
            return row;
        }

        private void ClearRows()
        {
            foreach (HubCaseRow row in rows)
            {
                if (row == null) continue;
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
            rows.Clear();
        }

        private void StartCase(CaseDefinition definition)
        {
            if (starting || view == null) return;
            view.Tick();
            if (!view.CanSave) { Refresh(); return; }
            if (!view.Registry.CanStart(definition, out string reason)) { SetStatus(reason); return; }
            // Check the actual write path before opening a run, not just the last known storage state.
            view.Save.SaveNow();
            view.Tick();
            if (!view.CanSave) { Refresh(); return; }
            GameRuntime runtime = GameRuntime.Instance;
            if (runtime == null || runtime.SceneFlow == null) { SetStatus("场景服务尚未就绪，暂时无法进入案件。"); return; }
            starting = true;
            foreach (HubCaseRow row in rows) row.SetSaveAllowed(false);
            SetStatus("正在进入案件……");
            if (!runtime.SceneFlow.StartCase(definition))
            {
                starting = false;
                Refresh();
                SetStatus("无法进入案件，请检查案件配置与场景设置。");
            }
        }

        public void ShowCases() { ShowPanel(0); }
        public void ShowArchive() { ShowPanel(1); }
        public void ShowBestiary() { ShowPanel(2); }

        public void RetrySave()
        {
            if (starting || view == null || !view.HasStorageError) return;
            if (view.Save.IsReadOnly) view.Save.Reload();
            else view.Save.SaveNow();
            Refresh();
        }

        private void ShowPanel(int panel)
        {
            if (casesPanel != null) casesPanel.SetActive(panel == 0);
            if (archiveUI != null) archiveUI.gameObject.SetActive(panel == 1);
            if (bestiaryUI != null) bestiaryUI.gameObject.SetActive(panel == 2);
            if (casesButton != null) casesButton.interactable = panel != 0;
            if (archiveButton != null) archiveButton.interactable = panel != 1;
            if (bestiaryButton != null) bestiaryButton.interactable = panel != 2;
        }

        private void SetStatus(string value) { if (statusText != null) statusText.text = value; }
    }
}
