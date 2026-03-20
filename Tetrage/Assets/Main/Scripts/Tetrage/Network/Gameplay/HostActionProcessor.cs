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
        private readonly IPlayerIdMapper _playerIdMapper;

        public DefaultHostActionProcessor(IGameplayNetworkController netCtl, IPlayerIdMapper playerIdMapper)
        {
            _netCtl = netCtl;
            _playerIdMapper = playerIdMapper;
        }

        /// <summary>
        /// 指定ActorNumber以外の全ActorNumberを取得する
        /// </summary>
        private int[] GetOtherActorNumbers(int excludeActorNumber)
        {
            return _playerIdMapper.GetAllActorNumbers()
                .Where(a => a != excludeActorNumber)
                .ToArray();
        }

        public void Process(ActionRequestedEvent e)
        {
            var seq = _netCtl.Sequence;
            Debug.Log($"HostActionProcessor: Process {e.actionType}, ActorPlayerId: {e.actorPlayerId}, TargetCardIds: {string.Join(", ", e.targetCardIds ?? System.Array.Empty<int>())}");
            switch (e.actionType)
            {
                case ActionType.Draw:
                    ProcessDraw(e, seq);
                    break;
                case ActionType.TetrageSolo:
                    ProcessTetrageSolo(e, seq);
                    break;
                default:
                    ProcessDefault(e, seq);
                    break;
            }
        }

        #region アクション処理

        /// <summary>
        /// Drawアクション処理。
        /// targetCardIdsの順序:
        ///   [0]: Handsへ（Stack -> Hands）
        ///   [1]: Stackに戻したカード（戻した順）
        ///   [2]: Trashに送ったカード（送った順）
        /// </summary>
        private void ProcessDraw(ActionRequestedEvent e, SequenceService seq)
        {
            var othersInt = GetOtherActorNumbers(e.actorPlayerId);
            Debug.Log($"HostActionProcessor: Process Draw, ActorPlayerId: {e.actorPlayerId}, Others: [{string.Join(", ", othersInt)}]");

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

                // [1]: Stackへ戻したカード — Stack内移動は省略
                // [2]: Trashへ送ったカード（Hands -> Trash）
                var total = validIds.Length;
                if (total >= 3)
                {
                    var lastIdx = total - 1;
                    var trashCardId = new CardId(validIds[lastIdx]);
                    var movedToTrash = new CardMovedEvent
                    {
                        sequence = seq.NextSequence(),
                        stateVersion = seq.NextStateVersion(),
                        cardId = trashCardId.Value,
                        fromPileId = PileIds.PlayerHands(e.actorPlayerId).Value,
                        toPileId = PileIds.Trash.Value,
                    };
                    _netCtl.Broadcaster.RaiseToActors(EventCode.CardMoved, movedToTrash, othersInt);
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
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        private void ProcessTetrageSolo(ActionRequestedEvent e, SequenceService seq)
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
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        private void ProcessDefault(ActionRequestedEvent e, SequenceService seq)
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
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        #endregion
    }
}


