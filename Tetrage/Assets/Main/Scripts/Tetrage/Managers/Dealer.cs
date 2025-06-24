using System;
using System.Collections.Generic;
using System.Linq;
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
        /* -------- 1. 戦略パターン用インターフェース（将来追加予定） -------- */
        // private ITurnStrategy _turnStrategy;
        // private IFirstPlayerStrategy _firstPlayerStrategy;

        /* -------- 2. イベント -------- */
        public event Action RoundStart;
        public event Action RoundEnd;

        /* -------- 3. フィールド -------- */
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

        /* -------- 4. コンストラクタ -------- */
        /// <summary>
        /// Dealerクラスのコンストラクタ
        /// </summary>
        public Dealer()
        {
            _players = new List<IPlayer>();
            Debug.Log("Dealer: インスタンスが作成されました");
        }

        /// <summary>
        /// 戦略パターン対応コンストラクタ（将来用）
        /// </summary>
        /// <param name="turnStrategy">ターン戦略</param>
        /// <param name="firstPlayerStrategy">開始プレイヤー選択戦略</param>
        // public Dealer(ITurnStrategy turnStrategy = null, IFirstPlayerStrategy firstPlayerStrategy = null)
        // {
        //     _players = new List<IPlayer>();
        //     _turnStrategy = turnStrategy;
        //     _firstPlayerStrategy = firstPlayerStrategy;
        //     Debug.Log("Dealer: 戦略パターン付きインスタンスが作成されました");
        // }

        /* -------- 5. 初期化メソッド -------- */
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
            _players = players ?? new List<IPlayer>();
        }

        /// <summary>
        /// 現在のプレイヤーを設定する
        /// </summary>
        public void SetCurrentPlayer(IPlayer player)
        {
            _currentPlayer = player;
        }

        /* -------- 6. ラウンド管理 -------- */
        public void OnRoundStart()
        {
            RoundStart?.Invoke();
        }

        public void OnRoundEnd()
        {
            RoundEnd?.Invoke();
        }

        /* -------- 7. ゲームロジック -------- */
        /// <summary>
        /// 各プレイヤーにカードを配布する。
        /// </summary>
        public void DistributeCards()
        {
            // 実装予定
        }

        /// <summary>
        /// 最初にターンを持つプレイヤーを決定する。
        /// </summary>
        public void DecideFirstPlayer()
        {
            // 戦略パターン版：
            // if (_firstPlayerStrategy != null && _players?.Count > 0)
            // {
            //     _currentPlayer = _firstPlayerStrategy.DecideFirstPlayer(_players);
            // }
            
            // 仮実装：最初のプレイヤーを選択
            if (_players?.Count > 0)
            {
                _currentPlayer = _players[0];
            }
        }

        /// <summary>
        /// 次のプレイヤーのターンに移行する。
        /// </summary>
        public void NextTurn()
        {
            // 戦略パターン版：
            // if (_turnStrategy != null && _currentPlayer != null && _players?.Count > 0)
            // {
            //     _currentPlayer = _turnStrategy.GetNextPlayer(_currentPlayer, _players);
            // }
            
            // 仮実装：時計回り
            if (_currentPlayer != null && _players?.Count > 0)
            {
                var currentIndex = _players.ToList().IndexOf(_currentPlayer);
                if (currentIndex >= 0)
                {
                    var nextIndex = (currentIndex + 1) % _players.Count;
                    _currentPlayer = _players[nextIndex];
                }
            }
        }
    }
}