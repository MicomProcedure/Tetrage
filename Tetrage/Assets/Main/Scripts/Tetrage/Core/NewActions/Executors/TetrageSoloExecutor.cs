using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Tetrage.Core.Contracts;
using Tetrage.Network.Gameplay;
using Tetrage.Core.Enums;

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

                // 2. 他のプレイヤー全員Targetの取得
                var otherPlayers = context.OtherPlayers?.ToList();
                if (otherPlayers == null || !otherPlayers.Any())
                {
                    return ActionResult.Failure("他のプレイヤーのTargetカードが見つかりません");
                }

                // 3. 自分のスートが他のプレイヤー全員のスートと異なるかチェック
                var isWin = CheckUniqueSuit(myTargetCard, otherPlayers, out var matchedPlayers);

                // 4. 勝者の決定
                var winners = DetermineWinners(context, isWin, matchedPlayers);

                // 5. 結果の返答
                var resultMessage = isWin ? "勝利" : "敗北";

                Debug.Log($"TetrageSolo アクション実行完了: プレイヤー {context.RequesterPlayer.UserId} のスート {myTargetCard.Suit} - 結果: {resultMessage}");

                var descriptor = new ActionRequestDescriptor
                {
                    actionType = ActionType.TetrageSolo,
                    actorPlayerId = context.RequesterPlayer.PlayerId,
                    targetCardIds = new[] { myTargetCard.Id },
                    actionStatusInt = isWin ? 1 : 0
                };

                return ActionResult.Success(descriptor);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"TetrageSolo アクション実行中にエラーが発生: {ex.Message}");
                return ActionResult.Failure($"TetrageSolo アクション実行エラー: {ex.Message}");
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
        /// 自分のスートが他の全プレイヤーのスートと異なるかチェック
        /// </summary>
        /// <param name="myCard">自分のTargetカード</param>
        /// <param name="otherPlayers">他のプレイヤー一覧</param>
        /// <param name="matchedPlayers">スートが一致したプレイヤーのリスト（outパラメータ）</param>
        /// <returns>全員と異なる場合true、誰か一人でも同じスートがあればfalse</returns>
        private bool CheckUniqueSuit(Models.Card myCard, List<IPlayer> otherPlayers, out List<IPlayer> matchedPlayers)
        {
            var mySuit = myCard.Suit;
            matchedPlayers = new List<IPlayer>();

            // 他のプレイヤーのカードの中に同じスートがあるかチェック
            foreach (var player in otherPlayers)
            {
                var targetCard = GetTargetCard(player);
                if (targetCard == null)
                {
                    Debug.LogWarning($"プレイヤー {player.UserId} のTargetカードが見つかりません");
                    continue;
                }

                if (targetCard.Suit == mySuit)
                {
                    matchedPlayers.Add(player);
                    Debug.Log($"スート一致検出: 自分 {mySuit} vs {player.UserId} {targetCard.Suit}");
                }
            }

            if (matchedPlayers.Any())
            {
                return false;
            }

            Debug.Log($"スート完全不一致: 自分 {mySuit} は他の全プレイヤーと異なります");
            return true; // 全員と異なるので勝利
        }

        /// <summary>
        /// 勝者を決定する
        /// </summary>
        /// <param name="context">アクションコンテキスト</param>
        /// <param name="isWin">リクエスターが勝利したかどうか</param>
        /// <param name="matchedPlayers">リクエスターとスートが一致したプレイヤーのリスト</param>
        /// <returns>勝者のリスト</returns>
        private List<IPlayer> DetermineWinners(
            IActionContext context,
            bool isWin,
            List<IPlayer> matchedPlayers)
        {
            var winners = new List<IPlayer>();

            if (isWin)
            {
                // 成功時: リクエスターが勝利
                winners.Add(context.RequesterPlayer);
                Debug.Log($"勝者: リクエスター（{context.RequesterPlayer.UserId}）");
            }
            else
            {
                // 失敗時: リクエスターとスートが一致したプレイヤー以外が勝利
                var allPlayers = new List<IPlayer> { context.RequesterPlayer };
                allPlayers.AddRange(context.OtherPlayers);

                var losers = new List<IPlayer> { context.RequesterPlayer };
                losers.AddRange(matchedPlayers);

                winners = allPlayers
                    .Where(p => !losers.Any(loser => loser.PlayerId == p.PlayerId))
                    .ToList();

                Debug.Log($"勝者: リクエスターとスート一致者以外（{winners.Count}人）");
            }

            return winners;
        }

        #endregion
    }
}