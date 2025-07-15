using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tetrage.Models;
using Tetrage.Managers;
using Tetrage.Managers.DealerStrategies;
using Tetrage.Core.Contracts;
using Tetrage.Factories;
using Tetrage.Core.Actions;
using Cysharp.Threading.Tasks;
using Tetrage.Components;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;

namespace Tetrage.Tests
{
    /// <summary>
    /// Dealer と Action システムのテストを行うクラス
    /// ActionFocusedDealerStrategy を使用してプレイヤーアクションの動作を確認
    /// </summary>
    public class DealerActionTester : MonoBehaviour, IDebuggable
    {
        [Header("テスト設定")]
        [SerializeField] private bool autoStartTest = true;
        [SerializeField] private TestScenario testScenario = TestScenario.Scenario1;
        [SerializeField] private float passActionDelay = 5f;
        [SerializeField] private int playerCount = 2;
        [SerializeField] private int fixedPlayerIndex = 0;
        [SerializeField] private float timeoutSeconds = 0;
        [SerializeField] private int maxRounds = 10;  // 最大ラウンド数

        [Header("シナリオ2専用設定")]
        [SerializeField] private float timeoutSecondsForScenario2 = 3f;  // タイムアウト時間を3秒に変更
        [SerializeField] private int timeoutTargetRound = 3;
        [SerializeField] private float extendedActionDelay = 5f;  // ラウンド3で延長するアクション秒数

        [Header("シナリオ3専用設定")]
        [SerializeField] private int scenario3Rounds = 3;  // シナリオ3のラウンド数
        [SerializeField] private ActionPanelController actionPanelController;  // UIパネル参照
        [SerializeField] private bool enableAutoClickForScenario3 = false;  // 自動クリック機能
        [SerializeField] private float autoClickDelayForScenario3 = 3f;  // 自動クリック実行までの遅延

        [Header("シナリオ4専用設定")]
        [SerializeField] private FieldSetupComponent fieldSetupComponent;
        [SerializeField] private int scenario4Rounds = 3; // シナリオ4のラウンド数

