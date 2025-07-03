using UnityEngine;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;

namespace Tetrage.Managers.TimeoutHandlers
{
    /// <summary>
    /// タイムアウト時にUI通知を表示するハンドラー
    /// </summary>
    public class UINotificationTimeoutHandler : ITimeoutHandler
    {
        private readonly float _notificationDuration;

        public UINotificationTimeoutHandler(float notificationDuration = 3f)
        {
            _notificationDuration = notificationDuration;
        }

        public bool CanHandle(TimeoutContext context)
        {
            // UIが必要な状況でのみ処理
            return Application.isPlaying;
        }

        public async UniTask<TimeoutHandleResult> HandleTimeoutAsync(TimeoutContext context)
        {
            Debug.Log($"UINotificationTimeoutHandler: プレイヤー {context.Player.PlayerId} のタイムアウト通知を表示");

            try
            {
                // TODO: 実際のUIManagerがあれば以下のように呼び出し
                // UIManager.ShowTimeoutNotification(context.Player.PlayerId, context.TimeoutSeconds);

                // 現在はデバッグ表示のみ
                await ShowTimeoutNotificationAsync(context);

                return TimeoutHandleResult.ContinueGame(
                    advanceTurn: false, // UI表示だけなのでターンは進めない
                    message: $"プレイヤー {context.Player.PlayerId} のタイムアウト通知を表示しました"
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"UINotificationTimeoutHandler: UI通知表示エラー - {ex.Message}");

                return TimeoutHandleResult.ContinueGame(
                    advanceTurn: false,
                    message: "UI通知の表示に失敗しました"
                );
            }
        }

        /// <summary>
        /// タイムアウト通知の表示（サンプル実装）
        /// </summary>
        private async UniTask ShowTimeoutNotificationAsync(TimeoutContext context)
        {
            // TODO: 実際のUI実装に置き換える
            // 例：
            // var notification = UIFactory.CreateTimeoutNotification();
            // notification.SetMessage($"プレイヤー {context.Player.PlayerId} がタイムアウトしました");
            // notification.Show();
            // await UniTask.Delay((int)(_notificationDuration * 1000));
            // notification.Hide();

            Debug.Log($"[UI通知] プレイヤー {context.Player.PlayerId} が {context.TimeoutSeconds}秒でタイムアウトしました");

            // 通知表示時間をシミュレート
            await UniTask.Delay((int)(_notificationDuration * 1000));
        }
    }
}