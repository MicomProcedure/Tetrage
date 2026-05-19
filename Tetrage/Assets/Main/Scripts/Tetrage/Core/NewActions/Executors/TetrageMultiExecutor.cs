using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Tetrage.Core.Constants;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Core.Events;
using Tetrage.Core.Ids;
using Tetrage.Models;
using Tetrage.Network.Gameplay;
using Tetrage.UI;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// TetrageMulti アクションの実行処理。
    /// </summary>
    public class TetrageMultiExecutor : IActionExecutor
    {
        #region IActionExecutor

        public async UniTask<ActionResult> ExecuteAsync(IActionContext context)
        {
            try
            {
                if (context.Network == null)
                    return await ExecuteOfflineFallback(context);

                return await ExecuteNetworked(context);
            }
            catch (System.OperationCanceledException)
            {
                Debug.LogWarning("TetrageMulti: 実行がキャンセルされました");
                return ActionResult.Failure("キャンセル");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"TetrageMulti: 実行中にエラー: {ex.Message}");
                return ActionResult.Failure($"実行エラー: {ex.Message}");
            }
        }

        #endregion

        #region Networked flow

        private async UniTask<ActionResult> ExecuteNetworked(IActionContext context)
        {
            var ct = context.ActionAwaiter?.CurrentCancellationToken ?? CancellationToken.None;
            var mapper = context.Network.PlayerIdMapper;

            TetrageMultiDispatcher.PublishSelectionStarted();
            var nominatedPlayerIds = await WaitForPlayerSelectionAsync(context, ct);
            if (nominatedPlayerIds == null || nominatedPlayerIds.Count == 0)
                return ActionResult.Failure("共同プレイヤーが選択されませんでした");

            var nominatedActorNumbers = ResolveActorNumbers(nominatedPlayerIds, mapper);
            if (nominatedActorNumbers.Count == 0)
                return ActionResult.Failure("指名プレイヤーの ActorNumber を解決できませんでした");

            var initPacket = new ActionRequestedEventPacket
            {
                clientSequence  = context.Network.NextClientSequence(),
                actorPlayerId   = context.RequesterPlayer.PlayerId,
                actionType      = ActionType.TetrageMulti,
                targetIds       = nominatedActorNumbers.ToArray(),
                actionStatusInt = InGameConsts.TetrageMultiStatus.StartRequest
            };
            context.Network.Request(initPacket);

            Debug.Log($"TetrageMulti: StartRequest 送信 (nominated={nominatedActorNumbers.Count})");

            var finalEvent = await context.GameContext.Events.ActionResult
                .Where(e => e.ActionType     == ActionType.TetrageMulti
                         && e.ActorPlayerId   == context.RequesterPlayer.Id
                         && e.ActionStatusInt != InGameConsts.TetrageMultiStatus.ResponseRequested)
                .FirstAsync(ct);

            Debug.Log($"TetrageMulti: 最終結果受信 (status={finalEvent.ActionStatusInt}, accepted={finalEvent.Accepted})");

            var descriptor = new ActionRequestDescriptorPacket
            {
                actionType      = ActionType.TetrageMulti,
                actorPlayerId   = context.RequesterPlayer.PlayerId,
                targetIds       = finalEvent.WinnerActorNumbers?.ToArray(),
                actionStatusInt = finalEvent.ActionStatusInt
            };

            return finalEvent.Accepted
                ? ActionResult.Success(descriptor)
                : ActionResult.Failure(finalEvent.Reason, descriptor);
        }

        #endregion

        #region Player selection

        private async UniTask<List<PlayerId>> WaitForPlayerSelectionAsync(
            IActionContext context, CancellationToken ct)
        {
            var selectableTargets = BuildSelectableTargetMap(context);
            if (selectableTargets.Count == 0)
            {
                Debug.LogWarning("TetrageMulti: 選択可能な対象プレイヤーがいません");
                return new List<PlayerId>();
            }

            var selectedPlayerIds = new HashSet<PlayerId>();
            var tcs               = new UniTaskCompletionSource<List<PlayerId>>();

            var clickDisposable = CardClickDispatcher.CardClicked
                .Where(card => card != null && selectableTargets.ContainsKey(card))
                .Subscribe(card =>
                {
                    var player = selectableTargets[card];
                    if (!selectedPlayerIds.Add(player.Id))
                    {
                        selectedPlayerIds.Remove(player.Id);
                        PublishCardHighlight(context, card.Id, highlighted: false);
                    }
                    else
                        PublishCardHighlight(context, card.Id, highlighted: true);

                    Debug.Log($"TetrageMulti: 選択トグル playerId={player.Id.Value} 現在選択数={selectedPlayerIds.Count}");
                });

            var confirmDisposable = TetrageMultiDispatcher.SelectionConfirmed
                .Subscribe(playerIds =>
                {
                    if (playerIds != null && playerIds.Count > 0)
                        tcs.TrySetResult(new List<PlayerId>(playerIds));
                    else
                        tcs.TrySetResult(new List<PlayerId>(selectedPlayerIds));
                });

            try
            {
                Debug.Log("TetrageMulti: 共同プレイヤー選択を待機中...");
                return await tcs.Task.AttachExternalCancellation(ct);
            }
            catch (System.OperationCanceledException)
            {
                var fallback = new List<PlayerId>(selectedPlayerIds);
                if (fallback.Count == 0 && selectableTargets.Count > 0)
                    fallback.Add(selectableTargets.Values.First().Id);
                Debug.LogWarning($"TetrageMulti: 選択がキャンセルされました。フォールバック={fallback.Count}件");
                return fallback;
            }
            finally
            {
                ClearTetrageMultiSelectionHighlights(context, selectableTargets);
                clickDisposable.Dispose();
                confirmDisposable.Dispose();
            }
        }

        private static List<int> ResolveActorNumbers(
            IReadOnlyList<PlayerId> playerIds, IPlayerIdMapper mapper)
        {
            var result = new List<int>();
            if (mapper == null || playerIds == null) return result;

            foreach (var pid in playerIds)
            {
                if (mapper.TryGetActorNumber(pid, out var actor))
                    result.Add(actor);
            }
            return result;
        }

        private static void PublishCardHighlight(IActionContext context, CardId cardId, bool highlighted)
        {
            if (context?.GameContext?.Events == null) return;

            context.GameContext.Events.Publish(new CardStateChangedEvent(
                sequence: 0,
                cardId: cardId,
                isFaceUp: false,
                stateType: CardStateType.IsHighlighted,
                stateValue: highlighted));
        }

        private static void ClearTetrageMultiSelectionHighlights(
            IActionContext context, Dictionary<Card, IPlayer> selectableTargets)
        {
            if (selectableTargets == null || selectableTargets.Count == 0) return;
            foreach (var card in selectableTargets.Keys)
                PublishCardHighlight(context, card.Id, highlighted: false);
        }

        private Dictionary<Card, IPlayer> BuildSelectableTargetMap(IActionContext context)
        {
            var map = new Dictionary<Card, IPlayer>();
            foreach (var player in context.OtherPlayers)
            {
                var targetCard = player.Target.FirstOrDefault();
                if (targetCard != null)
                    map[targetCard] = player;
            }
            return map;
        }

        #endregion

        #region Offline fallback

        private async UniTask<ActionResult> ExecuteOfflineFallback(IActionContext context)
        {
            await UniTask.Yield();

            var parent = context.RequesterPlayer;
            var parentCard = parent.Target.FirstOrDefault();
            if (parentCard == null)
                return ActionResult.Failure("自分のTargetカードが見つかりません");

            var nominatedChildIds = context.OtherPlayers.Select(p => p.Id).ToList();
            var allPlayers = context.AllPlayers
                .Select(p => (id: p.Id, suit: p.Target.FirstOrDefault()?.Suit ?? default))
                .ToList();

            var submissions = new Dictionary<PlayerId, bool>();
            foreach (var id in nominatedChildIds)
                submissions[id] = true;
            submissions[parent.Id] = true;

            var judge = TetrageMultiResultCalculator.Judge(
                parent.Id, parentCard.Suit, nominatedChildIds, submissions, allPlayers);

            var winnerActorNumbers = judge.Winners.Select(id => id.Value).ToArray();

            var descriptor = new ActionRequestDescriptorPacket
            {
                actionType      = ActionType.TetrageMulti,
                actorPlayerId   = parent.PlayerId,
                targetIds       = winnerActorNumbers,
                actionStatusInt = judge.IsSuccess ? 1 : 0
            };

            Debug.Log($"TetrageMulti (offline): isSuccess={judge.IsSuccess}");
            return ActionResult.Success(descriptor);
        }

        #endregion
    }
}
