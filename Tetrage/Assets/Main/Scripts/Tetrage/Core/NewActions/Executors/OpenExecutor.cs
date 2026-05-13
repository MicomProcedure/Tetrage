using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using Tetrage.Models;
using System.Collections.Generic;
using Tetrage.UI; // CardClickDispatcherを使用するために追加
using System.Threading; // CancellationTokenを追加
using R3;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Open アクションの実行処理を担当するクラス
    /// </summary>
    public class OpenExecutor : IActionExecutor
    {
        public async UniTask<ActionResult> ExecuteAsync(IActionContext context)
        {
            try
            {
                // 1. 相手プレイヤーの裏向きカードの一覧を取得
                var hiddenCards = GetHiddenCardsFromOtherPlayers(context);
                if (!hiddenCards.Any())
                {
                    return ActionResult.Failure("相手プレイヤーに裏向きのカードがありません");
                }

                // 2. プレイヤーにカード選択のUIを表示（現在は自動選択で仮実装）
                var selectedCard = await SelectHiddenCard(context, hiddenCards);
                if (selectedCard == null)
                {
                    return ActionResult.Failure("表向きにするカードの選択に失敗しました");
                }

                // 3. 現段階ではローカルのFlipは実行せず（Host権威適用を待つ）
                Debug.Log($"Open アクション実行完了(送信準備): プレイヤー {context.RequesterPlayer.UserId} が表向き対象を選択");

                var descriptor = new Tetrage.Network.Gameplay.ActionRequestDescriptorPacket
                {
                    actionType = Tetrage.Core.Enums.ActionType.Open,
                    actorPlayerId = context.RequesterPlayer.PlayerId,
                    targetCardIds = new[] { selectedCard.Id }
                };
                return ActionResult.Success(descriptor);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Open アクション実行中にエラーが発生: {ex.Message}");
                return ActionResult.Failure($"Open アクション実行エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 他のプレイヤーの手札から裏向きのカードを取得
        /// </summary>
        private List<Card> GetHiddenCardsFromOtherPlayers(IActionContext context)
        {
            var hiddenCards = new List<Card>();

            foreach (var player in context.OtherPlayers)
            {
                var playerHiddenCards = player.Hands.Where(card => !card.IsFaceUp).ToList();
                hiddenCards.AddRange(playerHiddenCards);
            }

            return hiddenCards;
        }

        /// <summary>
        /// 裏向きカードを選択
        /// hiddenCardsの中からプレイヤーがクリックしたカードを待機して返す
        /// </summary>
        private async UniTask<Card> SelectHiddenCard(IActionContext context, List<Card> hiddenCards)
        {
            if (hiddenCards == null || hiddenCards.Count == 0)
            {
                Debug.LogWarning("選択可能な裏向きカードがありません。");
                return null;
            }

            var actionAwaiter = context.ActionAwaiter;
            var cancellationToken = actionAwaiter?.CurrentCancellationToken ?? CancellationToken.None;

            var tcs = new UniTaskCompletionSource<Card>();

            var cardClickdisposable = CardClickDispatcher.CardClicked
                .Where(c => hiddenCards.Contains(c))
                .Subscribe(c => tcs.TrySetResult(c));

            try
            {
                Debug.Log("裏向きカードの選択を待機中...");
                return await tcs.Task.AttachExternalCancellation(cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                var fallbackCard = hiddenCards.FirstOrDefault();
                Debug.LogWarning($"カード選択がキャンセルされました。フォールバックとして {fallbackCard} を選択します。");
                return fallbackCard;
            }
            finally
            {
                cardClickdisposable.Dispose();
            }
        }
    }
}