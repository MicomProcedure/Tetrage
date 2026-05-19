using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using Tetrage.Core;
using Tetrage.Core.Constants;
using Tetrage.Core.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Network;
using Tetrage.Network.Gameplay;
using Tetrage.Title;

namespace Tetrage.Managers
{
    /// <summary>
    /// アプリケーション全体のライフサイクル管理とエントリポイント。
    /// TitleScene で生成し、DontDestroyOnLoad で GameScene まで持ち越します。
    /// GameScene では GameManager の Initialize を呼び出します。
    /// </summary>
    public class ApplicationManager : MonoBehaviour
    {
        #region Singleton
        public static ApplicationManager Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // PUN2: ホストのレベルロードで他クライアントも同期させる
            PhotonNetwork.AutomaticallySyncScene = true;

            SceneManager.sceneLoaded += OnSceneLoaded;
            _lifecycleCts = new System.Threading.CancellationTokenSource();
            _playerSession = new PlayerSession();
            InitializeLocalPlayerSession();
            Debug.Log("ApplicationManager: 初期化");
        }
        #endregion

        #region PUN制御
        /// <summary>現在のクライアントがシーン遷移を制御できるか（オフライン or ホスト）</summary>
        public static bool CanControlScene => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
        #endregion

        #region NetworkMode管理
        /// <summary>
        /// 現在のNetworkModeを取得する
        /// </summary>
        public NetworkMode CurrentNetworkMode => _networkMode;
        public IPlayerSession PlayerSession => _playerSession;

        /// <summary>
        /// NetworkModeを設定する。
        /// GameManager.Initialize()後は変更不可（ロック機構）。
        /// </summary>
        /// <param name="mode">設定するNetworkMode</param>
        /// <returns>設定に成功した場合true、ロック後の場合false</returns>
        public bool SetNetworkMode(NetworkMode mode)
        {
            if (_networkModeLocked)
            {
                Debug.LogWarning($"ApplicationManager: NetworkModeはロック済みです。変更できません。(現在: {_networkMode})");
                return false;
            }

            _networkMode = mode;
            Debug.Log($"ApplicationManager: NetworkModeを {mode} に設定しました");
            return true;
        }

        /// <summary>
        /// NetworkModeをロックする。
        /// GameManager.Initialize()呼び出し時に自動的にロックされる。
        /// </summary>
        internal void LockNetworkMode()
        {
            if (!_networkModeLocked)
            {
                _networkModeLocked = true;
                Debug.Log($"ApplicationManager: NetworkModeをロックしました (モード: {_networkMode})");
            }
        }
        #endregion

        #region Fields
        private System.Threading.CancellationTokenSource _lifecycleCts;
        private const string TitleSceneName = "TitleScene";
        private const string GameSceneName = "GameScene";
        private const string ResultSceneName = "ResultScene";
        private const string PlayerProfileSaveKey = "PlayerProfile";
        private INetworkContext _networkContext; // NetworkModeに応じたNetworkContext（現時点はPhoton実装）
        private IPlayerIdMapper _playerIdMapper; // PlayerId/ActorNumberマッピング
        private IPlayerSession _playerSession; // Photon非依存プレイヤーセッション
        private readonly Dictionary<int, int> _sessionIdToActorNumberMap = new Dictionary<int, int>();
        private readonly List<string> _sceneHistory = new List<string>();
        private bool _isLoading = false;

        // Phase 4: NetworkMode管理
        private NetworkMode _networkMode = NetworkMode.RealPhoton; // デフォルトはRealPhoton
        private bool _networkModeLocked = false; // GameManager.Initialize()後はロック
        #endregion

