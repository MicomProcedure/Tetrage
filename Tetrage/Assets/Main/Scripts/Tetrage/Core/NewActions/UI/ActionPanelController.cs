using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// 新しいActionシステムに対応したアクションパネルコントローラー（ActionType対応版）
    /// </summary>
    public class ActionPanelController : MonoBehaviour
    {
        [Header("Action Buttons")]
        [SerializeField] private Button drawButton;
        [SerializeField] private Button openButton;
        [SerializeField] private Button reachButton;
        [SerializeField] private Button checkButton;

        [Header("Settings")]
        [SerializeField] private bool autoUpdateButtons = true;
        [SerializeField] private float updateInterval = 0.1f;

        private ActionManager _actionManager;
        private IGameContextProvider _gameContextProvider;
        private Dictionary<ActionType, Button> _actionButtons;
        private IPlayer _currentPlayer;

        private void Awake()
        {
            InitializeButtonMappings();
            SetupButtonClickHandlers();
        }

        private void Start()
        {
            InitializeActionSystem();

            if (autoUpdateButtons)
            {
                StartButtonUpdateLoop();
            }
        }

        /// <summary>
        /// ボタンとActionTypeのマッピングを初期化
        /// </summary>
        private void InitializeButtonMappings()
        {
            _actionButtons = new Dictionary<ActionType, Button>
            {
                { ActionType.Draw, drawButton },
                { ActionType.Open, openButton },
                { ActionType.Reach, reachButton },
                { ActionType.Check, checkButton }
            };
        }

        /// <summary>
        /// ボタンのクリックハンドラーを設定
        /// </summary>
        private void SetupButtonClickHandlers()
        {
            foreach (var pair in _actionButtons)
            {
                var actionType = pair.Key;
                var button = pair.Value;

                if (button != null)
                {
                    button.onClick.AddListener(() => ExecuteActionAsync(actionType).Forget());
                }
            }
        }

        /// <summary>
        /// アクションシステムを初期化
        /// </summary>
        private void InitializeActionSystem()
        {
            _actionManager = ActionManager.Instance;
            _gameContextProvider = FindGameContextProvider();

            if (_gameContextProvider == null)
            {
                Debug.LogError("GameContextProviderが見つかりません");
                return;
            }

            // ActionManagerがまだ初期化されていない場合は初期化
            if (_actionManager != null)
            {
                _actionManager.SetGameContextProvider(_gameContextProvider);
                ActionFactory.RegisterAllActions(_actionManager);
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
        /// ボタン更新ループを開始
        /// </summary>
        private async void StartButtonUpdateLoop()
        {
            while (this != null && gameObject.activeInHierarchy)
            {
                UpdateButtonStates();
                await UniTask.Delay((int)(updateInterval * 1000));
            }
        }

        /// <summary>
        /// ボタンの状態（有効/無効）を更新
        /// </summary>
        public void UpdateButtonStates()
        {
            if (_actionManager == null || _gameContextProvider == null)
                return;

            _currentPlayer = _gameContextProvider.CurrentPlayer;

            if (_currentPlayer == null)
            {
                DisableAllButtons();
                return;
            }

            // 各ボタンの状態を更新（ActionType版）
            foreach (var pair in _actionButtons)
            {
                var actionType = pair.Key;
                var button = pair.Value;

                if (button != null)
                {
                    bool canExecute = _currentPlayer.CanExecuteNewAction(actionType, _gameContextProvider);
                    button.interactable = canExecute;
                }
            }
        }

        /// <summary>
        /// 全てのボタンを無効化
        /// </summary>
        private void DisableAllButtons()
        {
            foreach (var button in _actionButtons.Values)
            {
                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }

        /// <summary>
        /// アクションを実行（ActionType版）
        /// </summary>
        private async UniTask ExecuteActionAsync(ActionType actionType)
        {
            if (_actionManager == null || _currentPlayer == null)
            {
                Debug.LogWarning("ActionManagerまたは現在のプレイヤーが設定されていません");
                return;
            }

            try
            {
                // 実行前にボタンを一時的に無効化
                DisableAllButtons();

                Debug.Log($"アクション実行開始: {actionType}");

                var result = await _currentPlayer.ExecuteNewActionAsync(actionType, _gameContextProvider);

                if (result.IsSuccess)
                {
                    Debug.Log($"アクション成功: {actionType}");
                    OnActionSuccess(actionType, result);
                }
                else
                {
                    Debug.LogWarning($"アクション失敗: {actionType} - {result.ErrorMessage}");
                    OnActionFailure(actionType, result);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"アクション実行中にエラー: {actionType} - {ex.Message}");
            }
            finally
            {
                // ボタン状態を再更新
                await UniTask.Delay(100); // 少し待ってから更新
                UpdateButtonStates();
            }
        }

        /// <summary>
        /// アクション成功時の処理
        /// </summary>
        private void OnActionSuccess(ActionType actionType, ActionResult result)
        {
            // TODO: 成功時のフィードバック（UI、音、エフェクトなど）
            Debug.Log($"アクション成功フィードバック: {actionType}");
        }

        /// <summary>
        /// アクション失敗時の処理
        /// </summary>
        private void OnActionFailure(ActionType actionType, ActionResult result)
        {
            // TODO: 失敗時のフィードバック（エラーメッセージ表示など）
            Debug.LogWarning($"アクション失敗フィードバック: {actionType} - {result.ErrorMessage}");
        }

        // === Inspector用のテストメソッド ===

        [ContextMenu("Show Available Actions")]
        private void ShowAvailableActions()
        {
            if (_currentPlayer == null)
            {
                Debug.LogWarning("現在のプレイヤーが設定されていません");
                return;
            }

            var availableActions = _currentPlayer.GetAvailableNewActionTypes(_gameContextProvider);
            Debug.Log($"実行可能なアクション: {string.Join(", ", availableActions)}");
        }

        [ContextMenu("Force Update Button States")]
        private void ForceUpdateButtonStates()
        {
            UpdateButtonStates();
            Debug.Log("ボタン状態を強制更新しました");
        }

        [ContextMenu("Test Draw Action")]
        private async void TestDrawAction()
        {
            if (_currentPlayer != null)
            {
                var result = await _currentPlayer.DrawAsync(_gameContextProvider);
                Debug.Log($"Test Draw Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }

        [ContextMenu("Test Open Action")]
        private async void TestOpenAction()
        {
            if (_currentPlayer != null)
            {
                var result = await _currentPlayer.OpenAsync(_gameContextProvider);
                Debug.Log($"Test Open Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }

        [ContextMenu("Test Reach Action")]
        private async void TestReachAction()
        {
            if (_currentPlayer != null)
            {
                var result = await _currentPlayer.ReachAsync(_gameContextProvider);
                Debug.Log($"Test Reach Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }

        [ContextMenu("Test Check Action")]
        private async void TestCheckAction()
        {
            if (_currentPlayer != null)
            {
                var result = await _currentPlayer.CheckAsync(_gameContextProvider);
                Debug.Log($"Test Check Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }
    }
}