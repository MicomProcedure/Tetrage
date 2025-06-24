using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクションシステムの初期化を担当するクラス
    /// </summary>
    public class ActionSystemInitializer : MonoBehaviour
    {
        [SerializeField] private bool _initializeOnAwake = true;

        private ActionManager _actionManager;
        private IGameContextProvider _gameContextProvider;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_initializeOnAwake)
            {
                InitializeActionSystem();
            }
        }

        /// <summary>
        /// Actionシステムを初期化する
        /// </summary>
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（省略時は Dealerインスタンスを使用）</param>
        /// <param name="forceReinitialize">強制的に再初期化するかどうか</param>
        public void InitializeActionSystem(IGameContextProvider gameContextProvider = null, bool forceReinitialize = false)
        {
            if (_isInitialized && !forceReinitialize)
            {
                Debug.Log("Actionシステムは既に初期化済みです");
                return;
            }

            gameContextProvider ??= FindGameContextProvider();

            if (gameContextProvider == null)
            {
                Debug.LogError("GameContextProviderが見つかりません。Dealerインスタンスが適切に設定されているか確認してください。");
                return;
            }

            try
            {
                // GameContextProviderの保存
                _gameContextProvider = gameContextProvider;

                // ActionManagerの初期化
                _actionManager = ActionManager.Instance;
                _actionManager.SetGameContextProvider(gameContextProvider);

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
        /// GameContextProviderを自動検索
        /// </summary>
        private IGameContextProvider FindGameContextProvider()
        {
            // GameManagerからDealerインスタンスを取得
            var gameManager = FindObjectOfType<Tetrage.Managers.GameManager>();
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
                    Debug.LogWarning("GameManagerが初期化されていません。別のIGameContextProviderを検索します。");
                }
            }

            // その他のIGameContextProvider実装をMonoBehaviourから探す
            var providers = FindObjectsOfType<MonoBehaviour>();
            foreach (var provider in providers)
            {
                if (provider is IGameContextProvider gameContextProvider)
                {
                    return gameContextProvider;
                }
            }

            return null;
        }

        /// <summary>
        /// 現在のActionManagerインスタンスを取得
        /// </summary>
        public ActionManager GetActionManager()
        {
            return _actionManager ?? ActionManager.Instance;
        }

        /// <summary>
        /// Inspector上でアクションをテスト実行するためのメソッド
        /// </summary>
        [ContextMenu("Test Draw Action")]
        public async void TestDrawAction()
        {
            if (_gameContextProvider?.CurrentPlayer != null)
            {
                var result = await GetActionManager().ExecuteActionAsync(ActionType.Draw, _gameContextProvider.CurrentPlayer);
                Debug.Log($"Draw Action結果: {(result.IsSuccess ? "成功" : "失敗")} - {result.ErrorMessage}");
            }
            else
            {
                Debug.LogWarning("テスト実行用のプレイヤーが見つかりません");
            }
        }

        [ContextMenu("Test Open Action")]
        public async void TestOpenAction()
        {
            if (_gameContextProvider?.CurrentPlayer != null)
            {
                var result = await GetActionManager().ExecuteActionAsync(ActionType.Open, _gameContextProvider.CurrentPlayer);
                Debug.Log($"Open Action結果: {(result.IsSuccess ? "成功" : "失敗")} - {result.ErrorMessage}");
            }
            else
            {
                Debug.LogWarning("テスト実行用のプレイヤーが見つかりません");
            }
        }

        [ContextMenu("Test Reach Action")]
        public async void TestReachAction()
        {
            if (_gameContextProvider?.CurrentPlayer != null)
            {
                var result = await GetActionManager().ExecuteActionAsync(ActionType.Reach, _gameContextProvider.CurrentPlayer);
                Debug.Log($"Reach Action結果: {(result.IsSuccess ? "成功" : "失敗")} - {result.ErrorMessage}");
            }
            else
            {
                Debug.LogWarning("テスト実行用のプレイヤーが見つかりません");
            }
        }

        [ContextMenu("Test Check Action")]
        public async void TestCheckAction()
        {
            if (_gameContextProvider?.CurrentPlayer != null)
            {
                var result = await GetActionManager().ExecuteActionAsync(ActionType.Check, _gameContextProvider.CurrentPlayer);
                Debug.Log($"Check Action結果: {(result.IsSuccess ? "成功" : "失敗")} - {result.ErrorMessage}");
            }
            else
            {
                Debug.LogWarning("テスト実行用のプレイヤーが見つかりません");
            }
        }
    }
}