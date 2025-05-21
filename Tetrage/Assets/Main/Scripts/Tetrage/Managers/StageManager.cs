using UnityEngine;
using System.Collections.Generic; // 必要に応じて追加
using Tetrage.Models; // CardPile, Card を使用するため

namespace Tetrage.Models
{
    /// <summary>
    /// ゲームステージ全体の管理を担うMonoBehaviourコンポーネント。
    /// UnityシーンのGameObjectにアタッチされて使用されます。
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        /// <summary>
        /// ゲームで使用するデッキ（山札）。
        /// </summary>
        public CardPile Deck { get; private set; } // private set により外部からの直接的な代入を防ぐ

        /// <summary>
        /// ゲームで使用する捨て山。
        /// </summary>
        public CardPile DiscardPile { get; private set; } // private set により外部からの直接的な代入を防ぐ

        /// <summary>
        /// StageManagerを初期化し、デッキと捨て山を設定します。
        /// </summary>
        /// <param name="deck">ゲームで使用するデッキのCardPileインスタンス。</param>
        /// <param name="discardPile">ゲームで使用する捨て山のCardPileインスタンス。</param>
        public void Initialize(CardPile deck, CardPile discardPile)
        {
            Deck = deck;
            DiscardPile = discardPile;
            Debug.Log($"StageManager: 初期化完了 - デッキ名: {Deck.Name}, 捨て山名: {DiscardPile.Name}");
        }
    }
}