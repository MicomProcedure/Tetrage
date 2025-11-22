using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Tetrage.Managers;
using Tetrage.Managers.DealerStrategies;
using Tetrage.Core.Contracts;
using Tetrage.Factories;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;
using Tetrage.Models;
using R3;
using DomainEvents = Tetrage.Core.Events;
using Tetrage.Core.Enums;

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
        private IGameplayNetworkController _netCtl;
        private INetworkContext _networkContext;
        private RealDealerPlanner _dealerPlanner;
        private IPlayerIdMapper _playerIdMapper;
        private bool _testStarted = false;
        #endregion

        #region private fields
        private CompositeDisposable _disposables = new();
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
                _netCtl?.Stop();
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
                players.Add(playerModelFactory.CreatePlayer(new PlayerId(i), $"TestPlayer_{i}", i));
            }

            // Bus を用意（オフラインでも購読可能にするが、適用発火はネット経路のみ）
            var bus = new R3EventBus();
            _gameContext = new Tetrage.Core.GameContext(stage, players, players[fixedPlayerIndex], bus);

            // PlayerIdMapperを生成し、プレイヤーIDとアクター番号をマッピング
            _playerIdMapper = new PlayerIdMapper();
            for (int i = 0; i < playerCount; i++)
            {
                // ActorNumberは1から始まる
                _playerIdMapper.Register(new PlayerId(i), i + 1);
            }

            // テスト環境ではHostとして動作させる
            _networkContext = new VirtualNetworkContext(1, true, playerCount, true, true);

            var pileRegistry = new IdRegistry<PileId, CardPile>();
            var cardRegistry = new IdRegistry<CardId, Card>();
            var playerRegistry = new IdRegistry<PlayerId, Player>();

            // レジストリにプレイヤーを登録
            foreach (var player in players)
            {
                if (player is Player playerModel)
                {
                    playerRegistry.Register(playerModel);
                }
            }

            _netCtl = new GameplayNetworkController(
                true, // isHost
                pileRegistry,
                cardRegistry,
                playerRegistry,
                new VirtualNetworkAdapterFactory(),
                _playerIdMapper
            );

            // GameContextをアタッチ
            _netCtl.AttachGameContext(_gameContext as Tetrage.Core.GameContext);
            _netCtl.Start();

            _dealer = DealerFactory.CreateDealer(GameMode.Debug, _gameContext, _networkContext, _netCtl);
        }

        private void SetupEventListeners()
        {
            _gameContext.Events.GameStarted.Subscribe(_ => OnGameStart()).AddTo(_disposables);
            _gameContext.Events.GameEnded.Subscribe(_ => OnGameEnd()).AddTo(_disposables);
            _gameContext.Events.TurnStarted.Subscribe(_ => OnTurnStart()).AddTo(_disposables);
            _gameContext.Events.TurnEnded.Subscribe(_ => OnTurnEnd()).AddTo(_disposables);
        }

        private void RemoveEventListeners()
        {

            // R3購読解除
            _disposables.Dispose();
            _disposables = new();

        }
        #endregion

        #region イベントハンドラー
        private void OnGameStart() => Log("ゲーム開始");
        private void OnGameEnd() => Log("ゲーム終了");
        private void OnRoundStart() => Log($"ラウンド開始 {_dealer?.RoundCount}/{_dealer?.MaxRounds}");
        private void OnRoundEnd() => Log($"ラウンド終了 {_dealer?.RoundCount}/{_dealer?.MaxRounds}");
        private void OnTurnStart() => Log($"ターン開始 {_dealer?.TurnCount}");
        private void OnTurnStartedBus(DomainEvents.TurnStartedEvent e) => Log($"ターン開始(バス) playerId={e.CurrentPlayerId}");
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