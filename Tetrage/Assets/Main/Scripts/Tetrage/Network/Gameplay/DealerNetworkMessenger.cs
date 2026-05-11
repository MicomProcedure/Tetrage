using System.Collections.Generic;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// Dealerの進行・制御イベントを一元管理して送信するメッセンジャー。
    /// DealerPlanの内容を個別のネットワークイベントに分解して送信する。
    /// PlayerId→ActorNumber変換はこのクラスで行う。
    /// ターン開始・終了・ゲーム終了・スキャンフェーズ開始・終了・ゲーム終了はこのクラスで送信する。
    /// </summary>
    public sealed class DealerNetworkMessenger
    {
        private readonly INetworkBroadcaster _broadcaster;
        private readonly SequenceService _seq;
        private readonly IPlayerIdMapper _playerIdMapper;

        public DealerNetworkMessenger(INetworkBroadcaster broadcaster, SequenceService sequenceService, IPlayerIdMapper playerIdMapper)
        {
            _broadcaster = broadcaster;
            _seq = sequenceService ?? new SequenceService();
            _playerIdMapper = playerIdMapper;
        }

        #region ライフサイクル・ターン進行

        public void PublishTurnStarted(PlayerId currentPlayerId)
        {
            var currentPlayerActorNumber = GetActorNumber(currentPlayerId);

            var e = new TurnStartedEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
                currentPlayerActorNumber = currentPlayerActorNumber
            };
            _broadcaster.Raise(EventCode.TurnStarted, e);
        }

        public void PublishTurnEnded(PlayerId previousPlayerId)
        {
            var previousPlayerActorNumber = GetActorNumber(previousPlayerId);

            var e = new TurnEndedEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
                previousPlayerActorNumber = previousPlayerActorNumber
            };
            _broadcaster.Raise(EventCode.TurnEnded, e);
        }

        public void PublishGameEnded(List<PlayerId> winnerPlayerIds)
        {
            var winnerActorNumbers = GetActorNumbers(winnerPlayerIds);
            var e = new GameEndedEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
                winnerActorNumbers = winnerActorNumbers
            };
            _broadcaster.Raise(EventCode.GameEnded, e);
        }

        public void PublishScanPhaseStart(PlayerId userPlayerId, List<PlayerId> playerIds)
        {
            var userPlayerActorNumber = GetActorNumber(userPlayerId);
            var playerActorNumbers = GetActorNumbers(playerIds);
            var e = new StartScanPhaseEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
                userPlayerActorNumber = userPlayerActorNumber,
                playerActorNumbers = playerActorNumbers,
            };
            _broadcaster.Raise(EventCode.StartScanPhase, e);
        }

        public void PublishScanPhaseEnd()
        {
            var e = new EndScanPhaseEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
            };
            _broadcaster.Raise(EventCode.EndScanPhase, e);
        }

        public void PublishScanResultToActor(PlayerId receiverPlayerId, PlayerId targetPlayerId, Suit targetSuit)
        {
            var receiverActorNumber = GetActorNumber(receiverPlayerId);
            var targetActorNumber = GetActorNumber(targetPlayerId);
            var e = new ScanResultEvent
            {
                sequence = _seq.NextSequence(),
                targetActorNumber = targetActorNumber,
                targetSuit = (int)targetSuit,
            };
            _broadcaster.RaiseToActor(EventCode.ScanResult, e, receiverActorNumber);
        }

        public void PublishFinishingGame(List<PlayerId> winnerPlayerIds)
        {
            var winnerActorNumbers = GetActorNumbers(winnerPlayerIds);
            var e = new FinishingGameEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
                winnerActorNumbers = winnerActorNumbers,
            };
            _broadcaster.Raise(EventCode.FinishingGame, e);
        }

        #endregion

        #region DealerPlan (カード操作・順序)

        /// <summary>
        /// DealerPlanの内容を個別のネットワークイベントに分解して送信する
        /// </summary>
        public void PublishDealerPlan(DealerPlan plan)
        {
            // 1. シャッフルシード
            if (plan.ShuffleSeeds != null)
            {
                foreach (var s in plan.ShuffleSeeds)
                {
                    PublishPileShuffleSeed(s.PileId.Value, s.Seed);
                }
            }

            // 2. ターン順序
            if (plan.TurnOrder != null && plan.TurnOrder.Count > 0)
            {
                PublishTurnOrder(plan.TurnOrder);
            }

            // 3. カード移動
            if (plan.Moves != null)
            {
                foreach (var m in plan.Moves)
                {
                    var dto = new CardMovedEvent
                    {
                        sequence = _seq.NextSequence(),
                        stateVersion = _seq.NextStateVersion(),
                        cardId = m.CardId.Value,
                        fromPileId = m.FromPileId.Value,
                        toPileId = m.ToPileId.Value,
                    };
                    _broadcaster.Raise(EventCode.CardMoved, dto);
                }
            }

            // 4. 可視性変更
            if (plan.Visibility != null)
            {
                foreach (var v in plan.Visibility)
                {
                    var dto = new CardStateChangedEvent
                    {
                        sequence = _seq.NextSequence(),
                        stateVersion = _seq.NextStateVersion(),
                        cardId = v.CardId.Value,
                        stateCode = CardStateCode.FaceUp,
                        stateValue = v.IsVisible,
                    };
                    _broadcaster.Raise(EventCode.CardVisibilityChanged, dto);
                }
            }
        }

        private void PublishPileShuffleSeed(int pileId, int seed)
        {
            var dto = new PileShuffledWithSeedEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
                pileId = pileId,
                seed = seed,
            };
            _broadcaster.Raise(EventCode.PileShuffledWithSeed, dto);
        }

        private void PublishTurnOrder(IReadOnlyList<TurnOrderEffect> turnOrder)
        {
            // orderで昇順にソートしてからActorNumber配列に変換
            var temp = new List<TurnOrderEffect>(turnOrder);
            temp.Sort((a, b) => a.Order.CompareTo(b.Order));

            var ids = new int[temp.Count];
            for (int i = 0; i < temp.Count; i++) ids[i] = temp[i].PlayerId.Value;

            var dto = new ListOrderDeclaredEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
                idKind = ListOrderIdKind.PlayerId,
                listKey = ListOrderKey.TurnOrder,
                orderedIds = ids,
            };
            _broadcaster.Raise(EventCode.ListOrderDeclared, dto);
        }

        #endregion

        #region Private Methods

        private int GetActorNumber(PlayerId playerId)
        {
            if (!_playerIdMapper.TryGetActorNumber(playerId, out var actorNumber))
            {
                UnityEngine.Debug.LogWarning($"DealerNetworkMessenger: PlayerId {playerId} のマッピングが見つかりません");
                return -1;
            }
            return actorNumber;
        }

        private int[] GetActorNumbers(List<PlayerId> playerIds)
        {
            var actorNumbers = new int[playerIds.Count];
            for (int i = 0; i < playerIds.Count; i++)
            {
                actorNumbers[i] = GetActorNumber(playerIds[i]);
            }
            return actorNumbers;
        }

        #endregion
    }
}

