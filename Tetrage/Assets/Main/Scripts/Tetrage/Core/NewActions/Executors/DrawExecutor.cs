using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using Tetrage.Models;
using Tetrage.UI; // CardClickDispatcherを使用するために追加
using System.Threading; // CancellationTokenを追加
using Tetrage.Core.Ids;
using System.Collections.Generic;
using Tetrage.Network.Gameplay;
using Tetrage.Core.Enums;
using DomainEvents = Tetrage.Core.Events;
namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Draw アクションの実行処理を担当するクラス
    /// </summary>
    public class DrawExecutor : IActionExecutor
    {
        public async UniTask<ActionResult> ExecuteAsync(IActionContext context)
        {
            try
            {
                // 1. Stackから2枚のカードをプレイヤーのTmpに移動
                var result = await MoveCardsFromStackToTmp(context);
                if (!result.IsSuccess)
                {
                    return result;
                }

                // 2. プレイヤーにカード選択をUIで促し、待機
                var selectedCard = await SelectCardFromPile(context, context.RequesterPlayer.Tmp);  // Tmpからカードを選択

                // 選択されたカードがない場合はエラー
                if (selectedCard == null)
                {
                    return ActionResult.Failure("選択すべきカードの取得に失敗しました。");
                }

                // 3. 選択結果に基づいてカードを処理
                var transferResult = await ProcessCardSelection(context, selectedCard);
                if (!transferResult.IsSuccess)
                {
                    return transferResult;
                }

                Debug.Log($"Draw アクション実行完了: プレイヤー {context.RequesterPlayer.UserId}");
                // ProcessCardSelection 内で ActionRequestDescriptor を組み立てて返す
                return transferResult;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Draw アクション実行中にエラーが発生: {ex.Message}");
                return ActionResult.Failure($"Draw アクション実行エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// Stackから2枚のカードをTmpに移動
        /// </summary>
        private async UniTask<ActionResult> MoveCardsFromStackToTmp(IActionContext context)
        {
            var stage = context.CurrentStage;
            var stack = stage.Stack;
            var tmp = context.RequesterPlayer.Tmp;

            // CardMovedEvent経由で2回移動（責務をDomainEventHandlerへ統一）
            for (int i = 0; i < 2; i++)
            {
                if (stack.Count == 0)
                {
                    return ActionResult.Failure($"カードの移動に失敗しました: {i + 1}枚目（Stackが空です）");
                }

                var card = stack.Peek(1)[0];
                bool success = MoveCardViaEvent(context, card, stack, tmp);
                if (!success)
                {
                    return ActionResult.Failure($"カードの移動に失敗しました: {i + 1}枚目（CardMoved適用失敗）");
                }
            }

            await UniTask.Delay(100); // UI更新の時間を確保
            return ActionResult.Success();
        }

        /// <summary>
        /// 選択されたカードの処理（選択カードをHandsへ、残りをStackの1番上に戻す）
        /// </summary>
        private async UniTask<ActionResult> ProcessCardSelection(IActionContext context, Card selectedCard)
        {
            var tmp = context.RequesterPlayer.Tmp;
            var hands = context.RequesterPlayer.Hands;
            var stack = context.CurrentStage.Stack;
            // 送信用に、選択/スタック返却/トラッシュ送りカードIDを順序付きで収集
            CardId movedToStackCardId = new CardId(0);
            CardId movedToTrashCardId = new CardId(0);

            // 1. 選択されたカードをHandsに移動
            bool toHandsSuccess = MoveCardViaEvent(context, selectedCard, tmp, hands);

            // 選択されたカードをHandsに移動できなかった場合はエラー
            if (!toHandsSuccess)
            {
                return ActionResult.Failure($"選択されたカード {selectedCard} をHandsに移動できませんでした");
            }
            Debug.Log($"選択されたカード {selectedCard} をHandsに移動しました。");

            // 2. 残りのカード（選択されなかったカード）をTmpからStackの1番上に戻す
            var remainingCards = tmp.ToList(); // ToList()でコピーを作成
            foreach (var card in remainingCards)
            {
                bool toTrashSuccess = MoveCardViaEvent(context, card, tmp, stack);
                if (!toTrashSuccess)
                {
                    return ActionResult.Failure($"選択されなかったカード {card} をStackの1番上に戻すことができませんでした");
                }
                Debug.Log($"選択されなかったカード {card} をStackの1番上に戻しました。");
                movedToStackCardId = card.Id;
            }

            // 3.手札が保持容量を超える場合、1枚を選んでTrashへ移動する
            if (hands.Count == hands.MaxCount)
            {
                var discard = await SelectCardFromPile(context, hands);  // Handsからカードを選択

                // 選択されたカードがない場合はエラー
                if (discard == null)
                {
                    return ActionResult.Failure("捨て札にするカードの取得に失敗しました。");
                }

                // 選択されたカードをTrashへ移動
                if (!MoveCardViaEvent(context, discard, hands, context.CurrentStage.Trash))
                {
                    return ActionResult.Failure($"手札超過カード {discard} をTrashに移動できませんでした");
                }
                Debug.Log($"手札超過により {discard} を捨て札へ移動しました");
                movedToTrashCardId = discard.Id;
            }

            await UniTask.Delay(100); // アニメーション時間の確保
            // targetCardIds の順序規約:
            // [0] 選択カード（Handsへ）
            // [1] Stackに戻したカード（戻した順）
            // [2] Trashに送ったカード（送った順）
            var cardIds = new List<CardId>();
            cardIds.Add(selectedCard.Id);
            cardIds.Add(movedToStackCardId);
            cardIds.Add(movedToTrashCardId);

            var descriptor = new ActionRequestDescriptor
            {
                actionType = ActionType.Draw,
                actorPlayerId = context.RequesterPlayer.PlayerId,
                targetCardIds = cardIds.ToArray()
            };
            Debug.Log($"DrawExecutor: ActionRequestDescriptor: ActorPlayerId: {context.RequesterPlayer.PlayerId}, TargetCardIds: {string.Join(", ", cardIds.Select(id => id.Value))}");
            return ActionResult.Success(descriptor);
        }

        #region Private Methods
        /// <summary>
        /// CardMovedEventを発行し、DomainEventHandlerでの適用結果を検証する。
        /// </summary>
        private bool MoveCardViaEvent(IActionContext context, Card card, CardPile from, CardPile to)
        {
            if (context?.GameContext?.Events == null || card == null || from == null || to == null)
            {
                return false;
            }

            context.GameContext.Events.Publish(
                new DomainEvents.CardMovedEvent(
                    sequence: 0,
                    cardId: card.Id,
                    fromPileId: from.Id,
                    toPileId: to.Id,
                    stateVersion: 0));

            // DomainEventHandler適用後の整合性確認
            return !from.Contains(card) && to.Contains(card);
        }
        #endregion

        /// <summary>
        /// 指定のCardPileから1枚選択を待機。外部キャンセル時はフォールバック選択を返し、必ず購読解除する。
        /// </summary>
        private async UniTask<Card> SelectCardFromPile(IActionContext context, CardPile pile)
        {
            if (pile == null || pile.Count == 0)
            {
                Debug.LogWarning($"{pile.Name}に選択すべきカードがありません。");
                return null;
            }

            var actionAwaiter = context.ActionAwaiter;
            var cancellationToken = actionAwaiter?.CurrentCancellationToken ?? CancellationToken.None;

            var tcs = new UniTaskCompletionSource<Card>();

            System.Action<Card> cardClickedHandler = null;
            cardClickedHandler = (clickedCard) =>
            {
                if (pile.Contains(clickedCard))
                {
                    tcs.TrySetResult(clickedCard);
                }
            };

            CardClickDispatcher.OnCardClicked += cardClickedHandler;

            try
            {
                Debug.Log($"{pile.Name}からカード選択を待機中...");
                return await tcs.Task.AttachExternalCancellation(cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                var fallbackCard = pile.FirstOrDefault();
                Debug.LogWarning($"{pile.Name}の選択がキャンセルされました。フォールバックとして {fallbackCard} を選択します。");
                return fallbackCard;
            }
            finally
            {
                CardClickDispatcher.OnCardClicked -= cardClickedHandler;
            }
        }
    }
}