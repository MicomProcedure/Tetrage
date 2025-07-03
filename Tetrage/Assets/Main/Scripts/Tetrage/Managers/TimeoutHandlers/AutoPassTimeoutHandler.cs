using UnityEngine;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Actions;

namespace Tetrage.Managers.TimeoutHandlers
{
    /// <summary>
    /// タイムアウト時に自動でパスアクションを実行するハンドラー
    /// </summary>
    public class AutoPassTimeoutHandler : ITimeoutHandler
    {
        public bool CanHandle(TimeoutContext context)
        {
            // 基本的にすべてのタイムアウトを処理可能
            return true;
        }

        public async UniTask<TimeoutHandleResult> HandleTimeoutAsync(TimeoutContext context)
        {
            Debug.Log($"AutoPassTimeoutHandler: プレイヤー {context.Player.PlayerId} が {context.TimeoutSeconds}秒でタイムアウト - 自動パス実行");

            try
            {
                // 自動でパスアクションを実行
                var passResult = await context.Player.PassAsync();

                if (passResult.IsSuccess)
                {
                    return TimeoutHandleResult.ContinueGame(
                        advanceTurn: true,
                        message: $"プレイヤー {context.Player.PlayerId} がタイムアウトのため自動パスしました"
                    );
                }
                else
                {
                    Debug.LogWarning($"AutoPassTimeoutHandler: 自動パス失敗 - {passResult.ErrorMessage}");

                    // パス失敗時はターンを進めるのみ
                    return TimeoutHandleResult.ContinueGame(
                        advanceTurn: true,
                        message: $"プレイヤー {context.Player.PlayerId} のタイムアウト処理でエラーが発生"
                    );
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"AutoPassTimeoutHandler: 例外発生 - {ex.Message}");

                // 例外時もゲーム続行
                return TimeoutHandleResult.ContinueGame(
                    advanceTurn: true,
                    message: "タイムアウト処理中に例外が発生しました"
                );
            }
        }
    }
}