using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Tetrage.Services
{
    /// <summary>
    /// タイムアウト機能を提供するサービスクラス
    /// ステートレス設計で、任意の非同期タスクに対してタイムアウト機能を付加する
    /// </summary>
    public static class TimeoutService
    {
        #region 公開メソッド

        /// <summary>
        /// 指定された非同期タスクを指定時間内で待機する
        /// </summary>
        /// <typeparam name="T">タスクの戻り値の型</typeparam>
        /// <param name="task">待機するタスク</param>
        /// <param name="timeoutSeconds">タイムアウト時間（秒）、0以下で無限待機</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>タスクの実行結果</returns>
        /// <exception cref="TimeoutException">タイムアウトが発生した場合</exception>
        /// <exception cref="OperationCanceledException">キャンセルされた場合</exception>
        public static async UniTask<T> WaitWithTimeout<T>(
            UniTask<T> task,
            float timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            // タイムアウトなしの場合は直接待機
            if (timeoutSeconds <= 0)
            {
                return await task;
            }

            // タイムアウト用のCancellationTokenSourceを作成
            using var timeoutCts = CreateTimeoutCancellationTokenSource(timeoutSeconds, cancellationToken);

            try
            {
                // タイムアウト制御を付加してタスクを実行
                return await task.AttachExternalCancellation(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (IsTimeoutCancellation(timeoutCts, cancellationToken))
            {
                // タイムアウト発生時はTimeoutExceptionをスロー
                throw new TimeoutException(timeoutSeconds, typeof(T));
            }
            // 外部キャンセル時はOperationCanceledExceptionをそのまま伝播
        }

        /// <summary>
        /// 指定された非同期タスクを指定時間内で待機する（戻り値なし）
        /// </summary>
        /// <param name="task">待機するタスク</param>
        /// <param name="timeoutSeconds">タイムアウト時間（秒）、0以下で無限待機</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <exception cref="TimeoutException">タイムアウトが発生した場合</exception>
        /// <exception cref="OperationCanceledException">キャンセルされた場合</exception>
        public static async UniTask WaitWithTimeout(
            UniTask task,
            float timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            // タイムアウトなしの場合は直接待機
            if (timeoutSeconds <= 0)
            {
                await task;
                return;
            }

            // タイムアウト用のCancellationTokenSourceを作成
            using var timeoutCts = CreateTimeoutCancellationTokenSource(timeoutSeconds, cancellationToken);

            try
            {
                // タイムアウト制御を付加してタスクを実行
                await task.AttachExternalCancellation(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (IsTimeoutCancellation(timeoutCts, cancellationToken))
            {
                // タイムアウト発生時はTimeoutExceptionをスロー
                throw new TimeoutException(timeoutSeconds, typeof(void));
            }
            // 外部キャンセル時はOperationCanceledExceptionをそのまま伝播
        }

        #endregion

        #region 内部メソッド

        /// <summary>
        /// タイムアウト用のCancellationTokenSourceを作成する
        /// </summary>
        /// <param name="timeoutSeconds">タイムアウト時間（秒）</param>
        /// <param name="externalToken">外部キャンセルトークン</param>
        /// <returns>タイムアウトと外部キャンセルを組み合わせたCancellationTokenSource</returns>
        private static CancellationTokenSource CreateTimeoutCancellationTokenSource(
            float timeoutSeconds,
            CancellationToken externalToken)
        {
            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            return timeoutCts;
        }

        /// <summary>
        /// キャンセルがタイムアウトによるものかを判定する
        /// </summary>
        /// <param name="timeoutCts">タイムアウト用CancellationTokenSource</param>
        /// <param name="externalToken">外部キャンセルトークン</param>
        /// <returns>タイムアウトによるキャンセルの場合true</returns>
        private static bool IsTimeoutCancellation(
            CancellationTokenSource timeoutCts,
            CancellationToken externalToken)
        {
            return timeoutCts.Token.IsCancellationRequested && !externalToken.IsCancellationRequested;
        }

        #endregion

        #region 例外定義

        /// <summary>
        /// タイムアウト発生時にスローされる例外
        /// </summary>
        public class TimeoutException : Exception
        {
            /// <summary>
            /// タイムアウト時間（秒）
            /// </summary>
            public float TimeoutSeconds { get; }

            /// <summary>
            /// タイムアウトしたタスクの戻り値型
            /// </summary>
            public Type TaskReturnType { get; }

            /// <summary>
            /// タイムアウト発生時刻
            /// </summary>
            public DateTime TimeoutOccurred { get; }

            /// <summary>
            /// TimeoutExceptionのコンストラクタ
            /// </summary>
            /// <param name="timeoutSeconds">タイムアウト時間（秒）</param>
            /// <param name="taskReturnType">タスクの戻り値型</param>
            public TimeoutException(float timeoutSeconds, Type taskReturnType)
                : base($"タスクが{timeoutSeconds}秒以内に完了しませんでした（戻り値型: {taskReturnType?.Name ?? "void"}）")
            {
                TimeoutSeconds = timeoutSeconds;
                TaskReturnType = taskReturnType;
                TimeoutOccurred = DateTime.Now;
            }

            /// <summary>
            /// TimeoutExceptionのコンストラクタ（カスタムメッセージ）
            /// </summary>
            /// <param name="timeoutSeconds">タイムアウト時間（秒）</param>
            /// <param name="taskReturnType">タスクの戻り値型</param>
            /// <param name="message">カスタムメッセージ</param>
            public TimeoutException(float timeoutSeconds, Type taskReturnType, string message)
                : base(message)
            {
                TimeoutSeconds = timeoutSeconds;
                TaskReturnType = taskReturnType;
                TimeoutOccurred = DateTime.Now;
            }

            /// <summary>
            /// TimeoutExceptionのコンストラクタ（内部例外付き）
            /// </summary>
            /// <param name="timeoutSeconds">タイムアウト時間（秒）</param>
            /// <param name="taskReturnType">タスクの戻り値型</param>
            /// <param name="message">メッセージ</param>
            /// <param name="innerException">内部例外</param>
            public TimeoutException(float timeoutSeconds, Type taskReturnType, string message, Exception innerException)
                : base(message, innerException)
            {
                TimeoutSeconds = timeoutSeconds;
                TaskReturnType = taskReturnType;
                TimeoutOccurred = DateTime.Now;
            }
        }

        #endregion
    }
}