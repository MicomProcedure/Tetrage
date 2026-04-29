using UnityEngine;

namespace Tetrage.UI
{
    /// <summary>
    /// インゲーム中に表示するローディングUIの表示制御を担当する。
    /// </summary>
    public class InGameLoadingUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Loading UI Root")]
        [SerializeField] private GameObject _loadingRoot;

        #endregion

        private void Awake(){

            this.gameObject.SetActive(true);

        }

        #region Public API

        /// <summary>
        /// ローディングUIを表示する。
        /// </summary>
        public void Show()
        {
            SetVisible(true);
        }

        /// <summary>
        /// ローディングUIを非表示にする。
        /// </summary>
        public void Hide()
        {
            SetVisible(false);
        }

        #endregion

        #region Private Methods

        private void SetVisible(bool visible)
        {
            if (_loadingRoot == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            _loadingRoot.SetActive(visible);
        }

        #endregion
    }
}
