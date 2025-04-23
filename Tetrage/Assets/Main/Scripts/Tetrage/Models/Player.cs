using UnityEngine;
using System.Collections.Generic;
using Tetrage.Core;

namespace Tetrage.Models
{

    /// <summary>
    /// ゲーム内のプレイヤーを表します。
    /// </summary>
    public class Player
    {
        /// <summary>
        /// プレイヤーの一意な識別子。
        /// </summary>
        public int PlayerID;

        /// <summary>
        /// 現在選択されているターゲットカード。
        /// </summary>
        public Card target;

        /// <summary>
        /// プレイヤーが所持している手札の一覧。
        /// </summary>
        public List<Card> hands;

        /// <summary>
        /// 一時的に保持しているカードの一覧。
        /// </summary>
        public List<Card> tmp;

        /// <summary>
        /// 指定されたアクションを実行します。
        /// </summary>
        /// <param name="action">実行するゲームアクション。</param>
        public void PerformAction(GameAction action)
        {
            // アクションの処理内容をここに記述
        }
    }

}