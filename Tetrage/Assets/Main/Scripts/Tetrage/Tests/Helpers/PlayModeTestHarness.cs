using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Managers;
using Tetrage.Network;
using Tetrage.Network.Gameplay;

namespace Tetrage.Tests.PlayMode
{
    /// <summary>
    /// PlayModeテスト用のエントリポイント。
    /// ApplicationManagerの代わりにGameSceneの初期化を担当。
    /// Photon接続不要で軽量・高速に動作する。
    /// </summary>
    public class PlayModeTestHarness
    {
        private GameManager _gameManager;
        private INetworkContext _networkContext;
        private IPlayerIdMapper _playerIdMapper;
        private NetworkMode _networkMode;
        private int _playerCount;

        /// <summary>
        /// GameManagerへの参照を取得
        /// </summary>
        public GameManager GameManager => _gameManager;

        /// <summary>
        /// NetworkContextへの参照を取得
        /// </summary>
        public INetworkContext NetworkContext => _networkContext;

        /// <summary>
        /// PlayerIdMapperへの参照を取得
        /// </summary>
        public IPlayerIdMapper PlayerIdMapper => _playerIdMapper;

        /// <summary>
        /// EventBusへのアクセス（LogicInjection用）
        /// </summary>
        public IGameplayEventBus GetEventBus()
        {
            return _gameManager?.GameContext?.Events;
        }

        /// <summary>
        /// テスト用GameSceneセットアップ
        /// </summary>
        /// <param name="playerCount">プレイヤー数（2-4）</param>
        /// <param name="mode">NetworkMode（デフォルト: VirtualTransport）</param>
        /// <param name="randomSeed">乱数シード（再現性のあるテスト用、-1で無効）</param>
        public async UniTask SetupGameScene(
            int playerCount = 2,
            NetworkMode mode = NetworkMode.VirtualTransport,
            int randomSeed = -1)
        {
            _networkMode = mode;
            _playerCount = playerCount;

            Debug.Log($"<color=cyan>PlayModeTestHarness: GameScene初期化開始 (Players: {playerCount}, Mode: {mode})</color>");

            // 乱数シード固定（再現性のあるテスト用）
            if (randomSeed >= 0)
            {
                Random.InitState(randomSeed);
                Debug.Log($"PlayModeTestHarness: 乱数シード固定 (Seed: {randomSeed})");
            }

            // 1. NetworkContextを生成（Photon不要）
            _networkContext = CreateNetworkContext(mode);

            // 2. テスト用PlayerInfoを生成
            var players = GenerateTestPlayerInfos(playerCount, out _playerIdMapper);
            var userInfo = players[0]; // 最初のプレイヤーをユーザーとする

            // 3. GameManagerを探す（シーンに配置されている前提）
            _gameManager = Object.FindFirstObjectByType<GameManager>();
            if (_gameManager == null)
            {
                throw new System.Exception("PlayModeTestHarness: GameManagerがシーンに見つかりません");
            }

            // 4. GameManagerを初期化（Photon待機なし）
            _gameManager.Initialize(players, userInfo, _networkContext, _networkMode, _playerIdMapper);

            Debug.Log("<color=green>PlayModeTestHarness: GameScene初期化完了</color>");

            // 1フレーム待機（初期化処理の完了を待つ）
            await UniTask.Yield();
        }

        /// <summary>
        /// ゲーム開始（オプション）
        /// </summary>
        public async UniTask StartGame()
        {
            if (_gameManager == null)
            {
                throw new System.Exception("PlayModeTestHarness: GameManagerが初期化されていません");
            }

            Debug.Log("PlayModeTestHarness: ゲーム開始");
            await _gameManager.StartGame();
        }

        /// <summary>
        /// テスト終了時のクリーンアップ
        /// </summary>
        public void Teardown()
        {
            Debug.Log("PlayModeTestHarness: クリーンアップ開始");

            if (_gameManager != null)
            {
                _gameManager.StopAndReset();
            }

            // VirtualTransportHubのリセット（シングルトンの場合のみ必要）
            if (_networkMode == NetworkMode.VirtualTransport)
            {
                // 将来的にはインスタンスベース化されるため不要になる
                VirtualTransportHub.DestroyInstance();
            }

            _gameManager = null;
            _networkContext = null;
            _playerIdMapper = null;

            Debug.Log("<color=yellow>PlayModeTestHarness: クリーンアップ完了</color>");
        }

        /// <summary>
        /// NetworkModeに応じたNetworkContextを生成
        /// </summary>
        private INetworkContext CreateNetworkContext(NetworkMode mode)
        {
            switch (mode)
            {
                case NetworkMode.VirtualTransport:
                case NetworkMode.LogicInjection:
                    return new VirtualNetworkContext(
                        actorNumber: 1,
                        isHost: true,
                        playerCount: _playerCount,
                        isReady: true,
                        isInRoom: true
                    );

                case NetworkMode.RealPhoton:
                    // テストではPhotonモードは非推奨
                    Debug.LogWarning("PlayModeTestHarness: RealPhotonモードはテストでは非推奨です");
                    return new PhotonNetworkContext();

                default:
                    throw new System.ArgumentException($"未対応のNetworkMode: {mode}");
            }
        }

        /// <summary>
        /// テスト用PlayerInfo生成
        /// </summary>
        private List<PlayerInfo> GenerateTestPlayerInfos(int count, out IPlayerIdMapper mapper)
        {
            if (count < 2 || count > 4)
            {
                throw new System.ArgumentException("プレイヤー数は2-4の範囲で指定してください");
            }

            var list = new List<PlayerInfo>();
            var playerIdMapper = new PlayerIdMapper();

            for (int i = 0; i < count; i++)
            {
                var playerId = new PlayerId(i + 1);
                var actorNumber = i + 1;

                playerIdMapper.Register(playerId, actorNumber);

                list.Add(new PlayerInfo
                {
                    Id = playerId,
                    UserId = $"TestPlayer_{i + 1}",
                    PlayerType = i == 0 ? PlayerType.Local : PlayerType.Remote,
                    PlayerIconIndex = i % 4 // 0-3のアイコン
                });
            }

            mapper = playerIdMapper;
            Debug.Log($"PlayModeTestHarness: PlayerInfo生成完了 ({count}人)");
            return list;
        }

        /// <summary>
        /// 特定のプレイヤー情報を取得（テスト検証用）
        /// </summary>
        public PlayerInfo GetPlayerInfo(int index)
        {
            if (_gameManager == null)
            {
                throw new System.Exception("PlayModeTestHarness: GameManagerが初期化されていません");
            }

            var players = _gameManager.GameContext?.Players;
            if (players == null || index < 0 || index >= players.Count)
            {
                throw new System.Exception("PlayModeTestHarness: プレイヤー情報が取得できません");
            }

            return new PlayerInfo
            {
                Id = players[index].Id,
                UserId = $"TestPlayer_{index + 1}",
                PlayerType = index == 0 ? PlayerType.Local : PlayerType.Remote,
                PlayerIconIndex = index % 4
            };
        }
    }
}

