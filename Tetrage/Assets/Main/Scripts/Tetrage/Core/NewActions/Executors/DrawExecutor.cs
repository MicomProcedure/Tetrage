using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using Tetrage.Models;
using Tetrage.UI; // CardClickDispatcherを使用するために追加
using System.Threading; // CancellationTokenを追加

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
                var selectedCard = await SelectCardFromTmp(context);
                if (selectedCard == null)
                {
                    // このケースは、SelectCardFromTmpの最初のチェックでTmpが空の場合のみ発生
                    return ActionResult.Failure("選択すべきカードがありませんでした。");
                }

                // 3. 選択結果に基づいてカードを処理
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
        /// Tmpからカードを選択（プレイヤーのクリック入力を待機）
        /// ActionAwaiterのタイムアウトでキャンセルされた場合は自動で1枚選択する
        /// </summary>
        private async UniTask<Card> SelectCardFromTmp(IActionContext context)
        {
            var tmp = context.RequesterPlayer.Tmp;
            if (tmp.Count == 0)
            {
                Debug.LogError("Tmpに選択すべきカードがありません。");
                return null;
            }

            // ActionAwaiterのCancellationTokenを取得
            var actionAwaiter = context.ActionAwaiter; // IActionContextにはActionAwaiterがある
            var cancellationToken = actionAwaiter?.CurrentCancellationToken ?? CancellationToken.None;

            var tcs = new UniTaskCompletionSource<Card>();

            System.Action<Card> cardClickedHandler = null;
            cardClickedHandler = (clickedCard) =>
            {
                // タイムアウトをリセット（UI入力があったため）
                actionAwaiter?.ResetTimeout();

                // Tmpにあるカードがクリックされた場合のみ処理
                if (tmp.Contains(clickedCard))
                {
                    tcs.TrySetResult(clickedCard);
                }
            };

            CardClickDispatcher.OnCardClicked += cardClickedHandler;

            try
            {
                Debug.Log($"プレイヤーのカード選択を待っています...");
                // TODO: 選択可能なカードをUI上でハイライトする処理

                // 外部キャンセル対応のみ（独自タイムアウトなし）
                return await tcs.Task.AttachExternalCancellation(cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                Debug.LogWarning("カード選択が外部からキャンセルされました。強制的に1枚を選択します。");
                // キャンセル時は強制選択を行う
                var fallbackCard = tmp.FirstOrDefault();
                if (fallbackCard != null)
                {
                    Debug.Log($"キャンセル時のフォールバックとして {fallbackCard} を選択しました。");
                }
                return fallbackCard;
            }
            finally
            {
                // 選択完了後、必ずイベント購読を解除
                CardClickDispatcher.OnCardClicked -= cardClickedHandler;
                // TODO: ハイライト解除処理
            }
        }

        /// <summary>
        /// 選択されたカードの処理（選択カードをHandsへ、残りをTrashへ）
        /// </summary>
        private async UniTask<ActionResult> ProcessCardSelection(IActionContext context, Card selectedCard)
        {
            var tmp = context.RequesterPlayer.Tmp;
            var hands = context.RequesterPlayer.Hands;
            var trash = context.CurrentStage.Trash;

            // 1. 選択されたカードをHandsに移動
            bool toHandsSuccess = CardPile.TransferService.Transfer(tmp, hands, selectedCard);
            if (!toHandsSuccess)
            {
                return ActionResult.Failure($"選択されたカード {selectedCard} をHandsに移動できませんでした");
            }
            Debug.Log($"選択されたカード {selectedCard} をHandsに移動しました。");

            // 2. 残りのカード（選択されなかったカード）をTmpからTrashに移動
            var remainingCards = tmp.ToList(); // ToList()でコピーを作成
            foreach (var card in remainingCards)
            {
                bool toTrashSuccess = CardPile.TransferService.Transfer(tmp, trash, card);
                if (!toTrashSuccess)
                {
                    return ActionResult.Failure($"選択されなかったカード {card} をTrashに移動できませんでした");
                }
                Debug.Log($"選択されなかったカード {card} をTrashに移動しました。");
            }

            await UniTask.Delay(100); // アニメーション時間の確保
            return ActionResult.Success();
        }
    }
}