using UnityEngine;
using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.Actions;

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
        /// プレイヤーの最初の一枚(本来のカード)
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
        /// Playerのコンストラクタ。PlayerIDを指定して初期化します。
        /// </summary>
        /// <param name="playerID">プレイヤーの一意な識別子</param>
        public Player(int playerID)
        {
            PlayerID = playerID;
            hands = new List<Card>();
            tmp = new List<Card>();  // 一時的なカードリストも初期化
        }

        /// <summary>
        /// Playerのコンストラクタ。プレイヤーIDを指定せずに初期化します。
        /// </summary>
        public Player()
        {
            hands = new List<Card>();
            tmp = new List<Card>();
        }

        /// <summary>
        /// 指定されたアクションを実行します。
        /// </summary>
        /// <param name="action">実行するゲームアクション。</param>
        public void PerformAction(GameAction action)
        {
            // アクションの処理内容
        }
    }
}
