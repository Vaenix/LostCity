using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LostCity.CombatSandbox
{
    public sealed class UpgradeSelectionController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Button fireRateButton;
        [SerializeField] private Text fireRateButtonText;
        [SerializeField] private Button damageButton;
        [SerializeField] private Text damageButtonText;
        [SerializeField] private Button droneButton;
        [SerializeField] private Text droneButtonText;
        [SerializeField] private bool pauseWhileChoosing = true;

        private CombatUpgradeStats activeStats;
        private Action choiceCompleted;
        private float previousTimeScale = 1f;

        public bool IsShowing => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            EnsureInputSystemUiModule();

            if (fireRateButton != null)
            {
                fireRateButton.onClick.AddListener(() => Choose(CombatUpgradeType.FireRate));
            }

            if (damageButton != null)
            {
                damageButton.onClick.AddListener(() => Choose(CombatUpgradeType.ProjectileDamage));
            }

            if (droneButton != null)
            {
                droneButton.onClick.AddListener(() => Choose(CombatUpgradeType.DroneProjectile));
            }

            Hide();
        }

        private static void EnsureInputSystemUiModule()
        {
            EventSystem eventSystem = EventSystem.current ?? FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }

            BaseInputModule[] inputModules = eventSystem.GetComponents<BaseInputModule>();
            for (int i = 0; i < inputModules.Length; i++)
            {
                if (inputModules[i] != inputModule)
                {
                    inputModules[i].enabled = false;
                }
            }
        }

        public void Show(CombatUpgradeStats stats, int level, Action onChoiceCompleted)
        {
            if (stats == null || panelRoot == null)
            {
                return;
            }

            activeStats = stats;
            choiceCompleted = onChoiceCompleted;

            if (titleText != null)
            {
                titleText.text = $"等级 {level}";
            }

            if (fireRateButtonText != null)
            {
                fireRateButtonText.text = "+20%攻击速度";
            }

            if (damageButtonText != null)
            {
                damageButtonText.text = "+20%攻击力";
            }

            if (droneButtonText != null)
            {
                droneButtonText.text = "+1 记忆浮游炮";
            }

            if (pauseWhileChoosing)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            panelRoot.SetActive(true);
        }

        private void Choose(CombatUpgradeType upgradeType)
        {
            if (activeStats == null)
            {
                return;
            }

            activeStats.ApplyUpgrade(upgradeType);
            Hide();
            choiceCompleted?.Invoke();
            choiceCompleted = null;
            activeStats = null;
        }

        private void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (pauseWhileChoosing && Time.timeScale == 0f)
            {
                Time.timeScale = previousTimeScale;
            }
        }
    }
}