        [Header("デバッグ情報")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private LogLevel logLevel = LogLevel.Important;
        [SerializeField] private bool enableDetailedStepInfo = false;
        [SerializeField] private bool enablePlayerTracking = false;
        [SerializeField] private bool enableStageTracking = false;

        // ログレベル定義
        public enum LogLevel
        {
            Essential = 0,  // 必須情報のみ（開始、終了、エラー）
            Important = 1,  // 重要イベント（ラウンド開始/終了、アクション実行）
            Detailed = 2,   // 詳細情報（プレイヤー状態、ステージ状態）
            Debug = 3       // 全ての情報（旧来の動作）
        }

        // テスト用インスタンス
        private Dealer _dealer;
        private Stage _stage;
        private List<IPlayer> _players;
        private ActionFocusedDealerStrategy _strategy;
        private bool _testStarted = false;
        private bool _passActionExecuted = false;
        private int _currentRoundForScenario = 0;

        /// <summary>
        /// テストシナリオの種類
        /// </summary>
        public enum TestScenario
        {
            Scenario1,  // 通常のPassアクション実行テスト
            Scenario2,  // 3回目のループでタイムアウト発生テスト
            Scenario3,   // ActionPanelControllerからの入力でPassAction実行テスト（3ラウンド）
            Scenario4   // FieldSetupManagerを利用したDrawActionのUIテスト
        }

        #region Unity生命周期

        private void Start()
        {
            if (autoStartTest)
            {
                StartTest();
            }
        }

        public void DrawDebugGUI()
        {
            GUILayout.Label("=== Dealer Action Tester ===");
            GUILayout.Label($"現在のシナリオ: {testScenario}");

            if (!_testStarted)
            {
                // シナリオ選択
                GUILayout.Label("シナリオ選択:");
                if (GUILayout.Button("シナリオ1 (通常テスト)"))
                {
                    testScenario = TestScenario.Scenario1;
                    timeoutSeconds = 0;
                }

                if (GUILayout.Button("シナリオ2 (タイムアウトテスト)"))
                {
                    testScenario = TestScenario.Scenario2;
                    timeoutSeconds = timeoutSecondsForScenario2;
                }

                if (GUILayout.Button("シナリオ3 (ActionPanelControllerテスト)"))
                {
                    testScenario = TestScenario.Scenario3;
                    timeoutSeconds = 0;
                }

                if (GUILayout.Button("シナリオ4 (FieldSetup + DrawActionテスト)"))
                {
                    testScenario = TestScenario.Scenario4;
                    timeoutSeconds = 0;
                }

                // ログレベル選択
                GUILayout.Label("ログレベル:");
                if (GUILayout.Button($"現在: {logLevel}"))
                {
                    // ログレベルを循環
                    logLevel = (LogLevel)(((int)logLevel + 1) % 4);
                }

                if (GUILayout.Button("テスト開始"))
                {
                    StartTest();
                }
            }
            else
            {
                GUILayout.Label($"テスト実行中...");
                GUILayout.Label($"現在のプレイヤー: {_dealer?.CurrentPlayer?.UserId ?? "なし"}");
                GUILayout.Label($"ラウンド数: {_dealer?.RoundCount ?? 0}/{_dealer?.MaxRounds ?? maxRounds}");
                GUILayout.Label($"Pass実行済み: {_passActionExecuted}");
                GUILayout.Label($"ログレベル: {logLevel}");

                if (testScenario == TestScenario.Scenario2)
                {
                    GUILayout.Label($"タイムアウト設定: {timeoutSeconds}秒");
                    GUILayout.Label($"タイムアウト予定ラウンド: {timeoutTargetRound}");
                }
                else if (testScenario == TestScenario.Scenario3)
                {
                    GUILayout.Label($"シナリオ3ラウンド数: {scenario3Rounds}");
                    GUILayout.Label($"ActionPanelController: {(actionPanelController != null ? "設定済み" : "未設定")}");
                    GUILayout.Label($"自動クリック: {(enableAutoClickForScenario3 ? $"有効({autoClickDelayForScenario3}秒)" : "無効")}");

                    if (!enableAutoClickForScenario3)
                    {
                        GUILayout.Label("*** 手動でPassボタンをクリックしてください ***", GUI.skin.box);
                    }
                }
                else if (testScenario == TestScenario.Scenario4)
                {
                    GUILayout.Label($"シナリオ4設定: FieldSetupComponent: {(fieldSetupComponent != null ? "設定済み" : "未設定")}");
                    GUILayout.Label($"プレイヤー数: {playerCount}");
                    GUILayout.Label($"最大ラウンド: {scenario4Rounds}");
                }

                if (GUILayout.Button("テスト停止"))
                {
                    StopTest();
                }
            }
        }

        #endregion

        #region テスト実行メソッド

        /// <summary>
        /// テストを開始する
        /// </summary>
        public void StartTest()
        {
            if (_testStarted)
            {
                Debug.LogWarning("DealerActionTester: テストは既に実行中です");
                return;
            }

            try
            {
                LogEssential("=== テスト開始 ===");

                // テスト環境を構築
                SetupTestEnvironment();

                // イベントリスナーを設定
                SetupEventListeners();

                LogEssential("テストセットアップ完了、ゲーム開始");
                DisplayStepDebugInfo("テスト開始");

                // シナリオ別の初期化
                InitializeScenario();

                // ゲームを開始
                StartGameAsync().Forget();

                _testStarted = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"DealerActionTester: テスト開始中にエラーが発生しました - {ex.Message}");
            }
        }

        /// <summary>
        /// シナリオ別の初期化を行う
        /// </summary>
        private void InitializeScenario()
        {
            _currentRoundForScenario = 0;
            _passActionExecuted = false;

            switch (testScenario)
            {
                case TestScenario.Scenario1:
                    LogImportant("シナリオ1 - 通常のPassアクション実行テスト");
                    // シナリオ1では設定された最大ラウンド数をそのまま使用
                    _dealer?.SetMaxRounds(maxRounds);
                    break;
                case TestScenario.Scenario2:
                    LogImportant($"シナリオ2 - {timeoutTargetRound}回目のループでタイムアウト発生テスト（タイムアウト時間: {timeoutSeconds}秒, 延長アクション秒数: +{extendedActionDelay}秒）");
                    // シナリオ2ではタイムアウト予定ラウンドでゲーム終了
                    _dealer?.SetMaxRounds(timeoutTargetRound);
                    LogImportant($"[シナリオ2] 最大ラウンド数を{timeoutTargetRound}に設定（タイムアウトテストのため）");
                    break;
                case TestScenario.Scenario3:
                    LogImportant("シナリオ3 - ActionPanelControllerからの入力でPassAction実行テスト（3ラウンド）");
                    // シナリオ3ではシナリオ3Rounds回のラウンドを設定
                    _dealer?.SetMaxRounds(scenario3Rounds);
                    break;
                case TestScenario.Scenario4:
                    LogImportant("シナリオ4 - FieldSetupManagerを利用したDrawActionのUIテスト（プレイヤー4人）");
                    playerCount = 4; // プレイヤー数を4人に固定
                    _dealer?.SetMaxRounds(scenario4Rounds); // シナリオ4のラウンド数を設定
                    LogImportant($"[シナリオ4] プレイヤー数を{playerCount}に設定し、最大ラウンドを{scenario4Rounds}に設定しました。");
                    break;
            }
        }

