using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

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

            SceneManager.sceneLoaded += OnSceneLoaded;
            _lifecycleCts = new System.Threading.CancellationTokenSource();
            Debug.Log("ApplicationManager: 初期化");
        }
        #endregion

        #region Fields
        private System.Threading.CancellationTokenSource _lifecycleCts;
        private const string TitleSceneName = "TitleScene";
        private const string GameSceneName = "GameScene";
        private const string ResultSceneName = "ResultScene";
        private readonly List<string> _sceneHistory = new List<string>();
        private bool _isLoading = false;
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
            string previous = _sceneHistory[_sceneHistory.Count - 1];
            _sceneHistory.RemoveAt(_sceneHistory.Count - 1);
            await LoadSceneDirectAsync(previous);
        }

        // UI ボタン等から呼べる薄いラッパー
        public void GoToTitle() { GoToTitleAsync().Forget(); }
        public void GoToGame() { GoToGameAsync().Forget(); }
        public void GoToResult() { GoToResultAsync().Forget(); }
        public void GoBack() { GoBackAsync().Forget(); }

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
            _isLoading = true;
            try
            {
                var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                await op.ToUniTask(cancellationToken: _lifecycleCts.Token);
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
            InitializeGameSceneAsync(_lifecycleCts.Token).Forget();
        }
        #endregion

        #region Initialize Flow
        private async UniTaskVoid InitializeGameSceneAsync(System.Threading.CancellationToken ct)
        {
            // Photonの接続・InRoomを待機
            await UniTask.WaitUntil(() => PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom, cancellationToken: ct);

            // GameManager の出現を待機
            GameManager gameManager = null;
            await UniTask.WaitUntil(() =>
            {
                gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Exclude);
                return gameManager != null;
            }, cancellationToken: ct);

            // PlayerInfo リストを構築
            var players = BuildPlayerInfosFromPhoton();
            if (players == null || players.Count == 0)
            {
                Debug.LogError("ApplicationManager: PlayerInfo の構築に失敗");
                return;
            }

            // ローカルプレイヤーを特定
            var localActorNumber = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
            if (localActorNumber < 0)
            {
                Debug.LogError("ApplicationManager: LocalPlayer が無効");
                return;
            }

            var userInfoIndex = players.FindIndex(p => p.Id.Value == localActorNumber);
            if (userInfoIndex < 0)
            {
                Debug.LogError($"ApplicationManager: ローカルプレイヤー({localActorNumber})が PlayerInfo に存在しません");
                return;
            }

            var userInfo = players[userInfoIndex];

            // GameManager を初期化
            try
            {
                gameManager.Initialize(players, userInfo);
                Debug.Log("ApplicationManager: GameManager.Initialize を呼び出しました");
            }
            catch (System.SystemException ex)
            {
                Debug.LogError($"ApplicationManager: GameManager 初期化エラー: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region PlayerInfo Builder
        private List<PlayerInfo> BuildPlayerInfosFromPhoton()
        {
            var list = new List<PlayerInfo>();

            var actors = PhotonNetwork.PlayerList;
            if (actors == null || actors.Length == 0) return list;

            for (int i = 0; i < actors.Length; i++)
            {
                var actor = actors[i];
                int iconIndex = 0;
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

                var info = new PlayerInfo
                {
                    Id = new PlayerId(actor.ActorNumber),
                    UserId = string.IsNullOrEmpty(actor.NickName) ? $"Player_{actor.ActorNumber}" : actor.NickName,
                    PlayerType = actor.IsLocal ? PlayerType.Local : PlayerType.Remote,
                    PlayerIconIndex = iconIndex,
                };
                list.Add(info);
            }

            return list;
        }
        #endregion
    }
}