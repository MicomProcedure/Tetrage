using System;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// 全てのActionの基底クラス
    /// Template Method パターンを使用して共通の処理フローを定義
    /// </summary>
    public abstract class ActionBase : IAction
    {
        public abstract ActionType ActionType { get; }
        public IPlayer Requester { get; }

        protected readonly IActionValidator _validator;
        protected readonly IActionExecutor _executor;

        protected ActionBase(
            IPlayer requester,
            IActionValidator validator,
            IActionExecutor executor)
        {
            Requester = requester ?? throw new ArgumentNullException(nameof(requester));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        /// <summary>
        /// アクションの実行条件を検証する
        /// </summary>
        public virtual bool CanExecute(IActionContext context)
        {
            try
            {
                var validationResult = _validator.Validate(context);
                return validationResult.IsValid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"バリデーション中にエラーが発生しました: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アクションを実行する（Template Method）
        /// </summary>
        public virtual async UniTask<ActionResult> ExecuteAsync(IActionContext context)
        {
            try
            {
                // 事前検証
                if (!CanExecute(context))
                {
                    var validationResult = _validator.Validate(context);
                    return ActionResult.Failure($"実行条件を満たしていません: {validationResult.FailureReason}");
                }

                // 実行前処理
                await OnBeforeExecute(context);

                // メイン実行処理
                var result = await _executor.ExecuteAsync(context);

                // 実行後処理
                await OnAfterExecute(context, result);

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"アクション実行中にエラーが発生しました: {ex.Message}");
                return ActionResult.Failure($"実行エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 実行前の処理（派生クラスでオーバーライド可能）
        /// </summary>
        protected virtual async UniTask OnBeforeExecute(IActionContext context)
        {
            // デフォルトでは何もしない
            await UniTask.Yield();
        }

        /// <summary>
        /// 実行後の処理（派生クラスでオーバーライド可能）
        /// </summary>
        protected virtual async UniTask OnAfterExecute(IActionContext context, ActionResult result)
        {
            // デフォルトでは何もしない
            await UniTask.Yield();
        }
    }
}