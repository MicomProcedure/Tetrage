using Photon.Pun;
using Tetrage.Managers;
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

            var data = ResolveLocalProfileData();

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

        #region Private Helpers
        private PlayerProfileData ResolveLocalProfileData()
        {
            var app = ApplicationManager.Instance;
            var localSessionPlayer = app?.PlayerSession?.LocalPlayer;
            if (localSessionPlayer != null)
            {
                return new PlayerProfileData
                {
                    PlayerName = localSessionPlayer.PlayerName,
                    IconIndex = localSessionPlayer.IconIndex,
                };
            }

            if (profileManager == null)
            {
                profileManager = FindFirstObjectByType<PlayerProfileManager>();
            }

            if (profileManager != null)
            {
                profileManager.LoadProfile();
                if (profileManager.Data != null)
                {
                    return profileManager.Data;
                }
            }

            Debug.LogWarning("ProfileDataが存在しないためデフォルト値を使用します");
            return new PlayerProfileData();
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