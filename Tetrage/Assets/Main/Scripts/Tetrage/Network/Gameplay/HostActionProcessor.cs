using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using System.Linq;

namespace Tetrage.Network.Gameplay
{
    public interface IHostActionProcessor
    {
        void Process(ActionRequestedEvent e);
    }

    /// <summary>
    /// 既定のホスト側アクション処理。現状は簡易acceptで結果配信のみ。
    /// 後にDealer/戦略での検証・適用に差し替える。
    /// </summary>
    public sealed class DefaultHostActionProcessor : IHostActionProcessor
    {
        private readonly IGameplayNetworkController _netCtl;

        public DefaultHostActionProcessor(IGameplayNetworkController netCtl)
        {
            _netCtl = netCtl;
        }

        public void Process(ActionRequestedEvent e)
        {
            var seq = _netCtl.Sequence;

            switch (e.actionType)
            {
                case ActionType.Draw:
                    {
                        if (e.targetCardIds != null && e.targetCardIds.Length > 0)
                        {
                            var selected = e.targetCardIds[0];
                            var moved = new CardMovedEvent
                            {
                                sequence = seq.NextSequence(),
                                stateVersion = seq.NextStateVersion(),
                                cardId = selected.Value,
                                fromPileId = PileIds.PlayerTmp(e.actorPlayerId).Value,
                                toPileId = PileIds.PlayerHands(e.actorPlayerId).Value,
                            };
                            // Others を算出して個別送信（All送信は使わない）
                            var othersInt = Photon.Pun.PhotonNetwork.PlayerList.Select(p => p.ActorNumber)
                                                                                .Where(a => a != e.actorPlayerId)
                                                                                .ToArray();
                            _netCtl.Broadcaster.RaiseToActors(EventCode.CardMoved, moved, othersInt);

                        }

                        var res = new ActionResultEvent
                        {
                            sequence = seq.NextSequence(),
                            clientSequence = e.clientSequence,
                            actorPlayerId = e.actorPlayerId,
                            actionType = e.actionType,
                            accepted = true,
                            reason = string.Empty,
                            targetCardIds = e.targetCardIds,
                        };
                        _netCtl.Broadcaster.RaiseToActor(EventCode.ActionResult, res, e.actorPlayerId);
                        break;
                    }
                default:
                    {
                        var res = new ActionResultEvent
                        {
                            sequence = seq.NextSequence(),
                            clientSequence = e.clientSequence,
                            actorPlayerId = e.actorPlayerId,
                            actionType = e.actionType,
                            accepted = true,
                            reason = string.Empty,
                            targetCardIds = e.targetCardIds,
                        };
                        _netCtl.Broadcaster.RaiseToActor(EventCode.ActionResult, res, e.actorPlayerId);
                        break;
                    }
            }
        }
    }
}


