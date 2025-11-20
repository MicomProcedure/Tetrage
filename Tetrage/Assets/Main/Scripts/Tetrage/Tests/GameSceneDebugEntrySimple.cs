using UnityEngine;
#if UNITY_EDITOR
using Tetrage.Tests.PlayMode;
using Cysharp.Threading.Tasks;
using Tetrage.Network;
using Tetrage.Core.Constants;
using Tetrage.Managers;
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

        [Tooltip("ネットワークモード")]
        [SerializeField] private NetworkMode _networkMode = NetworkMode.LogicInjection;

        [Tooltip("ローカルプレイヤーのインデックス（0始まり）")]
        [SerializeField, Range(0, SettingConsts.MAX_PLAYER_COUNT - 1)] private int _localPlayerIndex = 0;

        [Tooltip("初期化後に自動的にゲームを開始する")]
        [SerializeField] private bool _autoStartGame = false;

        [Tooltip("乱数シード（-1で無効、0以上で固定）")]
        [SerializeField] private int _randomSeed = -1;

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
                // PlayModeTestHelperを使用してGameManagerを初期化
                _gameManager = PlayModeTestHelper.QuickSetup(
                    playerCount: _playerCount,
                    mode: _networkMode,
                    localPlayerIndex: _localPlayerIndex,
                    randomSeed: _randomSeed
                );

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

