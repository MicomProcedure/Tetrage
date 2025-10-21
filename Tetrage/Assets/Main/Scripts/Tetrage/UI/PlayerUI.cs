using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tetrage.Core.DTO;



namespace Tetrage.UI
{
    /// <summary>
    /// 個々のプレイヤー情報を表示するパネル
    /// ローカルプロファイル、PlayerInfo、Photonプレイヤーの全てに対応
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PlayerUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [SerializeField] private Image iconImage;                 // プレイヤーアイコン
        [SerializeField] private TextMeshProUGUI playerNameText;  // プレイヤー名 (UserId)
        [SerializeField] private Sprite[] availableIcons;         // 使用可能なアイコン一覧
        
        [Header("Optional")]
        [SerializeField] private TextMeshProUGUI playerNumberText; // 何Pかを表示するテキスト
        [SerializeField] private TextMeshProUGUI currentplayerText; // 現在のプレイヤーを表示するテキスト

        public void SetPlayerInfo(PlayerInfo playerInfo)
        {
            //playerNumberText.text = playerInfo.Id.ToString();
            playerNameText.text = playerInfo.UserId;
            iconImage.sprite = availableIcons[playerInfo.PlayerIconIndex];
        }
        public void SetCurrentPlayer(bool isCurrentPlayer)
        {
            currentplayerText.text = isCurrentPlayer ? "Now" : "";
        }

        public void SetPlayerIcon(int iconIndex)
        {
            // アイコンを設定
            if (iconImage != null && availableIcons != null && 
                iconIndex >= 0 && iconIndex < availableIcons.Length)
            {
                iconImage.sprite = availableIcons[iconIndex];
            }
            else if (iconIndex < 0 || iconIndex >= (availableIcons?.Length ?? 0))
            {
                Debug.LogWarning($"PlayerUI: IconIndex={iconIndex} が範囲外です (利用可能: 0-{availableIcons?.Length - 1 ?? 0})");
            }

        }
        #endregion
    }
}
