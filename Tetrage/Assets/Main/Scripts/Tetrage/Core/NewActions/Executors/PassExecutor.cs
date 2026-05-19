using Cysharp.Threading.Tasks;
using UnityEngine;
using Tetrage.Core.Enums;
using Tetrage.Network.Gameplay;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Pass アクションの実行処理を担当するクラス。
    /// ゲーム状態の変更はなく、Host へ ActionResult 同期のみ行う。
    /// </summary>
    public class PassExecutor : IActionExecutor
    {
        public async UniTask<ActionResult> ExecuteAsync(IActionContext context)
        {
            try
            {
                Debug.Log($"Pass アクション実行完了(送信準備): プレイヤー {context.RequesterPlayer.UserId}");

                var descriptor = new ActionRequestDescriptorPacket
                {
                    actionType = ActionType.Pass,
                    actorPlayerId = context.RequesterPlayer.PlayerId,
                    targetIds = System.Array.Empty<int>(),
                };

                await UniTask.Yield();
                return ActionResult.Success(descriptor);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Pass アクション実行中にエラーが発生: {ex.Message}");
                return ActionResult.Failure($"Pass アクション実行エラー: {ex.Message}");
            }
        }
    }
}
