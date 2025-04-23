using System.Collections.Generic;
using UnityEngine;
using Tetrage.Models;

/// <summary>
/// ゲームのディーラークラス。カードの配布、ターン管理、勝敗判定を行う。
/// </summary>
namespace Tetrage.Managers
{
    public class Dealer : MonoBehaviour
    {
        /// <summary>
        /// 現在のステージ情報。
        /// </summary>
        private Stage stage;

        /// <summary>
        /// ゲームに参加しているプレイヤーのリスト。
        /// </summary>
        // private List<Player> players;（いつか復活させてください）

        /// <summary>
        /// 現在のプレイヤー。
        /// </summary>
        //private Player currentPlayer;（いつか復活させてください）

        /// <summary>
        /// 各プレイヤーにカードを配布する。
        /// </summary>
        public void DistributeCards()
        {

        }

        /// <summary>
        /// 最初にターンを持つプレイヤーを決定する。
        /// </summary>
        public void DecideFirstPlayer()
        {

        }

        /// <summary>
        /// 次のプレイヤーのターンに移行する。
        /// </summary>
        public void NextTurn()
        {

        }

        /// <summary>
        /// 指定されたアクションタイプに基づいてテトラージュの勝敗を判定する。
        /// </summary>

        /*public void JudgeTetrage(ActionType actionType)
        {

        }
        （いつか復活させてください）*/
    }
}