using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using Tetrage.Models;
using System.Collections.Generic;

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

                // 3. 選択されたカードを表向きにする
                selectedCard.Flip();

                Debug.Log($"Open アクション実行完了: プレイヤー {context.RequesterPlayer.UserId} が {selectedCard.Suit} {selectedCard.Number} を表向きにしました");
                
                return ActionResult.Success(new { 
                    OpenedCard = new { selectedCard.Suit, selectedCard.Number },
                    Message = "Open アクションが正常に実行されました" 
                });
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
                var playerHiddenCards = player.Hands.Where(card => !card.IsVisible).ToList();
                hiddenCards.AddRange(playerHiddenCards);
            }

            return hiddenCards;
        }

        /// <summary>
        /// 裏向きカードを選択（現在は仮実装：ランダム選択）
        /// </summary>
        private async UniTask<Card> SelectHiddenCard(IActionContext context, List<Card> hiddenCards)
        {
            // TODO: 実際のUI選択処理を実装
            // プレイヤーが相手の手札から裏向きカードを選択するUI
            
            await UniTask.Delay(100); // UI表示の仮の時間

            // 仮実装：ランダムに選択
            if (hiddenCards.Any())
            {
                var randomIndex = UnityEngine.Random.Range(0, hiddenCards.Count);
                var selectedCard = hiddenCards[randomIndex];
                
                Debug.Log($"仮実装：ランダムに選択されたカード - {selectedCard.Suit} {selectedCard.Number}");
                return selectedCard;
            }

            return null;
        }
    }
} 