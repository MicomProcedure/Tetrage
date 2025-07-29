using UnityEngine;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;

namespace Tetrage.Managers.TimeoutHandlers
{
    /// <summary>
    /// タイムアウト発生時に即座にゲームを終了するハンドラー
    /// </summary>
    public class GameEndTimeoutHandler : ITimeoutHandler
    {
        public bool CanHandle(TimeoutContext context)
        {
            // すべてのタイムアウトを処理可能
            return true;
        }

        public async UniTask<TimeoutHandleResult> HandleTimeoutAsync(TimeoutContext context)
        {
            Debug.Log($"GameEndTimeoutHandler: プレイヤー {context.Player.PlayerId} が {context.TimeoutSeconds}秒でタイムアウト - ゲーム終了");

            try
            {
                // 短い待機時間でUI表示などの余裕を作る
                await UniTask.Delay(100);

                // ゲーム終了を指示
                return TimeoutHandleResult.EndGame($"プレイヤー {context.Player.PlayerId} のタイムアウトによりゲームが終了しました");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameEndTimeoutHandler: 例外発生 - {ex.Message}");

                // 例外が発生してもゲーム終了を指示
                return TimeoutHandleResult.EndGame("タイムアウト処理中に例外が発生しました");
            }
        }
    }
}