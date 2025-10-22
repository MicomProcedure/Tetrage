using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.Core.DTO;

namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// 旧 IDealerStrategy の代替。モデルを直接変更せず、適用すべき効果（プラン）を返す。
    /// </summary>
    public interface IDealerPlanner
    {
        /// <summary>最初のプレイヤーを決定</summary>
        IPlayer DecideFirstPlayer(IReadOnlyList<IPlayer> players);

        /// <summary>次のプレイヤーを決定</summary>
        IPlayer GetNextPlayer(IPlayer currentPlayer, IReadOnlyList<IPlayer> players);

        /// <summary>条件付き次のプレイヤー</summary>
        IPlayer GetNextPlayerWithConditions(IPlayer currentPlayer, IReadOnlyList<IPlayer> players, TurnSkipConditions skipConditions = null);

        /// <summary>
        /// ターン順リセットを効果（TurnOrder）として返す（副作用なし）
        /// </summary>
        DealerPlan PlanResetTurnOrder(IReadOnlyList<IPlayer> players);

        /// <summary>
        /// 山札の並び替えを効果として返す（副作用なし）
        /// </summary>
        DealerPlan PlanShuffleDeck(CardPile stack);

        /// <summary>
        /// 初期ターゲット設定を効果として返す（副作用なし）
        /// </summary>
        DealerPlan PlanTargetSetup(IReadOnlyList<IPlayer> players, CardPile stack);

        /// <summary>
        /// 初期配布を効果として返す（副作用なし）
        /// </summary>
        DealerPlan PlanDistribution(IReadOnlyList<IPlayer> players, CardPile stack, int cardsPerPlayer);
    }
}