        /// <summary>
        /// テストを停止する
        /// </summary>
        public void StopTest()
        {
            if (!_testStarted) return;

            try
            {
                DisplayStepDebugInfo("テスト停止");

                // ゲームを終了
                _dealer?.EndGame();

                // イベントリスナーを解除
                RemoveEventListeners();

                _testStarted = false;
                _passActionExecuted = false;
                _currentRoundForScenario = 0;

                LogEssential("=== テスト停止完了 ===");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"DealerActionTester: テスト停止中にエラーが発生しました - {ex.Message}");
            }
        }

        #endregion

        #region テスト環境セットアップ

        /// <summary>
        /// テスト環境を構築する
        /// </summary>
        private void SetupTestEnvironment()
        {
            LogDetailed("テスト環境をセットアップします");

            if (testScenario == TestScenario.Scenario4)
            {
                SetupTestEnvironmentForScenario4();
            }
            else
            {
                SetupTestEnvironmentForScenarios123();
            }

            // 戦略を作成
            _strategy = new ActionFocusedDealerStrategy(fixedPlayerIndex);
            LogDetailed($"戦略作成完了 - 固定プレイヤー: {fixedPlayerIndex}");

            // Dealer を作成
            _dealer = new Dealer(_stage, _players, _strategy);
            LogDetailed("Dealer作成完了");

            // 最大ラウンド数はシナリオ初期化で設定するため、ここではデフォルト値のまま
            // _dealer.SetMaxRounds(maxRounds);
            // Debug.Log($"DealerActionTester: 最大ラウンド数設定完了 - {maxRounds}ラウンド");
        }

        /// <summary>
        /// シナリオ1, 2, 3用のテスト環境を構築する
        /// </summary>
        private void SetupTestEnvironmentForScenarios123()
        {
            // ファクトリーを作成
            var cardModelFactory = new CardModelFactory();
            var cardPileFactory = new CardPileFactory();
            var stageModelFactory = new StageModelFactory(cardPileFactory, cardModelFactory);
            var playerModelFactory = new PlayerModelFactory(cardPileFactory, cardModelFactory);

            // ステージを作成
            _stage = stageModelFactory.SetupStage(playerCount);
            LogDetailed($"ステージ作成完了 - Stack: {_stage.Stack.Count}枚");

            // プレイヤーを作成
            _players = new List<IPlayer>();
            for (int i = 0; i < playerCount; i++)
            {
                var player = playerModelFactory.CreatePlayer($"TestPlayer_{i}");
                _players.Add(player);
                LogDetailed($"プレイヤー作成完了 - {player.UserId}");
            }
        }

