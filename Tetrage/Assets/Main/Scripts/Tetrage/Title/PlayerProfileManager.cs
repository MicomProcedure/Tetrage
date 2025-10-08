using UnityEngine;

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
        }
        else
        {
            Data = new PlayerProfileData { iconIndex = 0, playerName = "Player" };
        }
    }

    public void SaveProfile()
    {
        string json = JsonUtility.ToJson(Data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public void UpdateProfile(int iconIndex, string playerName)
    {
        Data.iconIndex = iconIndex;
        Data.playerName = playerName;
        SaveProfile();
    }
}
