using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Network.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Tetrage.Core;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// 新しいActionシステムに対応したアクションパネルコントローラー
    /// </summary>
    public class ActionPanelController : MonoBehaviour
    {
        [Header("Action Buttons")]
        [SerializeField] private Button drawButton;
        [SerializeField] private Button openButton;
        [SerializeField] private Button reachButton;
        [SerializeField] private Button checkButton;
        [SerializeField] private Button passButton;

        [Header("Settings")]
        [SerializeField] private float _retryInterval = 0.5f;
        [SerializeField] private int _maxRetries = 10;

        private ActionManager _actionManager;
        private IGameContextProvider _gameContextProvider;
        private Dictionary<ActionType, Button> _actionButtons;
        private IPlayer _currentPlayer;
        private bool _isInitialized = false;

        private void Awake()
        {
            InitializeButtonMappings();
            SetupButtonClickHandlers();
        }

        public void Initialize(IGameContextProvider gameContextProvider)
        {
            // ActionManagerの取得と初期化
            _actionManager = ActionManager.Instance;

            if (_actionManager == null)
            {
                Debug.LogError("ActionPanelController: ActionManagerが見つかりません");
                return;
            }

            // 遅延初期化を試行
            if (!TryInitialize())
            {
                // 初期化に失敗した場合、定期的にリトライ
                StartCoroutine(RetryInitialization());
            }
        }

        /// <summary>
        /// ActionPanelControllerの初期化を試行。Dealerへの、UIボタン更新メソッドの登録を行う。
        /// </summary>
        /// <returns>初期化が成功した場合true</returns>
        private bool TryInitialize()
        {
            if (_isInitialized) return true;

            // ActionSystemInitializerが初期化されているかチェック
            if (!ActionSystemInitializer.IsInitialized)
            {
                Debug.LogWarning("ActionPanelController: ActionSystemInitializerが未初期化です（リトライします）");
                return false;
            }

            // GameContextProvider（Dealer）をActionManagerから取得
            _gameContextProvider = _actionManager.GetGameContextProvider();
            if (_gameContextProvider == null)
            {
                Debug.LogWarning("ActionPanelController: GameContextProviderが未設定です（リトライします）");
                return false;
            }

            // RoundManager（Dealer）をActionManagerから取得
            _gameContextProvider.Events.TurnStartedApplied += OnTurnStartedEvent;

            // 初回ボタン状態更新。UIボタン更新メソッドを実行
            UpdateButtonStates();

            _isInitialized = true;
            Debug.Log("ActionPanelController: 初期化が完了しました");
            return true;
        }

        /// <summary>
        /// 初期化のリトライを行うコルーチン
        /// </summary>
        private IEnumerator RetryInitialization()
        {

            int retryCount = 0;

            while (!_isInitialized && retryCount < _maxRetries)
            {
                yield return new WaitForSeconds(_retryInterval);

                if (TryInitialize())
                {
                    yield break; // 初期化成功
                }

                retryCount++;
                Debug.LogWarning($"ActionPanelController: 初期化リトライ {retryCount}/{_maxRetries}");
            }

            if (!_isInitialized)
            {
                Debug.LogError("ActionPanelController: 初期化に失敗しました（最大リトライ回数に達しました）");
                DisableAllButtons();
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
                { ActionType.Check, checkButton },
                { ActionType.Pass, passButton }
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

        public void OnTurnStartedEvent(TurnStartedEvent e)
        {
            UpdateButtonStates();
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
                    bool canExecute = _currentPlayer.CanExecuteNewAction(actionType);
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

                var result = await _currentPlayer.ExecuteNewActionAsync(actionType);

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

        private void OnDestroy()
        {
            _gameContextProvider.Events.TurnStartedApplied -= OnTurnStartedEvent;
        }

        #region テスト用メソッド

        [ContextMenu("Show Available Actions")]
        private void ShowAvailableActions()
        {
            if (_currentPlayer == null)
            {
                Debug.LogWarning("現在のプレイヤーが設定されていません");
                return;
            }

            var availableActions = _currentPlayer.GetAvailableNewActionTypes();
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
                var result = await _currentPlayer.DrawAsync();
                Debug.Log($"Test Draw Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }

        [ContextMenu("Test Open Action")]
        private async void TestOpenAction()
        {
            if (_currentPlayer != null)
            {
                var result = await _currentPlayer.OpenAsync();
                Debug.Log($"Test Open Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }

        [ContextMenu("Test Reach Action")]
        private async void TestReachAction()
        {
            if (_currentPlayer != null)
            {
                var result = await _currentPlayer.ReachAsync();
                Debug.Log($"Test Reach Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }

        [ContextMenu("Test Check Action")]
        private async void TestCheckAction()
        {
            if (_currentPlayer != null)
            {
                var result = await _currentPlayer.CheckAsync();
                Debug.Log($"Test Check Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }

        [ContextMenu("Test Pass Action")]
        private async void TestPassAction()
        {
            if (_currentPlayer != null)
            {
                var result = await _currentPlayer.PassAsync();
                Debug.Log($"Test Pass Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }
        #endregion
    }
}