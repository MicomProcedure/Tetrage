using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Tetrage.Core.Contracts;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// TetrageMulti アクションの実行処理を担当するクラス
    /// 自分のターゲットカードのスートが他のいずれかのプレイヤーのターゲットカードのスートと一致する場合に勝利
    /// </summary>
    public class TetrageMultiExecutor : IActionExecutor
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

                // 3. 一緒にテトラージュマルチをする人を選択
                var targetPlayers = await SelectTargetPlayers(context);
                if (targetPlayers == null || !targetPlayers.Any())
                {
                    return ActionResult.Failure("一緒にテトラージュマルチをする人を選択できませんでした");
                }

                // 4. 選択した人が出すかどうかを選択
                var openPlayers = await SelectIsOpen(context, targetPlayers);
                if (openPlayers == null)
                {
                    return ActionResult.Failure("出すかどうかを選択できませんでした");
                }

                // 参加者リスト（リクエスター + ターゲットプレイヤー）
                var participants = new List<IPlayer> { context.RequesterPlayer };
                participants.AddRange(targetPlayers);

                // 5. テトラージュマルチの成功判定
                var isSuccess = CheckTetrageMultiSuccess(context, participants, openPlayers);

                // 6. 勝者の決定
                var winners = DetermineWinners(context, participants, openPlayers, isSuccess, myTargetCard);

                // 7. 結果の返答
                var resultMessage = isSuccess ? "テトラージュマルチ成功" : "テトラージュマルチ失敗";
                
                Debug.Log($"TetrageMulti アクション実行完了: プレイヤー {context.RequesterPlayer.UserId} - 結果: {resultMessage}");

                return ActionResult.Success(new
                {
                    MyTargetCard = new { myTargetCard.Suit, myTargetCard.Number },
                    Participants = participants.Select(p => p.UserId).ToArray(),
                    OpenPlayers = openPlayers.Select(p => p.UserId).ToArray(),
                    Winners = winners.Select(p => p.UserId).ToArray(),
                    IsSuccess = isSuccess,
                    Message = resultMessage
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"TetrageMulti アクション実行中にエラーが発生: {ex.Message}");
                return ActionResult.Failure($"TetrageMulti アクション実行エラー: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods - Card Operations

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

        #endregion

        #region Private Methods - UI Selection

        /// <summary>
        /// 一緒にテトラージュマルチをする人を選択
        /// </summary>
        private async UniTask<List<IPlayer>> SelectTargetPlayers(IActionContext context)
        {
            // TODO: 実際のUI選択処理を実装
            // プレイヤーが一緒にテトラージュマルチをする人を選択するUI
            await UniTask.Delay(100); // UI表示の仮の時間

            // クリックしたプレイヤー達を返す
            // var targetPlayers = context.OtherPlayers.Where(player => player.IsSelected).ToList();
            // return targetPlayers;
            
            // 仮実装：全員を返す
            return context.OtherPlayers.ToList();
        }

        /// <summary>
        /// 選択した人が出すかどうかを選択
        /// </summary>
        private async UniTask<List<IPlayer>> SelectIsOpen(IActionContext context, List<IPlayer> targetPlayers)
        {
            // TODO: 実際のUI選択処理を実装
            // プレイヤーがOpenするかどうかを選択するUI
            await UniTask.Delay(100); // UI表示の仮の時間

            // 出すことにしたプレイヤー達を返す
            // var openPlayers = targetPlayers.Where(player => player.IsOpen).ToList();
            // return openPlayers;
            
            // 仮実装：リクエスターは常にOpenとして扱う
            return new List<IPlayer> { context.RequesterPlayer };
        }

        #endregion

        #region Private Methods - Game Logic

        /// <summary>
        /// テトラージュマルチの成功判定
        /// </summary>
        /// <param name="context">アクションコンテキスト</param>
        /// <param name="participants">参加者リスト（リクエスター含む）</param>
        /// <param name="openPlayers">Openしたプレイヤーのリスト</param>
        /// <returns>成功ならtrue、失敗ならfalse</returns>
        private bool CheckTetrageMultiSuccess(IActionContext context, List<IPlayer> participants, List<IPlayer> openPlayers)
        {
            // 1. 参加者全員がOpenしたかどうかを判定
            if (participants.Count != openPlayers.Count)
            {
                Debug.Log($"テトラージュマルチ失敗: 参加者全員がOpenしていません（参加者:{participants.Count}, Open:{openPlayers.Count}）");
                return false;
            }

            // 2. 参加者全員のターゲットカードのスートが同じかどうか
            var participantCards = participants.Select(player => GetTargetCard(player)).ToList();
            
            // null チェック
            if (participantCards.Any(card => card == null))
            {
                Debug.LogError("テトラージュマルチ失敗: 参加者のTargetカードが見つかりません");
                return false;
            }

            var firstSuit = participantCards.First().Suit;
            var allSameSuit = participantCards.All(card => card.Suit == firstSuit);
            
            if (!allSameSuit)
            {
                Debug.Log($"テトラージュマルチ失敗: 参加者全員のスートが一致していません");
                return false;
            }

            Debug.Log($"テトラージュマルチ成功: 参加者全員のスートが {firstSuit} で一致しました");
            return true;
        }

        /// <summary>
        /// 勝者を決定する
        /// </summary>
        /// <param name="context">アクションコンテキスト</param>
        /// <param name="participants">参加者リスト（リクエスター含む）</param>
        /// <param name="openPlayers">Openしたプレイヤーのリスト</param>
        /// <param name="isSuccess">成功したかどうか</param>
        /// <param name="requesterCard">リクエスターのTargetカード</param>
        /// <returns>勝者のリスト</returns>
        private List<IPlayer> DetermineWinners(
            IActionContext context,
            List<IPlayer> participants,
            List<IPlayer> openPlayers,
            bool isSuccess,
            Models.Card requesterCard)
        {
            var winners = new List<IPlayer>();

            if (isSuccess)
            {
                // 成功時: 参加者全員が勝ち
                winners.AddRange(participants);
                Debug.Log($"勝者: 参加者全員（{participants.Count}人）");
            }
            else
            {
                // 失敗時の処理
                
                // Openしたプレイヤーのスートを取得
                var openPlayersSuits = openPlayers
                    .Select(p => GetTargetCard(p)?.Suit)
                    .Where(suit => suit.HasValue)
                    .Select(suit => suit.Value)
                    .Distinct()
                    .ToList();

                // 条件1: Openした参加者 かつ リクエスターと違うスートの人
                var openWinners = openPlayers
                    .Where(p => p.PlayerId != context.RequesterPlayer.PlayerId) // リクエスター以外
                    .Where(p => 
                    {
                        var card = GetTargetCard(p);
                        return card != null && card.Suit != requesterCard.Suit;
                    })
                    .ToList();
                
                winners.AddRange(openWinners);
                Debug.Log($"勝者（条件1 - Openかつリクエスターと違うスート）: {openWinners.Count}人");

                // 条件2: Openしていない かつ Openした人たちとスートが違う人
                var allPlayers = new List<IPlayer> { context.RequesterPlayer };
                allPlayers.AddRange(context.OtherPlayers);
                
                var notOpenWinners = allPlayers
                    .Where(p => !openPlayers.Any(op => op.PlayerId == p.PlayerId)) // Openしていない
                    .Where(p =>
                    {
                        var card = GetTargetCard(p);
                        if (card == null) return false;
                        
                        // Openした人たちのスートと全て異なる
                        return !openPlayersSuits.Contains(card.Suit);
                    })
                    .ToList();
                
                winners.AddRange(notOpenWinners);
                Debug.Log($"勝者（条件2 - Openしていないかつスート違い）: {notOpenWinners.Count}人");
            }

            return winners.Distinct().ToList(); // 重複を除去
        }

        #endregion
    }
}