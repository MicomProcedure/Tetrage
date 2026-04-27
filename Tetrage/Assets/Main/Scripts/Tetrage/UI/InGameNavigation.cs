using TMPro;
using UnityEngine;

namespace Tetrage.UI
{
    /// <summary>
    /// インゲーム用ナビゲーション表示。TextMeshPro に文言を出し分ける。
    /// </summary>
    public class InGameNavigation : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private TextMeshProUGUI _navigationText;

        #endregion

        #region Public API

        /// <summary>
        /// ナビ用テキストを代入する。参照が null のときは何もしない。
        /// </summary>
        public void SetNavigationText(string text)
        {
            if (_navigationText == null)
            {
                return;
            }

            _navigationText.text = text ?? string.Empty;
        }

        /// <summary>
        /// 現在表示中の文章を取得する。参照が null のときは null。
        /// </summary>
        public string GetNavigationText()
        {
            return _navigationText != null ? _navigationText.text : null;
        }

        #endregion
    }
}
