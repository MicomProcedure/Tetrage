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

    public void SaveProfile()
    {
        string json = JsonUtility.ToJson(Data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
        Debug.Log($"プロファイル保存成功: IconIndex={Data.IconIndex}, PlayerName={Data.PlayerName}");
    }

    public void UpdateProfile(int iconIndex, string playerName)
    {
        Data.IconIndex = iconIndex;
        Data.PlayerName = playerName;
        SaveProfile();
    }
    }
}
