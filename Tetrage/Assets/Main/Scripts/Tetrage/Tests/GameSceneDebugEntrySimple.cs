using UnityEngine;
#if UNITY_EDITOR
using System;
using System.IO;
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
using Tetrage.Tests.Data;
using Photon.Pun;
using Photon.Realtime;
#endif

namespace Tetrage.Tests
{
    /// <summary>
    /// GameSceneを直接起動した時のDebugエントリポイント（シンプル版）。
    /// エディタ実行時のみ動作し、ビルドには影響しない。
    /// ApplicationManagerが存在する場合は自動的に自己無効化する。
    /// MPPM 時はメインEditorのインスペクタ（NetworkMode 等）を Library 経由で仮想Playerへ同期できる。
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
        [SerializeField] private NetworkMode _networkMode = NetworkMode.RealPhoton;

        [Tooltip("ローカルプレイヤーのインデックス（0始まり）")]
        [SerializeField, Range(0, SettingConsts.MAX_PLAYER_COUNT - 1)] private int _localPlayerIndex = 0;

        [Header("RealPhoton Debug Settings")]
        [Tooltip("RealPhoton時に参加するルーム名")]
        [SerializeField] private string _realPhotonRoomName = "Tetrage_DebugRoom";

        [Tooltip("RealPhoton時にJoinOrCreateRoomを使う（単体エディタPlay時のフォールバック。Multi-Play時は内部でHost/Guestを振り分ける）")]
        [SerializeField] private bool _useJoinOrCreateRoom = true;

        [Tooltip("MPPMでメインEditorに -name が付かず ReadOnlyTags も空のとき CreateRoom する（デフォルトオン: Master取り合い回避。単体エディタで同じRoomを JoinOrCreate したい・CreateRoom が衝突する場合はオフ）")]
        [SerializeField] private bool _realPhotonUnnamedMainUsesCreateRoom = true;

        [Tooltip("エディタ上の整合用（OnValidate で _playerCount と同期）。待機人数は _playerCount のみを使用する（MPPM でインスタンス間の値が食い違うとメインだけ待ち続ける非対称が起きるため）")]
        [SerializeField, Range(SettingConsts.MIN_PLAYER_COUNT, SettingConsts.MAX_PLAYER_COUNT)] private int _realPhotonRequiredPlayerCount = 2;

        [Tooltip("Photon接続待機タイムアウト（秒）")]
        [SerializeField, Min(1f)] private float _realPhotonConnectTimeoutSec = 20f;

        [Tooltip("Photon入室待機タイムアウト（秒）")]
        [SerializeField, Min(1f)] private float _realPhotonJoinRoomTimeoutSec = 20f;

        [Tooltip("必要人数待機タイムアウト（秒）。MPPMでは仮想Playerの接続がメインより遅れることが多いため、4人待ちなら120〜300秒を推奨")]
        [SerializeField, Min(1f)] private float _realPhotonWaitPlayersTimeoutSec = 180f;

        [Tooltip("初期化後に自動的にゲームを開始する")]
        [SerializeField] private bool _autoStartGame = false;

        [Tooltip("乱数シード（-1で無効、0以上で固定）")]
        [SerializeField] private int _randomSeed = -1;

        [Header("Optional Settings")]
        [Tooltip("詳細なプレイヤー設定（任意）")]
        [SerializeField] private GameScenePlayerDebugSettings _playerDebugSettings;

        [Tooltip("ネットワークイベントデバッガ（任意）")]
        [SerializeField] private NetworkEventDebugger _networkDebugger;

        [Header("MPPM インスペクタ同期")]
        [Tooltip("オン時: メインEditorのPlay開始で Library にスナップショットを書き、仮想Playerプロセスが読み取って NetworkMode 等を揃える（シーン上書きが仮想に届かない場合の対策）")]
        [SerializeField] private bool _mppmShareInspectorSnapshotAcrossProcesses = true;

        private GameManager _gameManager;
        private List<DebugPlayerInfo> _mppmSyncedDebugPlayerInfos;
        private bool _hasMppmSyncedDebugPlayerInfos;

        /// <summary>
        /// ApplicationManager 不在の直接 GameScene 起動時のみ、RealPhoton デバッグ中に PUN のシーン同期を一時的に無効化する。
        /// </summary>
        private bool _punAutoSyncSceneGuardActive;

        /// <summary>
        /// ガード適用前の <see cref="PhotonNetwork.AutomaticallySyncScene"/> を保持する。
        /// </summary>
        private bool _punAutoSyncScenePrevious;

        private void Awake()
        {
            // 本番起動（ApplicationManager経由）の場合は即座に自己無効化
            if (Managers.ApplicationManager.Instance != null)
            {
                Debug.Log("GameSceneDebugEntrySimple: ApplicationManager検出。本番環境のため自己無効化します。");
                gameObject.SetActive(false);
                return;
            }

            TryPublishMppmInspectorSnapshotIfMainEditor();
        }

        /// <summary>
        /// MPPM 等で同一シーンでもインスタンスごとにシリアライズ値がずれると、必要人数待ちだけ非対称になる。
        /// _realPhotonRequiredPlayerCount は _playerCount に揃える。
        /// </summary>
        private void OnValidate()
        {
            _playerCount = Mathf.Clamp(_playerCount, SettingConsts.MIN_PLAYER_COUNT, SettingConsts.MAX_PLAYER_COUNT);
            _realPhotonRequiredPlayerCount = _playerCount;
        }

