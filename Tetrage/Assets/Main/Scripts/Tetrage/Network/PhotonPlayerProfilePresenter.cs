using Photon.Realtime;
using Tetrage.UI;
using UnityEngine;

namespace Tetrage.Network
{
    /// <summary>
    /// Photon プレイヤー情報を <see cref="ProfileDisplayView"/> に反映する Presenter。
    /// </summary>
    public class PhotonPlayerProfilePresenter : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private ProfileDisplayView profileDisplayView;

        #endregion

        #region Public Methods

        /// <summary>
        /// プレイヤー表示用オブジェクトに Presenter を後付けする。
        /// </summary>
        public static PhotonPlayerProfilePresenter Attach(GameObject target)
        {
            if (target == null)
            {
                Debug.LogWarning($"{nameof(PhotonPlayerProfilePresenter)}: target が null です。");
                return null;
            }

            if (target.GetComponent<ProfileDisplayView>() == null)
            {
                Debug.LogWarning(
                    $"{nameof(PhotonPlayerProfilePresenter)}: {nameof(ProfileDisplayView)} が見つかりません。",
                    target);
                return null;
            }

            return target.GetComponent<PhotonPlayerProfilePresenter>()
                   ?? target.AddComponent<PhotonPlayerProfilePresenter>();
        }

        /// <summary>
        /// Photon プレイヤーから表示を更新する。
        /// </summary>
        public void SetPlayer(Player player)
        {
            if (!Validate())
            {
                return;
            }

            if (player == null)
            {
                Debug.LogWarning($"{nameof(PhotonPlayerProfilePresenter)}: Player が null です。", this);
                return;
            }

            int iconIndex = player.CustomProperties.ContainsKey(TitlePhotonPropertyKeys.IconIndex)
                ? (int)player.CustomProperties[TitlePhotonPropertyKeys.IconIndex]
                : 0;

            profileDisplayView.SetProfile(iconIndex, player.NickName);
        }

        #endregion

        #region Private Methods

        private bool Validate()
        {
            if (profileDisplayView == null)
            {
                profileDisplayView = GetComponent<ProfileDisplayView>();
            }

            if (profileDisplayView == null)
            {
                Debug.LogWarning($"{nameof(PhotonPlayerProfilePresenter)}: {nameof(ProfileDisplayView)} が見つかりません。", this);
                return false;
            }

            return true;
        }

        #endregion
    }
}
