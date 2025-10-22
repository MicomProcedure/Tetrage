using Photon.Pun;
using UnityEngine;
using Tetrage.Title;

namespace Tetrage.Network
{
    /// <summary>
    /// Photonにプレイヤープロファイルを設定する専用クラス
    /// </summary>
    public class ProfileSettingPUN : MonoBehaviourPunCallbacks
    {
        #region Serialized Fields
        
        [Header("Profile Management")]
        [SerializeField] private PlayerProfileManager profileManager;
        
        #endregion

        #region Private Fields
        
        // プロパティ設定済みフラグ
        private bool isPropertiesSet = false;
        
        #endregion

        #region Custom Properties Management
        
        /// <summary>
        /// プレイヤーのプロフィール情報をPhotonのCustomPropertiesに設定
        /// </summary>
        public void SetPlayerProperties()
        {
            // 既に設定済みの場合はスキップ
            if (isPropertiesSet && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("PlayerName"))
            {
                Debug.Log("プレイヤープロパティは既に設定済みです。スキップします。");
                return;
            }

            profileManager.LoadProfile();

            var data = profileManager.Data;
            if (data == null)
            {
                Debug.LogWarning("ProfileDataが存在しません");
                return;
            }

            // NickNameも設定
            PhotonNetwork.NickName = data.PlayerName;

            // CustomPropertiesに設定
            ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable
            {
                { "IconIndex", data.IconIndex },
                { "PlayerName", data.PlayerName }
            };

            PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
            isPropertiesSet = true;
            Debug.Log($"プレイヤープロパティを設定: IconIndex={data.IconIndex}, PlayerName={data.PlayerName}");
        }
        
        #endregion

        #region Photon Callbacks
        
        /// <summary>
        /// 部屋に参加したときに自動でプロパティを設定
        /// </summary>
        public override void OnJoinedRoom()
        {
            SetPlayerProperties();
        }

        /// <summary>
        /// 部屋を退出したときにフラグをリセット
        /// </summary>
        public override void OnLeftRoom()
        {
            isPropertiesSet = false;
            Debug.Log("部屋を退出したため、プロパティ設定フラグをリセットしました");
        }
        
        #endregion
    }
}