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
    /// テスト用戦略: 固定したプレイヤーから開始し、常に同じプレイヤーにターンを回す戦略
    /// </summary>
    /// <remarks>
    /// DecideFirstPlayer               : 指定したインデックスのプレイヤーを親にする
    /// GetNextPlayer                   : 常に同じ指定したプレイヤーにターンを回す
    /// GetNextPlayerWithConditions     : 条件に関係なく常に同じ指定したプレイヤーにターンを回す
    /// ResetTurnOrder                  : 何もしない
    /// ShuffleDeck                     : デッキを「スートAの1, スートBの1, ... スートnの1, スートAの2, スートBの2, ...」の順序にする
    /// DistributeCards                 : カードを指定したプレイヤーに順番に配布する
    /// SetupInitialTargets             : 初期ターゲットカードを指定したプレイヤーに設定する
    /// </remarks>
    public class FixedPlayerStrategy : IDealerStrategy
    {
        #region フィールドとコンストラクタ

        private IPlayer _fixedPlayer;
        private readonly int _fixedPlayerIndex;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="fixedPlayerIndex">固定するプレイヤーのインデックス（0ベース）</param>
        public FixedPlayerStrategy(int fixedPlayerIndex = 0)
        {
            _fixedPlayerIndex = fixedPlayerIndex;
        }

        #endregion

        #region IDealerStrategy インターフェース実装

        /// <summary>
        /// ゲーム開始時の最初のプレイヤーを決定する（固定プレイヤーを選択）
        /// </summary>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <returns>固定されたIPlayer</returns>
        public IPlayer DecideFirstPlayer(IReadOnlyList<IPlayer> players)
        {
            // nullチェック
            if (players == null)
                throw new ArgumentNullException(nameof(players), "プレイヤーリストがnullです");

            if (players.Count == 0)
                throw new ArgumentException("プレイヤーリストが空です", nameof(players));
                

            // 指定されたインデックスが範囲外の場合は最初のプレイヤーを選択
            var playerIndex = _fixedPlayerIndex < players.Count ? _fixedPlayerIndex : 0;
            _fixedPlayer = players[playerIndex];

            Debug.Log($"FixedPlayerStrategy: 固定プレイヤーを選択しました - Player {_fixedPlayer.PlayerId} (Index: {playerIndex})");
            return _fixedPlayer;
        }

        /// <summary>
        /// 次のターンプレイヤーを決定する（常に同じプレイヤーを返す）
        /// </summary>
        /// <param name="currentPlayer">現在のターンプレイヤー</param>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <returns>固定されたプレイヤー（常に同じ）</returns>
        public IPlayer GetNextPlayer(IPlayer currentPlayer, IReadOnlyList<IPlayer> players)
        {
            if (players == null)
                throw new ArgumentNullException(nameof(players), "プレイヤーリストがnullです");

            // 固定プレイヤーが設定されていない場合は再設定
            if (_fixedPlayer == null)
            {
                _fixedPlayer = DecideFirstPlayer(players);
            }

            Debug.Log($"FixedPlayerStrategy: 次のターンも同じプレイヤー - Player {_fixedPlayer.PlayerId}");
            return _fixedPlayer;
        }

        /// <summary>
        /// 条件付きで次のプレイヤーを決定する（条件に関係なく常に同じプレイヤーを返す）
        /// </summary>
        /// <param name="currentPlayer">現在のターンプレイヤー</param>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <param name="skipConditions">スキップ条件（この戦略では無視される）</param>
        /// <returns>固定されたプレイヤー</returns>
        public IPlayer GetNextPlayerWithConditions(IPlayer currentPlayer, IReadOnlyList<IPlayer> players, TurnSkipConditions skipConditions = null)
        {
            // 固定プレイヤー戦略では条件を無視して常に同じプレイヤーを返す
            if (skipConditions != null)
            {
                Debug.Log("FixedPlayerStrategy: スキップ条件が指定されましたが、固定プレイヤー戦略では無視されます");
            }

            return GetNextPlayer(currentPlayer, players);
        }

        /// <summary>
        /// ターン順序をリセットする（固定プレイヤー戦略では何もしない）
        /// </summary>
        /// <param name="players">参加プレイヤーのリスト</param>
        public void ResetTurnOrder(IReadOnlyList<IPlayer> players)
        {
            // 固定プレイヤー戦略では順序の概念がないため、何もしない
            Debug.Log("FixedPlayerStrategy: ターン順序のリセットが要求されましたが、固定プレイヤー戦略では何もしません");

            // 必要に応じて固定プレイヤーを再設定
            if (_fixedPlayer == null && players?.Count > 0)
            {
                _fixedPlayer = DecideFirstPlayer(players);
            }
        }

        /// <summary>
        /// デッキを特定の順序にシャッフルする
        /// 「スートAの1, スートBの1, ... スートnの1, スートAの2, スートBの2, ...」の順序にする
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

            // SortCardsメソッドを使用して効率的にソート
            // 期待される順序: 「ランク1のスート順, ランク2のスート順, ...」
            stack.SortCards((card1, card2) =>
            {
                // まずランク（Number）で比較
                int rankComparison = card1.Number.CompareTo(card2.Number);
                if (rankComparison != 0)
                    return rankComparison;

                // ランクが同じ場合はカスタムスート順序で比較
                return card1.Suit.CompareByTrumpOrder(card2.Suit);
            });

            Debug.Log($"{GetType().Name}: 山札を固定順序でシャッフルしました - {stack.Count}枚");
            Debug.Log($"{GetType().Name}: シャッフル完了 - ランク順・スート順配置");
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

            // nullチェック
            if (stack == null)
                throw new ArgumentNullException(nameof(stack), "山札がnullです");

            // プレイヤーが存在しない場合はスキップ
            if (players.Count == 0)
            {
                Debug.Log("FixedPlayerStrategy: プレイヤーが存在しないため配布をスキップします");
                return;
            }

            // 配布枚数が0以下の場合はスキップ
            if (cardsPerPlayer <= 0)
            {
                Debug.Log("FixedPlayerStrategy: 配布枚数が0以下のため配布をスキップします");
                return;
            }

            // 必要なカード枚数を計算
            var totalCardsNeeded = players.Count * cardsPerPlayer;
            if (stack.Count < totalCardsNeeded)
            {
                Debug.LogWarning($"FixedPlayerStrategy: 山札のカード不足 - 必要:{totalCardsNeeded}枚, 利用可能:{stack.Count}枚");
            }

            Debug.Log($"FixedPlayerStrategy: カード配布開始 - {players.Count}人に{cardsPerPlayer}枚ずつ配布");

            // プレイヤー順に1枚ずつ配布を繰り返す
            for (int cardIndex = 0; cardIndex < cardsPerPlayer; cardIndex++)
            {
                for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
                {
                    var player = players[playerIndex];
                    var cardToDistribute = stack.Peek(1)[0]; // 先頭のカードを取得

                    bool success = CardPile.TransferService.Transfer(stack, player.Hands, cardToDistribute);
                    if (success)
                    {
                        Debug.Log($"FixedPlayerStrategy: Player {player.PlayerId} に {cardToDistribute.Suit} {cardToDistribute.Number} を配布");
                    }
                    else
                    {
                        Debug.LogWarning($"FixedPlayerStrategy: Player {player.PlayerId} への配布に失敗しました");
                    }
                }
            }

            Debug.Log("FixedPlayerStrategy: カード配布完了");
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
                Debug.Log("FixedPlayerStrategy: プレイヤーが存在しないためターゲット設定をスキップします");
                return;
            }

            if (stack.Count < players.Count)
            {
                Debug.LogWarning($"FixedPlayerStrategy: ターゲット設定用のカード不足 - 必要:{players.Count}枚, 利用可能:{stack.Count}枚");
            }

            Debug.Log($"FixedPlayerStrategy: ターゲットカード設定開始 - {players.Count}人");

            // プレイヤー順にデッキの先頭から1枚ずつ配布
            foreach (var player in players)
            {
                if (stack.Count == 0)
                {
                    Debug.LogWarning("FixedPlayerStrategy: 山札が空になったためターゲット設定を終了します");
                    return;
                }

                var targetCard = stack.Peek(1)[0]; // 先頭のカードを取得

                bool success = CardPile.TransferService.Transfer(stack, player.Target, targetCard);
                if (success)
                {
                    Debug.Log($"FixedPlayerStrategy: Player {player.PlayerId} のターゲットに {targetCard.Suit} {targetCard.Number} を設定");
                }
                else
                {
                    Debug.LogWarning($"FixedPlayerStrategy: Player {player.PlayerId} のターゲット設定に失敗しました");
                }
            }

            Debug.Log("FixedPlayerStrategy: ターゲットカード設定完了");
        }

        #endregion

        #region クラス独自の実装（インターフェース要求外）

        /// <summary>
        /// 戦略の説明
        /// </summary>
        public string StrategyDescription => "ターンを固定したプレイヤーに回す（主にデバッグ用）";

        /// <summary>
        /// 現在固定されているプレイヤーを取得する（デバッグ用）
        /// </summary>
        /// <returns>現在固定されているプレイヤー</returns>
        public IPlayer GetFixedPlayer()
        {
            return _fixedPlayer;
        }

        /// <summary>
        /// 固定プレイヤーを表示する（デバッグ用）
        /// </summary>
        public void ShowFixedPlayer()
        {
            Debug.Log($"FixedPlayerStrategy: 固定プレイヤー - Player {_fixedPlayer.PlayerId}");
        }

        /// <summary>
        /// 固定プレイヤーを変更する（テスト中に動的に変更する場合）
        /// </summary>
        /// <param name="newFixedPlayer">新しい固定プレイヤー</param>
        public void SetFixedPlayer(IPlayer newFixedPlayer)
        {
            _fixedPlayer = newFixedPlayer;
            Debug.Log($"FixedPlayerStrategy: 固定プレイヤーが変更されました - Player {newFixedPlayer?.PlayerId}");
        }

        #endregion
    }
}