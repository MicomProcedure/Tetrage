using Tetrage.Core.Contracts;
using Tetrage.Managers.TimeoutHandlers;
using UnityEngine;

namespace Tetrage.Factories
{
    /// <summary>
    /// タイムアウトハンドラーの作成と組み合わせを行うファクトリー
    /// </summary>
    public static class TimeoutHandlerFactory
    {
        /// <summary>
        /// 自動パス処理のみのシンプルなハンドラー
        /// </summary>
        public static ITimeoutHandler CreateAutoPass()
        {
            return new AutoPassTimeoutHandler();
        }

        /// <summary>
        /// UI通知のみのハンドラー
        /// </summary>
        public static ITimeoutHandler CreateUINotification(float duration = 3f)
        {
            return new UINotificationTimeoutHandler(duration);
        }

        /// <summary>
        /// タイムアウト時にゲーム終了するハンドラー
        /// </summary>
        public static ITimeoutHandler CreateGameEnd()
        {
            return new GameEndTimeoutHandler();
        }

        /// <summary>
        /// UI通知 + ゲーム終了の組み合わせ
        /// </summary>
        public static ITimeoutHandler CreateNotificationWithGameEnd(float notificationDuration = 2f)
        {
            return new CompositeTimeoutHandler()
                .AddHandler(new UINotificationTimeoutHandler(notificationDuration))
                .AddHandler(new GameEndTimeoutHandler());
        }

        /// <summary>
        /// UI通知 + 自動パスの組み合わせ
        /// </summary>
        public static ITimeoutHandler CreateNotificationWithAutoPass(float notificationDuration = 3f)
        {
            return new CompositeTimeoutHandler()
                .AddHandler(new UINotificationTimeoutHandler(notificationDuration))
                .AddHandler(new AutoPassTimeoutHandler());
        }

        /// <summary>
        /// デバッグ用：ログ出力のみ
        /// </summary>
        public static ITimeoutHandler CreateDebugOnly()
        {
            return new DebugTimeoutHandler();
        }

        /// <summary>
        /// カスタマイズ可能なCompositeハンドラー
        /// </summary>
        public static CompositeTimeoutHandler CreateComposite(bool stopOnFirstSuccess = false)
        {
            return new CompositeTimeoutHandler(stopOnFirstSuccess);
        }

        /// <summary>
        /// ゲームモード別の推奨ハンドラー
        /// </summary>
        public static ITimeoutHandler CreateForGameMode(GameMode mode)
        {
            return mode switch
            {
                GameMode.Casual => CreateNotificationWithAutoPass(2f),
                GameMode.Competitive => CreateAutoPass(),
                GameMode.Debug => CreateDebugOnly(),
                _ => CreateAutoPass()
            };
        }
    }

    /// <summary>
    /// ゲームモード（サンプル）
    /// </summary>
    public enum GameMode
    {
        Casual,
        Competitive,
        Debug
    }

    /// <summary>
    /// デバッグ用のタイムアウトハンドラー
    /// </summary>
    public class DebugTimeoutHandler : ITimeoutHandler
    {
        public bool CanHandle(TimeoutContext context)
        {
            return true;
        }

        public async Cysharp.Threading.Tasks.UniTask<TimeoutHandleResult> HandleTimeoutAsync(TimeoutContext context)
        {
            Debug.Log($"[DEBUG] タイムアウト発生: プレイヤー{context.Player.PlayerId}, {context.TimeoutSeconds}秒, {context.OccurredAt}");

            // 実際の処理なし、ログ出力のみ
            await Cysharp.Threading.Tasks.UniTask.Delay(100);

            return TimeoutHandleResult.ContinueGame(
                advanceTurn: true,
                message: "デバッグモード: タイムアウトをログ出力しました"
            );
        }
    }
}