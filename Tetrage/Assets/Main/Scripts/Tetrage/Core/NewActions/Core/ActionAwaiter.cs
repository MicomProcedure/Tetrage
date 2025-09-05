using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクション待機を担う専用クラス
    /// Dealer などの上位クラスは本クラスを利用して非同期にプレイヤーアクションを待機する
    /// </summary>
    public class ActionAwaiter : IDisposable
    {
        #region フィールド
        private readonly ActionManager _actionManager;

        private UniTaskCompletionSource<ActionResult> _completionSource;
        private CancellationTokenSource _actionCts;
        private IPlayer _waitingPlayer;
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
        public ActionAwaiter(ActionManager actionManager)
        {
            _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));

            // Action 完了イベントに購読
            _actionManager.OnActionCompleted += OnActionCompleted;
        }
        #endregion


        #region 公開API


        /// <summary>
        /// 指定プレイヤーのアクションを非同期に待機します。
        /// </summary>
        /// <param name="player">待機対象プレイヤー</param>
        /// <param name="timeoutSeconds">タイムアウト時間（0以下で無制限）</param>
        /// <returns>アクション実行結果</returns>
        /// <exception cref="OperationCanceledException">キャンセルされた場合</exception>
        public async UniTask<ActionResult> WaitForPlayerActionAsync(IPlayer player)
        {
            if (player == null) return ActionResult.Failure("現在のプレイヤーが設定されていません");

            // 既存の待機をキャンセル
            CancelWaiting();

            _waitingPlayer = player;
            _completionSource = new UniTaskCompletionSource<ActionResult>();
            _actionCts = new CancellationTokenSource();

            var token = _actionCts.Token;

            try
            {
                Debug.Log($"ActionAwaiter: プレイヤー {_waitingPlayer.PlayerId} のアクションを待機中...");

 
                    // 無制限待機
                    return await _completionSource.Task.AttachExternalCancellation(token);

            }
            catch (OperationCanceledException)
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
            _actionCts?.Dispose();
            _actionCts = null;
        }

        public void Dispose()
        {
            CancelWaiting();
            _actionManager.OnActionCompleted -= OnActionCompleted;
        }
        #endregion
    }
}