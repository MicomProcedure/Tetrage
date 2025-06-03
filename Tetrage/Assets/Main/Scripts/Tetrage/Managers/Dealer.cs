using System.Collections.Generic;
using UnityEngine;
using Tetrage.Models;
using Tetrage.Core.Contracts;

/// <summary>
/// ゲームのディーラークラス。カードの配布、ターン管理、勝敗判定を行う。
/// </summary>
namespace Tetrage.Managers
{
    public class Dealer : IGameContextProvider
    {
        /* -------- 1. 唯一のインスタンスを公開 -------- */
        private static Dealer _instance;
        private static readonly object _lock = new object();
        
        public static Dealer Instance 
        { 
            get 
        {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
            {
                            _instance = new Dealer();
                        }
                    }
                }
                return _instance;
            }
            internal set => _instance = value; // テスト用
        }

        /* -------- 2. プライベートコンストラクタでSingleton実装 -------- */
        private Dealer()
        {
            // 初期化処理があればここに記述
        }

        /* -------- 3. 通常の Dealer ロジック -------- */

        /// <summary>
        /// 現在のステージ情報。
        /// </summary>
        private Stage _stage;
        public Stage Stage { get { return _stage; } }

        /// <summary>
        /// ゲームに参加しているプレイヤーのリスト。
        /// </summary>
        private List<IPlayer> _players;
        public IReadOnlyList<IPlayer> Players => _players;

        /// <summary>
        /// 現在のプレイヤー。
        /// </summary>
        private IPlayer _currentPlayer;
        public IPlayer CurrentPlayer {  get { return _currentPlayer; } }

        /// <summary>
        /// Stageを設定する（初期化用）
        /// </summary>
        public void SetStage(Stage stage)
        {
            _stage = stage;
        }

        /// <summary>
        /// プレイヤーリストを設定する（初期化用）
        /// </summary>
        public void SetPlayers(List<IPlayer> players)
        {
            _players = players;
        }

        /// <summary>
        /// 現在のプレイヤーを設定する
        /// </summary>
        public void SetCurrentPlayer(IPlayer player)
        {
            _currentPlayer = player;
        }

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

        /// <summary>
        /// Dealerインスタンスをリセットする（テスト用）
        /// </summary>
        public static void ResetInstance()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }
    }
}