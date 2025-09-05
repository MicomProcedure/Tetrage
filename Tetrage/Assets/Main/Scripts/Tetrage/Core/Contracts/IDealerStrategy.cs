using System.Collections.Generic;
using Tetrage.Models;

namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// Dealerのターン管理戦略を定義するインターフェース
    /// </summary>
    public interface IDealerStrategy
    {
        #region インターフェースで定義されているメソッド

        /// <summary>
        /// ゲーム開始時の最初のプレイヤーを決定する
        /// </summary>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <returns>最初のターンを持つプレイヤー</returns>
        IPlayer DecideFirstPlayer(IReadOnlyList<IPlayer> players);

        /// <summary>
        /// 現在のプレイヤーから次のターンプレイヤーを決定する
        /// </summary>
        /// <param name="currentPlayer">現在のターンプレイヤー</param>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <returns>次のターンを持つプレイヤー</returns>
        IPlayer GetNextPlayer(IPlayer currentPlayer, IReadOnlyList<IPlayer> players);

        /// <summary>
        /// 特定の条件下での次のプレイヤーを決定する（リーチ時のスキップ等）
        /// </summary>
        /// <param name="currentPlayer">現在のターンプレイヤー</param>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <param name="skipConditions">スキップ条件（任意）</param>
        /// <returns>次のターンを持つプレイヤー</returns>
        IPlayer GetNextPlayerWithConditions(IPlayer currentPlayer, IReadOnlyList<IPlayer> players, TurnSkipConditions skipConditions = null);

        /// <summary>
        /// ターン順序をリセットする（ラウンド開始時等）
        /// </summary>
        /// <param name="players">参加プレイヤーのリスト</param>
        void ResetTurnOrder(IReadOnlyList<IPlayer> players);

        /// <summary>
        /// デッキ（山札）をシャッフルする戦略
        /// </summary>
        /// <param name="stack">シャッフル対象のカードパイル</param>
        void ShuffleDeck(CardPile stack);

        /// <summary>
        /// プレイヤーにカードを配布する戦略
        /// </summary>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <param name="stack">配布元の山札</param>
        /// <param name="cardsPerPlayer">各プレイヤーに配布するカード枚数</param>
        void DistributeCards(IReadOnlyList<IPlayer> players, CardPile stack, int cardsPerPlayer);

        /// <summary>
        /// 各プレイヤーの初期ターゲットカードを設定する戦略
        /// </summary>
        /// <param name="players">参加プレイヤーのリスト</param>
        /// <param name="stack">配布元の山札</param>
        void SetupInitialTargets(IReadOnlyList<IPlayer> players, CardPile stack);

        #endregion
    }

    /// <summary>
    /// ターンスキップの条件を定義するクラス
    /// </summary>
    public class TurnSkipConditions
    {
        /// <summary>リーチ状態のプレイヤーをスキップするか</summary>
        public bool SkipReachPlayers { get; set; } = false;

        /// <summary>手札が空のプレイヤーをスキップするか</summary>
        public bool SkipEmptyHandPlayers { get; set; } = false;

        /// <summary>カスタムスキップ条件（任意）</summary>
        public System.Func<IPlayer, bool> CustomSkipCondition { get; set; }
    }
}