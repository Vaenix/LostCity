using System;
using LostCity.CombatSandbox;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.EvidencePresentation
{
    public sealed class EvidenceThumbnailRow : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image thumbnail;
        [SerializeField] private Text titleText;
        [SerializeField] private Text categoryText;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectedSprite;
        private string clueId;
        private Action<string> selected;

        public void Bind(ClueDefinition clue, Sprite illustration, Action<string> onSelected)
        {
            Unsubscribe();
            clueId = clue != null ? clue.Id : null;
            selected = onSelected;
            if (titleText != null) titleText.text = clue != null ? clue.Name : string.Empty;
            if (categoryText != null) categoryText.text = clue != null ? clue.Category : string.Empty;
            if (thumbnail != null)
            {
                thumbnail.sprite = illustration;
                thumbnail.preserveAspect = true;
                thumbnail.enabled = illustration != null;
            }
            if (button != null) button.interactable = clue != null && !string.IsNullOrWhiteSpace(clue.Id);
            if (isActiveAndEnabled) Subscribe();
        }

        public void SetSelected(bool value)
        {
            if (background != null) background.sprite = value ? selectedSprite : normalSprite;
        }
        private void OnEnable() { Subscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }
        private void Subscribe()
        {
            if (button == null) return;
            button.onClick.RemoveListener(Choose);
            button.onClick.AddListener(Choose);
        }
        private void Unsubscribe() { if (button != null) button.onClick.RemoveListener(Choose); }
        private void Choose() { if (!string.IsNullOrWhiteSpace(clueId)) selected?.Invoke(clueId); }
    }
}
