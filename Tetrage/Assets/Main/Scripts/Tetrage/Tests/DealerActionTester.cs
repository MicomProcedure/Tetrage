using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Tetrage.Models;
using Tetrage.Managers;
using Tetrage.Managers.DealerStrategies;
using Tetrage.Core.Contracts;
using Tetrage.Factories;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;

namespace Tetrage.Tests
{
    /// <summary>
    /// Dealer/Action の最小テストハーネス。タイムアウトやシナリオ固有の具体的テスト処理を削除。
    /// </summary>
    public class DealerActionTester : MonoBehaviour, IDebuggable
    {
        #region 設定
        [SerializeField] private bool autoStart = true;
        [SerializeField] private int playerCount = 2;
        [SerializeField] private int fixedPlayerIndex = 0;
        [SerializeField] private int maxRounds = 3;
        [SerializeField] private bool showDebugInfo = true;
        #endregion

        #region テスト用インスタンス
        private Dealer _dealer;
        private IGameContextProvider _gameContext;
        private ActionFocusedDealerStrategy _strategy;
        private DealerPlanEmitter _dealerPlanEmitter;
        private RealDealerPlanner _dealerPlanner;
        private bool _testStarted = false;
        #endregion

        #region Unityイベント
        private void Start()
        {
            if (autoStart)
            {
                StartTest();
            }
        }
        #endregion

        #region デバッグGUI
        public void DrawDebugGUI()
        {
            GUILayout.Label("=== Dealer Minimal Tester ===");
            if (!_testStarted)
            {
                if (GUILayout.Button("テスト開始"))
                {
                    StartTest();
                }
            }
            else
            {
                GUILayout.Label($"現在のプレイヤー: {_gameContext?.UserPlayer?.UserId ?? "なし"}");
                GUILayout.Label($"ラウンド数: {_dealer?.RoundCount ?? 0}/{_dealer?.MaxRounds ?? maxRounds}");
                if (GUILayout.Button("テスト停止"))
                {
                    StopTest();
                }
            }
        }
        #endregion

        #region テスト制御
        [ContextMenu("テスト開始")]
        public void StartTest()
        {
            if (_testStarted) return;
            try
            {
                SetupTestEnvironment();
                SetupEventListeners();
                _dealer.SetMaxRounds(maxRounds);
                _dealer.StartGameAsync().Forget();
                _testStarted = true;
                Log("テスト開始");
            }
            catch (Exception ex)
            {
                Debug.LogError($"DealerActionTester: テスト開始中にエラー - {ex.Message}");
            }
        }

        [ContextMenu("テスト停止")]
        public void StopTest()
        {
            if (!_testStarted) return;
            try
            {
                _dealer?.EndGame();
                RemoveEventListeners();
                _testStarted = false;
                Log("テスト停止");
            }
            catch (Exception ex)
            {
                Debug.LogError($"DealerActionTester: テスト停止中にエラー - {ex.Message}");
            }
        }
        #endregion

        #region セットアップ
        private void SetupTestEnvironment()
        {
            var cardModelFactory = new CardModelFactory();
            var cardPileFactory = new CardPileFactory();
            var stageModelFactory = new StageModelFactory(cardPileFactory, cardModelFactory);
            var playerModelFactory = new PlayerModelFactory(cardPileFactory, cardModelFactory);

            var stage = stageModelFactory.SetupStage(playerCount);
            var players = new List<IPlayer>();
            for (int i = 0; i < playerCount; i++)
            {
                players.Add(playerModelFactory.CreatePlayer(new PlayerId(i), $"TestPlayer_{i}"));
            }

            // Bus を用意（オフラインでも購読可能にするが、適用発火はネット経路のみ）
            var bus = new SimpleGameplayEventBus();
            _gameContext = new Tetrage.Core.GameContext(stage, players, players[fixedPlayerIndex], bus);

            _strategy = new ActionFocusedDealerStrategy(fixedPlayerIndex);
            _dealerPlanner = new RealDealerPlanner();
            _dealerPlanEmitter = new DealerPlanEmitter(null);
            _dealer = new Dealer(_gameContext, _dealerPlanner, _dealerPlanEmitter);
        }

        private void SetupEventListeners()
        {
            if (_dealer == null) return;
            _dealer.GameStart += OnGameStart;
            _dealer.GameEnd += OnGameEnd;
            _dealer.RoundStart += OnRoundStart;
            _dealer.RoundEnd += OnRoundEnd;
            // Turn系は Bus 優先。Bus が無い場合だけ Dealer イベントを使う
            if (_gameContext?.Events != null)
            {
                _gameContext.Events.TurnStartedApplied += OnTurnStartedBus;
            }
            else
            {
                _dealer.TurnStart += OnTurnStart;
            }
            _dealer.TurnEnd += OnTurnEnd;
        }

        private void RemoveEventListeners()
        {
            if (_dealer == null) return;
            _dealer.GameStart -= OnGameStart;
            _dealer.GameEnd -= OnGameEnd;
            _dealer.RoundStart -= OnRoundStart;
            _dealer.RoundEnd -= OnRoundEnd;
            if (_gameContext?.Events != null)
            {
                _gameContext.Events.TurnStartedApplied -= OnTurnStartedBus;
            }
            else
            {
                _dealer.TurnStart -= OnTurnStart;
            }
            _dealer.TurnEnd -= OnTurnEnd;
        }
        #endregion

        #region イベントハンドラー
        private void OnGameStart() => Log("ゲーム開始");
        private void OnGameEnd() => Log("ゲーム終了");
        private void OnRoundStart() => Log($"ラウンド開始 {_dealer?.RoundCount}/{_dealer?.MaxRounds}");
        private void OnRoundEnd() => Log($"ラウンド終了 {_dealer?.RoundCount}/{_dealer?.MaxRounds}");
        private void OnTurnStart() => Log($"ターン開始 {_dealer?.TurnCount}");
        private void OnTurnStartedBus(TurnStartedEvent e) => Log($"ターン開始(バス) actor={e.currentPlayerActorNumber}");
        private void OnTurnEnd() => Log($"ターン終了 {_dealer?.TurnCount}");
        #endregion

        #region ログ
        private void Log(string message)
        {
            if (!showDebugInfo) return;
            Debug.Log($"[DealerActionTester] {message}");
        }
        #endregion
    }
}