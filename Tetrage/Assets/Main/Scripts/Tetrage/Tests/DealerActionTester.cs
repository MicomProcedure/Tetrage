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

namespace Tetrage.Tests
{
    /// <summary>
    /// Dealer と Action システムのテストを行うクラス
    /// ActionFocusedDealerStrategy を使用してプレイヤーアクションの動作を確認
    /// </summary>
    public class DealerActionTester : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private bool autoStartTest = true;
        [SerializeField] private float passActionDelay = 5f;
        [SerializeField] private int playerCount = 2;
        [SerializeField] private int fixedPlayerIndex = 0;

        [Header("デバッグ情報")]
        [SerializeField] private bool showDebugInfo = true;

        // テスト用インスタンス
        private Dealer _dealer;
        private Stage _stage;
        private List<IPlayer> _players;
        private ActionFocusedDealerStrategy _strategy;
        private bool _testStarted = false;
        private bool _passActionExecuted = false;

        #region Unity生命周期

        private void Start()
        {
            if (autoStartTest)
            {
                StartTest();
            }
        }



        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));

            GUILayout.Label("=== Dealer Action Tester ===");

            if (!_testStarted)
            {
                if (GUILayout.Button("テスト開始"))
                {
                    StartTest();
                }
            }
            else
            {
                GUILayout.Label($"テスト実行中...");
                GUILayout.Label($"現在のプレイヤー: {_dealer?.CurrentPlayer?.UserId ?? "なし"}");
                GUILayout.Label($"ラウンド数: {_dealer?.RoundCount ?? 0}");
                GUILayout.Label($"Pass実行済み: {_passActionExecuted}");

                if (GUILayout.Button("テスト停止"))
                {
                    StopTest();
                }
            }

            GUILayout.EndArea();
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
                Debug.LogWarning("DealerActionTester: テストは既に開始されています");
                return;
            }

            Debug.Log("DealerActionTester: テストを開始します");

            try
            {
                // テスト環境をセットアップ
                SetupTestEnvironment();

                // イベントリスナーを設定
                SetupEventListeners();

                Debug.Log("DealerActionTester: テストセットアップ完了、ゲーム開始");
                DisplayStepDebugInfo("テスト開始");

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
        /// テストを停止する
        /// </summary>
        public void StopTest()
        {
            if (!_testStarted)
            {
                Debug.LogWarning("DealerActionTester: テストは開始されていません");
                return;
            }

            Debug.Log("DealerActionTester: テストを停止します");

            try
            {
                DisplayStepDebugInfo("テスト停止");

                // ゲームを終了
                _dealer?.EndGame();

                // イベントリスナーを解除
                RemoveEventListeners();

                _testStarted = false;
                _passActionExecuted = false;

                Debug.Log("DealerActionTester: テスト停止完了");
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
            Debug.Log("DealerActionTester: テスト環境をセットアップします");

            // ファクトリーを作成
            var cardModelFactory = new CardModelFactory();
            var cardPileFactory = new CardPileFactory();
            var stageModelFactory = new StageModelFactory(cardPileFactory, cardModelFactory);
            var playerModelFactory = new PlayerModelFactory(cardPileFactory, cardModelFactory);

            // ステージを作成（カード移動なしのため最小限）
            _stage = stageModelFactory.SetupStage(countPerSuit: 1);
            Debug.Log($"DealerActionTester: ステージ作成完了 - Stack: {_stage.Stack.Count}枚");

            // プレイヤーを作成
            _players = new List<IPlayer>();
            for (int i = 0; i < playerCount; i++)
            {
                var player = playerModelFactory.CreatePlayer($"TestPlayer_{i}");
                _players.Add(player);
                Debug.Log($"DealerActionTester: プレイヤー作成完了 - {player.UserId}");
            }

            // ActionFocusedDealerStrategy を作成
            _strategy = new ActionFocusedDealerStrategy(fixedPlayerIndex);
            Debug.Log($"DealerActionTester: 戦略作成完了 - 固定プレイヤー: {fixedPlayerIndex}");

            // Dealer を作成
            _dealer = new Dealer(_stage, _players, _strategy);
            Debug.Log($"DealerActionTester: Dealer作成完了");
        }

        /// <summary>
        /// イベントリスナーを設定する
        /// </summary>
        private void SetupEventListeners()
        {
            if (_dealer == null) return;

            _dealer.RoundStart += OnRoundStart;
            _dealer.RoundEnd += OnRoundEnd;

            Debug.Log("DealerActionTester: イベントリスナー設定完了");
        }

        /// <summary>
        /// イベントリスナーを解除する
        /// </summary>
        private void RemoveEventListeners()
        {
            if (_dealer == null) return;

            _dealer.RoundStart -= OnRoundStart;
            _dealer.RoundEnd -= OnRoundEnd;

            Debug.Log("DealerActionTester: イベントリスナー解除完了");
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
                Debug.Log("DealerActionTester: ゲームループ開始");
                DisplayStepDebugInfo("ゲームループ開始");

                await _dealer.StartGameAsync();

                Debug.Log("DealerActionTester: ゲームループ終了");
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
            Debug.Log($"DealerActionTester: ラウンド開始 - ラウンド{_dealer.RoundCount}");
            DisplayStepDebugInfo("ラウンド開始");

            if (!_passActionExecuted)
            {
                Debug.Log($"DealerActionTester: {passActionDelay}秒後にPassアクション実行を予約");
                // 5秒後にPassアクションを実行
                ExecutePassActionAfterDelay().Forget();
            }
        }

        /// <summary>
        /// ラウンド終了イベントのハンドラー
        /// </summary>
        private void OnRoundEnd()
        {
            Debug.Log($"DealerActionTester: ラウンド終了 - ラウンド{_dealer.RoundCount}");
            DisplayStepDebugInfo("ラウンド終了");
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
                    _passActionExecuted = true;
                }
                else
                {
                    Debug.LogError($"DealerActionTester: Passアクション失敗 - {result.ErrorMessage}");
                }

                // 結果をログに出力
                result.Log("DealerActionTester: Passアクション結果");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"DealerActionTester: Passアクション実行中にエラーが発生しました - {ex.Message}");
            }
        }

        #endregion

        #region デバッグ情報表示

        /// <summary>
        /// ステップごとのデバッグ情報を表示する
        /// </summary>
        /// <param name="stepName">ステップ名</param>
        private void DisplayStepDebugInfo(string stepName)
        {
            if (!showDebugInfo || _dealer == null) return;

            var currentPlayer = _dealer.CurrentPlayer;
            var roundCount = _dealer.RoundCount;
            var playerInfo = currentPlayer != null ? $"{currentPlayer.UserId} (hands: {currentPlayer.Hands.Count})" : "なし";

            Debug.Log($"DealerActionTester: [Step: {stepName}] ラウンド{roundCount} - 現在のプレイヤー: {playerInfo}");

            // 詳細情報
            if (currentPlayer != null)
            {
                Debug.Log($"DealerActionTester: [Step: {stepName}] プレイヤー詳細 - ID: {currentPlayer.PlayerId}, 手札: {currentPlayer.Hands.Count}枚, ターゲット: {currentPlayer.Target?.Count ?? 0}枚");
            }

            Debug.Log($"DealerActionTester: [Step: {stepName}] ステージ状態 - Stack: {_stage?.Stack?.Count ?? 0}枚, Trash: {_stage?.Trash?.Count ?? 0}枚");
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
                Debug.Log("DealerActionTester: 手動でPassアクション実行を開始");
                DisplayStepDebugInfo("手動Passアクション開始");
                ExecutePassActionAfterDelay().Forget();
            }
            else
            {
                Debug.LogWarning("DealerActionTester: 現在のプレイヤーが見つかりません");
            }
        }

        /// <summary>
        /// テスト状態をリセットする
        /// </summary>
        [ContextMenu("テスト状態リセット")]
        public void ResetTestState()
        {
            Debug.Log("DealerActionTester: テスト状態リセット開始");
            DisplayStepDebugInfo("テスト状態リセット");

            StopTest();
            _dealer = null;
            _stage = null;
            _players = null;
            _strategy = null;
            _passActionExecuted = false;

            Debug.Log("DealerActionTester: テスト状態リセット完了");
        }

        #endregion
    }
}