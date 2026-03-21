using System;
using Tetrage.Network;
using UnityEngine;

namespace Tetrage.Title
{
    public class PlayerProfileManager : MonoBehaviour
{
    private const string SaveKey = "PlayerProfile";
    public PlayerProfileData Data { get; private set; }

    void Awake()
    {
        LoadProfile();
    }

    public void LoadProfile()
    {
        // Multi Playmode では各インスタンスに -name が渡される（Player1=メインEditor, Player2..n=仮想プレイヤー）
        // これを使って、ローカルプロファイル（Title表示）とPhoton用プロファイル（WaitingRoom表示）を
        // インスタンスごとに分岐させる。
        if (TryGetMultiPlayModePlayerIndex(out var multiIndex))
        {
            Data = new PlayerProfileData
            {
                // 要件: Player1..4 / iconIndex 1..4
                PlayerName = $"Player{multiIndex}",
                IconIndex = multiIndex
            };

            Debug.Log($"[MultiPlayMode] Override PlayerProfile: PlayerName={Data.PlayerName}, IconIndex={Data.IconIndex}");
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
    }

        private static bool TryGetMultiPlayModePlayerIndex(out int index)
        {
            index = -1;

            // 例: [..., "-name", "Player3", ...]
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

                // 要件では 1..4 を想定（それ以外はクランプ）
                index = Mathf.Clamp(parsed, 1, 4);
                return true;
            }

            return false;
        }

    public void SaveProfile()
    {
        string json = JsonUtility.ToJson(Data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
        Debug.Log($"プロファイル保存成功: IconIndex={Data.IconIndex}, PlayerName={Data.PlayerName}");

        ProfileSettingPUN.TryPushProfileToPhotonIfInRoom();
    }

    public void UpdateProfile(int iconIndex, string playerName)
    {
        Data.IconIndex = iconIndex;
        Data.PlayerName = playerName;
        SaveProfile();
    }
    }
}
