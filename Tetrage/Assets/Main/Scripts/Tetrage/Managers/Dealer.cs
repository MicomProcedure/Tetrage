using System;
using System.Collections.Generic;
using UnityEngine;
using Tetrage.Models;
using Tetrage.Core.Contracts;

/// <summary>
/// ゲームのディーラークラス。カードの配布、ターン管理、勝敗判定を行う。
/// </summary>
namespace Tetrage.Managers
{
    public class Dealer : IGameContextProvider, IRoundManager
    {
        /* -------- 1. Singleton実装 -------- */
        private static readonly Dealer _instance = new Dealer();
        public static Dealer Instance => _instance;

        /* -------- 2. プライベートコンストラクタでSingleton実装 -------- */
        private Dealer()
        {
            // 初期化処理があればここに記述
        }

        /* -------- 3. 通常の Dealer ロジック -------- */


        public event Action RoundStart;
        public event Action RoundEnd;

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
        public IPlayer CurrentPlayer { get { return _currentPlayer; } }

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

        public void OnRoundStart()
        {
            RoundStart?.Invoke();
        }

        public void OnRoundEnd()
        {
            RoundEnd?.Invoke();
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
        /// Dealerインスタンスをリセットする（テスト用）
        /// </summary>
        public static void ResetInstance()
        {
            // このメソッドは通常使用されないため、実装は不要
        }

    }
}