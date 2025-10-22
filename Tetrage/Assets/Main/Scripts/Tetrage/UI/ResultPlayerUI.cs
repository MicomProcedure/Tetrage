using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tetrage.Core.Enums;
using Tetrage.Core.Contracts;
using System.Linq;

namespace Tetrage.UI
{
    /// <summary>
    /// 結果画面に表示する個々のプレイヤー情報UIコンポーネント
    /// </summary>
    public class ResultPlayerUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI suitText;
        [SerializeField] private Sprite[] availableIcons;

        #endregion

        #region Private Fields

        private Suit _currentSuit;

        #endregion

        #region Public Methods

        /// <summary>
        /// プレイヤー情報を設定
        /// </summary>
        public void SetPlayerData(IPlayer player)
        {
            if (player == null)
            {
                Debug.LogWarning("ResultPlayerUI: プレイヤーがnullです");
                return;
            }

            // 名前設定
            if (nameText != null)
            {
                nameText.text = player.UserId;
            }

            // Targetカードからスートを取得
            var targetCard = player.Target.FirstOrDefault();
            if (targetCard != null)
            {
                _currentSuit = targetCard.Suit;
                if (suitText != null)
                {
                    suitText.text = GetSuitSymbol(targetCard.Suit);
                    suitText.color = GetSuitColor(targetCard.Suit);
                }
            }
        }

        /// <summary>
        /// アイコンを設定
        /// </summary>
        /// <param name="iconIndex">アイコンのインデックス</param>
        public void SetIcon(int iconIndex)
        {
            if (iconImage != null && availableIcons != null && 
                iconIndex >= 0 && iconIndex < availableIcons.Length)
            {
                iconImage.sprite = availableIcons[iconIndex];
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// スート記号を取得
        /// </summary>
        private string GetSuitSymbol(Suit suit)
        {
            switch (suit)
            {
                case Suit.Heart: return "♥";
                case Suit.Diamond: return "♦";
                case Suit.Club: return "♣";
                case Suit.Spade: return "♠";
                default: return suit.ToString();
            }
        }

        /// <summary>
        /// スートの色を取得
        /// ハート・ダイヤは赤、クラブ・スペードは黒
        /// </summary>
        private Color GetSuitColor(Suit suit)
        {
            switch (suit)
            {
                case Suit.Heart:
                case Suit.Diamond:
                    return new Color(0.8f, 0.1f, 0.1f); // 赤色
                case Suit.Club:
                case Suit.Spade:
                    return Color.black; // 黒色
                default:
                    return Color.white; // デフォルトは白
            }
        }

        #endregion
    }
}