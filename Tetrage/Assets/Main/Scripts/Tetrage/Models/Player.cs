using UnityEngine;
using System.Collections.Generic;
using Tetrage.Actions;
using Tetrage.Core.Enums;

namespace Tetrage.Models
{
    /// <summary>
    /// ゲーム内のプレイヤーを表します。
    /// </summary>
    public class Player : MonoBehaviour
    {
        /// <summary>
        /// プレイヤーの一意な識別子。
        /// </summary>
        public int PlayerID { get; set; }

        /// <summary>
        /// プレイヤーの最初の一枚(本来のカード)
        /// </summary>
        public Card Target { get; set; }


        private CardPile _hands = new CardPile(name: "Hand", CardOwner.Player, maxCount: 3);

        /// <summary>
        /// プレイヤーが所持している手札の一覧。
        /// </summary>
        public CardPile Hands => _hands;

        /// <summary>
        /// 一時的に保持しているカードの一覧。
        /// </summary>
        public CardPile Tmp { get; } = new CardPile(name: "Temporary", CardOwner.Player, maxCount: 2);


        private void Start()
        {
            
        }

        /// <summary>
        /// 指定されたアクションを実行します。
        /// </summary>
        /// <param name="action">実行するゲームアクション。</param>
        public void PerformAction(GameAction action)
        {
            // アクションの処理内容
            action.Execute();
        }
    }
}
