using System;
using R3;
using Tetrage.Network;
using UnityEngine;

namespace Tetrage.Title
{
    /// <summary>
    /// ローカルプレイヤープロファイルの読み込み・保存と変更通知を担当する。
    /// </summary>
    public class PlayerProfileManager : MonoBehaviour
    {
        #region Constants

        private const string SaveKey = "PlayerProfile";

        #endregion

        #region Private Fields

        private readonly Subject<PlayerProfileData> _profileChanged = new();    // プロファイルが変更されたときに通知する。

        #endregion

        #region Public Properties

        /// <summary>
        /// プロファイルデータ。
        /// </summary>
        public PlayerProfileData Data { get; private set; }  

        public Observable<PlayerProfileData> ProfileChanged => _profileChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            LoadProfile();
        }

        private void OnDestroy()
        {
            _profileChanged.Dispose();
        }

        #endregion

        #region Public Methods

        public void LoadProfile()
        {
            // Multi Playmode では各インスタンスに -name が渡される（Player1=メインEditor, Player2..n=仮想プレイヤー）
            if (TryGetMultiPlayModePlayerIndex(out var multiIndex))
            {
                Data = new PlayerProfileData
                {
                    PlayerName = $"Player{multiIndex}",
                    IconIndex = multiIndex
                };

                Debug.Log($"[MultiPlayMode] Override PlayerProfile: PlayerName={Data.PlayerName}, IconIndex={Data.IconIndex}");
                NotifyProfileChanged();
                return;
            }

            if (PlayerPrefs.HasKey(SaveKey))
            {
                string json = PlayerPrefs.GetString(SaveKey);
                Data = JsonUtility.FromJson<PlayerProfileData>(json);
                Debug.Log($"プロファイル読み込み成功: IconIndex={Data.IconIndex}, PlayerName={Data.PlayerName}");
            }
            else
            {
                Data = new PlayerProfileData();
                Debug.Log("プロファイルが存在しないため、初期値を使用: IconIndex=0, PlayerName=Player");
            }

            NotifyProfileChanged();
        }

        public void SaveProfile()
        {
            string json = JsonUtility.ToJson(Data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
            Debug.Log($"プロファイル保存成功: IconIndex={Data.IconIndex}, PlayerName={Data.PlayerName}");

            ProfileSettingPUN.TryPushProfileToPhotonIfInRoom();
            NotifyProfileChanged();
        }

        public void UpdateProfile(int iconIndex, string playerName)
        {
            Data.IconIndex = iconIndex;
            Data.PlayerName = playerName;
            SaveProfile();
        }

        #endregion

        #region Private Methods

        private void NotifyProfileChanged()
        {
            _profileChanged.OnNext(Data);
        }

        private static bool TryGetMultiPlayModePlayerIndex(out int index)
        {
            index = -1;

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "-name", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var name = args[i + 1];
                if (string.IsNullOrWhiteSpace(name))
                {
                    return false;
                }

                if (!name.StartsWith("Player", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var numberPart = name.Substring("Player".Length);
                if (!int.TryParse(numberPart, out var parsed))
                {
                    return false;
                }

                index = Mathf.Clamp(parsed, 1, 4);
                return true;
            }

            return false;
        }

        #endregion
    }
}
