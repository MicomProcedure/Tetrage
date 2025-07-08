using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Actions;
using Tetrage.Core.Contracts;
using Tetrage.Services;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクション待機・タイムアウト処理を担う専用クラス
    /// Dealer などの上位クラスは本クラスを利用して非同期にプレイヤーアクションを待機する
    /// </summary>
    public class ActionAwaiter : IDisposable
    {
        #region フィールド
        private readonly ActionManager _actionManager;
        private ITimeoutHandler _timeoutHandler;

        private UniTaskCompletionSource<ActionResult> _completionSource;
        private CancellationTokenSource _cancellationTokenSource;
        private IPlayer _waitingPlayer;
        #endregion

        #region コンストラクタ
        public ActionAwaiter(ActionManager actionManager, ITimeoutHandler timeoutHandler)
        {
            _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
            _timeoutHandler = timeoutHandler ?? throw new ArgumentNullException(nameof(timeoutHandler));

            // Action 完了イベントに購読
            _actionManager.OnActionCompleted += OnActionCompleted;
        }
        #endregion


        #region 公開API

        /// <summary>
        /// 指定プレイヤーのアクションを非同期に待機します。
        /// </summary>
        /// <param name="player">待機対象プレイヤー</param>
        /// <param name="doTimeout">タイムアウトを有効にするかどうか</param>
        /// <param name="timeoutSeconds">タイムアウト時間（0以下で無制限）</param>
        /// <returns>アクション実行結果</returns>
        public async UniTask<ActionResult> WaitForPlayerActionAsync(IPlayer player, bool doTimeout = false, float timeoutSeconds = 0)
        {
            if (doTimeout)
            {
                return await WaitForPlayerActionAsync(player, timeoutSeconds);
            }
            else
            {
                return await WaitForPlayerActionAsync(player, timeoutSeconds: 0);
            }
        }

        /// <summary>
        /// 指定プレイヤーのアクションを非同期に待機します。
        /// </summary>
        /// <param name="player">待機対象プレイヤー</param>
        /// <param name="timeoutSeconds">タイムアウト時間（0以下で無制限）</param>
        /// <returns>アクション実行結果</returns>
        /// <exception cref="TimeoutService.TimeoutException">タイムアウト発生時</exception>
        /// <exception cref="OperationCanceledException">キャンセルされた場合</exception>
        public async UniTask<ActionResult> WaitForPlayerActionAsync(IPlayer player, float timeoutSeconds = 0)
        {
            if (player == null) return ActionResult.Failure("現在のプレイヤーが設定されていません");

            // 既存の待機をキャンセル
            CancelWaiting();

            _waitingPlayer = player;
            _completionSource = new UniTaskCompletionSource<ActionResult>();
            _cancellationTokenSource = new CancellationTokenSource();

            CancellationToken token = _cancellationTokenSource.Token;

            try
            {
                Debug.Log($"ActionAwaiter: プレイヤー {_waitingPlayer.PlayerId} のアクションを待機中...");

                // TimeoutService を使用して待機
                return await TimeoutService.WaitWithTimeout(
                    _completionSource.Task,
                    timeoutSeconds,
                    token);
            }
            catch (TimeoutService.TimeoutException ex)
            {
                // タイムアウト時の処理を委譲
                Debug.Log($"ActionAwaiter: タイムアウト発生 - プレイヤー {_waitingPlayer.PlayerId}");

                var timeoutContext = new TimeoutContext(_waitingPlayer, ex.TimeoutSeconds, ActionResult.Failure("TIMEOUT"));
                var handleResult = await _timeoutHandler.HandleTimeoutAsync(timeoutContext);

                // TimeoutHandler の結果によってゲーム継続可否は呼び出し元で判断
                return timeoutContext.OriginalResult;
            }
            finally
            {
                Cleanup();
            }
        }


        /// <summary>
        /// 現在の待機をキャンセルします。
        /// </summary>
        public void CancelWaiting()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            _completionSource?.TrySetCanceled();
            Cleanup();
        }

        /// <summary>
        /// TimeoutHandler を差し替えます。
        /// </summary>
        public void SetTimeoutHandler(ITimeoutHandler timeoutHandler)
        {
            _timeoutHandler = timeoutHandler ?? throw new ArgumentNullException(nameof(timeoutHandler));
        }
        #endregion

        #region 内部イベントハンドラー
        /// <summary>
        /// ActionManager からのアクション完了イベント
        /// </summary>
        private void OnActionCompleted(IAction action, IActionContext context, ActionResult result)
        {
            if (_completionSource == null || _waitingPlayer == null)
                return;

            // 待機対象プレイヤーか確認
            if (!ReferenceEquals(context.RequesterPlayer, _waitingPlayer))
                return;

            Debug.Log($"ActionAwaiter: プレイヤー {_waitingPlayer.PlayerId} のアクション {action.ActionType} が完了");

            _completionSource.TrySetResult(result);
        }
        #endregion

        #region リソース解放
        private void Cleanup()
        {
            _waitingPlayer = null;
            _completionSource = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public void Dispose()
        {
            CancelWaiting();
            _actionManager.OnActionCompleted -= OnActionCompleted;
        }
        #endregion
    }
}