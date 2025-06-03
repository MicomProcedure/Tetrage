using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Reach アクションの実行処理を担当するクラス
    /// </summary>
    public class ReachExecutor : IActionExecutor
    {
        public async UniTask<ActionResult> ExecuteAsync(IActionContext context)
        {
            try
            {
                var hands = context.RequesterPlayer.Hands;
                
                // 1. プレイヤーの手札を全て表向きにする
                var cardsRevealed = 0;
                foreach (var card in hands)
                {
                    if (!card.IsVisible)
                    {
                        card.Flip();
                        cardsRevealed++;
                    }
                }

                // 2. TODO: プレイヤーをReach状態に設定
                // 現在のIPlayerインターフェースにReach状態のプロパティがないため、
                // 今後プレイヤーモデルに追加する必要がある
                
                // 3. Reach状態の通知/アニメーション
                await ShowReachAnimation(context);

                var suitName = hands.FirstOrDefault()?.Suit.ToString() ?? "Unknown";
                
                Debug.Log($"Reach アクション実行完了: プレイヤー {context.RequesterPlayer.UserId} が {suitName} でReach状態になりました");
                
                return ActionResult.Success(new { 
                    ReachedSuit = suitName,
                    CardsRevealed = cardsRevealed,
                    Message = "Reach アクションが正常に実行されました" 
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Reach アクション実行中にエラーが発生: {ex.Message}");
                return ActionResult.Failure($"Reach アクション実行エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// Reach状態のアニメーション/エフェクトを表示
        /// </summary>
        private async UniTask ShowReachAnimation(IActionContext context)
        {
            // TODO: 実際のReachアニメーション/エフェクトを実装
            // 手札のカードをハイライトしたり、特殊なエフェクトを表示
            
            // 仮実装：手札のカードを一時的にハイライト
            var hands = context.RequesterPlayer.Hands;
            foreach (var card in hands)
            {
                card.Highlight();
            }

            await UniTask.Delay(500); // アニメーション時間

            foreach (var card in hands)
            {
                card.Unhighlight();
            }

            Debug.Log("Reach アニメーション完了");
        }
    }
} 