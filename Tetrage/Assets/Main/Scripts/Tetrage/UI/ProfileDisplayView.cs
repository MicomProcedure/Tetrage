using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tetrage.UI
{
    /// <summary>
    /// 名前とアイコンを表示する共通View。
    /// </summary>
    public class ProfileDisplayView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Sprite[] availableIcons;

        #endregion

        #region Public Methods

        /// <summary>
        /// 表示参照を外部から注入する。
        /// </summary>
        public void ConfigureBindings(Image icon, TextMeshProUGUI name, Sprite[] icons)
        {
            iconImage = icon;
            nameText = name;
            availableIcons = icons;
        }

        /// <summary>
        /// 表示を更新する。
        /// </summary>
        public void SetProfile(int iconIndex, string playerName)
        {
            UpdateIcon(iconIndex);
            UpdateName(playerName);
        }

        /// <summary>
        /// アイコンのみ更新する。
        /// </summary>
        public void SetIcon(int iconIndex)
        {
            UpdateIcon(iconIndex);
        }

        #endregion

        #region Private Methods

        private void UpdateIcon(int iconIndex)
        {
            if (iconImage == null)
            {
                Debug.LogWarning("ProfileDisplayView: iconImage が未設定です。");
                return;
            }

            if (availableIcons == null || availableIcons.Length == 0)
            {
                Debug.LogWarning("ProfileDisplayView: availableIcons が未設定です。");
                return;
            }

            if (iconIndex < 0 || iconIndex >= availableIcons.Length)
            {
                Debug.LogWarning($"ProfileDisplayView: IconIndex={iconIndex} が範囲外です (利用可能: 0-{availableIcons.Length - 1})");
                return;
            }

            iconImage.sprite = availableIcons[iconIndex];
        }

        private void UpdateName(string playerName)
        {
            if (nameText == null)
            {
                Debug.LogWarning("ProfileDisplayView: nameText が未設定です。");
                return;
            }

            nameText.text = playerName ?? "Unknown Player";
        }

        #endregion
    }
}
