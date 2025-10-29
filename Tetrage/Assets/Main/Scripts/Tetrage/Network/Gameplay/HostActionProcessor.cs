using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using System.Linq;
using UnityEngine;
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
            Debug.Log($"HostActionProcessor: Process {e.actionType}, ActorPlayerId: {e.actorPlayerId}, TargetCardIds: {string.Join(", ", e.targetCardIds ?? System.Array.Empty<int>())}");
            switch (e.actionType)
            {
                case ActionType.Draw:
                    {
                        // targetCardIdsの順序:
                        // [0]: Handsへ（Stack -> Hands）
                        // [1]: Stackに戻したカード（戻した順）
                        // [2]: Trashに送ったカード（送った順）

                        Debug.Log($"HostActionProcessor: Process Draw, ActorPlayerId: {e.actorPlayerId}, TargetCardIds: {string.Join(", ", e.targetCardIds ?? System.Array.Empty<int>())}");
                        var othersInt = Photon.Pun.PhotonNetwork.PlayerList.Select(p => p.ActorNumber)
                                                                            .Where(a => a != e.actorPlayerId)
                                                                            .ToArray();
                        Debug.Log($"HostActionProcessor: OthersInt: {string.Join(", ", othersInt)}");

                        var validIds = e.targetCardIds?.Where(id => id != 0).ToArray();
                        if (validIds != null && validIds.Length > 0)
                        {
                            // [0]: Handsへ（Stack -> Hands）
                            var selected = new CardId(validIds[0]);
                            var movedSelected = new CardMovedEvent
                            {
                                sequence = seq.NextSequence(),
                                stateVersion = seq.NextStateVersion(),
                                cardId = selected.Value,
                                fromPileId = PileIds.Stack.Value,
                                toPileId = PileIds.PlayerHands(e.actorPlayerId).Value,
                            };
                            _netCtl.Broadcaster.RaiseToActors(EventCode.CardMoved, movedSelected, othersInt);

                            // [1]: Stackへ戻したカード（Stack -> Stack）
                            // [2]: Trashへ送ったカード（Hands -> Trash）
                            var total = validIds.Length;
                            if (total >= 2)
                            {
                                for (int i = 1; i < total; i++)
                                {
                                    var cid = new CardId(validIds[i]);
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
                                        // Stack -> Stack（意味がないのでコメントアウト）
                                        // var movedToStack = new CardMovedEvent
                                        // {
                                        //     sequence = seq.NextSequence(),
                                        //     stateVersion = seq.NextStateVersion(),
                                        //     cardId = cid.Value,
                                        //     fromPileId = PileIds.Stack.Value,
                                        //     toPileId = PileIds.Stack.Value,
                                        // };
                                        // _netCtl.Broadcaster.RaiseToActors(EventCode.CardMoved, movedToStack, othersInt);
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
                            actionStatusInt = 0,
                        };
                        // アクション結果は全プレイヤーに送信
                        _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
                        break;
                    }
                case ActionType.TetrageSolo:
                    {
                        var res = new ActionResultEvent
                        {
                            sequence = seq.NextSequence(),
                            clientSequence = e.clientSequence,
                            actorPlayerId = e.actorPlayerId,
                            actionType = e.actionType,
                            accepted = true,
                            reason = e.actionStatusInt == 1 ? $"{e.actorPlayerId} 勝利" : "敗北",
                            targetCardIds = e.targetCardIds,
                        };
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


