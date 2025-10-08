using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfileUIController : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite[] availableIcons;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private PlayerProfileManager profileManager;

    private int currentIconIndex = 0;

    void Start()
    {
        // 初期反映
        currentIconIndex = profileManager.Data.iconIndex;
        iconImage.sprite = availableIcons[currentIconIndex];
        nameInput.text = profileManager.Data.playerName;
    }

    public void OnNextIcon()
    {
        currentIconIndex = (currentIconIndex + 1) % availableIcons.Length;
        iconImage.sprite = availableIcons[currentIconIndex];
    }

    public void OnPrevIcon()
    {
        currentIconIndex = (currentIconIndex - 1 + availableIcons.Length) % availableIcons.Length;
        iconImage.sprite = availableIcons[currentIconIndex];
    }

    public void OnConfirm()
    {
        string playerName = nameInput.text;
        profileManager.UpdateProfile(currentIconIndex, playerName);
        // 将来的にPUN2経由で同期する部分をここに追加

        Debug.Log($"[Profile Confirmed] Name: {playerName}, Icon: {currentIconIndex}");
    }
}
