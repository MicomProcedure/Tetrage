using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.UI;
using R3;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Check アクションの実行処理を担当するクラス
    /// </summary>
    public class CheckExecutor : IActionExecutor
    {
        public async UniTask<ActionResult> ExecuteAsync(IActionContext context)
        {
            try
            {
                // 1. 相手プレイヤーの選択
                var targetPlayer = await SelectTargetPlayer(context);
                if (targetPlayer == null)
                {
                    return ActionResult.Failure("対象プレイヤーの選択に失敗しました");
                }

                // 2. 相手のTargetカードの取得
                var targetCard = GetTargetCard(targetPlayer);
                if (targetCard == null)
                {
                    return ActionResult.Failure("対象プレイヤーのTargetカードが見つかりません");
                }

                // 3. 自分の手札のスートと比較
                var isMatch = CheckSuitMatch(context, targetCard);

                Debug.Log($"Check アクション実行完了(送信準備): プレイヤー {context.RequesterPlayer.UserId} が {targetPlayer.UserId} をチェック");

                var descriptor = new Tetrage.Network.Gameplay.ActionRequestDescriptor
                {
                    actionType = Tetrage.Core.Enums.ActionType.Check,
                    actorPlayerId = context.RequesterPlayer.PlayerId,
                    targetCardIds = new[] { targetCard.Id }
                };
                return ActionResult.Success(descriptor);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Check アクション実行中にエラーが発生: {ex.Message}");
                return ActionResult.Failure($"Check アクション実行エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 相手プレイヤーのTargetカードクリックを待機し、選択されたカードの所有プレイヤーを返す。
        /// </summary>
        private async UniTask<IPlayer> SelectTargetPlayer(IActionContext context)
        {
            var selectableTargets = CreateSelectableTargetMap(context);
            if (selectableTargets.Count == 0)
            {
                Debug.LogWarning("Check対象として選択可能なTargetカードがありません。");
                return null;
            }

            var actionAwaiter = context.ActionAwaiter;
            var cancellationToken = actionAwaiter?.CurrentCancellationToken ?? CancellationToken.None;
            var tcs = new UniTaskCompletionSource<IPlayer>();
            var originalHighlightStates = CaptureHighlightStates(selectableTargets.Keys);

            var cardClickDisposable = CardClickDispatcher.CardClicked
                // Checkでは相手Targetカードだけを有効な入力として扱う。
                .Where(clickedCard => clickedCard != null && selectableTargets.ContainsKey(clickedCard))
                .Subscribe(clickedCard => tcs.TrySetResult(selectableTargets[clickedCard]));
            SetTargetHighlight(selectableTargets.Keys, true);

            try
            {
                Debug.Log("Check対象のTargetカード選択を待機中...");
                return await tcs.Task.AttachExternalCancellation(cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                var fallbackPlayer = selectableTargets.Values.FirstOrDefault();
                Debug.LogWarning($"Check対象選択がキャンセルされました。フォールバックとして {fallbackPlayer?.UserId} を選択します。");
                return fallbackPlayer;
            }
            finally
            {
                cardClickDisposable.Dispose();
                RestoreTargetHighlight(originalHighlightStates);
            }
        }

        /// <summary>
        /// Checkで選択可能な相手Targetカードと所有プレイヤーの対応を作成する。
        /// </summary>
        private Dictionary<Card, IPlayer> CreateSelectableTargetMap(IActionContext context)
        {
            var selectableTargets = new Dictionary<Card, IPlayer>();

            foreach (var player in context.OtherPlayers)
            {
                var targetCard = GetTargetCard(player);
                if (targetCard == null)
                {
                    continue;
                }

                selectableTargets[targetCard] = player;
            }

            return selectableTargets;
        }

        /// <summary>
        /// 選択可能なTargetカードのハイライト状態を切り替える。
        /// </summary>
        private void SetTargetHighlight(IEnumerable<Card> targetCards, bool isHighlighted)
        {
            foreach (var targetCard in targetCards)
            {
                // 選択待機中であることを表すため、候補Targetカードのみをハイライトする。
                if (isHighlighted)
                {
                    targetCard.Highlight();
                    continue;
                }

                targetCard.Unhighlight();
            }
        }

        /// <summary>
        /// 選択待機前のTargetカードハイライト状態を保存する。
        /// </summary>
        private Dictionary<Card, bool> CaptureHighlightStates(IEnumerable<Card> targetCards)
        {
            var highlightStates = new Dictionary<Card, bool>();
            foreach (var targetCard in targetCards)
            {
                highlightStates[targetCard] = targetCard.IsHighlighted;
            }

            return highlightStates;
        }

        /// <summary>
        /// 選択待機前のTargetカードハイライト状態へ戻す。
        /// </summary>
        private void RestoreTargetHighlight(IReadOnlyDictionary<Card, bool> highlightStates)
        {
            foreach (var pair in highlightStates)
            {
                // 選択待機開始前からハイライトされていたカードは、その状態を維持する。
                if (pair.Value)
                {
                    pair.Key.Highlight();
                    continue;
                }

                pair.Key.Unhighlight();
            }
        }

        /// <summary>
        /// 指定されたプレイヤーのTargetカードを取得
        /// </summary>
        private Models.Card GetTargetCard(IPlayer player)
        {
            return player.Target.FirstOrDefault();
        }

        /// <summary>
        /// 自分の手札のスートと対象のTargetカードのスートが一致するかチェック
        /// </summary>
        private bool CheckSuitMatch(IActionContext context, Models.Card targetCard)
        {
            var myHands = context.RequesterPlayer.Hands;

            if (!myHands.Any())
            {
                Debug.LogWarning("自分の手札が空です");
                return false;
            }

            // 手札の最初のカードのスートを取得（Reach状態では全て同じスートのはず）
            var mySuit = myHands.First().Suit;

            return mySuit == targetCard.Suit;
        }
    }
}
