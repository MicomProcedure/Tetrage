using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Tetrage.Core.Contracts;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Managers.DealerStrategies;
using Tetrage.Models;
using Tetrage.Tests.Data;

namespace Tetrage.Tests
{
    /// <summary>
    /// GameScene直起動デバッグ用に、Inspector指定の初期Target/HandカードをDealerPlanへ変換する。
    /// </summary>
    public sealed class GameSceneDebugInitialCardDealerPlanner : IDealerPlanner
    {
        #region Fields

        private static readonly DeckId DefaultDeckId = new(1);

        private readonly IReadOnlyList<DebugPlayerInfo> _debugPlayerInfos;
        private readonly IDealerPlanner _fallbackPlanner;

        #endregion

        #region Constructor

        /// <summary>
        /// 指定カード設定と委譲先Plannerを受け取る。
        /// </summary>
        public GameSceneDebugInitialCardDealerPlanner(
            IReadOnlyList<DebugPlayerInfo> debugPlayerInfos,
            IDealerPlanner fallbackPlanner = null)
        {
            _debugPlayerInfos = debugPlayerInfos ?? new List<DebugPlayerInfo>();
            _fallbackPlanner = fallbackPlanner ?? new RealDealerPlanner();
        }

        #endregion

        #region IDealerPlanner

        public IPlayer DecideFirstPlayer(IReadOnlyList<IPlayer> players)
        {
            return _fallbackPlanner.DecideFirstPlayer(players);
        }

        public IPlayer GetNextPlayer(IPlayer currentPlayer, IReadOnlyList<IPlayer> players)
        {
            return _fallbackPlanner.GetNextPlayer(currentPlayer, players);
        }

        public IPlayer GetNextPlayerWithConditions(IPlayer currentPlayer, IReadOnlyList<IPlayer> players, TurnSkipConditions skipConditions = null)
        {
            return _fallbackPlanner.GetNextPlayerWithConditions(currentPlayer, players, skipConditions);
        }

        public DealerPlan PlanResetTurnOrder(IReadOnlyList<IPlayer> players)
        {
            return _fallbackPlanner.PlanResetTurnOrder(players);
        }

        public DealerPlan PlanShuffleDeck(CardPile stack)
        {
            return _fallbackPlanner.PlanShuffleDeck(stack);
        }

        public DealerPlan PlanTargetSetup(IReadOnlyList<IPlayer> players, CardPile stack)
        {
            if (players == null || stack == null || _debugPlayerInfos.Count == 0)
            {
                return _fallbackPlanner.PlanTargetSetup(players, stack);
            }

            var moves = new List<CardMoveEffect>();
            var reservedCardIds = new HashSet<CardId>();
            var targetAssignedPlayerIds = new HashSet<PlayerId>();

            foreach (var player in players)
            {
                var debugInfo = GetDebugInfo(player);
                if (debugInfo == null || !debugInfo.SetInitialCards)
                {
                    continue;
                }

                TryAddTargetMove(player, stack, debugInfo, moves, reservedCardIds, targetAssignedPlayerIds);
                AddHandMoves(player, stack, debugInfo, moves, reservedCardIds);
            }

            AddFallbackTargetMoves(players, stack, moves, reservedCardIds, targetAssignedPlayerIds);

            return new DealerPlan
            {
                Moves = moves,
                Visibility = System.Array.Empty<VisibilityEffect>()
            };
        }

        public DealerPlan PlanDistribution(IReadOnlyList<IPlayer> players, CardPile stack, int cardsPerPlayer)
        {
            return _fallbackPlanner.PlanDistribution(players, stack, cardsPerPlayer);
        }

        #endregion

        #region Private Methods

        private DebugPlayerInfo GetDebugInfo(IPlayer player)
        {
            int index = player.Id.Value - 1;
            return index >= 0 && index < _debugPlayerInfos.Count
                ? _debugPlayerInfos[index]
                : null;
        }

