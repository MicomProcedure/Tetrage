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
        
        private const string PlayerProfileSaveKey = "PlayerProfile";

        // プロパティ設定済みフラグ
        private bool isPropertiesSet = false;
        
        #endregion

        #region Custom Properties Management
        
        /// <summary>
        /// プレイヤーのプロフィール情報をPhotonのCustomPropertiesに設定
        /// </summary>
        public void SetPlayerProperties()
        {
            var data = ResolveLocalProfileData();
            ApplyPlayerProfileDataToPhoton(data);
            isPropertiesSet = true;
            Debug.Log($"プレイヤープロパティを設定: IconIndex={data.IconIndex}, PlayerName={data.PlayerName}");
        }

        /// <summary>
        /// ローカル保存直後など、部屋にいるときだけPhotonへプロフィールを再送する。
        /// </summary>
        public void PushProfileToPhotonIfInRoom()
        {
            if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            {
                return;
            }

            var data = ResolveLocalProfileData();
            ApplyPlayerProfileDataToPhoton(data);
            isPropertiesSet = true;
            Debug.Log($"[Push] プレイヤープロパティを更新: IconIndex={data.IconIndex}, PlayerName={data.PlayerName}");
        }

        /// <summary>
        /// <see cref="PlayerProfileManager.SaveProfile"/> などから呼ぶ。シーンに <see cref="ProfileSettingPUN"/> が無ければ何もしない。
        /// </summary>
        public static void TryPushProfileToPhotonIfInRoom()
        {
            var pun = FindFirstObjectByType<ProfileSettingPUN>();
            pun?.PushProfileToPhotonIfInRoom();
        }

        private static void ApplyPlayerProfileDataToPhoton(PlayerProfileData data)
        {
            PhotonNetwork.NickName = data.PlayerName;

            ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable
            {
                { "IconIndex", data.IconIndex },
                { "PlayerName", data.PlayerName }
            };

            PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
        }
        
        #endregion

        #region Private Helpers
        private PlayerProfileData ResolveLocalProfileData()
        {
            if (profileManager == null)
            {
                profileManager = FindFirstObjectByType<PlayerProfileManager>();
            }

            if (profileManager != null)
            {
                profileManager.LoadProfile();
                return profileManager.Data;
            }

            var fromPrefs = TryLoadProfileFromPlayerPrefs();
            if (fromPrefs != null)
            {
                return fromPrefs;
            }

            Debug.LogWarning("ProfileDataが存在しないためデフォルト値を使用します");
            return new PlayerProfileData();
        }

        private static PlayerProfileData TryLoadProfileFromPlayerPrefs()
        {
            if (!PlayerPrefs.HasKey(PlayerProfileSaveKey))
            {
                return null;
            }

            var json = PlayerPrefs.GetString(PlayerProfileSaveKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<PlayerProfileData>(json);
            }
            catch
            {
                return null;
            }
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