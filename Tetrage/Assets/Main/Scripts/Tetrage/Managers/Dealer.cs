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
        /* -------- 1. 唯一のインスタンスを公開 -------- */
        public static Dealer Instance { get; internal set; } // ここのinteralについていまいちわかってない。　テストがしやすい、とだけ

        /* -------- 2. Awake で重複チェック -------- */
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);             // 既に存在→この複製を破棄
                return;
            }

            Instance = this;                     // 初回生成
            DontDestroyOnLoad(gameObject);       // シーンをまたいで保持したい場合
        }

        /* -------- 3. 通常の Dealer ロジック -------- */

        /// <summary>
        /// 現在のステージ情報。
        /// </summary>
        private Stage _stage;

        /// <summary>
        /// ゲームに参加しているプレイヤーのリスト。
        /// </summary>
        private List<Player> _players;
        public IReadOnlyList<Player> Players => _players;

        /// <summary>
        /// 現在のプレイヤー。
        /// </summary>
        private Player _currentPlayer;
        public Player CurrentPlayer {  get { return _currentPlayer; } }




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