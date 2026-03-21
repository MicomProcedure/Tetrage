using UnityEngine;
#if UNITY_EDITOR
using Tetrage.Tests.PlayMode;
using Cysharp.Threading.Tasks;
using Tetrage.Network;
using Tetrage.Core.Constants;
using Tetrage.Managers;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;
using Photon.Pun;
using Photon.Realtime;
#endif

namespace Tetrage.Tests
{
    /// <summary>
    /// GameSceneを直接起動した時のDebugエントリポイント（シンプル版）。
    /// エディタ実行時のみ動作し、ビルドには影響しない。
    /// ApplicationManagerが存在する場合は自動的に自己無効化する。
    /// </summary>
    public class GameSceneDebugEntrySimple : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("Debug Mode Settings")]
        [Tooltip("Debugモードを有効にする（ApplicationManager不在時のみ動作）")]
        [SerializeField] private bool _enableDebugMode = true;

        [Header("Game Settings")]
        [Tooltip("プレイヤー数")]
        [SerializeField, Range(SettingConsts.MIN_PLAYER_COUNT, SettingConsts.MAX_PLAYER_COUNT)] private int _playerCount = 4;

        [Tooltip("スートごとのカード枚数")]
        [SerializeField, Min(1)] private int _cardCountPerSuit = InGameConsts.DEFAULT_INITIAL_COUNT_PER_SUIT;

        [Tooltip("使用するスートの種類数")]
        [SerializeField, Range(1, 4)] private int _suitTypeCount = InGameConsts.DEFAULT_INITIAL_SUITS.Length;

        [Tooltip("ネットワークモード")]
        [SerializeField] private NetworkMode _networkMode = NetworkMode.LogicInjection;

        [Tooltip("ローカルプレイヤーのインデックス（0始まり）")]
        [SerializeField, Range(0, SettingConsts.MAX_PLAYER_COUNT - 1)] private int _localPlayerIndex = 0;

        [Header("RealPhoton Debug Settings")]
        [Tooltip("RealPhoton時に参加するルーム名")]
        [SerializeField] private string _realPhotonRoomName = "Tetrage_DebugRoom";

        [Tooltip("RealPhoton時にJoinOrCreateRoomを使う")]
        [SerializeField] private bool _useJoinOrCreateRoom = true;

        [Tooltip("RealPhoton初期化前に待機する最小プレイヤー数")]
        [SerializeField, Range(SettingConsts.MIN_PLAYER_COUNT, SettingConsts.MAX_PLAYER_COUNT)] private int _realPhotonRequiredPlayerCount = 2;

        [Tooltip("Photon接続待機タイムアウト（秒）")]
        [SerializeField, Min(1f)] private float _realPhotonConnectTimeoutSec = 20f;

        [Tooltip("Photon入室待機タイムアウト（秒）")]
        [SerializeField, Min(1f)] private float _realPhotonJoinRoomTimeoutSec = 20f;

        [Tooltip("必要人数待機タイムアウト（秒）")]
        [SerializeField, Min(1f)] private float _realPhotonWaitPlayersTimeoutSec = 60f;

        [Tooltip("初期化後に自動的にゲームを開始する")]
        [SerializeField] private bool _autoStartGame = false;

        [Tooltip("乱数シード（-1で無効、0以上で固定）")]
        [SerializeField] private int _randomSeed = -1;

        [Header("Optional Settings")]
        [Tooltip("詳細なプレイヤー設定（任意）")]
        [SerializeField] private GameScenePlayerDebugSettings _playerDebugSettings;

        [Tooltip("ネットワークイベントデバッガ（任意）")]
        [SerializeField] private NetworkEventDebugger _networkDebugger;

        private GameManager _gameManager;

        private void Awake()
        {
            // 本番起動（ApplicationManager経由）の場合は即座に自己無効化
            if (Managers.ApplicationManager.Instance != null)
            {
                Debug.Log("GameSceneDebugEntrySimple: ApplicationManager検出。本番環境のため自己無効化します。");
                gameObject.SetActive(false);
                return;
            }
        }

        private async void Start()
        {
            // Awakeで無効化されている場合はここには到達しない

            if (!_enableDebugMode)
            {
                Debug.Log("GameSceneDebugEntrySimple: Debugモードが無効です");
                gameObject.SetActive(false);
                return;
            }

            _localPlayerIndex = Mathf.Clamp(_localPlayerIndex, 0, _playerCount - 1);
            Debug.Log($"<color=cyan>GameSceneDebugEntrySimple: Debug環境でGameSceneを初期化します (Players: {_playerCount}, Mode: {_networkMode}, LocalPlayer: Player{_localPlayerIndex + 1})</color>");

            await InitializeDebugEnvironment();
        }

        private async UniTask InitializeDebugEnvironment()
        {
            try
            {
                // 乱数シード固定
                if (_randomSeed >= 0)
                {
                    PlayModeTestHelper.SetRandomSeed(_randomSeed);
                }

                // PlayerInfo生成
                List<PlayerInfo> players;
                INetworkContext networkContext;
                IPlayerIdMapper mapper;

                if (_networkMode == NetworkMode.RealPhoton)
                {
                    if (_playerDebugSettings != null && _playerDebugSettings.DebugPlayerInfos.Count > 0)
                    {
                        Debug.LogWarning("GameSceneDebugEntrySimple: RealPhotonモードではGameScenePlayerDebugSettingsを無視します");
                    }

                    await EnsureRealPhotonReadyAsync();
                    networkContext = new PhotonNetworkContext();
                    players = BuildPlayerInfosFromPhoton(out mapper);
                    _playerCount = players.Count;
                }
                else
                {
                    int? userPlayerIndex = null;
                    if (_playerDebugSettings != null && _playerDebugSettings.DebugPlayerInfos.Count > 0)
                    {
                        Debug.Log("GameSceneDebugEntrySimple: GameScenePlayerDebugSettingsを使用してプレイヤーを作成します");

                        // IsUserPlayerが設定されている場合はそれを使用、なければ_localPlayerIndexを使用
                        userPlayerIndex = _playerDebugSettings.GetUserPlayerIndex(_playerCount);
                        if (userPlayerIndex == null)
                        {
                            userPlayerIndex = _localPlayerIndex;
                            Debug.Log($"GameSceneDebugEntrySimple: UserPlayerが設定されていないため、_localPlayerIndex ({_localPlayerIndex}) を使用します");
                        }
                        else
                        {
                            int userPlayerCount = _playerDebugSettings.GetUserPlayerCount(_playerCount);
                            if (userPlayerCount > 1)
                            {
                                Debug.LogWarning($"GameSceneDebugEntrySimple: {userPlayerCount}人のプレイヤーがUserPlayerに設定されています。最初の一人（インデックス: {userPlayerIndex}）を使用します。");
                            }
                            Debug.Log($"GameSceneDebugEntrySimple: UserPlayerインデックス: {userPlayerIndex}");
                        }

                        players = _playerDebugSettings.CreatePlayerInfos(_playerCount, userPlayerIndex);
                    }
                    else
                    {
                        userPlayerIndex = _localPlayerIndex;
                        players = PlayModeTestHelper.CreateDefaultPlayers(_playerCount, _localPlayerIndex);
                    }

                    // NetworkContext生成（userPlayerIndexを使用）
                    int localActorNumber = (userPlayerIndex ?? _localPlayerIndex) + 1;
                    networkContext = PlayModeTestHelper.CreateNetworkContext(
                        _networkMode,
                        localActorNumber: localActorNumber,
                        playerCount: _playerCount,
                        isHost: true
                    );

                    // PlayerIdMapper生成
                    mapper = PlayModeTestHelper.CreatePlayerIdMapper(players);
                }

                // UserPlayer特定
                var userInfo = PlayModeTestHelper.GetUserPlayer(players, networkContext, mapper);

                // GameRuleDTO生成
                var gameRule = new GameRuleDTO(
                    playerCount: _playerCount,
                    cardCountPerSuit: _cardCountPerSuit,
                    suitTypeCount: _suitTypeCount
                );

                // GameManager初期化
                _gameManager = PlayModeTestHelper.FindGameManager();
                _gameManager.Initialize(players, userInfo, networkContext, _networkMode, mapper, gameRule);

                // NetworkEventDebuggerのセットアップ
                if (_networkDebugger != null)
                {
                    _networkDebugger.Setup(_gameManager.NetworkController);
                    Debug.Log("GameSceneDebugEntrySimple: NetworkEventDebuggerをセットアップしました");
                }

                Debug.Log("<color=green>GameSceneDebugEntrySimple: 初期化完了</color>");

                // ゲーム開始（オプション）
                if (_autoStartGame)
                {
                    Debug.Log("GameSceneDebugEntrySimple: ゲームを自動開始します");
                    await _gameManager.StartGame();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameSceneDebugEntrySimple: 初期化エラー: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #region RealPhoton Debug

        private async UniTask EnsureRealPhotonReadyAsync()
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.Log("GameSceneDebugEntrySimple: Photonへ接続します");
                PhotonNetwork.ConnectUsingSettings();
            }

            await WaitUntilWithTimeout(
                () => PhotonNetwork.IsConnectedAndReady,
                _realPhotonConnectTimeoutSec,
                "Photon接続");

            if (!PhotonNetwork.InRoom)
            {
                var roomName = string.IsNullOrWhiteSpace(_realPhotonRoomName)
                    ? "Tetrage_DebugRoom"
                    : _realPhotonRoomName.Trim();
                var maxPlayers = (byte)Mathf.Clamp(_playerCount, SettingConsts.MIN_PLAYER_COUNT, SettingConsts.MAX_PLAYER_COUNT);

                if (_useJoinOrCreateRoom)
                {
                    var options = new RoomOptions
                    {
                        MaxPlayers = maxPlayers,
                        IsVisible = true,
                        IsOpen = true
                    };
                    Debug.Log($"GameSceneDebugEntrySimple: JoinOrCreateRoomを実行します (Room: {roomName}, MaxPlayers: {maxPlayers})");
                    PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
                }
                else
                {
                    Debug.Log($"GameSceneDebugEntrySimple: JoinRoomを実行します (Room: {roomName})");
                    PhotonNetwork.JoinRoom(roomName);
                }
            }

            await WaitUntilWithTimeout(
                () => PhotonNetwork.InRoom,
                _realPhotonJoinRoomTimeoutSec,
                "Photon入室");

            int requiredPlayerCount = Mathf.Clamp(
                _realPhotonRequiredPlayerCount,
                SettingConsts.MIN_PLAYER_COUNT,
                SettingConsts.MAX_PLAYER_COUNT);

            await WaitUntilWithTimeout(
                () => PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= requiredPlayerCount,
                _realPhotonWaitPlayersTimeoutSec,
                $"必要人数({requiredPlayerCount})の参加");

            Debug.Log($"GameSceneDebugEntrySimple: RealPhoton準備完了 (Room: {PhotonNetwork.CurrentRoom?.Name}, Players: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0})");
        }

        private List<PlayerInfo> BuildPlayerInfosFromPhoton(out IPlayerIdMapper mapper)
        {
            var photonPlayers = PhotonNetwork.PlayerList
                .OrderBy(player => player.ActorNumber)
                .ToArray();

            if (photonPlayers.Length == 0)
            {
                throw new System.InvalidOperationException("Photonルームにプレイヤーが存在しません");
            }

            mapper = new PlayerIdMapper();
            var result = new List<PlayerInfo>(photonPlayers.Length);
            for (int i = 0; i < photonPlayers.Length; i++)
            {
                var photonPlayer = photonPlayers[i];
                var playerId = new PlayerId(i + 1);
                mapper.Register(playerId, photonPlayer.ActorNumber);

                result.Add(new PlayerInfo
                {
                    Id = playerId,
                    UserId = string.IsNullOrWhiteSpace(photonPlayer.NickName)
                        ? $"Player_{photonPlayer.ActorNumber}"
                        : photonPlayer.NickName,
                    PlayerType = photonPlayer.IsLocal ? PlayerType.Local : PlayerType.Remote,
                    PlayerIconIndex = ResolvePhotonIconIndex(photonPlayer)
                });
            }

            return result;
        }

        private static int ResolvePhotonIconIndex(Player photonPlayer)
        {
            if (photonPlayer.CustomProperties == null || !photonPlayer.CustomProperties.TryGetValue("IconIndex", out var iconValue))
            {
                return 0;
            }

            return iconValue is int iconIndex ? iconIndex : 0;
        }

        private static async UniTask WaitUntilWithTimeout(System.Func<bool> predicate, float timeoutSec, string waitLabel)
        {
            float startTime = Time.realtimeSinceStartup;
            while (!predicate())
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed >= timeoutSec)
                {
                    throw new System.TimeoutException($"タイムアウト: {waitLabel} ({timeoutSec:0.0}s)");
                }

                await UniTask.Delay(100);
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                PlayModeTestHelper.QuickCleanup(_gameManager, _networkMode);
                _gameManager = null;
            }
        }

        [ContextMenu("Manual Initialize")]
        private void ManualInitialize()
        {
            InitializeDebugEnvironment().Forget();
        }

        [ContextMenu("Manual Start Game")]
        private void ManualStartGame()
        {
            _gameManager?.StartGame().Forget();
        }
#else
        // ビルド時には完全に空のクラスになる
        private void Awake()
        {
            // ビルド版では念のため無効化
            gameObject.SetActive(false);
        }
#endif
    }
}

