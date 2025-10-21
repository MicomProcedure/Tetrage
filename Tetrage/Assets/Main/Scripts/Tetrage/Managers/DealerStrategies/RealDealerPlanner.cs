using System.Collections.Generic;
using UnityEngine;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.Core.DTO;

namespace Tetrage.Managers.DealerStrategies
{
    /// <summary>
    /// 本番用のプランナー実装。モデルを直接変更せず、適用すべき効果（DealerPlan）を返す。
    /// </summary>
    public class RealDealerPlanner : IDealerPlanner
    {
        #region 内部状態（ターン順管理）
        private IPlayer _firstPlayer;
        private List<IPlayer> _turnOrder;
        private int _currentTurnIndex;
        #endregion

        #region IDealerPlanner 実装
        public IPlayer DecideFirstPlayer(IReadOnlyList<IPlayer> players)
        {
            if (players == null || players.Count == 0)
            {
                throw new System.SystemException("RealDealerPlanner: players is null or empty");
            }

            if (_turnOrder == null || _turnOrder.Count == 0)
            {
                _ = PlanResetTurnOrder(players); // 内部状態を初期化
            }

            _firstPlayer = _turnOrder[0];
            Debug.Log($"RealDealerPlanner: 最初のプレイヤーは Player {_firstPlayer.PlayerId}");
            return _firstPlayer;
        }

        public IPlayer GetNextPlayer(IPlayer currentPlayer, IReadOnlyList<IPlayer> players)
        {
            if (players == null || players.Count == 0)
            {
                throw new System.SystemException("RealDealerPlanner: players is null or empty");
            }

            if (players.Count == 1)
            {
                return players[0];
            }

            if (_turnOrder == null || _turnOrder.Count == 0)
            {
                DecideFirstPlayer(players);
            }

            _currentTurnIndex = (_currentTurnIndex + 1) % _turnOrder.Count;
            var next = _turnOrder[_currentTurnIndex];
            Debug.Log($"RealDealerPlanner: 次のプレイヤーは Player {next.PlayerId}");
            return next;
        }

        public IPlayer GetNextPlayerWithConditions(IPlayer currentPlayer, IReadOnlyList<IPlayer> players, TurnSkipConditions skipConditions = null)
        {
            // 本番用の最小実装: 条件は無視して次プレイヤーを返す
            return GetNextPlayer(currentPlayer, players);
        }

        public IReadOnlyList<IPlayer> PlanResetTurnOrder(IReadOnlyList<IPlayer> players)
        {
            var list = new List<IPlayer>(players);
            for (int i = 0; i < list.Count; i++)
            {
                var temp = list[i];
                int r = UnityEngine.Random.Range(i, list.Count);
                list[i] = list[r];
                list[r] = temp;
            }
            _turnOrder = list;
            _currentTurnIndex = 0;
            _firstPlayer = _turnOrder[0];
            return _turnOrder.AsReadOnly();
        }

        public DealerPlan PlanShuffleDeck(CardPile stack)
        {
            // 決定論シャッフル用のSeedを配布するプランを返す（実適用はApplier側で行う）
            var seeds = new List<PileShuffleSeedEffect>();
            if (stack != null)
            {
                // Hostで決定したseed（任意）。ここではUnityの乱数から取得。
                int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                seeds.Add(new PileShuffleSeedEffect
                {
                    PileId = stack.Id,
                    Seed = seed
                });
            }

            return new DealerPlan
            {
                Moves = System.Array.Empty<CardMoveEffect>(),
                Visibility = System.Array.Empty<VisibilityEffect>(),
                ShuffleSeeds = seeds
            };
        }

        public DealerPlan PlanTargetSetup(IReadOnlyList<IPlayer> players, CardPile stack)
        {
            var moves = new List<CardMoveEffect>();
            if (players == null || stack == null) return new DealerPlan { Moves = moves, Visibility = System.Array.Empty<VisibilityEffect>() };

            int takeIndex = 0;
            var cards = stack.Cards; // 現在の順序の参照

            foreach (var p in players)
            {
                if (takeIndex >= cards.Count) break;
                var c = cards[takeIndex++];
                moves.Add(new CardMoveEffect
                {
                    CardId = c.Id,
                    FromPileId = stack.Id,
                    ToPileId = p.Target.Id
                });
            }

            return new DealerPlan
            {
                Moves = moves,
                Visibility = System.Array.Empty<VisibilityEffect>()
            };
        }

        public DealerPlan PlanDistribution(IReadOnlyList<IPlayer> players, CardPile stack, int cardsPerPlayer)
        {
            var moves = new List<CardMoveEffect>();
            if (players == null || stack == null || cardsPerPlayer <= 0)
            {
                return new DealerPlan { Moves = moves, Visibility = System.Array.Empty<VisibilityEffect>() };
            }

            int takeIndex = 0;
            var cards = stack.Cards;

            for (int i = 0; i < cardsPerPlayer; i++)
            {
                for (int p = 0; p < players.Count; p++)
                {
                    if (takeIndex >= cards.Count)
                    {
                        Debug.LogWarning("RealDealerPlanner: 山札が不足しています");
                        break;
                    }
                    var card = cards[takeIndex++];
                    moves.Add(new CardMoveEffect
                    {
                        CardId = card.Id,
                        FromPileId = stack.Id,
                        ToPileId = players[p].Hands.Id
                    });
                }
            }

            return new DealerPlan
            {
                Moves = moves,
                Visibility = System.Array.Empty<VisibilityEffect>()
            };
        }
        #endregion
    }
}

