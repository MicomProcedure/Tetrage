using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.Core.Enums;
using System;

namespace Tetrage.Managers.DealerStrategies
{
    /// <summary>
    /// 本番実装用戦略: 実際のゲームで使用される戦略
    /// </summary>
    /// <remarks>
    /// DecideFirstPlayer               : PlayerListから最初のプレイヤーを決定する
    /// GetNextPlayer                   : 次のプレイヤーを決定する
    /// GetNextPlayerWithConditions     : 条件に関係なく次のプレイヤーを決定する
    /// ResetTurnOrder                  : ターン順序をリセットする
    /// ShuffleDeck                     : デッキをシャッフルする
    /// DistributeCards                 : カードを配布する
    /// SetupInitialTargets             : 初期ターゲットカードを設定する
    /// </remarks>
    public class RealDealerStrategy : IDealerStrategy
    {
        #region フィールドとコンストラクタ

        private IPlayer _firstPlayer;
        private List<IPlayer> _turnOrder;
        private int _currentTurnIndex;

        #endregion

        #region IDealerStrategy インターフェース実装

        /// <summary>
        /// ゲーム開始時の最初のプレイヤーを決定する
        /// プレイヤーをランダムな順序でシャッフルして最初のプレイヤーを選択する
        /// </summary>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <returns>最初のプレイヤー</returns>
        public IPlayer DecideFirstPlayer(IReadOnlyList<IPlayer> players)
        {
            // プレイヤーリストが空の場合は例外をスロー
            if (players == null || players.Count == 0)
                throw new ArgumentException("プレイヤーリストがnullまたは空です", nameof(players));

            // ターン順序が未設定の場合は初期化
            if (_turnOrder == null || _turnOrder.Count == 0)
            {
                ResetTurnOrder(players);
            }

            _firstPlayer = _turnOrder[0];
            Debug.Log($"RealDealerStrategy: 最初のプレイヤーを決定しました - Player {_firstPlayer.PlayerId}");
            return _firstPlayer;
        }

        /// <summary>
        /// 次のターンプレイヤーを決定する
        /// 事前に決定されたランダム順序で次のプレイヤーを返す
        /// </summary>
        /// <param name="currentPlayer">現在のターンプレイヤー</param>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <returns>次のプレイヤー</returns>
        public IPlayer GetNextPlayer(IPlayer currentPlayer, IReadOnlyList<IPlayer> players)
        {
            // プレイヤーリストがnullまたは空の場合は例外をスロー
            if (players == null || players.Count == 0)
                throw new ArgumentException("プレイヤーリストがnullまたは空です", nameof(players));

            // プレイヤーが1人の場合はそのプレイヤーを返す
            if (players.Count == 1)
            {
                Debug.Log("RealDealerStrategy: プレイヤーが1人のため、同じプレイヤーを返します");
                return players[0];
            }

            // ターン順序が未設定の場合は初期化
            if (_turnOrder == null || _turnOrder.Count == 0)
            {
                Debug.LogWarning("RealDealerStrategy: ターン順序が未設定です。DecideFirstPlayerを先に呼び出してください。");
                DecideFirstPlayer(players);
            }

            // 次のターンインデックスに進む
            _currentTurnIndex = (_currentTurnIndex + 1) % _turnOrder.Count;
            var nextPlayer = _turnOrder[_currentTurnIndex];

            Debug.Log($"RealDealerStrategy: 次のプレイヤーを決定しました - Player {nextPlayer.PlayerId} (ターン {_currentTurnIndex + 1}/{_turnOrder.Count})");
            return nextPlayer;
        }

        /// <summary>
        /// 条件付きで次のプレイヤーを決定する
        /// 条件に関係なくランダムに次のプレイヤーを決定する
        /// </summary>
        /// <param name="currentPlayer">現在のターンプレイヤー</param>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <param name="skipConditions">スキップ条件（この戦略では無視される）</param>
        /// <returns>次のプレイヤー</returns>
        public IPlayer GetNextPlayerWithConditions(IPlayer currentPlayer, IReadOnlyList<IPlayer> players, TurnSkipConditions skipConditions = null)
        {
            // スキップ条件が指定された場合はログに記録
            if (skipConditions != null)
            {
                Debug.Log("RealDealerStrategy: スキップ条件が指定されましたが、実装では無視されます");
            }

            // ランダムな次のプレイヤー決定ロジックを使用
            return GetNextPlayer(currentPlayer, players);
        }

