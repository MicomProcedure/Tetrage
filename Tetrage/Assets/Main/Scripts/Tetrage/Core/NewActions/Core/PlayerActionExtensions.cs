using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// IPlayerインターフェースの拡張メソッド（ActionType専用版）
    /// 新しいActionシステムとの統合を提供
    /// </summary>
    public static class PlayerActionExtensions
    {
        /// <summary>
        /// 新しいActionシステムを使ってアクションを実行
        /// </summary>
        /// <param name="player">実行するプレイヤー</param>
        /// <param name="actionType">実行するアクションタイプ</param>
        /// <returns>アクション実行結果</returns>
        public static async UniTask<ActionResult> ExecuteNewActionAsync(
            this IPlayer player,
            ActionType actionType)
        {
            var actionManager = ActionManager.Instance;
            if (actionManager == null)
            {
                return ActionResult.Failure("ActionManagerが見つかりません");
            }

            return await actionManager.ExecuteActionAsync(actionType, player);
        }

        /// <summary>
        /// 指定されたアクションが実行可能かチェック
        /// </summary>
        /// <param name="player">チェックするプレイヤー</param>
        /// <param name="actionType">チェックするアクションタイプ</param>
        /// <returns>実行可能な場合true</returns>
        public static bool CanExecuteNewAction(
            this IPlayer player,
            ActionType actionType)
        {
            var actionManager = ActionManager.Instance;
            if (actionManager == null)
            {
                return false;
            }

            return actionManager.CanExecuteAction(actionType, player);
        }

        /// <summary>
        /// プレイヤーが実行可能なアクション一覧を取得
        /// </summary>
        /// <param name="player">対象プレイヤー</param>
        /// <returns>実行可能なActionTypeの一覧</returns>
        public static System.Collections.Generic.IReadOnlyList<ActionType> GetAvailableNewActionTypes(
            this IPlayer player)
        {
            var actionManager = ActionManager.Instance;
            if (actionManager == null)
            {
                return new System.Collections.Generic.List<ActionType>();
            }

            return actionManager.GetAvailableActionTypes(player);
        }

        // === 具体的なアクション実行用の便利メソッド ===

        /// <summary>
        /// Draw アクションを実行
        /// </summary>
        public static async UniTask<ActionResult> DrawAsync(this IPlayer player)
        {
            return await player.ExecuteNewActionAsync(ActionType.Draw);
        }

        /// <summary>
        /// Open アクションを実行
        /// </summary>
        public static async UniTask<ActionResult> OpenAsync(this IPlayer player)
        {
            return await player.ExecuteNewActionAsync(ActionType.Open);
        }

        /// <summary>
        /// Reach アクションを実行
        /// </summary>
        public static async UniTask<ActionResult> ReachAsync(this IPlayer player)
        {
            return await player.ExecuteNewActionAsync(ActionType.Reach);
        }

        /// <summary>
        /// Check アクションを実行
        /// </summary>
        public static async UniTask<ActionResult> CheckAsync(this IPlayer player)
        {
            return await player.ExecuteNewActionAsync(ActionType.Check);
        }

        /// <summary>
        /// Pass アクションを実行
        /// </summary>
        public static async UniTask<ActionResult> PassAsync(this IPlayer player)
        {
            return await player.ExecuteNewActionAsync(ActionType.Pass);
        }

        // === 実行可能性チェック用の便利メソッド ===

        /// <summary>
        /// Draw アクションが実行可能かチェック
        /// </summary>
        public static bool CanDraw(this IPlayer player)
        {
            return player.CanExecuteNewAction(ActionType.Draw);
        }

        /// <summary>
        /// Open アクションが実行可能かチェック
        /// </summary>
        public static bool CanOpen(this IPlayer player)
        {
            return player.CanExecuteNewAction(ActionType.Open);
        }

        /// <summary>
        /// Reach アクションが実行可能かチェック
        /// </summary>
        public static bool CanReach(this IPlayer player)
        {
            return player.CanExecuteNewAction(ActionType.Reach);
        }

        /// <summary>
        /// Check アクションが実行可能かチェック
        /// </summary>
        public static bool CanCheck(this IPlayer player)
        {
            return player.CanExecuteNewAction(ActionType.Check);
        }

        /// <summary>
        /// Pass アクションが実行可能かチェック
        /// </summary>
        public static bool CanPass(this IPlayer player)
        {
            return player.CanExecuteNewAction(ActionType.Pass);
        }
    }
}