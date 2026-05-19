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
    /// - 宣言者: 共同プレイヤーを選択し Host へ StartRequest を送信。
    ///           Host が応答収集・勝敗判定を行い最終 ActionResult を返すまで待機する。
    /// - Host からの ResponseRequested 通知はスルー（status フィルタで除外）。
    /// - ネットワークコンテキストがない場合は旧来のスタブ動作にフォールバック。
    /// </summary>
    public class TetrageMultiExecutor : IActionExecutor
    {
        #region IActionExecutor

        public async UniTask<ActionResult> ExecuteAsync(IActionContext context)
        {
            try
            {
                // ネットワークコンテキストなし → スタブ実行（オフライン / テスト用）
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

        /// <summary>
        /// 本実装フロー。
        /// 1. 宣言者が共同プレイヤーを選択して確定する。
        /// 2. Host へ StartRequest を送信する。
        /// 3. Host からの最終 ActionResult を待機して返す。
        /// </summary>
        private async UniTask<ActionResult> ExecuteNetworked(IActionContext context)
        {
            var ct = context.ActionAwaiter?.CurrentCancellationToken ?? CancellationToken.None;

            // 1. 共同プレイヤーの選択（UI に選択フェーズ開始を通知してから待機）
            TetrageMultiDispatcher.PublishSelectionStarted();
            var selectedCardIds = await WaitForPlayerSelectionAsync(context, ct);
            if (selectedCardIds == null || selectedCardIds.Count == 0)
                return ActionResult.Failure("共同プレイヤーが選択されませんでした");

            // 2. Host へ StartRequest を送信
            var initPacket = new ActionRequestedEventPacket
            {
                clientSequence  = context.Network.NextClientSequence(),
                actorPlayerId   = context.RequesterPlayer.PlayerId,
                actionType      = ActionType.TetrageMulti,
                targetCardIds   = selectedCardIds.Select(id => id.Value).ToArray(),
                actionStatusInt = InGameConsts.TetrageMultiStatus.StartRequest
            };
            context.Network.Request(initPacket);

            Debug.Log($"TetrageMulti: StartRequest 送信 (selectedCards={selectedCardIds.Count})");

            // 3. Host からの最終 ActionResult を待機（ResponseRequested は除外）
            var finalEvent = await context.GameContext.Events.ActionResult
                .Where(e => e.ActionType        == ActionType.TetrageMulti
                         && e.ActorPlayerId      == context.RequesterPlayer.Id
                         && e.ActionStatusInt    != InGameConsts.TetrageMultiStatus.ResponseRequested)
                .FirstAsync(ct);

            Debug.Log($"TetrageMulti: 最終結果受信 (status={finalEvent.ActionStatusInt}, accepted={finalEvent.Accepted})");

            // Dealer の HandleGameEndingByAction が使う descriptor を返す
            var descriptor = new ActionRequestDescriptorPacket
            {
                actionType      = ActionType.TetrageMulti,
                actorPlayerId   = context.RequesterPlayer.PlayerId,
                targetIds   = finalEvent.TargetCardIds?.Select(id => id.Value).ToArray(),
                actionStatusInt = finalEvent.ActionStatusInt
            };

            return finalEvent.Accepted
                ? ActionResult.Success(descriptor)
                : ActionResult.Failure(finalEvent.Reason, descriptor);
        }

        #endregion

        #region Player selection

        /// <summary>
        /// 宣言者が共同プレイヤーを選択するまで待機する。
        /// - 相手の Target カードをクリックするごとにトグル選択。
        /// - TetrageMultiDispatcher.SelectionConfirmed が発行されたら確定。
        /// </summary>
        private async UniTask<List<CardId>> WaitForPlayerSelectionAsync(
            IActionContext context, CancellationToken ct)
        {
            var selectableTargets = BuildSelectableTargetMap(context);
            if (selectableTargets.Count == 0)
            {
                Debug.LogWarning("TetrageMulti: 選択可能な対象プレイヤーがいません");
                return new List<CardId>();
            }

            var selectedCardIds = new HashSet<CardId>();
            var tcs             = new UniTaskCompletionSource<List<CardId>>();

            // カードクリックでトグル選択
            var clickDisposable = CardClickDispatcher.CardClicked
                .Where(card => card != null && selectableTargets.ContainsKey(card))
                .Subscribe(card =>
                {
                    // 選択に入ったカードはローカルのみハイライト（ネットワーク共有なし）
                    if (!selectedCardIds.Add(card.Id))
                    {
                        selectedCardIds.Remove(card.Id); // 選択済みなら解除
                        PublishCardHighlight(context, card.Id, highlighted: false);
                    }
                    else
                        PublishCardHighlight(context, card.Id, highlighted: true);

                    Debug.Log($"TetrageMulti: 選択トグル cardId={card.Id.Value} 現在選択数={selectedCardIds.Count}");
                });

            // 確定シグナルで TCS を解決
            var confirmDisposable = TetrageMultiDispatcher.SelectionConfirmed
                .Subscribe(playerIds =>
                {
                    // UI 側から PlayerId 一覧で確定される場合はそちらを使う
                    if (playerIds != null && playerIds.Count > 0)
                    {
                        var cardIds = ResolveCardIdsFromPlayerIds(playerIds, context);
                        tcs.TrySetResult(cardIds);
                    }
                    else
                    {
                        // カードクリックで選択したものをそのまま使う
                        tcs.TrySetResult(new List<CardId>(selectedCardIds));
                    }
                });

            try
            {
                Debug.Log("TetrageMulti: 共同プレイヤー選択を待機中...");
                return await tcs.Task.AttachExternalCancellation(ct);
            }
            catch (System.OperationCanceledException)
            {
                // タイムアウト / キャンセル時は現在選択を確定
                var fallback = new List<CardId>(selectedCardIds);
                if (fallback.Count == 0 && selectableTargets.Count > 0)
                    fallback.Add(selectableTargets.Keys.First().Id);
                Debug.LogWarning($"TetrageMulti: 選択がキャンセルされました。フォールバック={fallback.Count}件");
                return fallback;
            }
            finally
            {
                // 選択フェーズ終了時は選択可能な全ターゲットのハイライトを落とす（残留防止）
                ClearTetrageMultiSelectionHighlights(context, selectableTargets);
                clickDisposable.Dispose();
                confirmDisposable.Dispose();
            }
        }

        /// <summary>
        /// TetrageMulti 宣言者のカード選択ハイライトをドメインイベントで反映する（ローカルのみ）。
        /// </summary>
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

        /// <summary>
        /// 共同プレイヤー選択に使った全ターゲットカードのハイライトを解除する。
        /// </summary>
        private static void ClearTetrageMultiSelectionHighlights(
            IActionContext context, Dictionary<Card, IPlayer> selectableTargets)
        {
            if (selectableTargets == null || selectableTargets.Count == 0) return;
            foreach (var card in selectableTargets.Keys)
                PublishCardHighlight(context, card.Id, highlighted: false);
        }

        /// <summary>
        /// 対象プレイヤーの Target カード → プレイヤー の辞書を構築する。
        /// </summary>
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

        /// <summary>
        /// PlayerId 一覧からそれぞれの Target カード ID を解決する。
        /// </summary>
        private List<CardId> ResolveCardIdsFromPlayerIds(
            IReadOnlyList<PlayerId> playerIds, IActionContext context)
        {
            var result = new List<CardId>();
            foreach (var pid in playerIds)
            {
                var player = context.OtherPlayers.FirstOrDefault(p => p.Id == pid);
                var card   = player?.Target.FirstOrDefault();
                if (card != null)
                    result.Add(card.Id);
            }
            return result;
        }

        #endregion

        #region Offline fallback

        /// <summary>
        /// ネットワークなし環境向けのスタブ実行。
        /// 全員選択・スート照合のみで即時判定する。
        /// </summary>
        private async UniTask<ActionResult> ExecuteOfflineFallback(IActionContext context)
        {
            await UniTask.Yield();

            var myCard = context.RequesterPlayer.Target.FirstOrDefault();
            if (myCard == null)
                return ActionResult.Failure("自分のTargetカードが見つかりません");

            var targetCards = context.OtherPlayers
                .Select(p => p.Target.FirstOrDefault())
                .Where(c => c != null)
                .ToList();

            if (targetCards.Count == 0)
                return ActionResult.Failure("対象プレイヤーのTargetカードが見つかりません");

            var isSuccess = targetCards.All(c => c.Suit == myCard.Suit);

            var descriptor = new ActionRequestDescriptorPacket
            {
                actionType      = ActionType.TetrageMulti,
                actorPlayerId   = context.RequesterPlayer.PlayerId,
                targetIds   = targetCards.Select(c => c.Id.Value).ToArray(),
                actionStatusInt = isSuccess ? 1 : 0
            };

            Debug.Log($"TetrageMulti (offline): isSuccess={isSuccess}");
            return ActionResult.Success(descriptor);
        }

        #endregion
    }
}
