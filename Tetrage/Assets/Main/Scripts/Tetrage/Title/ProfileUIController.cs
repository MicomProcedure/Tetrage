using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Tetrage.Title
{
    public class ProfileUIController : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite[] availableIcons;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private PlayerProfileManager profileManager;

    private int currentIconIndex = 0;

    void Start()
    {
        // 初期反映
        currentIconIndex = profileManager.Data.IconIndex;
        iconImage.sprite = availableIcons[currentIconIndex];
        nameInput.text = profileManager.Data.PlayerName;
    }

    public void OnNextIcon()
    {
        currentIconIndex = (currentIconIndex + 1) % availableIcons.Length;
        iconImage.sprite = availableIcons[currentIconIndex];
        
        // リアルタイムで保存・更新
        SaveAndUpdateProfile();
    }

    public void OnPrevIcon()
    {
        currentIconIndex = (currentIconIndex - 1 + availableIcons.Length) % availableIcons.Length;
        iconImage.sprite = availableIcons[currentIconIndex];
        
        // リアルタイムで保存・更新
        SaveAndUpdateProfile();
    }

    public void OnConfirm()
    {
        string playerName = nameInput.text;
        profileManager.UpdateProfile(currentIconIndex, playerName);
        
        // 他のProfileDisplayUIを更新
        UpdateAllProfileDisplays();
        
        Debug.Log($"[Profile Confirmed] Name: {playerName}, Icon: {currentIconIndex}");
    }
    
    /// <summary>
    /// シーン内のすべてのProfileDisplayUIを更新
    /// </summary>
    private void UpdateAllProfileDisplays()
    {
        // シーン内のすべてのProfileDisplayUIを更新
        ProfileDisplayUI[] allDisplays = FindObjectsByType<ProfileDisplayUI>(FindObjectsSortMode.None);
        Debug.Log($"ProfileDisplayUI更新開始: {allDisplays.Length}個のコンポーネントを検索");
        
        foreach (var display in allDisplays)
        {
            if (display.GetDisplayMode() == ProfileDisplayUI.DisplayMode.LocalProfile)
            {
                display.UpdateFromLocalProfile();
                Debug.Log($"ProfileDisplayUI更新: {display.name} - モード: {display.GetDisplayMode()}");
            }
            else
            {
                Debug.Log($"ProfileDisplayUI更新スキップ: {display.name} - モード: {display.GetDisplayMode()}");
            }
        }
    }
    
    /// <summary>
    /// プロフィールを保存して他のUIを更新
    /// </summary>
    private void SaveAndUpdateProfile()
    {
        string playerName = nameInput.text;
        profileManager.UpdateProfile(currentIconIndex, playerName);
        UpdateAllProfileDisplays();
        
        Debug.Log($"[Profile Updated] Name: {playerName}, Icon: {currentIconIndex}");
    }
    }
}
