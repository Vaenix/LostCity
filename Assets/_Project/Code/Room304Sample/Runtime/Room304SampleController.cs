using LostCity.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace LostCity.Room304Sample
{
    public sealed class Room304SampleController : MonoBehaviour
    {
        [SerializeField] private Text statusText;

        private void Awake()
        {
            HubViewSupport.ApplyChineseFont(transform);
        }

        public void ReturnToHub()
        {
            var runtime = GameRuntime.Instance;
            if (runtime == null || runtime.SaveGame == null || runtime.SceneFlow == null)
            {
                SetStatus("未找到存档或场景服务，请检查场景配置。");
                return;
            }

            var save = runtime.SaveGame;
            if (!save.IsReadOnly && save.HasUnsavedChanges) save.SaveNow();
            if (save.IsReadOnly || save.HasUnsavedChanges)
            {
                SetStatus(save.IsReadOnly
                    ? "存档只读，暂不能返回大厅。请修复存档后重试。"
                    : "线索进度保存失败，暂不能返回大厅。请修复存储问题后重试。");
                return;
            }

            SetStatus(string.Empty);
            runtime.SceneFlow.LoadHub();
            if (!string.IsNullOrEmpty(runtime.SceneFlow.LastError))
                SetStatus("返回大厅失败，请检查场景配置后重试。");
        }

        private void SetStatus(string value)
        {
            if (statusText != null) statusText.text = value;
        }
    }
}
