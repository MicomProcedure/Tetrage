using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using Tetrage.Core.Enums;
using Tetrage.Network.Gameplay;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Reach アクションの実行処理を担当するクラス。
    /// 手札の表向き化と Reach 状態は Host 権威のネットワークイベントで適用する。
    /// </summary>
    public class ReachExecutor : IActionExecutor
    {
        public async UniTask<ActionResult> ExecuteAsync(IActionContext context)
        {
            try
            {
                var hands = context.RequesterPlayer.Hands;

                // 裏向きの手札IDを Host へ送る（表向き化は CardVisibilityChanged で全員同期）
                var faceDownCardIds = hands
                    .Where(card => !card.IsFaceUp)
                    .Select(card => card.Id.Value)
                    .ToArray();

                Debug.Log(
                    $"Reach アクション実行完了(送信準備): プレイヤー {context.RequesterPlayer.UserId}, 表向き化対象={faceDownCardIds.Length}枚");

                var descriptor = new ActionRequestDescriptorPacket
                {
                    actionType = ActionType.Reach,
                    actorPlayerId = context.RequesterPlayer.PlayerId,
                    targetIds = faceDownCardIds,
                };

                await UniTask.Yield();
                return ActionResult.Success(descriptor);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Reach アクション実行中にエラーが発生: {ex.Message}");
                return ActionResult.Failure($"Reach アクション実行エラー: {ex.Message}");
            }
        }
    }
}
