using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using R3;
using DomainEvents = Tetrage.Core.Events;

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
        [SerializeField] private Button tSoloButton;
        [SerializeField] private Button tMultiButton;

        [Header("Settings")]
        [SerializeField] private float _retryInterval = 0.5f;
        [SerializeField] private int _maxRetries = 10;

        private ActionManager _actionManager;
        private IGameContext _gameContextProvider;
        private Dictionary<ActionType, Button> _actionButtons;
        /// <summary>ローカルユーザー（操作対象）。手番時のみ設定される。</summary>
        private IPlayer _userPlayer;
        private bool _isInitialized = false;
        private CompositeDisposable _disposables = new();   // disposableをまとめて管理するためのコンテナ

        private void Awake()
        {
            InitializeButtonMappings();
            SetupButtonClickHandlers();
        }

        public void Initialize(IGameContext gameContextProvider)
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

            // R3のObservableでTurnStartedイベントを購読
            _gameContextProvider.Events.TurnStarted
                .Subscribe(OnTurnStartedEvent)
                .AddTo(_disposables);

            // Reach 等のドメイン状態反映後にボタン表示を同期する
            _gameContextProvider.Events.ActionResult
                .Subscribe(OnActionResultEvent)
                .AddTo(_disposables);

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
                { ActionType.Pass, passButton },
                { ActionType.TetrageSolo, tSoloButton },
                { ActionType.TetrageMulti, tMultiButton }
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

        private void OnTurnStartedEvent(DomainEvents.TurnStartedEvent e)
        {
            UpdateButtonStates();
        }

        private void OnActionResultEvent(DomainEvents.ActionResultEvent e)
        {
            UpdateButtonStates();
        }

        /// <summary>
        /// ローカルユーザーの手番かどうかを判定する。
        /// </summary>
        private bool IsLocalPlayerTurn()
        {
            var userPlayer = _gameContextProvider?.UserPlayer;
            var turnPlayer = _gameContextProvider?.CurrentPlayer;
            return userPlayer != null
                && turnPlayer != null
                && ReferenceEquals(userPlayer, turnPlayer);
        }

        /// <summary>
        /// ボタンの状態（有効/無効・表示/非表示）を更新する。
        /// 表示は <see cref="ShouldShowButton"/>、活性は Validator 経由の CanExecute で決める。
        /// </summary>
        public void UpdateButtonStates()
        {
            if (_actionManager == null || _gameContextProvider == null)
                return;

            var userPlayer = _gameContextProvider.UserPlayer;
            if (userPlayer == null || !IsLocalPlayerTurn())
            {
                _userPlayer = null;
                DisableAllButtons();
                return;
            }

            _userPlayer = userPlayer;

            // 各ボタンの状態（有効/無効、表示/非表示）を更新（ActionType版）
            foreach (var pair in _actionButtons)
            {
                var actionType = pair.Key;
                var button = pair.Value;

                if (button != null)
                {
                    bool canExecute = _userPlayer.CanExecuteNewAction(actionType);
                    button.interactable = canExecute;

                    bool shouldShow = ShouldShowButton(actionType, _userPlayer);
                    button.gameObject.SetActive(shouldShow);
                }
            }
        }

        /// <summary>
        /// プレイヤーの状態に応じてボタンを表示すべきか判定
        /// </summary>
        /// <param name="actionType">判定対象のアクション</param>
        /// <param name="player">ローカルユーザー</param>
        /// <returns>表示すべき場合true</returns>
        private bool ShouldShowButton(ActionType actionType, IPlayer player)
        {
            // リーチ状態の場合：Check, Pass, TetrageSoloのみ表示
            if (player.IsReach)
            {
                return actionType == ActionType.Check || actionType == ActionType.Pass || actionType == ActionType.TetrageSolo;
            }

            // 非リーチ状態：Draw, Open, Reach, TetrageSolo, TetrageMultiを表示
            return actionType == ActionType.Draw ||
                actionType == ActionType.Open ||
                actionType == ActionType.Reach ||
                actionType == ActionType.TetrageSolo ||
                actionType == ActionType.TetrageMulti;
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
                    button.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// アクションを実行（ActionType版）
        /// </summary>
        private async UniTask ExecuteActionAsync(ActionType actionType)
        {
            if (_actionManager == null || _userPlayer == null)
            {
                Debug.LogWarning("ActionManagerまたはローカルプレイヤーが未設定、または手番外です");
                return;
            }

            try
            {
                // 実行前にボタンを一時的に無効化
                DisableAllButtons();

                Debug.Log($"アクション実行開始: {actionType}");

                var result = await _userPlayer.ExecuteNewActionAsync(actionType);

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
            _disposables.Dispose();
        }

        #region テスト用メソッド

        [ContextMenu("Show Available Actions")]
        private void ShowAvailableActions()
        {
            if (_userPlayer == null)
            {
                Debug.LogWarning("現在のプレイヤーが設定されていません");
                return;
            }

            var availableActions = _userPlayer.GetAvailableNewActionTypes();
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
            if (_userPlayer != null)
            {
                var result = await _userPlayer.DrawAsync();
                Debug.Log($"Test Draw Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }

        [ContextMenu("Test Open Action")]
        private async void TestOpenAction()
        {
            if (_userPlayer != null)
            {
                var result = await _userPlayer.OpenAsync();
                Debug.Log($"Test Open Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }

        [ContextMenu("Test Reach Action")]
        private async void TestReachAction()
        {
            if (_userPlayer != null)
            {
                var result = await _userPlayer.ReachAsync();
                Debug.Log($"Test Reach Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }

        [ContextMenu("Test Check Action")]
        private async void TestCheckAction()
        {
            if (_userPlayer != null)
            {
                var result = await _userPlayer.CheckAsync();
                Debug.Log($"Test Check Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }

        [ContextMenu("Test Pass Action")]
        private async void TestPassAction()
        {
            if (_userPlayer != null)
            {
                var result = await _userPlayer.PassAsync();
                Debug.Log($"Test Pass Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }
        [ContextMenu("Test Tetrage Solo Action")]
        private async void TestTetrageSoloAction()
        {
            if (_userPlayer != null)
            {
                var result = await _userPlayer.ExecuteNewActionAsync(ActionType.TetrageSolo);
                Debug.Log($"Test Tetrage Solo Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }
        [ContextMenu("Test Tetrage Multi Action")]
        private async void TestTetrageMultiAction()
        {
            if (_userPlayer != null)
            {
                var result = await _userPlayer.ExecuteNewActionAsync(ActionType.TetrageMulti);
                Debug.Log($"Test Tetrage Multi Result: {result.IsSuccess} - {result.ErrorMessage}");
            }
        }
        #endregion

    }
}