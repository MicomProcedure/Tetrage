using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.UI;
using Tetrage.Core.Enums;
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

                // isMatchの結果をActionRequestDescriptorに詰めて返す。これによって結果が全員に通知され、各々でUIの処理がされる
                var descriptor = new Tetrage.Network.Gameplay.ActionRequestDescriptorPacket
                {
                    actionType = ActionType.Check,
                    actorPlayerId = context.RequesterPlayer.PlayerId,
                    targetIds = new[] { targetCard.Id.Value },
                    actionStatusInt = isMatch ? 1 : 0
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

            var cardClickDisposable = CardClickDispatcher.CardClicked
                // Checkでは相手Targetカードだけを有効な入力として扱う。
                .Where(clickedCard => clickedCard != null && selectableTargets.ContainsKey(clickedCard))
                .Subscribe(clickedCard => tcs.TrySetResult(selectableTargets[clickedCard]));

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
        /// 指定されたプレイヤーのTargetカードを取得
        /// </summary>
        private Card GetTargetCard(IPlayer player)
        {
            return player.Target.FirstOrDefault();
        }

        /// <summary>
        /// 自分の手札のスートと対象のTargetカードのスートが一致するかチェック
        /// </summary>
        private bool CheckSuitMatch(IActionContext context, Card targetCard)
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
