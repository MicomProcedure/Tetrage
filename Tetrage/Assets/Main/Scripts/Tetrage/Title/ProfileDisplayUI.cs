using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfileDisplayUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;                 // アイコン表示用
    [SerializeField] private TextMeshProUGUI nameText;        // 名前表示用
    [SerializeField] private Sprite[] availableIcons;         // 使用可能なアイコン一覧

    [Header("Profile Manager Reference")]
    [SerializeField] private PlayerProfileManager profileManager;

    void Start()
    {
        // プロフィールマネージャーの参照が無ければ自動検索
        if (profileManager == null)
        {
            profileManager = FindFirstObjectByType<PlayerProfileManager>();
        }

        // 現在のプロフィールを即反映
        if (profileManager != null && profileManager.Data != null)
        {
            UpdateDisplay();
        }
        else
        {
            Debug.LogWarning("PlayerProfileManager または ProfileData が見つかりません。");
        }
    }

    /// <summary>
    /// PlayerProfileManager のデータをもとに UI を更新
    /// </summary>
    public void UpdateDisplay()
    {
        var data = profileManager?.Data;
        if (data == null)
        {
            Debug.LogWarning("プロフィールデータが存在しません。");
            return;
        }

        // アイコン設定
        if (availableIcons != null && data.iconIndex >= 0 && data.iconIndex < availableIcons.Length)
        {
            iconImage.sprite = availableIcons[data.iconIndex];
        }
        else
        {
            Debug.LogWarning($"iconIndex={data.iconIndex} が範囲外です。");
        }

        // 名前設定
        nameText.text = data.playerName;
    }
}
