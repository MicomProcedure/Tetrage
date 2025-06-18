using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using Tetrage.Core.Contracts;

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

                // 4. 結果の返答（はい/いいえ）
                var resultMessage = isMatch ? "はい" : "いいえ";

                Debug.Log($"Check アクション実行完了: プレイヤー {context.RequesterPlayer.UserId} が {targetPlayer.UserId} をチェック - 結果: {resultMessage}");
                
                return ActionResult.Success(new { 
                    TargetPlayer = targetPlayer.UserId,
                    TargetCard = new { targetCard.Suit, targetCard.Number },
                    IsMatch = isMatch, 
                    Message = resultMessage 
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Check アクション実行中にエラーが発生: {ex.Message}");
                return ActionResult.Failure($"Check アクション実行エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 対象プレイヤーを選択（現在は仮実装：最初の他プレイヤー）
        /// </summary>
        private async UniTask<IPlayer> SelectTargetPlayer(IActionContext context)
        {
            // TODO: 実際のUI選択処理を実装
            // プレイヤーが相手を選択するUI
            
            await UniTask.Delay(100); // UI表示の仮の時間

            // 仮実装：最初の他プレイヤーを選択
            var targetPlayer = context.OtherPlayers.FirstOrDefault();
            
            if (targetPlayer != null)
            {
                Debug.Log($"仮実装：対象プレイヤー選択 - {targetPlayer.UserId}");
            }
            
            return targetPlayer;
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