        #region Unity Events
        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _lifecycleCts?.Cancel();
                _lifecycleCts?.Dispose();
                _lifecycleCts = null;
                Instance = null;
            }
        }
        #endregion

        #region Scene Navigation (SceneManager ベース)
        public async UniTask GoToTitleAsync() { await LoadSceneByNameAsync(TitleSceneName); }
        public async UniTask GoToGameAsync() { await LoadSceneByNameAsync(GameSceneName); }
        public async UniTask GoToResultAsync() { await LoadSceneByNameAsync(ResultSceneName); }

        public async UniTask GoBackAsync()
        {
            if (_isLoading) return;
            if (_sceneHistory.Count == 0) return;
            if (!CanControlScene)
            {
                Debug.LogWarning("ApplicationManager: ホストのみシーン遷移が可能です");
                return;
            }
            string previous = _sceneHistory[_sceneHistory.Count - 1];
            _sceneHistory.RemoveAt(_sceneHistory.Count - 1);
            await LoadSceneDirectAsync(previous);
        }

        // UI ボタン等から呼べる薄いラッパー
        public void GoToTitle() { GoToTitleAsync().Forget(); }
        public void GoToGame() { GoToGameAsync().Forget(); }
        public void GoToResult() { GoToResultAsync().Forget(); }
        public void GoBack() { GoBackAsync().Forget(); }
        #endregion

        #region Application Lifecycle
        /// <summary>
        /// Photon の部屋から退出する（同期）。部屋未参加時は false。
        /// </summary>
        public bool TryLeavePhotonRoom()
        {
            if (!ValidateLeavePhotonRoomPreconditions(out var reason))
            {
                Debug.LogWarning($"ApplicationManager: 部屋から退出できません - {reason}");
                return false;
            }

            Debug.Log("ApplicationManager: 部屋退出リクエストを送信します");
            return PhotonNetwork.LeaveRoom();
        }

        /// <summary>Photon の部屋から退出し、退出完了まで待機する</summary>
        public async UniTask LeavePhotonRoomAsync()
        {
            if (!ValidateLeavePhotonRoomPreconditions(out var reason))
            {
                Debug.LogWarning($"ApplicationManager: 部屋から退出できません - {reason}");
                return;
            }

            await LeavePhotonRoomInternalAsync(_lifecycleCts.Token);
        }

        /// <summary>UI 等から呼ぶ Photon 部屋退出の薄いラッパー</summary>
        public void LeavePhotonRoom() { LeavePhotonRoomAsync().Forget(); }

        /// <summary>アプリケーションを終了する（Photon 切断後に終了）</summary>
        public async UniTask QuitApplicationAsync()
        {
            await CleanupPhotonConnectionAsync(_lifecycleCts.Token);
            QuitApplicationImmediate();
        }

        /// <summary>UI 等から呼ぶアプリケーション終了の薄いラッパー</summary>
        public void QuitApplication() { QuitApplicationAsync().Forget(); }

        /// <summary>
        /// アプリケーションを再起動する（Photon 切断・状態リセット後にタイトルシーンへ戻る）
        /// </summary>
        public async UniTask RestartApplicationAsync()
        {
            if (_isLoading)
            {
                Debug.LogWarning("ApplicationManager: シーン読み込み中のため再起動をスキップします");
                return;
            }

            await CleanupPhotonConnectionAsync(_lifecycleCts.Token);
            ResetApplicationStateForRestart();
            await LoadBootstrapSceneAsync(_lifecycleCts.Token);
            Debug.Log("ApplicationManager: アプリケーションを再起動しました");
        }

        /// <summary>UI 等から呼ぶアプリケーション再起動の薄いラッパー</summary>
        public void RestartApplication() { RestartApplicationAsync().Forget(); }

        private async UniTask LeavePhotonRoomInternalAsync(CancellationToken ct)
        {
            if (!PhotonNetwork.InRoom)
            {
                return;
            }

            if (!PhotonNetwork.LeaveRoom())
            {
                Debug.LogWarning("ApplicationManager: LeaveRoom の送信に失敗しました");
                return;
            }

            try
            {
                // サーバーからの退出完了を待つ
                await UniTask.WaitUntil(() => !PhotonNetwork.InRoom, cancellationToken: ct)
                    .Timeout(TimeSpan.FromSeconds(ApplicationConsts.PHOTON_LEAVE_ROOM_TIMEOUT_SECONDS));
                Debug.Log("ApplicationManager: 部屋から退出しました");
            }
            catch (TimeoutException)
            {
                Debug.LogWarning(
                    $"ApplicationManager: 部屋退出が {ApplicationConsts.PHOTON_LEAVE_ROOM_TIMEOUT_SECONDS} 秒以内に完了しませんでした");
            }
        }

        private async UniTask DisconnectPhotonIfConnectedAsync(CancellationToken ct)
        {
            if (!PhotonNetwork.IsConnected)
            {
                return;
            }

            PhotonNetwork.Disconnect();
            try
            {
                await UniTask.WaitUntil(() => !PhotonNetwork.IsConnected, cancellationToken: ct)
                    .Timeout(TimeSpan.FromSeconds(ApplicationConsts.PHOTON_DISCONNECT_TIMEOUT_SECONDS));
                Debug.Log("ApplicationManager: Photon から切断しました");
            }
            catch (TimeoutException)
            {
                Debug.LogWarning(
                    $"ApplicationManager: Photon 切断が {ApplicationConsts.PHOTON_DISCONNECT_TIMEOUT_SECONDS} 秒以内に完了しませんでした");
            }
        }

        private async UniTask CleanupPhotonConnectionAsync(CancellationToken ct)
        {
            if (PhotonNetwork.InRoom)
            {
                await LeavePhotonRoomInternalAsync(ct);
            }

            await DisconnectPhotonIfConnectedAsync(ct);
        }

        private void ResetApplicationStateForRestart()
        {
            _networkModeLocked = false;
            _networkMode = NetworkMode.RealPhoton;
            _networkContext = null;
            _playerIdMapper = null;
            _sceneHistory.Clear();
            _isLoading = false;
            _sessionIdToActorNumberMap.Clear();
            _playerSession.Clear(false);
            InitializeLocalPlayerSession();
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        private async UniTask LoadBootstrapSceneAsync(CancellationToken ct)
        {
            _isLoading = true;
            try
            {
                var op = SceneManager.LoadSceneAsync(TitleSceneName, LoadSceneMode.Single);
                await op.ToUniTask(cancellationToken: ct);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private static void QuitApplicationImmediate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool ValidateLeavePhotonRoomPreconditions(out string reason)
        {
            reason = null;
            if (!PhotonNetwork.InRoom)
            {
                reason = "部屋に参加していません";
                return false;
            }

            return true;
        }
        #endregion

        #region Scene Loading (private)
        private async UniTask LoadSceneByNameAsync(string sceneName)
        {
            if (_isLoading) return;
            string current = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(current) && current != sceneName)
            {
                _sceneHistory.Add(current);
            }
            await LoadSceneDirectAsync(sceneName);
        }

        private async UniTask LoadSceneDirectAsync(string sceneName)
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                if (PhotonNetwork.InRoom)
                {
                    if (!PhotonNetwork.IsMasterClient)
                    {
                        Debug.LogWarning("ApplicationManager: ホストのみシーン遷移が可能です");
                        return;
                    }
                    // MasterClient が Photon 経由で同期遷移
                    PhotonNetwork.LoadLevel(sceneName);
                    await Cysharp.Threading.Tasks.UniTask.WaitUntil(
                        () => SceneManager.GetActiveScene().name == sceneName,
                        cancellationToken: _lifecycleCts.Token
                    );
                }
                else
                {
                    // オフライン時は通常ロード
                    var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                    await op.ToUniTask(cancellationToken: _lifecycleCts.Token);
                }
            }
            finally
            {
                _isLoading = false;
            }
        }
        #endregion

        #region Scene Handling
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameSceneName) return;
            Debug.Log("ApplicationManager: GameSceneが読み込まれました");
            InitializeGameSceneAsync(_lifecycleCts.Token).Forget();
        }
        #endregion

        #region Initialize Flow
        private async UniTaskVoid InitializeGameSceneAsync(System.Threading.CancellationToken ct)
        {
            List<PlayerInfo> players;
            PlayerInfo userInfo;

            switch (_networkMode)
            {
                case NetworkMode.RealPhoton:
                    await UniTask.WaitUntil(() => PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom, cancellationToken: ct);
                    _networkContext = new PhotonNetworkContext();
                    SyncSessionFromPhoton();
                    players = _playerSession.BuildPlayerInfos(out _playerIdMapper, _sessionIdToActorNumberMap);
                    if (!TryGetRealPhotonUserInfo(players, out userInfo))
                    {
                        return;
                    }
                    break;

                case NetworkMode.VirtualTransport:
                case NetworkMode.LogicInjection:
                case NetworkMode.LocalVsBot:
                    EnsureSessionPlayersForOfflineMode();
                    players = _playerSession.BuildPlayerInfos(out _playerIdMapper);
                    userInfo = players.Find(p => p.PlayerType == PlayerType.Local);
                    if (userInfo == null)
                    {
                        Debug.LogError("ApplicationManager: オフライン用のローカルプレイヤーが見つかりません");
                        return;
                    }

                    _networkContext = new VirtualNetworkContext(
                        actorNumber: userInfo.Id.Value,
                        isHost: true,
                        playerCount: players.Count,
                        isReady: true,
                        isInRoom: true
                    );
                    break;

                default:
                    Debug.LogError($"ApplicationManager: 未知のNetworkModeです。mode={_networkMode}");
                    return;
            }

            Debug.Log($"ApplicationManager: NetworkContext生成完了 (IsHost: {_networkContext.IsHost}, ActorNumber: {_networkContext.UserActorNumber})");

            // GameManager の出現を待機
            GameManager gameManager = null;
            await UniTask.WaitUntil(() =>
            {
                gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Exclude);
                Debug.Log("ApplicationManager: GameManagerが見つかりました");
                return gameManager != null;
            }, cancellationToken: ct);

            if (players == null || players.Count == 0)
            {
                Debug.LogError("ApplicationManager: PlayerInfo の構築に失敗");
                return;
            }

            // GameManager を初期化
            try
            {
                gameManager.Initialize(players, userInfo, _networkContext, _networkMode, _playerIdMapper);

                // Phase 4: NetworkModeをロック（以後変更不可）
                LockNetworkMode();

                Debug.Log("ApplicationManager: GameManager.Initialize を呼び出しました");
            }
            catch (System.SystemException ex)
            {
                Debug.LogError($"ApplicationManager: GameManager 初期化エラー: {ex.Message}");
                throw;
            }

            // ゲームを開始
            gameManager.StartGame().Forget();
        }
        #endregion

        #region Session Helpers
        private void InitializeLocalPlayerSession()
        {
            var loaded = TryLoadLocalProfile(out var playerName, out var iconIndex);
            if (!loaded)
            {
                playerName = "Player";
                iconIndex = 0;
            }

            _playerSession.RegisterLocalPlayer(playerName, iconIndex);
        }

        private bool TryLoadLocalProfile(out string playerName, out int iconIndex)
        {
            playerName = "Player";
            iconIndex = 0;

            if (!PlayerPrefs.HasKey(PlayerProfileSaveKey))
            {
                return false;
            }

            var json = PlayerPrefs.GetString(PlayerProfileSaveKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                var profile = JsonUtility.FromJson<PlayerProfileData>(json);
                if (profile == null)
                {
                    return false;
                }

                playerName = string.IsNullOrWhiteSpace(profile.PlayerName) ? "Player" : profile.PlayerName.Trim();
                iconIndex = profile.IconIndex;
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"ApplicationManager: ローカルプロファイル読み込みに失敗しました: {ex.Message}");
                return false;
            }
        }

        private void SyncSessionFromPhoton()
        {
            _playerSession.Clear(false);
            _sessionIdToActorNumberMap.Clear();

            var actors = PhotonNetwork.PlayerList;
            if (actors == null || actors.Length == 0)
            {
                return;
            }

            var sortedActors = actors.OrderBy(a => a.ActorNumber).ToArray();
            foreach (var actor in sortedActors)
            {
                var iconIndex = 0;
                if (actor.CustomProperties != null && actor.CustomProperties.ContainsKey("IconIndex"))
                {
                    try
                    {
                        iconIndex = (int)actor.CustomProperties["IconIndex"];
                    }
                    catch
                    {
                        iconIndex = 0;
                    }
                }

                var playerName = string.IsNullOrWhiteSpace(actor.NickName) ? $"Player_{actor.ActorNumber}" : actor.NickName;
                var sessionId = _playerSession.AddParticipant(playerName, iconIndex, actor.IsLocal);
                _sessionIdToActorNumberMap[sessionId] = actor.ActorNumber;
            }
        }

        private bool TryGetRealPhotonUserInfo(List<PlayerInfo> players, out PlayerInfo userInfo)
        {
            userInfo = null;
            var localActorNumber = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
            if (localActorNumber < 0)
            {
                Debug.LogError("ApplicationManager: LocalPlayer が無効です");
                return false;
            }

            if (!_playerIdMapper.TryGetPlayerId(localActorNumber, out var localPlayerId))
            {
                Debug.LogError($"ApplicationManager: ActorNumber={localActorNumber} のPlayerIdマッピングが見つかりません");
                return false;
            }

            var index = players.FindIndex(p => p.Id.Equals(localPlayerId));
            if (index < 0)
            {
                Debug.LogError($"ApplicationManager: ローカルプレイヤー(PlayerId={localPlayerId.Value})が PlayerInfo に存在しません");
                return false;
            }

            userInfo = players[index];
            return true;
        }

        private void EnsureSessionPlayersForOfflineMode()
        {
            if (_playerSession.ParticipantCount == 0)
            {
                InitializeLocalPlayerSession();
            }

            while (_playerSession.ParticipantCount < 4)
            {
                var index = _playerSession.ParticipantCount + 1;
                _playerSession.AddParticipant($"Player_{index}", 0, false);
            }
        }
        #endregion
    }
}