        /// <summary>
        /// ターン順序をリセットする
        /// ラウンド開始時等にターン順序をリセットし、新しいランダム順序を設定する
        /// </summary>
        /// <param name="players">参加プレイヤーのリスト</param>
        public void ResetTurnOrder(IReadOnlyList<IPlayer> players)
        {
            Debug.Log("RealDealerStrategy: ターン順序をリセットします");

            // プレイヤーリストが空の場合はスキップ
            if (players?.Count == 0)
            {
                Debug.LogWarning("RealDealerStrategy: プレイヤーリストが空のためターン順序リセットをスキップします");
                return;
            }

            // 新しいランダム順序を設定
            _turnOrder = new List<IPlayer>(players);
            for (int i = 0; i < _turnOrder.Count; i++)
            {
                var temp = _turnOrder[i];
                int randomIndex = UnityEngine.Random.Range(i, _turnOrder.Count);
                _turnOrder[i] = _turnOrder[randomIndex];
                _turnOrder[randomIndex] = temp;
            }

            _currentTurnIndex = 0;
            _firstPlayer = _turnOrder[_currentTurnIndex];

            Debug.Log($"RealDealerStrategy: ターン順序リセット完了 - 新しい順序: {string.Join(", ", _turnOrder.Select(p => $"Player {p.PlayerId}"))}");
        }

        /// <summary>
        /// デッキをランダムにシャッフルする
        /// </summary>
        /// <param name="stack">シャッフル対象のカードパイル</param>
        public void ShuffleDeck(CardPile stack)
        {
            if (stack == null)
                throw new ArgumentNullException(nameof(stack), "山札がnullです");
            if (stack.Count == 0)
            {
                Debug.Log($"{GetType().Name}: 山札が空のためシャッフルをスキップします");
                return;
            }

            // ランダムシャッフルを実装
            stack.RandomShuffle();

            Debug.Log($"{GetType().Name}: デッキをランダムにシャッフルしました - {stack.Count}枚");

        }

        /// <summary>
        /// プレイヤーにカードを順番に配布する
        /// プレイヤー順に1枚ずつ配布を繰り返す
        /// </summary>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <param name="stack">配布元の山札</param>
        /// <param name="cardsPerPlayer">各プレイヤーに配布するカード枚数</param>
        public void DistributeCards(IReadOnlyList<IPlayer> players, CardPile stack, int cardsPerPlayer)
        {
            // nullチェック
            if (players == null)
                throw new ArgumentNullException(nameof(players), "プレイヤーリストがnullです");

            if (stack == null)
                throw new ArgumentNullException(nameof(stack), "山札がnullです");

            // プレイヤーが存在しない場合はスキップ
            if (players.Count == 0)
            {
                Debug.Log("RealDealerStrategy: プレイヤーが存在しないため配布をスキップします");
                return;
            }

            // 配布枚数が0以下の場合はスキップ
            if (cardsPerPlayer <= 0)
            {
                Debug.Log("RealDealerStrategy: 配布枚数が0以下のため配布をスキップします");
                return;
            }

            // 必要なカード枚数を計算
            var totalCardsNeeded = players.Count * cardsPerPlayer;
            if (stack.Count < totalCardsNeeded)
            {
                Debug.LogWarning($"RealDealerStrategy: 山札のカード不足 - 必要:{totalCardsNeeded}枚, 利用可能:{stack.Count}枚");
            }

            Debug.Log($"RealDealerStrategy: カード配布開始 - {players.Count}人に{cardsPerPlayer}枚ずつ配布");

            // プレイヤー順に1枚ずつ配布を繰り返す（実際のカード配布方式）
            for (int cardIndex = 0; cardIndex < cardsPerPlayer; cardIndex++)
            {
                for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
                {
                    var player = players[playerIndex];

                    // 山札が空になった場合は配布を停止
                    if (stack.Count == 0)
                    {
                        Debug.LogWarning("RealDealerStrategy: 山札が空になったため配布を停止します");
                        return;
                    }

                    var cardToDistribute = stack.Peek(1)[0]; // 先頭のカードを取得

                    bool success = CardPile.TransferService.Transfer(stack, player.Hands, cardToDistribute);
                    if (success)
                    {
                        Debug.Log($"RealDealerStrategy: Player {player.PlayerId} に {cardToDistribute.Suit} {cardToDistribute.Number} を配布");
                    }
                    else
                    {
                        Debug.LogWarning($"RealDealerStrategy: Player {player.PlayerId} への配布に失敗しました");
                    }
                }
            }

            Debug.Log("RealDealerStrategy: カード配布完了");
        }