        private async void Start()
        {
            // Awakeで無効化されている場合はここには到達しない

            await TryConsumeMppmInspectorSnapshotIfVirtualPlayerAsync();

            if (!_enableDebugMode)
            {
                Debug.Log("GameSceneDebugEntrySimple: Debugモードが無効です");
                gameObject.SetActive(false);
                return;
            }

            _localPlayerIndex = ResolveVirtualDebugPlayerIndex(
                _localPlayerIndex,
                _playerCount,
                TryGetMultiplayerPlayModePlayerName(out var mppmPlayerName) ? mppmPlayerName : null);
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
                        Debug.Log("GameSceneDebugEntrySimple: RealPhotonモードではPlayer名/IDはPhotonを使用し、初期カード指定のみGameScenePlayerDebugSettingsを使用します");
                    }

                    await EnsureRealPhotonReadyAsync();
                    networkContext = new PhotonNetworkContext();
                    players = BuildPlayerInfosFromPhoton(out mapper);
                    _playerCount = players.Count;
                }
                else
                {
                    int? userPlayerIndex = null;
                    var debugPlayerInfos = GetActiveDebugPlayerInfos();
                    if (debugPlayerInfos.Count > 0)
                    {
                        Debug.Log("GameSceneDebugEntrySimple: GameScenePlayerDebugSettingsを使用してプレイヤーを作成します");

                        // MPPMではプロセスごとのPlayer名を優先し、InspectorのIsUserPlayer固定で全員同じプレイヤーになるのを防ぐ。
                        if (TryGetRuntimeMppmPlayerIndex(out var mppmIndex))
                        {
                            userPlayerIndex = mppmIndex;
                            Debug.Log($"GameSceneDebugEntrySimple: MPPM Player名からUserPlayerインデックスを決定します: {userPlayerIndex}");
                        }
                        else
                        {
                            userPlayerIndex = GetUserPlayerIndex(debugPlayerInfos, _playerCount);
                        }

                        if (userPlayerIndex == null)
                        {
                            userPlayerIndex = _localPlayerIndex;
                            Debug.Log($"GameSceneDebugEntrySimple: UserPlayerが設定されていないため、_localPlayerIndex ({_localPlayerIndex}) を使用します");
                        }
                        else
                        {
                            int userPlayerCount = GetUserPlayerCount(debugPlayerInfos, _playerCount);
                            if (userPlayerCount > 1)
                            {
                                Debug.LogWarning($"GameSceneDebugEntrySimple: {userPlayerCount}人のプレイヤーがUserPlayerに設定されています。最初の一人（インデックス: {userPlayerIndex}）を使用します。");
                            }
                            Debug.Log($"GameSceneDebugEntrySimple: UserPlayerインデックス: {userPlayerIndex}");
                        }

                        players = CreatePlayerInfos(debugPlayerInfos, _playerCount, userPlayerIndex);
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
                        isHost: IsVirtualDebugHostInstance()
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
                ConfigureDebugInitialCardPlanner();

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
                    if (_networkMode == NetworkMode.RealPhoton && !PhotonNetwork.IsMasterClient)
                    {
                        Debug.LogWarning("GameSceneDebugEntrySimple: RealPhotonのAutoStartはMasterClientのみ実行できます。今回は待機します。");
                    }
                    else
                    {
                        Debug.Log("GameSceneDebugEntrySimple: ゲームを自動開始します");
                        await _gameManager.StartGame();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameSceneDebugEntrySimple: 初期化エラー: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #region MPPM MainEditor インスペクタ同期

        [Serializable]
        private sealed class MppmGameSceneDebugSnapshot
        {
            public string formatVersion = "1";
            public bool enableDebugMode;
            public int playerCount;
            public int cardCountPerSuit;
            public int suitTypeCount;
            public int networkMode;
            public string realPhotonRoomName = "";
            public bool useJoinOrCreateRoom;
            public bool realPhotonUnnamedMainUsesCreateRoom;
            public int realPhotonRequiredPlayerCount;
            public float realPhotonConnectTimeoutSec;
            public float realPhotonJoinRoomTimeoutSec;
            public float realPhotonWaitPlayersTimeoutSec;
            public bool autoStartGame;
            public int randomSeed;
            public List<SerializableDebugPlayerInfo> debugPlayerInfos;
        }

        [Serializable]
        private sealed class SerializableDebugPlayerInfo
        {
            public string playerName;
            public int playerId;
            public int playerType;
            public bool isUserPlayer;
            public int iconIndex;
            public bool setInitialCards;
            public SerializableCardSpec targetCard;
            public List<SerializableCardSpec> handCards;
        }

        [Serializable]
        private sealed class SerializableCardSpec
        {
            public int suit;
            public int number;
        }

        private const string MppmSnapshotFormatVersion = "1";

        private static string GetMppmSnapshotAbsolutePath()
        {
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                return string.IsNullOrEmpty(projectRoot)
                    ? null
                    : Path.Combine(projectRoot, "Library", "TetrageMppmGameSceneDebugEntry.snapshot.json");
            }
            catch
            {
                return null;
            }
        }

        private void TryPublishMppmInspectorSnapshotIfMainEditor()
        {
            if (!_mppmShareInspectorSnapshotAcrossProcesses)
            {
                return;
            }

            if (!TryGetMultiplayerPlayModeMainEditor(out var isMain) || !isMain)
            {
                return;
            }

            string path = GetMppmSnapshotAbsolutePath();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                MppmGameSceneDebugSnapshot snap = BuildMppmSnapshotFromInspector();
                File.WriteAllText(path, JsonUtility.ToJson(snap));
                Debug.Log($"GameSceneDebugEntrySimple: MPPM用インスペクタスナップショットを保存しました ({path})");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GameSceneDebugEntrySimple: スナップショット保存に失敗しました: {ex.Message}");
            }
        }

        private MppmGameSceneDebugSnapshot BuildMppmSnapshotFromInspector()
        {
            return new MppmGameSceneDebugSnapshot
            {
                formatVersion = MppmSnapshotFormatVersion,
                enableDebugMode = _enableDebugMode,
                playerCount = _playerCount,
                cardCountPerSuit = _cardCountPerSuit,
                suitTypeCount = _suitTypeCount,
                networkMode = (int)_networkMode,
                realPhotonRoomName = _realPhotonRoomName ?? "",
                useJoinOrCreateRoom = _useJoinOrCreateRoom,
                realPhotonUnnamedMainUsesCreateRoom = _realPhotonUnnamedMainUsesCreateRoom,
                realPhotonRequiredPlayerCount = _realPhotonRequiredPlayerCount,
                realPhotonConnectTimeoutSec = _realPhotonConnectTimeoutSec,
                realPhotonJoinRoomTimeoutSec = _realPhotonJoinRoomTimeoutSec,
                realPhotonWaitPlayersTimeoutSec = _realPhotonWaitPlayersTimeoutSec,
                autoStartGame = _autoStartGame,
                randomSeed = _randomSeed,
                debugPlayerInfos = BuildSerializableDebugPlayerInfos(GetActiveDebugPlayerInfos())
            };
        }

        private async UniTask TryConsumeMppmInspectorSnapshotIfVirtualPlayerAsync()
        {
            if (!_mppmShareInspectorSnapshotAcrossProcesses)
            {
                return;
            }

            if (!TryGetMultiplayerPlayModeMainEditor(out var isMain) || isMain)
            {
                return;
            }

            string path = GetMppmSnapshotAbsolutePath();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            for (var attempt = 0; attempt < 150; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        DateTime writeTime = File.GetLastWriteTimeUtc(path);
                        if ((DateTime.UtcNow - writeTime).TotalMinutes > 15d)
                        {
                            Debug.LogWarning(
                                "GameSceneDebugEntrySimple: スナップショットが古すぎます（15分以上前）。メインEditorを再度Playしてください。");
                            return;
                        }

                        string json = File.ReadAllText(path);
                        var snap = JsonUtility.FromJson<MppmGameSceneDebugSnapshot>(json);
                        if (snap == null || snap.formatVersion != MppmSnapshotFormatVersion)
                        {
                            Debug.LogWarning("GameSceneDebugEntrySimple: スナップショット形式が不正のため同期をスキップします。");
                            return;
                        }

                        ApplyMppmInspectorSnapshot(snap);
                        ApplyLocalPlayerIndexFromMppmPlayerTag();
                        Debug.Log(
                            $"GameSceneDebugEntrySimple: MPPM仮想PlayerへメインEditorのインスペクタを同期しました (NetworkMode={_networkMode}, Players={_playerCount})");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"GameSceneDebugEntrySimple: スナップショット読込失敗: {ex.Message}");
                    return;
                }

                await UniTask.Delay(20);
            }

            Debug.LogWarning(
                "GameSceneDebugEntrySimple: メインEditorのスナップショットが得られませんでした（メインを先にPlayするか、同期を有効にしてください）");
        }

        private void ApplyMppmInspectorSnapshot(MppmGameSceneDebugSnapshot s)
        {
            _enableDebugMode = s.enableDebugMode;
            _playerCount = Mathf.Clamp(
                s.playerCount,
                SettingConsts.MIN_PLAYER_COUNT,
                SettingConsts.MAX_PLAYER_COUNT);
            _cardCountPerSuit = Mathf.Max(1, s.cardCountPerSuit);
            _suitTypeCount = Mathf.Clamp(s.suitTypeCount, 1, 4);
            if (Enum.IsDefined(typeof(NetworkMode), s.networkMode))
            {
                _networkMode = (NetworkMode)s.networkMode;
            }

            _realPhotonRoomName = string.IsNullOrEmpty(s.realPhotonRoomName)
                ? "Tetrage_DebugRoom"
                : s.realPhotonRoomName;
            _useJoinOrCreateRoom = s.useJoinOrCreateRoom;
            _realPhotonUnnamedMainUsesCreateRoom = s.realPhotonUnnamedMainUsesCreateRoom;
            _realPhotonConnectTimeoutSec = Mathf.Max(1f, s.realPhotonConnectTimeoutSec);
            _realPhotonJoinRoomTimeoutSec = Mathf.Max(1f, s.realPhotonJoinRoomTimeoutSec);
            _realPhotonWaitPlayersTimeoutSec = Mathf.Max(1f, s.realPhotonWaitPlayersTimeoutSec);
            _autoStartGame = s.autoStartGame;
            _randomSeed = s.randomSeed;
            _mppmSyncedDebugPlayerInfos = BuildDebugPlayerInfos(s.debugPlayerInfos);
            _hasMppmSyncedDebugPlayerInfos = true;
            OnValidate();
        }

        private void ApplyLocalPlayerIndexFromMppmPlayerTag()
        {
            if (!TryGetMultiplayerPlayModePlayerName(out var playerName) || string.IsNullOrEmpty(playerName))
            {
                return;
            }

            for (int i = 0; i < SettingConsts.MAX_PLAYER_COUNT; i++)
            {
                if (string.Equals(playerName, $"Player{i + 1}", StringComparison.OrdinalIgnoreCase))
                {
                    _localPlayerIndex = i;
                    return;
                }
            }
        }

        /// <summary>
        /// MPPMのPlayer名から0始まりのプレイヤーインデックスを解決する。
        /// </summary>
        public static bool TryResolveMppmPlayerIndex(string playerName, out int playerIndex)
        {
            playerIndex = -1;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return false;
            }

            const string prefix = "Player";
            if (!playerName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string numberText = playerName.Substring(prefix.Length);
            if (!int.TryParse(numberText, out var playerNumber))
            {
                return false;
            }

            if (playerNumber < 1 || playerNumber > SettingConsts.MAX_PLAYER_COUNT)
            {
                return false;
            }

            playerIndex = playerNumber - 1;
            return true;
        }

        /// <summary>
        /// VirtualTransport系のローカルプレイヤー番号を決定する。
        /// </summary>
        public static int ResolveVirtualDebugPlayerIndex(int inspectorLocalPlayerIndex, int playerCount, string mppmPlayerName)
        {
            int clampedPlayerCount = Mathf.Clamp(playerCount, SettingConsts.MIN_PLAYER_COUNT, SettingConsts.MAX_PLAYER_COUNT);
            if (TryResolveMppmPlayerIndex(mppmPlayerName, out var mppmIndex) && mppmIndex < clampedPlayerCount)
            {
                return mppmIndex;
            }

            return Mathf.Clamp(inspectorLocalPlayerIndex, 0, clampedPlayerCount - 1);
        }

        private bool TryGetRuntimeMppmPlayerIndex(out int playerIndex)
        {
            playerIndex = -1;
            return TryGetMultiplayerPlayModePlayerName(out var playerName)
                && TryResolveMppmPlayerIndex(playerName, out playerIndex)
                && playerIndex < _playerCount;
        }

        private bool IsVirtualDebugHostInstance()
        {
            return !TryGetRuntimeMppmPlayerIndex(out var playerIndex) || playerIndex == 0;
        }

        private List<DebugPlayerInfo> GetActiveDebugPlayerInfos()
        {
            if (_hasMppmSyncedDebugPlayerInfos)
            {
                return _mppmSyncedDebugPlayerInfos ?? new List<DebugPlayerInfo>();
            }

            return _playerDebugSettings != null
                ? _playerDebugSettings.DebugPlayerInfos
                : new List<DebugPlayerInfo>();
        }

        private static int? GetUserPlayerIndex(IReadOnlyList<DebugPlayerInfo> debugPlayerInfos, int playerCount)
        {
            for (int i = 0; i < playerCount && i < debugPlayerInfos.Count; i++)
            {
                if (debugPlayerInfos[i].IsUserPlayer)
                {
                    return i;
                }
            }

            return null;
        }

        private static int GetUserPlayerCount(IReadOnlyList<DebugPlayerInfo> debugPlayerInfos, int playerCount)
        {
            int count = 0;
            for (int i = 0; i < playerCount && i < debugPlayerInfos.Count; i++)
            {
                if (debugPlayerInfos[i].IsUserPlayer)
                {
                    count++;
                }
            }

            return count;
        }

        private static List<PlayerInfo> CreatePlayerInfos(IReadOnlyList<DebugPlayerInfo> debugPlayerInfos, int playerCount, int? userPlayerIndex)
        {
            var playerInfos = new List<PlayerInfo>();
            var usedPlayerIds = new HashSet<int>();

            for (int i = 0; i < playerCount; i++)
            {
                DebugPlayerInfo debugInfo = i < debugPlayerInfos.Count ? debugPlayerInfos[i] : null;
                int playerIdValue = ResolvePlayerId(debugInfo, usedPlayerIds, playerCount, i);
                usedPlayerIds.Add(playerIdValue);

                var playerType = debugInfo?.PlayerType ?? PlayerType.Remote;
                if (i == userPlayerIndex)
                {
                    playerType = PlayerType.Local;
                }

                playerInfos.Add(new PlayerInfo
                {
                    Id = new PlayerId(playerIdValue),
                    UserId = debugInfo?.PlayerName ?? $"Player {i + 1}",
                    PlayerType = playerType,
                    PlayerIconIndex = debugInfo?.IconIndex ?? (i % 4)
                });
            }

            return playerInfos;
        }

        private static int ResolvePlayerId(DebugPlayerInfo debugInfo, HashSet<int> usedPlayerIds, int playerCount, int playerIndex)
        {
            if (debugInfo != null && debugInfo.PlayerId > 0 && !usedPlayerIds.Contains(debugInfo.PlayerId))
            {
                return debugInfo.PlayerId;
            }

            if (debugInfo != null && debugInfo.PlayerId > 0)
            {
                Debug.LogWarning($"GameSceneDebugEntrySimple: PlayerId {debugInfo.PlayerId} が重複しています。自動生成に切り替えます。");
            }

            int candidateValue = playerIndex + 1;
            return usedPlayerIds.Contains(candidateValue)
                ? FindNextAvailablePlayerId(usedPlayerIds, playerCount)
                : candidateValue;
        }

        private static int FindNextAvailablePlayerId(HashSet<int> usedIds, int playerCount)
        {
            for (int candidate = 1; candidate <= playerCount; candidate++)
            {
                if (!usedIds.Contains(candidate))
                {
                    return candidate;
                }
            }

            int nextId = playerCount + 1;
            while (usedIds.Contains(nextId))
            {
                nextId++;
            }

            return nextId;
        }

        private static List<SerializableDebugPlayerInfo> BuildSerializableDebugPlayerInfos(IReadOnlyList<DebugPlayerInfo> debugPlayerInfos)
        {
            var result = new List<SerializableDebugPlayerInfo>();
            if (debugPlayerInfos == null)
            {
                return result;
            }

            foreach (var info in debugPlayerInfos)
            {
                if (info == null)
                {
                    continue;
                }

                result.Add(new SerializableDebugPlayerInfo
                {
                    playerName = info.PlayerName,
                    playerId = info.PlayerId,
                    playerType = (int)info.PlayerType,
                    isUserPlayer = info.IsUserPlayer,
                    iconIndex = info.IconIndex,
                    setInitialCards = info.SetInitialCards,
                    targetCard = BuildSerializableCardSpec(info.TargetCard),
                    handCards = BuildSerializableCardSpecs(info.HandCards)
                });
            }

            return result;
        }

        private static SerializableCardSpec BuildSerializableCardSpec(CardSpec spec)
        {
            if (spec == null)
            {
                return null;
            }

            return new SerializableCardSpec
            {
                suit = (int)spec.Suit,
                number = spec.Number
            };
        }

        private static List<SerializableCardSpec> BuildSerializableCardSpecs(IReadOnlyList<CardSpec> specs)
        {
            var result = new List<SerializableCardSpec>();
            if (specs == null)
            {
                return result;
            }

            foreach (var spec in specs)
            {
                result.Add(BuildSerializableCardSpec(spec));
            }

            return result;
        }

        private static List<DebugPlayerInfo> BuildDebugPlayerInfos(IReadOnlyList<SerializableDebugPlayerInfo> serializedInfos)
        {
            var result = new List<DebugPlayerInfo>();
            if (serializedInfos == null)
            {
                return result;
            }

            foreach (var info in serializedInfos)
            {
                if (info == null)
                {
                    continue;
                }

                result.Add(new DebugPlayerInfo
                {
                    PlayerName = info.playerName,
                    PlayerId = info.playerId,
                    PlayerType = Enum.IsDefined(typeof(PlayerType), info.playerType) ? (PlayerType)info.playerType : PlayerType.Remote,
                    IsUserPlayer = info.isUserPlayer,
                    IconIndex = info.iconIndex,
                    SetInitialCards = info.setInitialCards,
                    TargetCard = BuildCardSpec(info.targetCard),
                    HandCards = BuildCardSpecs(info.handCards)
                });
            }

            return result;
        }

        private static CardSpec BuildCardSpec(SerializableCardSpec spec)
        {
            if (spec == null)
            {
                return new CardSpec();
            }

            return new CardSpec
            {
                Suit = Enum.IsDefined(typeof(Suit), spec.suit) ? (Suit)spec.suit : Suit.Spade,
                Number = spec.number
            };
        }

        private static List<CardSpec> BuildCardSpecs(IReadOnlyList<SerializableCardSpec> specs)
        {
            var result = new List<CardSpec>();
            if (specs == null)
            {
                return result;
            }

            foreach (var spec in specs)
            {
                result.Add(BuildCardSpec(spec));
            }

            return result;
        }

        private void ConfigureDebugInitialCardPlanner()
        {
            var debugPlayerInfos = GetActiveDebugPlayerInfos();
            if (_gameManager == null || debugPlayerInfos == null || !debugPlayerInfos.Any(info => info != null && info.SetInitialCards))
            {
                return;
            }

            var dealer = _gameManager.Dealer;
            dealer.DealerPlanner = new GameSceneDebugInitialCardDealerPlanner(debugPlayerInfos, dealer.DealerPlanner);
            Debug.Log("GameSceneDebugEntrySimple: GameScenePlayerDebugSettingsの初期カード指定をDealerPlannerへ適用しました");
        }

        #endregion

        #region RealPhoton Debug

        /// <summary>
        /// ApplicationManager 経由でない GameScene 直起動では、<c>AutomaticallySyncScene</c> が有効だと
        /// ルームのカレントシーン prop とビルドインデックスの不一致などで <c>PhotonNetwork.LoadLevel</c> が走り、
        /// このシーンがアンロードされて GameManager が破棄されることがある。
        /// 本番フロー（ApplicationManager あり）では触らない。
        /// </summary>
        private void ApplyPunAutoSyncSceneGuardIfDirectGameScenePlay()
        {
            if (ApplicationManager.Instance != null)
            {
                return;
            }

            if (_punAutoSyncSceneGuardActive)
            {
                return;
            }

            _punAutoSyncScenePrevious = PhotonNetwork.AutomaticallySyncScene;
            if (_punAutoSyncScenePrevious)
            {
                Debug.Log(
                    "GameSceneDebugEntrySimple: ApplicationManagerなしの直接起動のため PhotonNetwork.AutomaticallySyncScene をオフにします（ルームのシーン同期で GameScene が載せ替わるのを防ぐ）");
            }

            PhotonNetwork.AutomaticallySyncScene = false;
            _punAutoSyncSceneGuardActive = true;
        }

        private void RestorePunAutoSyncSceneGuardIfNeeded()
        {
            if (!_punAutoSyncSceneGuardActive)
            {
                return;
            }

            PhotonNetwork.AutomaticallySyncScene = _punAutoSyncScenePrevious;
            _punAutoSyncSceneGuardActive = false;
        }

        private async UniTask EnsureRealPhotonReadyAsync()
        {
            ApplyPunAutoSyncSceneGuardIfDirectGameScenePlay();

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
                await EnterRealPhotonDebugRoomAsync();
            }

            await WaitUntilWithTimeout(
                () => PhotonNetwork.InRoom,
                _realPhotonJoinRoomTimeoutSec,
                "Photon入室");

            // 待機人数は _playerCount のみ（_realPhotonRequiredPlayerCount は OnValidate で同期済み想定だが、実行時もここを正とする）
            int requiredPlayerCount = Mathf.Clamp(
                _playerCount,
                SettingConsts.MIN_PLAYER_COUNT,
                SettingConsts.MAX_PLAYER_COUNT);

            Debug.Log(
                $"GameSceneDebugEntrySimple: ルーム人数が {requiredPlayerCount} 人に達するまで待機します（最大 {_realPhotonWaitPlayersTimeoutSec:0.#}s）。基準は _playerCount のみ。");

            await WaitUntilWithTimeout(
                () => RealPhotonRoomHasAtLeastPlayerCount(requiredPlayerCount),
                _realPhotonWaitPlayersTimeoutSec,
                $"必要人数({requiredPlayerCount})の参加",
                BuildPhotonRoomPlayerCountProgress(requiredPlayerCount),
                10f);

            LogRealPhotonRuntimeStatus("RealPhoton準備完了");
            if (IsRealPhotonDebugHostInstance(out _) && !PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("GameSceneDebugEntrySimple: メインEditor側（Host想定）ですがMasterClientではありません。GameManager.StartGameは実行されません。");
            }

            Debug.Log($"GameSceneDebugEntrySimple: RealPhoton準備完了 (Room: {PhotonNetwork.CurrentRoom?.Name}, Players: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0})");
        }

        /// <summary>
        /// Multiplayer Play Mode 時は <c>-name</c> に応じて CreateRoom / JoinRoom を振り分ける。
        /// 単体エディタPlay（<c>-name</c> なし）は従来どおり <see cref="_useJoinOrCreateRoom"/> に従う。
        /// </summary>
        private async UniTask EnterRealPhotonDebugRoomAsync()
        {
            var roomName = string.IsNullOrWhiteSpace(_realPhotonRoomName)
                ? "Tetrage_DebugRoom"
                : _realPhotonRoomName.Trim();
            var maxPlayers = (byte)Mathf.Clamp(_playerCount, SettingConsts.MIN_PLAYER_COUNT, SettingConsts.MAX_PLAYER_COUNT);

            // IsMainEditor は単体エディタでも true になり得るため、CreateRoom は -name=Player1 / ReadOnlyTags / 明示オプションのときだけ行う。
            if (TryGetMultiplayerPlayModeMainEditor(out var isMainEditor))
            {
                bool hasCmdName = TryGetMultiplayerPlayModePlayerName(out var cmdPlayerName);
                bool tagsNonEmpty = TryHasNonEmptyMppmReadOnlyTags();
                Debug.Log(
                    $"GameSceneDebugEntrySimple: MPPM CurrentPlayer.IsMainEditor={isMainEditor}, cmdLineName={(hasCmdName ? cmdPlayerName : "なし")}, readOnlyTagsNonEmpty={tagsNonEmpty}, unnamedMainCreateRoomOpt={_realPhotonUnnamedMainUsesCreateRoom}");

                if (isMainEditor)
                {
                    bool createAsHost =
                        (hasCmdName && string.Equals(cmdPlayerName, "Player1", System.StringComparison.OrdinalIgnoreCase))
                        || (!hasCmdName && (tagsNonEmpty || _realPhotonUnnamedMainUsesCreateRoom));

                    if (createAsHost)
                    {
                        var hostRoomOptions = new RoomOptions
                        {
                            MaxPlayers = maxPlayers,
                            IsVisible = true,
                            IsOpen = true
                        };
                        string reason = hasCmdName && string.Equals(cmdPlayerName, "Player1", System.StringComparison.OrdinalIgnoreCase)
                            ? "-name=Player1"
                            : tagsNonEmpty
                                ? "ReadOnlyTags"
                                : "インスペクタオプション(unnamedMainUsesCreateRoom)";
                        Debug.Log($"GameSceneDebugEntrySimple: メインEditorとしてCreateRoomします（{reason}） (Room: {roomName}, MaxPlayers: {maxPlayers})");
                        PhotonNetwork.CreateRoom(roomName, hostRoomOptions, TypedLobby.Default);
                        return;
                    }

                    if (!hasCmdName)
                    {
                        Debug.LogWarning(
                            "GameSceneDebugEntrySimple: メインEditorで -name が無く、ReadOnlyTags も空、CreateRoom オプションもオフです。単体Playとして JoinOrCreate/Join します。MPPM で Master が取れない場合はオプションをオンにするかシナリオでタグを付けてください。");
                        EnterRealPhotonSingleEditorFallbackRoom(roomName, maxPlayers);
                        return;
                    }

                    Debug.LogWarning(
                        $"GameSceneDebugEntrySimple: メインEditorだが -name が '{cmdPlayerName}' のため JoinRoom を試みます (Room: {roomName})");
                    PhotonNetwork.JoinRoom(roomName);
                    return;
                }

                if (hasCmdName && IsMultiplayVirtualPlayerName(cmdPlayerName, out var staggerMs))
                {
                    Debug.Log($"GameSceneDebugEntrySimple: Multi-Play 仮想Player({cmdPlayerName}): HostのCreateRoom完了待ちのため {staggerMs}ms 待機してから JoinRoom します (Room: {roomName})");
                    await UniTask.Delay(staggerMs);
                    Debug.Log($"GameSceneDebugEntrySimple: Multi-Play 仮想Player({cmdPlayerName})としてJoinRoomします (Room: {roomName})");
                    PhotonNetwork.JoinRoom(roomName);
                    return;
                }

                if (hasCmdName && string.Equals(cmdPlayerName, "Player1", System.StringComparison.OrdinalIgnoreCase))
                {
                    var hostRoomOptions = new RoomOptions
                    {
                        MaxPlayers = maxPlayers,
                        IsVisible = true,
                        IsOpen = true
                    };
                    Debug.LogWarning(
                        $"GameSceneDebugEntrySimple: 仮想Player側なのに -name=Player1。CreateRoom を試みます (Room: {roomName})");
                    PhotonNetwork.CreateRoom(roomName, hostRoomOptions, TypedLobby.Default);
                    return;
                }

                Debug.LogWarning($"GameSceneDebugEntrySimple: MPPM仮想Playerで -name が取得できないか未対応です。600ms 待機後に JoinRoom します (Room: {roomName})");
                await UniTask.Delay(600);
                PhotonNetwork.JoinRoom(roomName);
                return;
            }

            if (!TryGetMultiplayerPlayModePlayerName(out var mppmPlayerName))
            {
                EnterRealPhotonSingleEditorFallbackRoom(roomName, maxPlayers);
                return;
            }

            if (string.Equals(mppmPlayerName, "Player1", System.StringComparison.OrdinalIgnoreCase))
            {
                var hostRoomOptions = new RoomOptions
                {
                    MaxPlayers = maxPlayers,
                    IsVisible = true,
                    IsOpen = true
                };
                Debug.Log($"GameSceneDebugEntrySimple: Multi-Play メインEditor(Player1)としてCreateRoomします (Room: {roomName}, MaxPlayers: {maxPlayers})");
                PhotonNetwork.CreateRoom(roomName, hostRoomOptions, TypedLobby.Default);
                return;
            }

            if (IsMultiplayVirtualPlayerName(mppmPlayerName, out var nameStaggerMs))
            {
                Debug.Log($"GameSceneDebugEntrySimple: Multi-Play 仮想Player({mppmPlayerName}): HostのCreateRoom完了待ちのため {nameStaggerMs}ms 待機してから JoinRoom します (Room: {roomName})");
                await UniTask.Delay(nameStaggerMs);
                Debug.Log($"GameSceneDebugEntrySimple: Multi-Play 仮想Player({mppmPlayerName})としてJoinRoomします (Room: {roomName})");
                PhotonNetwork.JoinRoom(roomName);
                return;
            }

            Debug.LogWarning($"GameSceneDebugEntrySimple: 未対応の -name '{mppmPlayerName}' のため JoinRoom を試みます (Room: {roomName})");
            PhotonNetwork.JoinRoom(roomName);
        }

        /// <summary>
        /// MPPM API が無い、または <c>-name</c> が無いときの単体エディタ向け入室。
        /// </summary>
        private void EnterRealPhotonSingleEditorFallbackRoom(string roomName, byte maxPlayers)
        {
            if (_useJoinOrCreateRoom)
            {
                var options = new RoomOptions
                {
                    MaxPlayers = maxPlayers,
                    IsVisible = true,
                    IsOpen = true
                };
                Debug.Log($"GameSceneDebugEntrySimple: 単体PlayのためJoinOrCreateRoomします (Room: {roomName}, MaxPlayers: {maxPlayers})");
                PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
            }
            else
            {
                Debug.Log($"GameSceneDebugEntrySimple: 単体PlayのためJoinRoomします (Room: {roomName})");
                PhotonNetwork.JoinRoom(roomName);
            }
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

        /// <summary>
        /// CurrentRoom.PlayerCount と PlayerList のどちらかが先に更新されることがあるため、多い方を採用する。
        /// </summary>
        private static bool RealPhotonRoomHasAtLeastPlayerCount(int requiredPlayerCount)
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            {
                return false;
            }

            int roomCount = PhotonNetwork.CurrentRoom.PlayerCount;
            int listCount = PhotonNetwork.PlayerList != null ? PhotonNetwork.PlayerList.Length : 0;
            return Mathf.Max(roomCount, listCount) >= requiredPlayerCount;
        }

        private static System.Func<string> BuildPhotonRoomPlayerCountProgress(int requiredPlayerCount)
        {
            return () =>
            {
                var room = PhotonNetwork.CurrentRoom;
                int roomCnt = room != null ? room.PlayerCount : 0;
                int listCnt = PhotonNetwork.PlayerList != null ? PhotonNetwork.PlayerList.Length : 0;
                int effective = Mathf.Max(roomCnt, listCnt);
                return
                    $"effective={effective}/{requiredPlayerCount} (Room.PlayerCount={roomCnt}, PlayerList.Length={listCnt}), Room={room?.Name ?? "(none)"}, InRoom={PhotonNetwork.InRoom}, IsMaster={PhotonNetwork.IsMasterClient}";
            };
        }

        private static async UniTask WaitUntilWithTimeout(
            System.Func<bool> predicate,
            float timeoutSec,
            string waitLabel,
            System.Func<string> progressDescription = null,
            float progressLogIntervalSec = 0f)
        {
            float startTime = Time.realtimeSinceStartup;
            float lastProgressLog = startTime;
            while (!predicate())
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed >= timeoutSec)
                {
                    throw new System.TimeoutException($"タイムアウト: {waitLabel} ({timeoutSec:0.0}s)");
                }

                if (progressDescription != null && progressLogIntervalSec > 0f)
                {
                    float now = Time.realtimeSinceStartup;
                    if (now - lastProgressLog >= progressLogIntervalSec)
                    {
                        Debug.Log(
                            $"GameSceneDebugEntrySimple: 待機中… {waitLabel} | {progressDescription()} | 経過 {elapsed:0.#}s / {timeoutSec:0.#}s");
                        lastProgressLog = now;
                    }
                }

                await UniTask.Delay(100);
            }
        }

        /// <summary>
        /// Multiplayer Play Mode では起動引数 <c>-name</c> が <c>Player1</c>（メインEditor）または <c>Player2</c>〜<c>Player4</c>（仮想Player）になる。
        /// メイン（Player1）および単体Play（<c>-name</c> なし）をゲーム進行ホスト想定とする。
        /// </summary>
        private static bool IsRealPhotonDebugHostInstance(out string mppmPlayerName)
        {
            if (TryGetMultiplayerPlayModeMainEditor(out var isMain))
            {
                TryGetMultiplayerPlayModePlayerName(out mppmPlayerName);
                if (string.IsNullOrEmpty(mppmPlayerName))
                {
                    mppmPlayerName = isMain ? "(MainEditor)" : "(VirtualPlayer)";
                }

                return isMain;
            }

            if (!TryGetMultiplayerPlayModePlayerName(out mppmPlayerName))
            {
                return true;
            }

            if (string.Equals(mppmPlayerName, "Player1", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IsMultiplayVirtualPlayerName(mppmPlayerName, out _))
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// <c>com.unity.multiplayer.playmode</c> の <c>CurrentPlayer.IsMainEditor</c> を参照する。
        /// asmdef 依存を増やさないためリフレクションを使用する（パッケージ未導入時は false を返す）。
        /// </summary>
        private static System.Type ResolveMultiplayerPlaymodeCurrentPlayerType()
        {
            return System.Type.GetType("Unity.Multiplayer.Playmode.CurrentPlayer, Unity.Multiplayer.Playmode")
                ?? System.Type.GetType("Unity.Multiplayer.PlayMode.CurrentPlayer, Unity.Multiplayer.PlayMode");
        }

        private static bool TryGetMultiplayerPlayModeMainEditor(out bool isMainEditor)
        {
            isMainEditor = true;
            try
            {
                var currentPlayerType = ResolveMultiplayerPlaymodeCurrentPlayerType();
                if (currentPlayerType == null)
                {
                    return false;
                }

                var property = currentPlayerType.GetProperty(
                    "IsMainEditor",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (property == null)
                {
                    return false;
                }

                if (property.GetValue(null) is bool value)
                {
                    isMainEditor = value;
                    return true;
                }
            }
            catch
            {
                // Multiplayer Play Mode Assembly が無い、または API 変更時は無視
            }

            return false;
        }

        /// <summary>
        /// MPPM の <c>CurrentPlayer.ReadOnlyTags()</c> が1件以上あるか（シナリオでタグを付けた場合、メインが -name 無しでもホスト判定に使える）。
        /// </summary>
        private static bool TryHasNonEmptyMppmReadOnlyTags()
        {
            try
            {
                var currentPlayerType = ResolveMultiplayerPlaymodeCurrentPlayerType();
                if (currentPlayerType == null)
                {
                    return false;
                }

                var method = currentPlayerType.GetMethod(
                    "ReadOnlyTags",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method == null)
                {
                    return false;
                }

                if (method.Invoke(null, null) is string[] tags)
                {
                    return tags.Length > 0;
                }
            }
            catch
            {
                // API 変更時は無視
            }

            return false;
        }

        private static bool IsMultiplayVirtualPlayerName(string playerName, out int staggerMs)
        {
            staggerMs = 600;
            if (string.Equals(playerName, "Player2", System.StringComparison.OrdinalIgnoreCase))
            {
                staggerMs = 300;
                return true;
            }

            if (string.Equals(playerName, "Player3", System.StringComparison.OrdinalIgnoreCase))
            {
                staggerMs = 600;
                return true;
            }

            if (string.Equals(playerName, "Player4", System.StringComparison.OrdinalIgnoreCase))
            {
                staggerMs = 900;
                return true;
            }

            return false;
        }

        private static bool TryGetMultiplayerPlayModePlayerName(out string playerName)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-name", System.StringComparison.OrdinalIgnoreCase))
                {
                    playerName = args[i + 1];
                    return !string.IsNullOrEmpty(playerName);
                }
            }

            playerName = null;
            return false;
        }

        #endregion

        private void OnDestroy()
        {
            RestorePunAutoSyncSceneGuardIfNeeded();

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
            if (_gameManager == null)
            {
                Debug.LogWarning("GameSceneDebugEntrySimple: GameManagerが未初期化のためゲーム開始できません");
                return;
            }

            if (_networkMode == NetworkMode.RealPhoton && !PhotonNetwork.IsMasterClient)
            {
                LogRealPhotonRuntimeStatus("Manual Start Blocked");
                Debug.LogWarning("GameSceneDebugEntrySimple: RealPhotonではMasterClientのみゲーム開始可能です。");
                return;
            }

            _gameManager?.StartGame().Forget();
        }

        private void LogRealPhotonRuntimeStatus(string label)
        {
            if (_networkMode != NetworkMode.RealPhoton)
            {
                return;
            }

            int localActor = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
            int masterActor = PhotonNetwork.MasterClient?.ActorNumber ?? -1;
            string roomName = PhotonNetwork.CurrentRoom?.Name ?? "(none)";
            int roomPlayerCount = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
            var hostInstance = IsRealPhotonDebugHostInstance(out var mppmName);
            Debug.Log(
                $"GameSceneDebugEntrySimple: [{label}] RealPhotonHostInstance={hostInstance}, MppmName={mppmName ?? "なし"}, InRoom={PhotonNetwork.InRoom}, IsMasterClient={PhotonNetwork.IsMasterClient}, LocalActor={localActor}, MasterActor={masterActor}, Room={roomName}, RoomPlayers={roomPlayerCount}");
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

