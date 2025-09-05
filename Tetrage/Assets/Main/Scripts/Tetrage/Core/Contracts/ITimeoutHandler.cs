using System;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Actions;

namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// プレイヤーアクションタイムアウト時の処理を定義するインターフェース
    /// </summary>
    public interface ITimeoutHandler
    {
        /// <summary>
        /// タイムアウト時の処理を実行する
        /// </summary>
        /// <param name="context">タイムアウトコンテキスト</param>
        /// <returns>処理結果</returns>
        UniTask<TimeoutHandleResult> HandleTimeoutAsync(TimeoutContext context);

        /// <summary>
        /// このハンドラーが指定されたタイムアウトを処理できるかを判定
        /// </summary>
        /// <param name="context">タイムアウトコンテキスト</param>
        /// <returns>処理可能な場合true</returns>
        bool CanHandle(TimeoutContext context);
    }

    /// <summary>
    /// タイムアウト発生時のコンテキスト情報
    /// </summary>
    public class TimeoutContext
    {
        public IPlayer Player { get; }
        public float TimeoutSeconds { get; }
        public DateTime OccurredAt { get; }
        public ActionResult OriginalResult { get; }

        public TimeoutContext(IPlayer player, float timeoutSeconds, ActionResult originalResult)
        {
            Player = player;
            TimeoutSeconds = timeoutSeconds;
            OccurredAt = DateTime.Now;
            OriginalResult = originalResult;
        }
    }

    /// <summary>
    /// タイムアウト処理結果
    /// </summary>
    public class TimeoutHandleResult
    {
        public bool ShouldContinueGame { get; }
        public bool ShouldAdvanceTurn { get; }
        public string Message { get; }
        public object AdditionalData { get; }

        public TimeoutHandleResult(bool shouldContinueGame, bool shouldAdvanceTurn, string message = null, object additionalData = null)
        {
            ShouldContinueGame = shouldContinueGame;
            ShouldAdvanceTurn = shouldAdvanceTurn;
            Message = message;
            AdditionalData = additionalData;
        }

        public static TimeoutHandleResult ContinueGame(bool advanceTurn = true, string message = null)
            => new TimeoutHandleResult(true, advanceTurn, message);

        public static TimeoutHandleResult EndGame(string message = null)
            => new TimeoutHandleResult(false, false, message);
    }
}