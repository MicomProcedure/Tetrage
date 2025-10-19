using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Tetrage.Core.Contracts;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// TetrageSolo アクションの実行処理を担当するクラス
    /// 自分のターゲットカードのスートが他の全プレイヤーのターゲットカードのスートと全て異なる場合に勝利
    /// </summary>
    public class TetrageSoloExecutor : IActionExecutor
    {
        #region IActionExecutor Implementation

        public async UniTask<ActionResult> ExecuteAsync(IActionContext context)
        {
            try
            {
                await UniTask.Yield();
                // 1. 自分のTargetカードの取得
                var myTargetCard = GetTargetCard(context.RequesterPlayer);
                if (myTargetCard == null)
                {
                    return ActionResult.Failure("自分のTargetカードが見つかりません");
                }

                // 2. 他のプレイヤー全員のTargetカードの取得
                var otherTargetCards = GetAllOtherTargetCards(context);
                if (otherTargetCards == null || !otherTargetCards.Any())
                {
                    return ActionResult.Failure("他のプレイヤーのTargetカードが見つかりません");
                }

                // 3. 自分のスートが他のプレイヤー全員のスートと異なるかチェック
                var isWin = CheckUniqueSuit(myTargetCard, otherTargetCards);

                // 4. 結果の返答（勝利/敗北）
                var resultMessage = isWin ? "勝利" : "敗北";

                Debug.Log($"TetrageSolo アクション実行完了: プレイヤー {context.RequesterPlayer.UserId} のスート {myTargetCard.Suit} - 結果: {resultMessage}");

                return ActionResult.Success(new
                {
                    MyTargetCard = new { myTargetCard.Suit, myTargetCard.Number },
                    OtherTargetCards = otherTargetCards.Select(card => new { card.Suit, card.Number }).ToArray(),
                    IsWin = isWin,
                    Message = resultMessage
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"TetrageSolo アクション実行中にエラーが発生: {ex.Message}");
                return ActionResult.Failure($"TetrageSolo アクション実行エラー: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 指定されたプレイヤーのTargetカードを取得
        /// </summary>
        private Models.Card GetTargetCard(IPlayer player)
        {
            return player.Target.FirstOrDefault();
        }

        /// <summary>
        /// 他の全プレイヤーのTargetカードを取得
        /// </summary>
        private List<Models.Card> GetAllOtherTargetCards(IActionContext context)
        {
            var targetCards = new List<Models.Card>();

            foreach (var player in context.OtherPlayers)
            {
                var targetCard = GetTargetCard(player);
                if (targetCard != null)
                {
                    targetCards.Add(targetCard);
                }
                else
                {
                    Debug.LogWarning($"プレイヤー {player.UserId} のTargetカードが見つかりません");
                }
            }

            return targetCards;
        }

        /// <summary>
        /// 自分のスートが他の全プレイヤーのスートと異なるかチェック
        /// </summary>
        /// <param name="myCard">自分のTargetカード</param>
        /// <param name="otherCards">他のプレイヤーのTargetカードリスト</param>
        /// <returns>全員と異なる場合true、誰か一人でも同じスートがあればfalse</returns>
        private bool CheckUniqueSuit(Models.Card myCard, List<Models.Card> otherCards)
        {
            var mySuit = myCard.Suit;

            // 他のプレイヤーのカードの中に同じスートがあるかチェック
            foreach (var card in otherCards)
            {
                if (card.Suit == mySuit)
                {
                    Debug.Log($"スート一致検出: 自分 {mySuit} vs 他プレイヤー {card.Suit}");
                    return false; // 同じスートが見つかったので敗北
                }
            }

            Debug.Log($"スート完全不一致: 自分 {mySuit} は他の全プレイヤーと異なります");
            return true; // 全員と異なるので勝利
        }

        #endregion
    }
} 