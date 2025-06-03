using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using Tetrage.Models;

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

                // 2. プレイヤーにカード選択のUIを表示（現在は自動選択で仮実装）
                var selectedCard = await SelectCardFromTmp(context);
                if (selectedCard == null)
                {
                    return ActionResult.Failure("カード選択に失敗しました");
                }

                // 3. 選択されたカードをStackに戻し、選択されなかったカードをHandsに追加
                var transferResult = await ProcessCardSelection(context, selectedCard);
                if (!transferResult.IsSuccess)
                {
                    return transferResult;
                }

                Debug.Log($"Draw アクション実行完了: プレイヤー {context.RequesterPlayer.UserId}");
                
                return ActionResult.Success("Draw アクションが正常に実行されました");
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
            var tmp = context.RequesterPlayer.Tmp;

            // Stage.DrawFromStackメソッドを使用して2回実行
            for (int i = 0; i < 2; i++)
            {
                bool success = stage.DrawFromStack(tmp);
                if (!success)
                {
                    return ActionResult.Failure($"カードの移動に失敗しました: {i + 1}枚目");
                }
            }

            await UniTask.Delay(100); // UI更新の時間を確保
            return ActionResult.Success();
        }

        /// <summary>
        /// Tmpからカードを選択（現在は仮実装：最初のカードを選択）
        /// </summary>
        private async UniTask<Card> SelectCardFromTmp(IActionContext context)
        {
            var tmp = context.RequesterPlayer.Tmp;
            
            // TODO: 実際のUI選択処理を実装
            // 現在は仮実装として最初のカードを選択
            await UniTask.Delay(100); // UI表示の仮の時間

            return tmp.FirstOrDefault();
        }

        /// <summary>
        /// 選択されたカードの処理（Stackに戻す、残りをHandsに移動）
        /// </summary>
        private async UniTask<ActionResult> ProcessCardSelection(IActionContext context, Card selectedCard)
        {
            var stack = context.CurrentStage.Stack;
            var tmp = context.RequesterPlayer.Tmp;
            var hands = context.RequesterPlayer.Hands;
            var trash = context.CurrentStage.Trash;

            // 選択されたカードをStackの一番上に戻す
            bool returnSuccess = CardPile.TransferService.Transfer(tmp, stack, selectedCard);
            if (!returnSuccess)
            {
                return ActionResult.Failure("選択されたカードをStackに戻すことができませんでした");
            }

            // 残りのカードをHandsに移動（満杯の場合はTrashに移動）
            var remainingCards = tmp.ToList();
            foreach (var card in remainingCards)
            {
                // Handsが満杯かチェック
                if (hands.Count >= hands.MaxCount)
                {
                    // Handsから1枚をTrashに移動してから新しいカードを追加
                    var cardToTrash = hands.FirstOrDefault();
                    if (cardToTrash != null)
                    {
                        CardPile.TransferService.Transfer(hands, trash, cardToTrash);
                    }
                }

                bool addSuccess = CardPile.TransferService.Transfer(tmp, hands, card);
                if (!addSuccess)
                {
                    return ActionResult.Failure($"カードをHandsに移動できませんでした: {card.Suit} {card.Number}");
                }
            }

            await UniTask.Delay(100); // アニメーション時間の確保
            return ActionResult.Success();
        }
    }
} 