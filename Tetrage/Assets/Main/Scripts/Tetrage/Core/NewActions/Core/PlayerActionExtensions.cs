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
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（省略時は自動検索）</param>
        /// <returns>アクション実行結果</returns>
        public static async UniTask<ActionResult> ExecuteNewActionAsync(
            this IPlayer player,
            ActionType actionType,
            IGameContextProvider gameContextProvider = null)
        {
            var actionManager = ActionManager.Instance;

            // GameContextProviderが指定されていない場合は自動検索
            if (gameContextProvider == null)
            {
                gameContextProvider = FindGameContextProvider();
                if (gameContextProvider == null)
                {
                    return ActionResult.Failure("GameContextProviderが見つかりません");
                }
            }

            // ActionManagerの初期化確認
            if (actionManager != null)
            {
                actionManager.SetGameContextProvider(gameContextProvider);
                ActionFactory.RegisterAllActions(actionManager);
            }

            return await actionManager.ExecuteActionAsync(actionType, player);
        }

        /// <summary>
        /// 指定されたアクションが実行可能かチェック
        /// </summary>
        /// <param name="player">チェックするプレイヤー</param>
        /// <param name="actionType">チェックするアクションタイプ</param>
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（省略時は自動検索）</param>
        /// <returns>実行可能な場合true</returns>
        public static bool CanExecuteNewAction(
            this IPlayer player,
            ActionType actionType,
            IGameContextProvider gameContextProvider = null)
        {
            var actionManager = ActionManager.Instance;

            // GameContextProviderが指定されていない場合は自動検索
            if (gameContextProvider == null)
            {
                gameContextProvider = FindGameContextProvider();
                if (gameContextProvider == null)
                {
                    return false;
                }
            }

            // ActionManagerの初期化確認
            if (actionManager != null)
            {
                actionManager.SetGameContextProvider(gameContextProvider);
                ActionFactory.RegisterAllActions(actionManager);
            }

            return actionManager.CanExecuteAction(actionType, player);
        }

        /// <summary>
        /// プレイヤーが実行可能なアクション一覧を取得
        /// </summary>
        /// <param name="player">対象プレイヤー</param>
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（省略時は自動検索）</param>
        /// <returns>実行可能なActionTypeの一覧</returns>
        public static System.Collections.Generic.IReadOnlyList<ActionType> GetAvailableNewActionTypes(
            this IPlayer player,
            IGameContextProvider gameContextProvider = null)
        {
            var actionManager = ActionManager.Instance;

            // GameContextProviderが指定されていない場合は自動検索
            if (gameContextProvider == null)
            {
                gameContextProvider = FindGameContextProvider();
                if (gameContextProvider == null)
                {
                    return new System.Collections.Generic.List<ActionType>();
                }
            }

            // ActionManagerの初期化確認
            if (actionManager != null)
            {
                actionManager.SetGameContextProvider(gameContextProvider);
                ActionFactory.RegisterAllActions(actionManager);
            }

            return actionManager.GetAvailableActionTypes(player);
        }

        // === 具体的なアクション実行用の便利メソッド ===

        /// <summary>
        /// Draw アクションを実行
        /// </summary>
        public static async UniTask<ActionResult> DrawAsync(this IPlayer player, IGameContextProvider gameContextProvider = null)
        {
            return await player.ExecuteNewActionAsync(ActionType.Draw, gameContextProvider);
        }

        /// <summary>
        /// Open アクションを実行
        /// </summary>
        public static async UniTask<ActionResult> OpenAsync(this IPlayer player, IGameContextProvider gameContextProvider = null)
        {
            return await player.ExecuteNewActionAsync(ActionType.Open, gameContextProvider);
        }

        /// <summary>
        /// Reach アクションを実行
        /// </summary>
        public static async UniTask<ActionResult> ReachAsync(this IPlayer player, IGameContextProvider gameContextProvider = null)
        {
            return await player.ExecuteNewActionAsync(ActionType.Reach, gameContextProvider);
        }

        /// <summary>
        /// Check アクションを実行
        /// </summary>
        public static async UniTask<ActionResult> CheckAsync(this IPlayer player, IGameContextProvider gameContextProvider = null)
        {
            return await player.ExecuteNewActionAsync(ActionType.Check, gameContextProvider);
        }

        /// <summary>
        /// Pass アクションを実行
        /// </summary>
        public static async UniTask<ActionResult> PassAsync(this IPlayer player, IGameContextProvider gameContextProvider = null)
        {
            return await player.ExecuteNewActionAsync(ActionType.Pass, gameContextProvider);
        }

        // === 実行可能性チェック用の便利メソッド ===

        /// <summary>
        /// Draw アクションが実行可能かチェック
        /// </summary>
        public static bool CanDraw(this IPlayer player, IGameContextProvider gameContextProvider = null)
        {
            return player.CanExecuteNewAction(ActionType.Draw, gameContextProvider);
        }

        /// <summary>
        /// Open アクションが実行可能かチェック
        /// </summary>
        public static bool CanOpen(this IPlayer player, IGameContextProvider gameContextProvider = null)
        {
            return player.CanExecuteNewAction(ActionType.Open, gameContextProvider);
        }

        /// <summary>
        /// Reach アクションが実行可能かチェック
        /// </summary>
        public static bool CanReach(this IPlayer player, IGameContextProvider gameContextProvider = null)
        {
            return player.CanExecuteNewAction(ActionType.Reach, gameContextProvider);
        }

        /// <summary>
        /// Check アクションが実行可能かチェック
        /// </summary>
        public static bool CanCheck(this IPlayer player, IGameContextProvider gameContextProvider = null)
        {
            return player.CanExecuteNewAction(ActionType.Check, gameContextProvider);
        }

        /// <summary>
        /// Pass アクションが実行可能かチェック
        /// </summary>
        public static bool CanPass(this IPlayer player, IGameContextProvider gameContextProvider = null)
        {
            return player.CanExecuteNewAction(ActionType.Pass, gameContextProvider);
        }

        /// <summary>
        /// GameContextProviderを自動検索する
        /// </summary>
        private static IGameContextProvider FindGameContextProvider()
        {
            // GameManagerからDealerインスタンスを取得
            var gameManager = UnityEngine.Object.FindObjectOfType<Tetrage.Managers.GameManager>();
            if (gameManager != null)
            {
                try
                {
                    var dealer = gameManager.Dealer;
                    if (dealer != null)
                    {
                        return dealer;
                    }
                }
                catch (System.InvalidOperationException)
                {
                    // GameManagerが初期化されていない場合は無視
                    UnityEngine.Debug.LogWarning("GameManagerが初期化されていません。別のIGameContextProviderを検索します。");
                }
            }

            // その他のIGameContextProvider実装をMonoBehaviourから探す
            var providers = UnityEngine.Object.FindObjectsOfType<UnityEngine.MonoBehaviour>();
            foreach (var provider in providers)
            {
                if (provider is IGameContextProvider gameContextProvider)
                {
                    return gameContextProvider;
                }
            }

            return null;
        }
    }
}