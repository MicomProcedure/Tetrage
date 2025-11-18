using UnityEngine;
#if UNITY_EDITOR
using Tetrage.Tests.PlayMode;
using Cysharp.Threading.Tasks;
using Tetrage.Network;
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
        [SerializeField, Range(2, 4)] private int _playerCount = 2;
        
        [Tooltip("ネットワークモード")]
        [SerializeField] private NetworkMode _networkMode = NetworkMode.VirtualTransport;
        
        [Tooltip("初期化後に自動的にゲームを開始する")]
        [SerializeField] private bool _autoStartGame = true;
        
        [Tooltip("乱数シード（-1で無効、0以上で固定）")]
        [SerializeField] private int _randomSeed = -1;

        private PlayModeTestHarness _harness;

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

            Debug.Log($"<color=cyan>GameSceneDebugEntrySimple: Debug環境でGameSceneを初期化します (Players: {_playerCount}, Mode: {_networkMode})</color>");
            
            await InitializeDebugEnvironment();
        }

        private async UniTask InitializeDebugEnvironment()
        {
            try
            {
                // テストハーネスでセットアップ
                _harness = new PlayModeTestHarness();
                await _harness.SetupGameScene(_playerCount, _networkMode, _randomSeed);
                
                Debug.Log("<color=green>GameSceneDebugEntrySimple: 初期化完了</color>");

                // ゲーム開始（オプション）
                if (_autoStartGame)
                {
                    Debug.Log("GameSceneDebugEntrySimple: ゲームを自動開始します");
                    await _harness.StartGame();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameSceneDebugEntrySimple: 初期化エラー: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void OnDestroy()
        {
            _harness?.Teardown();
        }

        [ContextMenu("Manual Initialize")]
        private void ManualInitialize()
        {
            InitializeDebugEnvironment().Forget();
        }

        [ContextMenu("Manual Start Game")]
        private void ManualStartGame()
        {
            _harness?.StartGame().Forget();
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

