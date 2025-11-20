using UnityEngine;
using System.Collections.Generic;
using Tetrage.Core.Enums;

namespace Tetrage.Tests.Data
{
    /// <summary>
    /// デバッグ用のプレイヤー情報設定
    /// リストのインデックスが入室順を表す（Element 0 = 1番目に入室、Element 1 = 2番目に入室...）
    /// </summary>
    [System.Serializable]
    public class DebugPlayerInfo
    {
        [Header("Player Settings")]
        [Tooltip("プレイヤー名")]
        public string PlayerName = "Player 1";

        [Tooltip("アイコンインデックス（0-3）")]
        [Range(0, 3)]
        public int IconIndex = 0;

        [Header("Initial Cards")]
        [Tooltip("初期カードを設定するか")]
        public bool SetInitialCards = false;

        [Tooltip("Target Card (1枚)")]
        public CardSpec TargetCard = new CardSpec { Suit = Suit.Spade, Number = 1 };

        [Tooltip("Hand Cards (0-3枚)")]
        public List<CardSpec> HandCards = new List<CardSpec>();
    }

    /// <summary>
    /// カード指定用の構造体
    /// </summary>
    [System.Serializable]
    public class CardSpec
    {
        public Suit Suit = Suit.Spade;

        [Range(1, 13)]
        public int Number = 1;

        /// <summary>
        /// カードが有効かどうか（Number が1-13の範囲内）
        /// </summary>
        public bool IsValid
        {
            get
            {
                // Number が0の場合は無効（Unityの初期化問題対策）
                if (Number == 0)
                {
                    Debug.LogWarning($"[CardSpec] Number=0は無効です。Suit={Suit}のカードはスキップされます。Inspector上でNumberを1-13に設定してください。");
                    return false;
                }
                return Number >= 1 && Number <= 13;
            }
        }

        /// <summary>
        /// コンストラクタ（デフォルト値を確実に設定）
        /// </summary>
        public CardSpec()
        {
            Suit = Suit.Spade;
            Number = 1;
        }
    }
}
