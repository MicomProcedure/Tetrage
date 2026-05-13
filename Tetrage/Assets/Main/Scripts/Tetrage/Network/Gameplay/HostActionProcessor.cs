using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Core.Contracts;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Tetrage.Network.Gameplay
{
    public interface IHostActionProcessor
    {
        void Process(ActionRequestedEventPacket e);
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

        private IPlayer ResolvePlayer(int actorNumber)
        {
            if (_playerIdMapper == null || _netCtl?.GameContext?.Players == null)
            {
                return null;
            }

            if (!_playerIdMapper.TryGetPlayerId(actorNumber, out var playerId))
            {
                return null;
            }

            return _netCtl.GameContext.Players.FirstOrDefault(player => player.Id == playerId);
        }

        private Tetrage.Models.Card GetTargetCard(IPlayer player)
        {
            return player?.Target?.FirstOrDefault();
        }

        private string BuildWinnersReason(IEnumerable<int> winnerActorNumbers)
        {
            return $"winners:{string.Join(",", winnerActorNumbers)}";
        }

        private bool TryResolvePlayerId(int actorNumber, out PlayerId playerId)
        {
            playerId = default;
            if (!_playerIdMapper.TryGetPlayerId(actorNumber, out var resolvedPlayerId))
            {
                Debug.LogError($"HostActionProcessor: ActorNumber {actorNumber} のPlayerId変換に失敗しました");
                return false;
            }

            playerId = resolvedPlayerId;
            return true;
        }

        public void Process(ActionRequestedEventPacket e)
        {
            var seq = _netCtl.Sequence;
            Debug.Log($"HostActionProcessor: Process {e.actionType}, ActorPlayerId: {e.actorPlayerId}, TargetCardIds: {string.Join(", ", e.targetCardIds ?? System.Array.Empty<int>())}");
            if (!TryResolvePlayerId(e.actorPlayerId, out var actorPlayerId))
            {
                return;
            }

            switch (e.actionType)
            {
                case ActionType.Draw:
                    ProcessDraw(e, actorPlayerId, seq);
                    break;
                case ActionType.Check:
                    ProcessCheck(e, seq);
                    break;
                case ActionType.TetrageSolo:
                    ProcessTetrageSolo(e, seq);
                    break;
                case ActionType.TetrageMulti:
                    ProcessTetrageMulti(e, seq);
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
        private void ProcessDraw(ActionRequestedEventPacket e, PlayerId actorPlayerId, SequenceService seq)
        {
            var othersInt = GetOtherActorNumbers(e.actorPlayerId);
            Debug.Log($"HostActionProcessor: Process Draw, ActorPlayerId: {e.actorPlayerId}, Others: [{string.Join(", ", othersInt)}]");

            var validIds = e.targetCardIds?.Where(id => id != 0).ToArray();
            if (validIds != null && validIds.Length > 0)
            {
                // [0]: Handsへ（Stack -> Hands）
                var selected = new CardId(validIds[0]);
                var movedSelected = new CardMovedEventPacket
                {
                    sequence = seq.NextSequence(),
                    stateVersion = seq.NextStateVersion(),
                    cardId = selected.Value,
                    fromPileId = PileIds.Stack.Value,
                    toPileId = PileIds.PlayerHands(actorPlayerId.Value).Value,
                };
                _netCtl.Broadcaster.RaiseToActors(EventCode.CardMoved, movedSelected, othersInt);

                // [1]: Stackへ戻したカード — Stack内移動は省略
                // [2]: Trashへ送ったカード（Hands -> Trash）
                var total = validIds.Length;
                if (total >= 3)
                {
                    var lastIdx = total - 1;
                    var trashCardId = new CardId(validIds[lastIdx]);
                    var movedToTrash = new CardMovedEventPacket
                    {
                        sequence = seq.NextSequence(),
                        stateVersion = seq.NextStateVersion(),
                        cardId = trashCardId.Value,
                        fromPileId = PileIds.PlayerHands(actorPlayerId.Value).Value,
                        toPileId = PileIds.Trash.Value,
                    };
                    _netCtl.Broadcaster.RaiseToActors(EventCode.CardMoved, movedToTrash, othersInt);
                }
            }

            var res = new ActionResultEventPacket
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

        private void ProcessCheck(ActionRequestedEventPacket e, SequenceService seq)
        {
            var isMatch = e.actionStatusInt == 1;

            // Checkに成功した場合は、相手のTargetカードを表示する。
            if (isMatch && e.targetCardIds != null && e.targetCardIds.Length > 0)
            {
                var targetCardStateChanged = new CardStateChangedEventPacket
                {
                    sequence = seq.NextSequence(),
                    stateVersion = seq.NextStateVersion(),
                    cardId = e.targetCardIds[0],
                    stateCode = CardStateCode.IsSuitVisible,
                    stateValue = true,
                };
                _netCtl.Broadcaster.Raise(EventCode.CardVisibilityChanged, targetCardStateChanged);
            }
            else{
                // TODO: Checkに失敗した場合はそのSuitではないという情報をCardModelに反映させるイベントを発行させる
            }

            var res = new ActionResultEventPacket
            {
                sequence = seq.NextSequence(),
                clientSequence = e.clientSequence,
                actorPlayerId = e.actorPlayerId,
                actionType = e.actionType,
                accepted = true,
                reason = string.Empty,
                targetCardIds = e.targetCardIds,
                actionStatusInt = e.actionStatusInt,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        private void ProcessTetrageSolo(ActionRequestedEventPacket e, SequenceService seq)
        {
            var requester = ResolvePlayer(e.actorPlayerId);
            var requesterCard = GetTargetCard(requester);
            if (requester == null || requesterCard == null)
            {
                var fallback = new ActionResultEventPacket
                {
                    sequence = seq.NextSequence(),
                    clientSequence = e.clientSequence,
                    actorPlayerId = e.actorPlayerId,
                    actionType = e.actionType,
                    accepted = false,
                    reason = "勝利判定に必要なターゲット情報を取得できません",
                    targetCardIds = e.targetCardIds,
                    actionStatusInt = 0,
                };
                _netCtl.Broadcaster.Raise(EventCode.ActionResult, fallback);
                return;
            }

            var matchedActorNumbers = _netCtl.GameContext.Players
                .Where(player => player.PlayerId != requester.PlayerId)
                .Where(player =>
                {
                    var card = GetTargetCard(player);
                    return card != null && card.Suit == requesterCard.Suit;
                })
                .Select(player =>
                {
                    return _playerIdMapper.TryGetActorNumber(player.Id, out var actorNumber) ? actorNumber : -1;
                })
                .Where(actor => actor > 0)
                .Distinct()
                .ToList();

            var isSuccess = matchedActorNumbers.Count == 0;
            var winnerActorNumbers = new List<int>();
            if (isSuccess)
            {
                winnerActorNumbers.Add(e.actorPlayerId);
            }
            else
            {
                var loserSet = new HashSet<int>(matchedActorNumbers) { e.actorPlayerId };
                winnerActorNumbers.AddRange(_playerIdMapper.GetAllActorNumbers().Where(actor => !loserSet.Contains(actor)));
            }

            var res = new ActionResultEventPacket
            {
                sequence = seq.NextSequence(),
                clientSequence = e.clientSequence,
                actorPlayerId = e.actorPlayerId,
                actionType = e.actionType,
                accepted = true,
                reason = BuildWinnersReason(winnerActorNumbers),
                targetCardIds = e.targetCardIds,
                actionStatusInt = isSuccess ? 1 : 0,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        private void ProcessTetrageMulti(ActionRequestedEventPacket e, SequenceService seq)
        {
            var requester = ResolvePlayer(e.actorPlayerId);
            var requesterCard = GetTargetCard(requester);
            if (requester == null || requesterCard == null)
            {
                var fallback = new ActionResultEventPacket
                {
                    sequence = seq.NextSequence(),
                    clientSequence = e.clientSequence,
                    actorPlayerId = e.actorPlayerId,
                    actionType = e.actionType,
                    accepted = false,
                    reason = "勝利判定に必要なターゲット情報を取得できません",
                    targetCardIds = e.targetCardIds,
                    actionStatusInt = 0,
                };
                _netCtl.Broadcaster.Raise(EventCode.ActionResult, fallback);
                return;
            }

            IPlayer selectedPlayer = null;
            if (e.targetCardIds != null && e.targetCardIds.Length > 0)
            {
                var selectedCardId = e.targetCardIds[0];
                selectedPlayer = _netCtl.GameContext.Players
                    .FirstOrDefault(player =>
                    {
                        if (player.PlayerId == requester.PlayerId) return false;
                        var targetCard = GetTargetCard(player);
                        return targetCard != null && targetCard.Id.Value == selectedCardId;
                    });
            }

            var selectedCard = GetTargetCard(selectedPlayer);
            var isSuccess = selectedCard != null && selectedCard.Suit == requesterCard.Suit;

            var winnerActorNumbers = new List<int>();
            if (isSuccess && _playerIdMapper.TryGetActorNumber(selectedPlayer.Id, out var selectedActorNumber))
            {
                winnerActorNumbers.Add(e.actorPlayerId);
                winnerActorNumbers.Add(selectedActorNumber);
            }
            else
            {
                winnerActorNumbers.AddRange(GetOtherActorNumbers(e.actorPlayerId));
            }

            var res = new ActionResultEventPacket
            {
                sequence = seq.NextSequence(),
                clientSequence = e.clientSequence,
                actorPlayerId = e.actorPlayerId,
                actionType = e.actionType,
                accepted = true,
                reason = BuildWinnersReason(winnerActorNumbers.Distinct()),
                targetCardIds = e.targetCardIds,
                actionStatusInt = isSuccess ? 1 : 0,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        private void ProcessDefault(ActionRequestedEventPacket e, SequenceService seq)
        {
            var res = new ActionResultEventPacket
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

        #endregion
    }
}


