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
                        var othersInt = Photon.Pun.PhotonNetwork.PlayerList.Select(p => p.ActorNumber)
                                                                            .Where(a => a != e.actorPlayerId)
                                                                            .ToArray();

                        if (e.targetCardIds != null && e.targetCardIds.Length > 0)
                        {
                            // [0]: Handsへ（Tmp -> Hands）
                            var selected = e.targetCardIds[0];
                            var movedSelected = new CardMovedEvent
                            {
                                sequence = seq.NextSequence(),
                                stateVersion = seq.NextStateVersion(),
                                cardId = selected.Value,
                                fromPileId = PileIds.PlayerTmp(e.actorPlayerId).Value,
                                toPileId = PileIds.PlayerHands(e.actorPlayerId).Value,
                            };
                            _netCtl.Broadcaster.RaiseToActors(EventCode.CardMoved, movedSelected, othersInt);

                            // [1..n-1]: Stackに戻したカード（Tmp -> Stack）
                            // [n]（存在する場合）: Trashへ送ったカード（Hands -> Trash）
                            var total = e.targetCardIds.Length;
                            if (total >= 2)
                            {
                                for (int i = 1; i < total; i++)
                                {
                                    var cid = e.targetCardIds[i];
                                    bool isLast = (i == total - 1);
                                    bool hasTrash = (total >= 3); // 最後尾はTrashの規約（選択+戻し+Trashの3つ以上）

                                    if (isLast && hasTrash)
                                    {
                                        // Hands -> Trash
                                        var movedToTrash = new CardMovedEvent
                                        {
                                            sequence = seq.NextSequence(),
                                            stateVersion = seq.NextStateVersion(),
                                            cardId = cid.Value,
                                            fromPileId = PileIds.PlayerHands(e.actorPlayerId).Value,
                                            toPileId = PileIds.Trash.Value,
                                        };
                                        _netCtl.Broadcaster.RaiseToActors(EventCode.CardMoved, movedToTrash, othersInt);
                                    }
                                    else
                                    {
                                        // Tmp -> Stack
                                        var movedToStack = new CardMovedEvent
                                        {
                                            sequence = seq.NextSequence(),
                                            stateVersion = seq.NextStateVersion(),
                                            cardId = cid.Value,
                                            fromPileId = PileIds.PlayerTmp(e.actorPlayerId).Value,
                                            toPileId = PileIds.Stack.Value,
                                        };
                                        _netCtl.Broadcaster.RaiseToActors(EventCode.CardMoved, movedToStack, othersInt);
                                    }
                                }
                            }
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
                        // デフォルトでは全プレイヤーに送信
                        _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
                        break;
                    }
            }
        }
    }
}


