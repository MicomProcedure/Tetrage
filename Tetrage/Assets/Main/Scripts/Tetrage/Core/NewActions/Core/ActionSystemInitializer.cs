using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Managers;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクションシステムの初期化を担当するクラス
    /// </summary>
    public static class ActionSystemInitializer
    {
        private static ActionManager _actionManager;
        private static IGameContextProvider _gameContextProvider;
        private static bool _isInitialized = false;
        /// <summary>
        /// アクションシステムが初期化されているかチェック
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Actionシステムを初期化する
        /// </summary>
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（必須）</param>
        /// <param name="forceReinitialize">強制的に再初期化するかどうか</param>
        public static void InitializeActionSystem(IGameContextProvider gameContextProvider, bool forceReinitialize = false)
        {
            if (_isInitialized && !forceReinitialize)
            {
                Debug.Log("Actionシステムは既に初期化済みです");
                return;
            }

            if (gameContextProvider == null)
            {
                Debug.LogError("GameContextProviderが指定されていません。");
                return;
            }

            try
            {
                // GameContextProviderの保存
                _gameContextProvider = gameContextProvider;

                // ActionManagerの初期化
                _actionManager = ActionManager.Instance;
                _actionManager.SetGameContextProvider(_gameContextProvider);

                // 全てのアクションファクトリを登録
                ActionFactory.RegisterAllActions(_actionManager);

                _isInitialized = true;
                Debug.Log("アクションシステムの初期化が完了しました");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"アクションシステムの初期化中にエラーが発生: {ex.Message}");
            }
        }

        /// <summary>
        /// Actionシステムを初期化する（依存をインターフェースで受ける新API）。
        /// </summary>
        public static void InitializeActionSystem(IGameContextProvider gameContextProvider, IRoundManager roundManager, bool forceReinitialize = false)
        {
            if (_isInitialized && !forceReinitialize)
            {
                Debug.Log("Actionシステムは既に初期化済みです");
                return;
            }
            if (gameContextProvider == null || roundManager == null)
            {
                Debug.LogError("InitializeActionSystem に無効な依存が渡されました");
                return;
            }
            try
            {
                _gameContextProvider = gameContextProvider;
                _actionManager = ActionManager.Instance;
                _actionManager.SetGameContextProvider(_gameContextProvider);
                ActionFactory.RegisterAllActions(_actionManager);
                _isInitialized = true;
                Debug.Log("アクションシステムの初期化が完了しました (interface-based)");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"アクションシステムの初期化中にエラーが発生: {ex.Message}");
            }
        }



        /// <summary>
        /// 現在のActionManagerインスタンスを取得
        /// </summary>
        public static ActionManager GetActionManager()
        {
            return _actionManager ?? ActionManager.Instance;
        }

        /// <summary>
        /// 現在のGameContextProviderを取得
        /// </summary>
        public static IGameContextProvider GetGameContextProvider()
        {
            return _gameContextProvider;
        }

        /// <summary>
        /// アクションシステムをリセット（テスト用）
        /// </summary>
        public static void ResetActionSystem()
        {
            _actionManager = null;
            _gameContextProvider = null;
            _isInitialized = false;
            Debug.Log("アクションシステムをリセットしました");
        }

        #region テスト用メソッド

        /// <summary>
        /// Draw アクションをテスト実行
        /// </summary>
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（省略時は現在のものを使用）</param>
        public static async void TestDrawAction(IGameContextProvider gameContextProvider = null)
        {
            var provider = gameContextProvider ?? _gameContextProvider;
            if (provider?.CurrentPlayer != null)
            {
                var result = await GetActionManager().ExecuteActionAsync(ActionType.Draw, provider.CurrentPlayer);
                Debug.Log($"Draw Action結果: {(result.IsSuccess ? "成功" : "失敗")} - {result.ErrorMessage}");
            }
            else
            {
                Debug.LogWarning("テスト実行用のプレイヤーが見つかりません");
            }
        }

        /// <summary>
        /// Open アクションをテスト実行
        /// </summary>
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（省略時は現在のものを使用）</param>
        public static async void TestOpenAction(IGameContextProvider gameContextProvider = null)
        {
            var provider = gameContextProvider ?? _gameContextProvider;
            if (provider?.CurrentPlayer != null)
            {
                var result = await GetActionManager().ExecuteActionAsync(ActionType.Open, provider.CurrentPlayer);
                Debug.Log($"Open Action結果: {(result.IsSuccess ? "成功" : "失敗")} - {result.ErrorMessage}");
            }
            else
            {
                Debug.LogWarning("テスト実行用のプレイヤーが見つかりません");
            }
        }

        /// <summary>
        /// Reach アクションをテスト実行
        /// </summary>
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（省略時は現在のものを使用）</param>
        public static async void TestReachAction(IGameContextProvider gameContextProvider = null)
        {
            var provider = gameContextProvider ?? _gameContextProvider;
            if (provider?.CurrentPlayer != null)
            {
                var result = await GetActionManager().ExecuteActionAsync(ActionType.Reach, provider.CurrentPlayer);
                Debug.Log($"Reach Action結果: {(result.IsSuccess ? "成功" : "失敗")} - {result.ErrorMessage}");
            }
            else
            {
                Debug.LogWarning("テスト実行用のプレイヤーが見つかりません");
            }
        }

        /// <summary>
        /// Check アクションをテスト実行
        /// </summary>
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（省略時は現在のものを使用）</param>
        public static async void TestCheckAction(IGameContextProvider gameContextProvider = null)
        {
            var provider = gameContextProvider ?? _gameContextProvider;
            if (provider?.CurrentPlayer != null)
            {
                var result = await GetActionManager().ExecuteActionAsync(ActionType.Check, provider.CurrentPlayer);
                Debug.Log($"Check Action結果: {(result.IsSuccess ? "成功" : "失敗")} - {result.ErrorMessage}");
            }
            else
            {
                Debug.LogWarning("テスト実行用のプレイヤーが見つかりません");
            }
        }

        /// <summary>
        /// Pass アクションをテスト実行
        /// </summary>
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（省略時は現在のものを使用）</param>
        public static async void TestPassAction(IGameContextProvider gameContextProvider = null)
        {
            var provider = gameContextProvider ?? _gameContextProvider;
            if (provider?.CurrentPlayer != null)
            {
                var result = await GetActionManager().ExecuteActionAsync(ActionType.Pass, provider.CurrentPlayer);
                Debug.Log($"Pass Action結果: {(result.IsSuccess ? "成功" : "失敗")} - {result.ErrorMessage}");
            }
            else
            {
                Debug.LogWarning("テスト実行用のプレイヤーが見つかりません");
            }
        }

        /// <summary>
        /// 指定されたアクションタイプをテスト実行
        /// </summary>
        /// <param name="actionType">実行するアクションタイプ</param>
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（省略時は現在のものを使用）</param>
        public static async void TestAction(ActionType actionType, IGameContextProvider gameContextProvider = null)
        {
            var provider = gameContextProvider ?? _gameContextProvider;
            if (provider?.CurrentPlayer != null)
            {
                var result = await GetActionManager().ExecuteActionAsync(actionType, provider.CurrentPlayer);
                Debug.Log($"{actionType} Action結果: {(result.IsSuccess ? "成功" : "失敗")} - {result.ErrorMessage}");
            }
            else
            {
                Debug.LogWarning("テスト実行用のプレイヤーが見つかりません");
            }
        }

        #endregion
    }
}