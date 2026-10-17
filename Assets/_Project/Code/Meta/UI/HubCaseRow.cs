using System;
using LostCity.CombatSandbox;
using LostCity.Meta.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.Meta
{
    public sealed class HubCaseRow : MonoBehaviour
    {
        [SerializeField] private Text caseNameText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text stateText;
        [SerializeField] private Button startButton;
        [SerializeField] private Text startButtonText;

        private CaseRegistryEntry entry;
        private Action<CaseDefinition> selected;
        private bool saveAllowed;

        public void Bind(CaseRegistryEntry value, Action<CaseDefinition> onSelected)
        {
            Unsubscribe();
            entry = value;
            selected = onSelected;
            saveAllowed = true;
            CaseDefinition definition = entry?.Definition;
            if (caseNameText != null) caseNameText.text = definition != null ? definition.CaseName : "案件配置不完整";
            if (descriptionText != null) descriptionText.text = definition != null ? definition.Description : string.Empty;
            string state = entry == null || entry.State == CaseState.Locked ? "未解锁"
                : entry.State == CaseState.Completed ? "已完成" : "已解锁";
            if (definition != null && !definition.IsPlayable) state += " · 开发中";
            else if (entry != null && !entry.CanStart && entry.State != CaseState.Locked)
                state += " · " + entry.StartBlockReason;
            if (stateText != null) stateText.text = state;
            if (startButtonText != null) startButtonText.text = definition != null && !definition.IsPlayable ? "开发中"
                : entry != null && entry.State == CaseState.Completed ? "重新调查" : "进入案件";
            if (startButton != null) startButton.gameObject.SetActive(true);
            SetSaveAllowed(true);
            if (isActiveAndEnabled) Subscribe();
        }

        public void SetSaveAllowed(bool allowed)
        {
            saveAllowed = allowed;
            if (startButton != null) startButton.interactable = allowed && entry != null && entry.CanStart
                && entry.State != CaseState.Locked && entry.Definition != null && entry.Definition.IsPlayable;
        }

        public void BindHeading(string heading)
        {
            Unsubscribe();
            entry = null;
            selected = null;
            if (caseNameText != null) caseNameText.text = heading;
            if (descriptionText != null) descriptionText.text = string.Empty;
            if (stateText != null) stateText.text = string.Empty;
            if (startButton != null) startButton.gameObject.SetActive(false);
            LayoutElement layout = GetComponent<LayoutElement>();
            if (layout != null) layout.preferredHeight = 42f;
        }

        private void OnEnable() { Subscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }
        private void Subscribe()
        {
            if (startButton == null) return;
            startButton.onClick.RemoveListener(OnSelected);
            startButton.onClick.AddListener(OnSelected);
        }
        private void Unsubscribe()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnSelected);
        }
        private void OnSelected()
        {
            if (saveAllowed && entry != null && entry.CanStart && entry.State != CaseState.Locked
                && entry.Definition != null && entry.Definition.IsPlayable) selected?.Invoke(entry.Definition);
        }
    }
}