        /// <summary>
        /// シナリオ4用のテスト環境を構築する
        /// </summary>
        private void SetupTestEnvironmentForScenario4()
        {
            LogImportant("[シナリオ4] FieldSetupManagerを使用して環境をセットアップします。");
            if (fieldSetupComponent == null)
            {
                Debug.LogError("DealerActionTester: [シナリオ4] FieldSetupComponentが設定されていません。テストを中止します。");
                throw new System.InvalidOperationException("FieldSetupComponent is not set.");
            }

            // 1. 参加者情報を作成
            playerCount = 4;
            var participantInfoList = new List<PlayerInfo>();
            for (int i = 0; i < playerCount; i++)
            {
                participantInfoList.Add(new PlayerInfo { UserId = $"Player{i + 1}", PlayerType = PlayerType.Local });
            }
            LogDetailed($"[シナリオ4] {playerCount}人分の参加者情報を作成しました。");

            // 2. 依存性を作成
            var cardFactory = new CardModelFactory();
            var cardPileFactory = new CardPileFactory();
            var stageModelFactory = new StageModelFactory(cardPileFactory, cardFactory);
            var playerModelFactory = new PlayerModelFactory(cardPileFactory, cardFactory);

            var dependencies = new FieldSetupDependencies(
                cardFactory,
                stageModelFactory,
                playerModelFactory,
                cardPileFactory
            );
            LogDetailed("[シナリオ4] FieldSetupDependenciesを作成しました。");

            // 3. 設定を取得
            var settings = fieldSetupComponent.GetValidatedFieldSetupSettings(playerCount);
            LogDetailed("[シナリオ4] FieldSetupSettingsを検証・取得しました。");

            // 4. FieldSetupManagerを作成してフィールドをセットアップ
            var fieldSetupManager = new FieldSetupManager(settings, dependencies);
            fieldSetupManager.SetupField(participantInfoList);

            // 5. 結果を取得
            _stage = fieldSetupManager.Stage;
            _players = fieldSetupManager.Players;
            LogImportant("[シナリオ4] FieldSetupManagerによるフィールドセットアップが完了しました。");
        }

        /// <summary>
        /// イベントリスナーを設定する
        /// </summary>
        private void SetupEventListeners()
        {
            if (_dealer == null) return;

            _dealer.RoundStart += OnRoundStart;
            _dealer.RoundEnd += OnRoundEnd;

            LogDetailed("イベントリスナー設定完了");
        }

        /// <summary>
        /// イベントリスナーを解除する
        /// </summary>
        private void RemoveEventListeners()
        {
            if (_dealer == null) return;

            _dealer.RoundStart -= OnRoundStart;
            _dealer.RoundEnd -= OnRoundEnd;

            LogDetailed("イベントリスナー解除完了");
        }

        #endregion

        #region ゲーム実行

