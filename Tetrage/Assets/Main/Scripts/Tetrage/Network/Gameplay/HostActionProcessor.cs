using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tetrage.Core.Actions;
using Tetrage.Core.Constants;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Core.Events;
using Tetrage.Core.Ids;
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
            Debug.Log($"HostActionProcessor: Process {e.actionType}, ActorPlayerId: {e.actorPlayerId}, TargetIds: {string.Join(", ", e.targetIds ?? System.Array.Empty<int>())}");
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
                    // 参加応答パケットはEventBusへ既に流れているため、StartRequest のみオーケストレーションを開始する
                    if (e.actionStatusInt == InGameConsts.TetrageMultiStatus.ResponseOpen
                        || e.actionStatusInt == InGameConsts.TetrageMultiStatus.ResponseDecline)
                        break;
                    ProcessTetrageMultiAsync(e, seq).Forget();
                    break;
                default:
                    ProcessDefault(e, seq);
                    break;
            }
        }

        #region アクション処理

        /// <summary>
        /// Drawアクション処理。
        /// targetIdsの順序（CardId.Value）:
        ///   [0]: Handsへ（Stack -> Hands）
        ///   [1]: Stackに戻したカード（戻した順）
        ///   [2]: Trashに送ったカード（送った順）
        /// </summary>
        private void ProcessDraw(ActionRequestedEventPacket e, PlayerId actorPlayerId, SequenceService seq)
        {
            var othersInt = GetOtherActorNumbers(e.actorPlayerId);
            Debug.Log($"HostActionProcessor: Process Draw, ActorPlayerId: {e.actorPlayerId}, Others: [{string.Join(", ", othersInt)}]");

            var validIds = e.targetIds?.Where(id => id != 0).ToArray();
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
                targetIds = e.targetIds,
                actionStatusInt = 0,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        private void ProcessCheck(ActionRequestedEventPacket e, SequenceService seq)
        {
            var isMatch = e.actionStatusInt == 1;

            // Checkに成功した場合は、相手のTargetカードを表示する。
            if (isMatch && e.targetIds != null && e.targetIds.Length > 0)
            {
                var targetCardStateChanged = new CardStateChangedEventPacket
                {
                    sequence = seq.NextSequence(),
                    stateVersion = seq.NextStateVersion(),
                    cardId = e.targetIds[0],
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
                targetIds = e.targetIds,
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
                    targetIds = e.targetIds,
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
                reason = string.Empty,
                targetIds = winnerActorNumbers.ToArray(),
                actionStatusInt = isSuccess ? 1 : 0,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        /// <summary>
        /// TetrageMulti の Host 側オーケストレーション（非同期）。
        /// </summary>
        private async UniTask ProcessTetrageMultiAsync(ActionRequestedEventPacket e, SequenceService seq)
        {
            await UniTask.Yield();

            var requester     = ResolvePlayer(e.actorPlayerId);
            var requesterCard = GetTargetCard(requester);

            if (requester == null || requesterCard == null)
            {
                BroadcastError(e, seq, "勝利判定に必要なターゲット情報を取得できません");
                return;
            }

            var selectedPlayers = ResolveSelectedPlayersByActor(e, requester);
            if (selectedPlayers.Count == 0)
            {
                BroadcastError(e, seq, "選択プレイヤーが見つかりません");
                return;
            }

            var nominatedChildIds = selectedPlayers.Select(p => p.Id).ToList();
            var participantActorNumbers = BuildParticipantActorNumbers(e.actorPlayerId, selectedPlayers);

            var responseRequestPacket = new ActionResultEventPacket
            {
                sequence        = seq.NextSequence(),
                clientSequence  = e.clientSequence,
                actorPlayerId   = e.actorPlayerId,
                actionType      = ActionType.TetrageMulti,
                accepted        = true,
                reason          = string.Empty,
                targetIds       = participantActorNumbers,
                actionStatusInt = InGameConsts.TetrageMultiStatus.ResponseRequested,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, responseRequestPacket);

            var responses = await CollectResponsesAsync(requester, selectedPlayers, e.actorPlayerId);

            var submissions = TetrageMultiResultCalculator.ApplySubmissionDefaults(
                requester.Id, nominatedChildIds, responses);

            var allPlayerInfo = _netCtl.GameContext.Players
                .Select(p => (id: p.Id, suit: GetTargetCard(p)?.Suit ?? default))
                .ToList();

            var judgeResult = TetrageMultiResultCalculator.Judge(
                requester.Id,
                requesterCard.Suit,
                nominatedChildIds,
                submissions,
                allPlayerInfo);

            var winnerActorNumbers = judgeResult.Winners
                .Where(id => _playerIdMapper.TryGetActorNumber(id, out _))
                .Select(id =>
                {
                    _playerIdMapper.TryGetActorNumber(id, out var actor);
                    return actor;
                })
                .ToArray();

            var res = new ActionResultEventPacket
            {
                sequence        = seq.NextSequence(),
                clientSequence  = e.clientSequence,
                actorPlayerId   = e.actorPlayerId,
                actionType      = ActionType.TetrageMulti,
                accepted        = true,
                reason          = string.Empty,
                targetIds       = winnerActorNumbers,
                actionStatusInt = judgeResult.IsSuccess ? 1 : 0,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        // ─── 応答収集 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 親・指名子からの提出応答を収集する。未応答は ApplySubmissionDefaults で補完する。
        /// </summary>
        private async UniTask<Dictionary<PlayerId, bool>> CollectResponsesAsync(
            IPlayer requester, IReadOnlyList<IPlayer> selectedPlayers, int requesterActorNumber)
        {
            var responses  = new Dictionary<PlayerId, bool>();
            var pendingIds = new HashSet<PlayerId> { requester.Id };
            foreach (var p in selectedPlayers)
                pendingIds.Add(p.Id);

            var eventBus = _netCtl?.EventBus;
            if (eventBus == null)
                return responses;

            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(SettingConsts.TETRAGE_MULTI_RESPONSE_TIMEOUT_SECONDS));

            var tcs = new UniTaskCompletionSource();

            var disposable = eventBus.ActionRequested
                .Where(ev => ev.ActionType == ActionType.TetrageMulti
                          && (ev.ActionStatusInt == InGameConsts.TetrageMultiStatus.ResponseOpen
                           || ev.ActionStatusInt == InGameConsts.TetrageMultiStatus.ResponseDecline)
                          && pendingIds.Contains(ev.ActorPlayerId))
                .Subscribe(ev =>
                {
                    var isOpen = ev.ActionStatusInt == InGameConsts.TetrageMultiStatus.ResponseOpen;
                    responses[ev.ActorPlayerId] = isOpen;
                    pendingIds.Remove(ev.ActorPlayerId);
                    Debug.Log($"TetrageMulti: 応答受信 player={ev.ActorPlayerId} open={isOpen} 残り={pendingIds.Count}");

                    if (pendingIds.Count == 0)
                        tcs.TrySetResult();
                });

            try
            {
                await tcs.Task.AttachExternalCancellation(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"TetrageMulti: 応答タイムアウト。未応答 {pendingIds.Count} 人（親=提出・子=不提出の既定を適用）");
            }
            finally
            {
                disposable.Dispose();
            }

            return responses;
        }

        // ─── ヘルパー ────────────────────────────────────────────────────────

        /// <summary>
        /// targetIds（指名 ActorNumber[]）から選択されたプレイヤーを解決する。
        /// </summary>
        private List<IPlayer> ResolveSelectedPlayersByActor(ActionRequestedEventPacket e, IPlayer requester)
        {
            if (e.targetIds == null || e.targetIds.Length == 0)
                return new List<IPlayer>();

            var actorSet = new HashSet<int>(e.targetIds);
            return _netCtl.GameContext.Players
                .Where(p => p.Id != requester.Id)
                .Where(p => _playerIdMapper.TryGetActorNumber(p.Id, out var actor) && actorSet.Contains(actor))
                .ToList();
        }

        private int[] BuildParticipantActorNumbers(int parentActorNumber, IReadOnlyList<IPlayer> selectedPlayers)
        {
            var list = new List<int> { parentActorNumber };
            foreach (var player in selectedPlayers)
            {
                if (_playerIdMapper.TryGetActorNumber(player.Id, out var actor))
                    list.Add(actor);
            }
            return list.Distinct().ToArray();
        }

        /// <summary>エラー時に失敗 ActionResult をブロードキャストして終了する。</summary>
        private void BroadcastError(ActionRequestedEventPacket e, SequenceService seq, string reason)
        {
            var packet = new ActionResultEventPacket
            {
                sequence        = seq.NextSequence(),
                clientSequence  = e.clientSequence,
                actorPlayerId   = e.actorPlayerId,
                actionType      = e.actionType,
                accepted        = false,
                reason          = reason,
                targetIds   = e.targetIds,
                actionStatusInt = 0,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, packet);
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
                targetIds = e.targetIds,
                actionStatusInt = 0,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        #endregion
    }
}


