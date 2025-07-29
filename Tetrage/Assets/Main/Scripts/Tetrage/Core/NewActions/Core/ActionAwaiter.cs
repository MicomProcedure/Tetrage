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
        private CancellationTokenSource _actionCts;
        private CancellationTokenSource _timeoutCts; // タイムアウト専用のCancellationTokenSource
        private IPlayer _waitingPlayer;
        private float _currentTimeoutSeconds; // 現在のタイムアウト時間
        private bool _isWaiting; // 待機中かどうかのフラグ
        #endregion

        #region 公開プロパティ
        /// <summary>
        /// 現在の待機に関連するCancellationToken
        /// 子タスクはこのトークンを使用してキャンセル伝播を受け取る
        /// </summary>
        public CancellationToken CurrentCancellationToken => _actionCts?.Token ?? CancellationToken.None;

        /// <summary>
        /// 現在の待機タスク（読み取り専用）
        /// </summary>
        public UniTask<ActionResult> CurrentWaitingTask =>
            _completionSource?.Task ?? UniTask.FromResult(ActionResult.Failure("Not waiting"));
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
        /// <exception cref="OperationCanceledException">キャンセルされた場合</exception>
        public async UniTask<ActionResult> WaitForPlayerActionAsync(IPlayer player, float timeoutSeconds = 0)
        {
            if (player == null) return ActionResult.Failure("現在のプレイヤーが設定されていません");

            // 既存の待機をキャンセル
            CancelWaiting();

            _waitingPlayer = player;
            _currentTimeoutSeconds = timeoutSeconds;
            _isWaiting = true;
            _completionSource = new UniTaskCompletionSource<ActionResult>();
            _actionCts = new CancellationTokenSource();

            var token = _actionCts.Token;

            try
            {
                Debug.Log($"ActionAwaiter: プレイヤー {_waitingPlayer.PlayerId} のアクションを待機中...");

                if (timeoutSeconds > 0)
                {
                    // タイムアウト用のCancellationTokenSourceを作成
                    _timeoutCts = new CancellationTokenSource();

                    // タイムアウトタスクを開始
                    StartTimeoutTask(timeoutSeconds);

                    // CompletionSourceのタスクのみを待機（タイムアウトは独自実装で処理）
                    return await _completionSource.Task.AttachExternalCancellation(token);
                }
                else
                {
                    return await _completionSource.Task.AttachExternalCancellation(token);
                }
            }
            catch (OperationCanceledException)    // キャンセルの時にここに入る
            {
                Debug.Log("ActionAwaiter: 待機が外部からキャンセルされました。");
                throw; // 呼び出し元にキャンセルを伝播させる
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
            if (_actionCts != null && !_actionCts.Token.IsCancellationRequested)
            {
                _actionCts.Cancel();
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

        /// <summary>
        /// 現在の待機中のタイムアウト時間をリセットします
        /// UI入力が発生した時などに呼び出すことで、タイムアウト時間を延長できます
        /// </summary>
        public void ResetTimeout()
        {
            if (!_isWaiting || _currentTimeoutSeconds <= 0)
            {
                Debug.Log("ActionAwaiter: タイムアウトリセット要求 - 待機中でないか、タイムアウト設定されていません");
                return;
            }

            Debug.Log($"ActionAwaiter: タイムアウトをリセットします ({_currentTimeoutSeconds}秒)");

            // 現在のタイムアウト用CancellationTokenをキャンセル
            _timeoutCts?.Cancel();
            _timeoutCts?.Dispose();

            // 新しいタイムアウト用CancellationTokenSourceを作成
            _timeoutCts = new CancellationTokenSource();

            // 新しいタイムアウトタスクを開始
            StartTimeoutTask(_currentTimeoutSeconds);
        }

        /// <summary>
        /// タイムアウトタスクを開始
        /// </summary>
        private void StartTimeoutTask(float timeoutSeconds)
        {
            if (_timeoutCts == null) return;

            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken: _timeoutCts.Token)
                .ContinueWith(() => HandleTimeoutAsync().Forget()); // タイムアウト時にタイムアウト処理を実行

        }

        /// <summary>
        /// タイムアウト発生時の処理
        /// </summary>
        private async UniTaskVoid HandleTimeoutAsync()
        {
            if (!_isWaiting || _completionSource == null) return; // 待機中でないか、CompletionSourceがnullの場合は処理しない

            Debug.Log($"ActionAwaiter: タイムアウト発生 - プレイヤー {_waitingPlayer?.PlayerId}");

            // CancellationTokenをキャンセルして子タスクも停止させる
            if (_actionCts != null && !_actionCts.Token.IsCancellationRequested)
            {
                _actionCts.Cancel();
                Debug.Log("ActionAwaiter: タイムアウト時にCancellationTokenをキャンセルしました");
            }

            // タイムアウトにまつわる処理を実行
            var timeoutContext = new TimeoutContext(_waitingPlayer, _currentTimeoutSeconds, ActionResult.Failure("TIMEOUT"));
            var timeoutResult = await _timeoutHandler.HandleTimeoutAsync(timeoutContext);

            // TimeoutHandleResultに基づいてActionResultを作成
            ActionResult finalResult;
            if (timeoutResult.ShouldContinueGame)
            {
                // ゲーム継続の場合は元のタイムアウト結果を返す
                finalResult = timeoutContext.OriginalResult;
            }
            else
            {
                // ゲーム終了の場合は特別なActionResultを作成
                finalResult = ActionResult.Failure("GAME_END_BY_TIMEOUT", additionalData: new { TimeoutResult = timeoutResult });
            }

            // CompletionSourceに結果を設定
            _completionSource?.TrySetResult(finalResult);
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
            _isWaiting = false;
            _currentTimeoutSeconds = 0;
            _waitingPlayer = null;
            _completionSource = null;
            _actionCts?.Dispose();
            _actionCts = null;
            _timeoutCts?.Cancel();
            _timeoutCts?.Dispose();
            _timeoutCts = null;
        }

        public void Dispose()
        {
            CancelWaiting();
            _actionManager.OnActionCompleted -= OnActionCompleted;
        }
        #endregion
    }
}