        private static void TryAddTargetMove(
            IPlayer player,
            CardPile stack,
            DebugPlayerInfo debugInfo,
            List<CardMoveEffect> moves,
            HashSet<CardId> reservedCardIds,
            HashSet<PlayerId> targetAssignedPlayerIds)
        {
            if (debugInfo.TargetCard == null || !debugInfo.TargetCard.IsValid)
            {
                Debug.LogWarning($"GameSceneDebugInitialCardDealerPlanner: Player {player.Id} のTargetCard指定が無効です。");
                return;
            }

            if (!TryFindStackCard(stack, debugInfo.TargetCard, reservedCardIds, out var card))
            {
                Debug.LogWarning($"GameSceneDebugInitialCardDealerPlanner: Player {player.Id} のTargetCardが山札に見つかりません ({debugInfo.TargetCard.Suit} {debugInfo.TargetCard.Number})");
                return;
            }

            AddMove(stack.Id, player.Target.Id, card.Id, moves, reservedCardIds);
            targetAssignedPlayerIds.Add(player.Id);
        }

        private static void AddHandMoves(
            IPlayer player,
            CardPile stack,
            DebugPlayerInfo debugInfo,
            List<CardMoveEffect> moves,
            HashSet<CardId> reservedCardIds)
        {
            if (debugInfo.HandCards == null)
            {
                return;
            }

            foreach (var handCard in debugInfo.HandCards.Take(3))
            {
                if (handCard == null || !handCard.IsValid)
                {
                    Debug.LogWarning($"GameSceneDebugInitialCardDealerPlanner: Player {player.Id} のHandCard指定が無効です。");
                    continue;
                }

                if (!TryFindStackCard(stack, handCard, reservedCardIds, out var card))
                {
                    Debug.LogWarning($"GameSceneDebugInitialCardDealerPlanner: Player {player.Id} のHandCardが山札に見つかりません ({handCard.Suit} {handCard.Number})");
                    continue;
                }

                AddMove(stack.Id, player.Hands.Id, card.Id, moves, reservedCardIds);
            }
        }

        private static void AddFallbackTargetMoves(
            IReadOnlyList<IPlayer> players,
            CardPile stack,
            List<CardMoveEffect> moves,
            HashSet<CardId> reservedCardIds,
            HashSet<PlayerId> targetAssignedPlayerIds)
        {
            foreach (var player in players)
            {
                if (targetAssignedPlayerIds.Contains(player.Id))
                {
                    continue;
                }

                var card = stack.Cards.FirstOrDefault(c => !reservedCardIds.Contains(c.Id));
                if (card == null)
                {
                    Debug.LogWarning($"GameSceneDebugInitialCardDealerPlanner: Player {player.Id} へ配るTargetCardが不足しています。");
                    continue;
                }

                AddMove(stack.Id, player.Target.Id, card.Id, moves, reservedCardIds);
                targetAssignedPlayerIds.Add(player.Id);
            }
        }

        private static bool TryFindStackCard(
            CardPile stack,
            CardSpec cardSpec,
            HashSet<CardId> reservedCardIds,
            out Card card)
        {
            var expectedCardId = CardIdComposer.Compose(DefaultDeckId, (int)cardSpec.Suit, cardSpec.Number);
            card = stack.Cards.FirstOrDefault(c => c.Id.Equals(expectedCardId) && !reservedCardIds.Contains(c.Id));
            return card != null;
        }

        private static void AddMove(
            PileId fromPileId,
            PileId toPileId,
            CardId cardId,
            List<CardMoveEffect> moves,
            HashSet<CardId> reservedCardIds)
        {
            moves.Add(new CardMoveEffect
            {
                CardId = cardId,
                FromPileId = fromPileId,
                ToPileId = toPileId
            });
            reservedCardIds.Add(cardId);
        }

        #endregion
    }
}