        /// <summary>
        /// 各プレイヤーの初期ターゲットカードを設定する
        /// プレイヤー順にデッキの先頭から1枚ずつ配布
        /// </summary>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <param name="stack">配布元の山札</param>
        public void SetupInitialTargets(IReadOnlyList<IPlayer> players, CardPile stack)
        {
            // nullチェック
            if (players == null)
                throw new ArgumentNullException(nameof(players), "プレイヤーリストがnullです");

            if (stack == null)
                throw new ArgumentNullException(nameof(stack), "山札がnullです");

            if (players.Count == 0)
            {
                Debug.Log("RealDealerStrategy: プレイヤーが存在しないためターゲット設定をスキップします");
                return;
            }

            if (stack.Count < players.Count)
            {
                Debug.LogWarning($"RealDealerStrategy: ターゲット設定用のカード不足 - 必要:{players.Count}枚, 利用可能:{stack.Count}枚");
            }

            Debug.Log($"RealDealerStrategy: 初期ターゲットカード設定開始 - {players.Count}人");

            // プレイヤー順にデッキの先頭から1枚ずつ配布
            foreach (var player in players)
            {
                if (stack.Count == 0)
                {
                    Debug.LogWarning("RealDealerStrategy: 山札が空になったためターゲット設定を終了します");
                    return;
                }

                var targetCard = stack.Peek(1)[0]; // 先頭のカードを取得

                bool success = CardPile.TransferService.Transfer(stack, player.Target, targetCard);
                if (success)
                {
                    Debug.Log($"RealDealerStrategy: Player {player.PlayerId} のターゲットに {targetCard.Suit} {targetCard.Number} を設定");
                }
                else
                {
                    Debug.LogWarning($"RealDealerStrategy: Player {player.PlayerId} のターゲット設定に失敗しました");
                }
            }

            Debug.Log("RealDealerStrategy: 初期ターゲットカード設定完了");
        }

        #endregion

        #region クラス独自の実装（インターフェース要求外）

        /// <summary>
        /// 戦略の説明
        /// </summary>
        public string StrategyDescription => "本番実装用戦略: ランダムプレイヤー選択で実際のゲームで使用される戦略";

        /// <summary>
        /// 現在最初のプレイヤーを取得する（デバッグ用）
        /// </summary>
        /// <returns>現在最初のプレイヤー</returns>
        public IPlayer GetFirstPlayer()
        {
            return _firstPlayer;
        }

        /// <summary>
        /// 現在のターン順序を取得する（デバッグ用）
        /// </summary>
        /// <returns>ターン順序のリスト</returns>
        public IReadOnlyList<IPlayer> GetTurnOrder()
        {
            return _turnOrder?.AsReadOnly();
        }

        /// <summary>
        /// 現在のターンインデックスを取得する（デバッグ用）
        /// </summary>
        /// <returns>現在のターンインデックス</returns>
        public int GetCurrentTurnIndex()
        {
            return _currentTurnIndex;
        }

        /// <summary>
        /// 最初のプレイヤーを表示する（デバッグ用）
        /// </summary>
        public void ShowFirstPlayer()
        {
            Debug.Log($"RealDealerStrategy: 最初のプレイヤー - Player {_firstPlayer.PlayerId}");
        }

        /// <summary>
        /// 最初のプレイヤーを変更する（テスト中に動的に変更する場合）
        /// </summary>
        /// <param name="newFixedPlayer">新しい最初のプレイヤー</param>
        public void SetFirstPlayer(IPlayer newFixedPlayer)
        {
            _firstPlayer = newFixedPlayer;
            Debug.Log($"RealDealerStrategy: 最初のプレイヤーが変更されました - Player {newFixedPlayer?.PlayerId}");
        }

        #endregion
    }
}