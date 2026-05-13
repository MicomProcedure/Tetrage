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

        /// <summary>
        /// TetrageMulti の Host 側オーケストレーション（非同期）。
        /// StartRequest を受けたあと、選択プレイヤーへ応答要求を送り、
        /// 収集したレスポンス（未応答は「出す」扱い）で勝敗を判定して最終結果を配信する。
        /// </summary>
        private async UniTask ProcessTetrageMultiAsync(ActionRequestedEventPacket e, SequenceService seq)
        {
            // 1フレーム空けてメインの同期呼び出しスタックから切り離す
            await UniTask.Yield();

            var requester     = ResolvePlayer(e.actorPlayerId);
            var requesterCard = GetTargetCard(requester);

            if (requester == null || requesterCard == null)
            {
                BroadcastError(e, seq, "勝利判定に必要なターゲット情報を取得できません");
                return;
            }

            // 宣言者が選択した参加者を targetCardIds から解決する
            var selectedPlayers = ResolveSelectedPlayers(e, requester);
            if (selectedPlayers.Count == 0)
            {
                BroadcastError(e, seq, "選択プレイヤーが見つかりません");
                return;
            }

            // 2. 応答要求を全員へブロードキャスト
            var responseRequestPacket = new ActionResultEventPacket
            {
                sequence        = seq.NextSequence(),
                clientSequence  = e.clientSequence,
                actorPlayerId   = e.actorPlayerId,
                actionType      = ActionType.TetrageMulti,
                accepted        = true,
                targetCardIds   = e.targetCardIds,
                actionStatusInt = InGameConsts.TetrageMultiStatus.ResponseRequested,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, responseRequestPacket);

            // 3. 選択プレイヤーの参加応答を収集（タイムアウトあり）
            var responses = await CollectResponsesAsync(selectedPlayers, e.actorPlayerId);

            // 4. 未応答は「出す」フォールバック → 開いたプレイヤー一覧を確定
            var selectedPlayerIds = selectedPlayers.Select(p => p.Id).ToList();
            var openPlayerIds     = TetrageMultiResultCalculator.DetermineOpenPlayers(selectedPlayerIds, responses);
            var openPlayers       = _netCtl.GameContext.Players
                .Where(p => openPlayerIds.Contains(p.Id))
                .ToList();

            // 5. 勝敗判定
            var openSuits  = openPlayers.Select(p => GetTargetCard(p)?.Suit).Where(s => s.HasValue).Select(s => s.Value).ToList();
            var isSuccess  = TetrageMultiResultCalculator.IsSuccess(requesterCard.Suit, openSuits);
            var winnerActorNumbers = CalculateWinnerActorNumbers(requester, openPlayers, isSuccess);

            // 6. 開いた参加者の Target カード ID（最終 ActionResult に載せる）
            var openedCardIds = openPlayers
                .Select(p => GetTargetCard(p))
                .Where(c => c != null)
                .Select(c => c.Id.Value)
                .ToArray();

            var res = new ActionResultEventPacket
            {
                sequence        = seq.NextSequence(),
                clientSequence  = e.clientSequence,
                actorPlayerId   = e.actorPlayerId,
                actionType      = ActionType.TetrageMulti,
                accepted        = true,
                reason          = BuildWinnersReason(winnerActorNumbers.Distinct()),
                targetCardIds   = openedCardIds,
                actionStatusInt = isSuccess ? 1 : 0,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        // ─── 応答収集 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 選択プレイヤーからの参加応答を EventBus 経由で収集する。
        /// タイムアウト（TETRAGE_MULTI_RESPONSE_TIMEOUT_SECONDS）を過ぎると収集を打ち切る。
        /// </summary>
        private async UniTask<Dictionary<PlayerId, bool>> CollectResponsesAsync(
            IReadOnlyList<IPlayer> selectedPlayers, int requesterActorNumber)
        {
            var responses     = new Dictionary<PlayerId, bool>();
            var pendingIds    = new HashSet<PlayerId>(selectedPlayers.Select(p => p.Id));
            var eventBus      = _netCtl?.EventBus;

            if (eventBus == null)
                return responses; // テスト環境など EventBus なし → 全員フォールバック

            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(SettingConsts.TETRAGE_MULTI_RESPONSE_TIMEOUT_SECONDS));

            var tcs = new UniTaskCompletionSource();

            // EventBus.ActionRequested を購読して応答を収集する
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
                        tcs.TrySetResult(); // 全員応答済み
                });

            try
            {
                // 全員応答またはタイムアウトまで待機
                await tcs.Task.AttachExternalCancellation(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"TetrageMulti: 応答タイムアウト。未応答 {pendingIds.Count} 人を「出す」扱いにします");
            }
            finally
            {
                disposable.Dispose();
            }

            return responses;
        }

        // ─── 勝敗判定ロジック ──────────────────────────────────────────────────

        /// <summary>
        /// 勝者の ActorNumber 一覧を計算する。
        /// TetrageMultiResultCalculator の純粋ロジックを用い、ActorNumber へ変換して返す。
        /// </summary>
        private List<int> CalculateWinnerActorNumbers(
            IPlayer requester, IReadOnlyList<IPlayer> openPlayers, bool isSuccess)
        {
            var allPlayerInfo = _netCtl.GameContext.Players
                .Select(p => (id: p.Id, suit: GetTargetCard(p)?.Suit ?? default))
                .ToList();

            var openPlayerIds = openPlayers.Select(p => p.Id).ToList();
            var requesterCard = GetTargetCard(requester);

            List<PlayerId> winnerPlayerIds;
            if (isSuccess)
            {
                winnerPlayerIds = TetrageMultiResultCalculator.CalculateSuccessWinners(requester.Id, openPlayerIds);
            }
            else
            {
                var allWithSuit = allPlayerInfo.Select(p => (p.id, p.suit)).ToList();
                winnerPlayerIds = TetrageMultiResultCalculator.CalculateFailureWinners(
                    requester.Id, requesterCard?.Suit ?? default, allWithSuit, openPlayerIds);
            }

            return winnerPlayerIds
                .Where(id => _playerIdMapper.TryGetActorNumber(id, out _))
                .Select(id =>
                {
                    _playerIdMapper.TryGetActorNumber(id, out var actor);
                    return actor;
                })
                .ToList();
        }

        // ─── ヘルパー ────────────────────────────────────────────────────────

        /// <summary>
        /// targetCardIds から選択されたプレイヤーを解決する（宣言者は除く）。
        /// </summary>
        private List<IPlayer> ResolveSelectedPlayers(ActionRequestedEventPacket e, IPlayer requester)
        {
            if (e.targetCardIds == null || e.targetCardIds.Length == 0)
                return new List<IPlayer>();

            var selectedCardIdSet = new HashSet<int>(e.targetCardIds);
            return _netCtl.GameContext.Players
                .Where(p => p.Id != requester.Id)
                .Where(p =>
                {
                    var card = GetTargetCard(p);
                    return card != null && selectedCardIdSet.Contains(card.Id.Value);
                })
                .ToList();
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
                targetCardIds   = e.targetCardIds,
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
                targetCardIds = e.targetCardIds,
                actionStatusInt = 0,
            };
            _netCtl.Broadcaster.Raise(EventCode.ActionResult, res);
        }

        #endregion
    }
}