        /// <summary>
        /// ゲームを開始する
        /// </summary>
        private async UniTask StartGameAsync()
        {
            try
            {
                LogImportant("ゲームループ開始");
                DisplayStepDebugInfo("ゲームループ開始");

                await _dealer.StartGameAsync(timeoutSeconds);

                LogImportant("ゲームループ終了");
                DisplayStepDebugInfo("ゲームループ終了");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"DealerActionTester: ゲーム実行中にエラーが発生しました - {ex.Message}");
            }
        }

        #endregion

        #region イベントハンドラー

        /// <summary>
        /// ラウンド開始イベントのハンドラー
        /// </summary>
        private void OnRoundStart()
        {
            _currentRoundForScenario++;
            LogImportant($"=== ラウンド{_dealer.RoundCount} 開始 ===");
            DisplayStepDebugInfo("ラウンド開始");

            // シナリオ別の処理
            switch (testScenario)
            {
                case TestScenario.Scenario1:
                    HandleScenario1RoundStart();
                    break;
                case TestScenario.Scenario2:
                    HandleScenario2RoundStart();
                    break;
                case TestScenario.Scenario3:
                    HandleScenario3RoundStart();
                    break;
                case TestScenario.Scenario4:
                    HandleScenario4RoundStart();
                    break;
            }
        }

        /// <summary>
        /// シナリオ1のラウンド開始処理
        /// </summary>
        private void HandleScenario1RoundStart()
        {
            LogDetailed("[シナリオ1] 通常のPassアクションテストを実行");
            if (!_passActionExecuted)
            {
                ExecutePassActionAfterDelay().Forget();
            }
        }

        /// <summary>
        /// シナリオ2のラウンド開始処理
        /// </summary>
        private void HandleScenario2RoundStart()
        {
            LogDetailed($"[シナリオ2] ラウンド{_currentRoundForScenario} - タイムアウト予定: {timeoutTargetRound}");

            if (_currentRoundForScenario < timeoutTargetRound)
            {
                // タイムアウト前のラウンドではPassアクションを実行
                LogDetailed($"[シナリオ2] ラウンド{_currentRoundForScenario} - {passActionDelay}秒後にPassアクション実行");
                ExecutePassActionAfterDelay().Forget();
            }
            else if (_currentRoundForScenario == timeoutTargetRound)
            {
                // タイムアウト発生ラウンド：長時間のアクションを実行してタイムアウトを誘発
                var totalActionTime = passActionDelay + extendedActionDelay;
                LogImportant($"[シナリオ2] ラウンド{_currentRoundForScenario} - タイムアウト発生予定（タイムアウト: {timeoutSeconds}秒, アクション実行: {totalActionTime}秒後 = {passActionDelay}+{extendedActionDelay}）");
                ExecutePassActionAfterDelayWithCustomTime(totalActionTime).Forget();
            }
        }

        /// <summary>
        /// シナリオ3のラウンド開始処理
        /// </summary>
        private void HandleScenario3RoundStart()
        {
            LogImportant($"[シナリオ3] ラウンド{_currentRoundForScenario}/{scenario3Rounds} - ActionPanelControllerからの入力でPassAction実行テスト");
            LogImportant("[シナリオ3] UI操作: ActionPanelのPassボタンをクリックしてください");

            // ActionPanelControllerが設定されているかチェック
            if (actionPanelController == null)
            {
                LogImportant("[シナリオ3] 注意: ActionPanelControllerが設定されていません。手動でPassボタンをクリックしてください。");
            }
            else
            {
                LogImportant("[シナリオ3] ActionPanelControllerが設定されています。Passボタンをクリックしてください。");
            }

            // 自動クリック機能が有効な場合
            if (enableAutoClickForScenario3)
            {
                LogImportant($"[シナリオ3] 自動クリック機能有効 - {autoClickDelayForScenario3}秒後にPassボタンを自動クリックします");
                ExecutePassActionViaUI().Forget();
            }
            else
            {
                LogImportant("[シナリオ3] 手動操作モード - 手動でPassボタンをクリックしてください");
            }
        }

        /// <summary>
        /// シナリオ4のラウンド開始処理
        /// </summary>
        private void HandleScenario4RoundStart()
        {
            LogImportant("[シナリオ4] ラウンド開始。UI上のDrawボタンが有効になるはずです。");
            LogImportant("[シナリオ4] UI操作: ActionPanelのDrawボタンをクリックして、カードを引けるかテストしてください。");
            // シナリオ4では自動的なアクションは実行せず、UIからの入力を待つ
        }

        /// <summary>
        /// ラウンド終了イベントのハンドラー
        /// </summary>
        private void OnRoundEnd()
        {
            LogImportant($"=== ラウンド{_dealer.RoundCount} 終了 ===");
            DisplayStepDebugInfo("ラウンド終了");

            // シナリオ2でタイムアウトが発生したラウンドの場合
            if (testScenario == TestScenario.Scenario2 && _currentRoundForScenario == timeoutTargetRound)
            {
                LogImportant("[シナリオ2] タイムアウト発生ラウンド完了 - ゲーム終了予定");
            }
        }

        /// <summary>
        /// 指定時間後にPassアクションを実行する
        /// </summary>
        private async UniTask ExecutePassActionAfterDelay()
        {
            try
            {
                Debug.Log($"DealerActionTester: {passActionDelay}秒待機開始");

                // 指定時間待機
                await UniTask.Delay((int)(passActionDelay * 1000));

                Debug.Log($"DealerActionTester: {passActionDelay}秒待機完了、Passアクション実行開始");
                DisplayStepDebugInfo("Passアクション実行前");

                // 現在のプレイヤーを取得
                var currentPlayer = _dealer.CurrentPlayer;
                if (currentPlayer == null)
                {
                    Debug.LogWarning("DealerActionTester: 現在のプレイヤーが見つかりません");
                    return;
                }

                Debug.Log($"DealerActionTester: プレイヤー {currentPlayer.UserId} でPassアクションを実行します");

                // Passアクションを実行
                var result = await currentPlayer.PassAsync();

                Debug.Log($"DealerActionTester: Passアクション実行完了");
                DisplayStepDebugInfo("Passアクション実行後");

                if (result.IsSuccess)
                {
                    result.Log("DealerActionTester: Passアクション成功");

                    // シナリオ1の場合のみフラグを設定（シナリオ2では継続的にアクションを実行）
                    if (testScenario == TestScenario.Scenario1)
                    {
                        _passActionExecuted = true;
                    }
                }
                else
                {
                    // エラーメッセージでタイムアウトかどうかを判別
                    if (result.ErrorMessage != null && result.ErrorMessage.Contains("タイムアウト"))
                    {
                        Debug.LogWarning($"DealerActionTester: Passアクションがタイムアウトしました - {result.ErrorMessage}");
                    }
                    else
                    {
                        Debug.LogError($"DealerActionTester: Passアクション失敗 - {result.ErrorMessage}");
                    }
                }

                // 結果をログに出力
                result.Log("DealerActionTester: Passアクション結果");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"DealerActionTester: Passアクション実行中にエラーが発生しました - {ex.Message}");
            }
        }

        /// <summary>
        /// 指定時間後にPassアクションを実行する（カスタム時間）
        /// </summary>
        private async UniTask ExecutePassActionAfterDelayWithCustomTime(float customTime)
        {
            try
            {
                LogDetailed($"{customTime}秒待機開始");

                // 指定時間待機
                await UniTask.Delay((int)(customTime * 1000));

                LogImportant("Passアクション実行開始（カスタム時間後）");
                DisplayStepDebugInfo("Passアクション実行前");

                // 現在のプレイヤーを取得
                var currentPlayer = _dealer.CurrentPlayer;
                if (currentPlayer == null)
                {
                    Debug.LogWarning("DealerActionTester: 現在のプレイヤーが見つかりません");
                    return;
                }

                LogDetailed($"プレイヤー {currentPlayer.UserId} でPassアクションを実行します");

                // Passアクションを実行
                var result = await currentPlayer.PassAsync();

                LogImportant("Passアクション実行完了");
                DisplayStepDebugInfo("Passアクション実行後");

                if (result.IsSuccess)
                {
                    LogImportant("Passアクション成功");

                    // シナリオ1の場合のみフラグを設定（シナリオ2では継続的にアクションを実行）
                    if (testScenario == TestScenario.Scenario1)
                    {
                        _passActionExecuted = true;
                    }
                }
                else
                {
                    // エラーメッセージでタイムアウトかどうかを判別
                    if (result.ErrorMessage != null && result.ErrorMessage.Contains("タイムアウト"))
                    {
                        Debug.LogWarning($"DealerActionTester: Passアクションがタイムアウトしました - {result.ErrorMessage}");
                    }
                    else
                    {
                        Debug.LogError($"DealerActionTester: Passアクション失敗 - {result.ErrorMessage}");
                    }
                }

                // 結果をログに出力（詳細レベル以上の場合のみ）
                if (logLevel >= LogLevel.Detailed)
                {
                    result.Log("DealerActionTester: Passアクション結果");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"DealerActionTester: Passアクション実行中にエラーが発生しました - {ex.Message}");
            }
        }

        /// <summary>
        /// ActionPanelController経由でPassアクションを実行する（シナリオ3専用）
        /// </summary>
        private async UniTask ExecutePassActionViaUI()
        {
            try
            {
                LogImportant($"[シナリオ3] {autoClickDelayForScenario3}秒待機後、ActionPanelController経由でPassアクション実行開始");

                // 指定時間待機
                await UniTask.Delay((int)(autoClickDelayForScenario3 * 1000));

                if (actionPanelController == null)
                {
                    LogImportant("[シナリオ3] ActionPanelControllerが設定されていないため、直接Passアクションを実行します");

                    // 現在のプレイヤーを取得
                    var currentPlayer = _dealer.CurrentPlayer;
                    if (currentPlayer == null)
                    {
                        Debug.LogWarning("DealerActionTester: 現在のプレイヤーが見つかりません");
                        return;
                    }

                    LogImportant($"[シナリオ3] プレイヤー {currentPlayer.UserId} でPassアクションを実行します");

                    // 直接Passアクションを実行
                    var result = await currentPlayer.PassAsync();
                    LogPassActionResult(result);
                }
                else
                {
                    LogImportant("[シナリオ3] ActionPanelControllerが設定されています");
                    LogImportant("[シナリオ3] 注意: 自動クリック機能が有効ですが、実際のUI操作をシミュレートするため、直接Passアクションを実行します");

                    // 現在のプレイヤーを取得してPassアクションを実行
                    var currentPlayer = _dealer.CurrentPlayer;
                    if (currentPlayer == null)
                    {
                        Debug.LogWarning("DealerActionTester: 現在のプレイヤーが見つかりません");
                        return;
                    }

                    LogImportant($"[シナリオ3] プレイヤー {currentPlayer.UserId} でPassアクションを実行します（ActionPanelController使用想定）");

                    // Passアクションを実行
                    var result = await currentPlayer.PassAsync();
                    LogPassActionResult(result);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"DealerActionTester: UI経由Passアクション実行中にエラーが発生しました - {ex.Message}");
            }
        }

        /// <summary>
        /// Passアクション結果のログ出力（共通処理）
        /// </summary>
        private void LogPassActionResult(ActionResult result)
        {
            if (result.IsSuccess)
            {
                LogImportant("Passアクション成功");
            }
            else
            {
                // エラーメッセージでタイムアウトかどうかを判別
                if (result.ErrorMessage != null && result.ErrorMessage.Contains("タイムアウト"))
                {
                    Debug.LogWarning($"DealerActionTester: Passアクションがタイムアウトしました - {result.ErrorMessage}");
                }
                else
                {
                    Debug.LogError($"DealerActionTester: Passアクション失敗 - {result.ErrorMessage}");
                }
            }

            // 結果をログに出力（詳細レベル以上の場合のみ）
            if (logLevel >= LogLevel.Detailed)
            {
                result.Log("DealerActionTester: Passアクション結果");
            }
        }

        #endregion

        #region スマートログシステム

        /// <summary>
        /// レベル別ログ出力
        /// </summary>
        private void LogEssential(string message) => LogWithLevel(LogLevel.Essential, message);
        private void LogImportant(string message) => LogWithLevel(LogLevel.Important, message);
        private void LogDetailed(string message) => LogWithLevel(LogLevel.Detailed, message);
        private void LogDebug(string message) => LogWithLevel(LogLevel.Debug, message);

        /// <summary>
        /// 指定レベル以上のログのみ出力
        /// </summary>
        private void LogWithLevel(LogLevel level, string message)
        {
            if (!showDebugInfo || level > logLevel) return;
            Debug.Log($"[{level}] {message}");
        }

        /// <summary>
        /// 簡潔なステップ情報を表示
        /// </summary>
        private void LogStepSummary(string stepName)
        {
            if (!showDebugInfo) return;

            var roundInfo = _dealer != null ? $"{_dealer.RoundCount}/{_dealer.MaxRounds}" : "0/0";
            var scenarioInfo = $"S{(int)testScenario + 1}R{_currentRoundForScenario}";

            LogImportant($"[{stepName}] {scenarioInfo} - Round {roundInfo}");
        }

        /// <summary>
        /// 変化があった場合のみ詳細情報を出力
        /// </summary>
        private void LogChangedInfo(string stepName)
        {
            if (!enableDetailedStepInfo || _dealer == null) return;

            // プレイヤー情報の変化チェック
            if (enablePlayerTracking)
            {
                var currentPlayer = _dealer.CurrentPlayer;
                var playerInfo = currentPlayer?.UserId ?? "なし";
                LogDetailed($"[{stepName}] Player: {playerInfo}");
            }

            // ステージ情報の変化チェック  
            if (enableStageTracking)
            {
                var stackCount = _stage?.Stack?.Count ?? 0;
                var trashCount = _stage?.Trash?.Count ?? 0;
                LogDetailed($"[{stepName}] Stage: Stack={stackCount}, Trash={trashCount}");
            }
        }

        /// <summary>
        /// レガシーのDisplayStepDebugInfoを簡潔版に置き換え
        /// </summary>
        private void DisplayStepDebugInfo(string stepName)
        {
            LogStepSummary(stepName);
            LogChangedInfo(stepName);

            // デバッグレベルの場合のみ旧来の詳細情報を出力
            if (logLevel >= LogLevel.Debug)
            {
                DisplayLegacyDebugInfo(stepName);
            }
        }

        /// <summary>
        /// 旧来の詳細デバッグ情報（デバッグレベル時のみ）
        /// </summary>
        private void DisplayLegacyDebugInfo(string stepName)
        {
            if (_dealer == null) return;

            var currentPlayer = _dealer.CurrentPlayer;
            var roundCount = _dealer.RoundCount;
            var maxRounds = _dealer.MaxRounds;
            var playerInfo = currentPlayer != null ? $"{currentPlayer.UserId} (hands: {currentPlayer.Hands.Count})" : "なし";

            LogDebug($"[Step: {stepName}] {testScenario} - ラウンド{roundCount}/{maxRounds} (シナリオ内: {_currentRoundForScenario}) - 現在のプレイヤー: {playerInfo}");

            if (currentPlayer != null)
            {
                LogDebug($"[Step: {stepName}] プレイヤー詳細 - ID: {currentPlayer.PlayerId}, 手札: {currentPlayer.Hands.Count}枚, ターゲット: {currentPlayer.Target?.Count ?? 0}枚");
            }

            LogDebug($"[Step: {stepName}] ステージ状態 - Stack: {_stage?.Stack?.Count ?? 0}枚, Trash: {_stage?.Trash?.Count ?? 0}枚");
            LogDebug($"[Step: {stepName}] ゲーム設定 - 最大ラウンド数: {maxRounds}");

            if (testScenario == TestScenario.Scenario2)
            {
                LogDebug($"[Step: {stepName}] タイムアウト情報 - 設定時間: {timeoutSeconds}秒, 予定ラウンド: {timeoutTargetRound}");
            }
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 手動でPassアクションを実行する（デバッグ用）
        /// </summary>
        [ContextMenu("手動でPassアクション実行")]
        public void ExecutePassActionManually()
        {
            if (_dealer?.CurrentPlayer != null)
            {
                LogImportant("手動でPassアクション実行を開始");
                DisplayStepDebugInfo("手動Passアクション開始");
                ExecutePassActionAfterDelay().Forget();
            }
            else
            {
                Debug.LogWarning("DealerActionTester: 現在のプレイヤーが見つかりません");
            }
        }

        /// <summary>
        /// シナリオ1に切り替える
        /// </summary>
        [ContextMenu("シナリオ1に切り替え")]
        public void SwitchToScenario1()
        {
            if (!_testStarted)
            {
                testScenario = TestScenario.Scenario1;
                timeoutSeconds = 0;
                LogImportant("シナリオ1に切り替えました");
            }
            else
            {
                Debug.LogWarning("DealerActionTester: テスト実行中はシナリオを変更できません");
            }
        }

        /// <summary>
        /// シナリオ2に切り替える
        /// </summary>
        [ContextMenu("シナリオ2に切り替え")]
        public void SwitchToScenario2()
        {
            if (!_testStarted)
            {
                testScenario = TestScenario.Scenario2;
                timeoutSeconds = timeoutSecondsForScenario2;
                LogImportant($"シナリオ2に切り替えました（タイムアウト: {timeoutSeconds}秒）");
            }
            else
            {
                Debug.LogWarning("DealerActionTester: テスト実行中はシナリオを変更できません");
            }
        }

        /// <summary>
        /// シナリオ3に切り替える
        /// </summary>
        [ContextMenu("シナリオ3に切り替え")]
        public void SwitchToScenario3()
        {
            if (!_testStarted)
            {
                testScenario = TestScenario.Scenario3;
                timeoutSeconds = 0;
                LogImportant("シナリオ3に切り替えました");
            }
            else
            {
                Debug.LogWarning("DealerActionTester: テスト実行中はシナリオを変更できません");
            }
        }

        /// <summary>
        /// シナリオ4に切り替える
        /// </summary>
        [ContextMenu("シナリオ4に切り替え")]
        public void SwitchToScenario4()
        {
            if (!_testStarted)
            {
                testScenario = TestScenario.Scenario4;
                timeoutSeconds = 0;
                LogImportant("シナリオ4に切り替えました");
            }
            else
            {
                Debug.LogWarning("DealerActionTester: テスト実行中はシナリオを変更できません");
            }
        }

        /// <summary>
        /// テスト状態をリセットする
        /// </summary>
        [ContextMenu("テスト状態リセット")]
        public void ResetTestState()
        {
            LogImportant("=== テスト状態リセット開始 ===");
            DisplayStepDebugInfo("テスト状態リセット");

            StopTest();
            _dealer = null;
            _stage = null;
            _players = null;
            _strategy = null;
            _passActionExecuted = false;
            _currentRoundForScenario = 0;

            LogImportant("=== テスト状態リセット完了 ===");
        }

        #endregion
    }
}