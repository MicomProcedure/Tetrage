using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Pass アクションの実行処理を担当するクラス
    /// 何もしないアクション（no-op）
    /// </summary>
    public class PassExecutor : IActionExecutor
    {
        public async UniTask<ActionResult> ExecuteAsync(IActionContext context)
        {
            try
            {
                // Passアクションは何もしないアクションなので、
                // 実際の処理は行わず、成功を返す
                Debug.Log($"Pass アクション実行: プレイヤー {context.RequesterPlayer.UserId}");

                // 短時間待機してリアルな処理感を演出（オプション）
                await UniTask.Delay(50);

                return ActionResult.Success("Pass アクションが正常に実行されました");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Pass アクション実行中にエラーが発生: {ex.Message}");
                return ActionResult.Failure($"Pass アクション実行エラー: {ex.Message}");
            }
        }
    }
}