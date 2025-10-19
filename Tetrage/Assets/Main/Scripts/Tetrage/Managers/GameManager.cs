using UnityEngine;
using Tetrage.Core.Enums;
using Tetrage.Core.DTO;
using Tetrage.Components;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using Tetrage.Core.Contracts;
using Tetrage.Network.Gameplay;
using Tetrage.Core.Ids;
using Tetrage.Models;
using Photon.Pun;
using Tetrage.Core.Constants;

namespace Tetrage.Managers
{
    public class GameManager : MonoBehaviour, IDebuggable
    {



        #region 設定コンポーネント
        [Header("Field Setup Configuration")]
        [SerializeField] private FieldSetupComponent _fieldSetupComponent;
        [SerializeField] private GameMode _gameMode = GameMode.Debug;

        #endregion

        #region 管理対象インスタンス
        private FieldSetupManager _fieldSetupManager;
        private Dealer _dealer;
        private IGameplayNetworkController _netCtl;

        #endregion

        #region 状態管理
        private bool _isInitialized = false;
        private bool _isGameRunning = false;
        private CancellationTokenSource _gameCts;
        [SerializeField] private bool _networkInitialized = false;

        #endregion


        #region プロパティ
        /// <summary>セットアップが完了したDealer</summary>
        public Dealer Dealer
        {
            get
            {
                if (!_isInitialized)
                {
                    throw new System.InvalidOperationException("GameManager が初期化されていません。");
                }
                return _dealer;
            }
        }

        /// <summary>ゲーム実行中かどうか</summary>
        public bool IsGameRunning => _isGameRunning;

        /// <summary>初期化済みかどうか</summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region ライフサイクル


        #endregion

        #region 初期化メソッド
        /// <summary>
        /// GameManagerの初期化
        /// </summary>
        /// <param name="participantInfoList">参加者情報リスト</param>
        /// <param name="dealerStrategy">DealerStrategy</param>
        public void Initialize(List<PlayerInfo> participantInfoList)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("GameManager: 既に初期化済みです");
                return;
            }

            try
            {
                // 1. FieldSetupManagerの生成
                _fieldSetupManager = CreateFieldSetupManager(participantInfoList.Count);

                // 1.5 ネットワーク接続時は PlayerId=ActorNumber へマッピング
                var effectiveParticipants = RemapPlayerInfosToActorNumbers(participantInfoList);

                // 2. フィールドのセットアップ
                _fieldSetupManager.SetupField(effectiveParticipants);

                // 3. Dealerの生成と初期化（ブロードキャスタを注入）
                _dealer = DealerFactory.CreateDealer(
                    _fieldSetupManager,
                    _gameMode,
                    broadcaster: null // InitializeNetworking 後に差し替える
                );

                _isInitialized = true;

                EventSubscribe();

                // ネットワーク受信・適用の初期化（ホスト/ゲスト共通）
                InitializeNetworking();

                // Dealerへ Broadcaster を提供（Hostのみ有効。Guestはnullのまま）
                var bc = _netCtl?.GetBroadcaster();
                if (bc != null)
                {
                    _dealer.SetEmitter(new DealerPlanEmitter(bc));
                }

                Debug.Log("GameManager: 初期化が完了しました");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameManager: 初期化中にエラーが発生: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// FieldSetupManagerを生成する
        /// </summary>
        /// <param name="participantCount">参加者数</param>
        private FieldSetupManager CreateFieldSetupManager(int participantCount)
        {
            if (_fieldSetupComponent == null)
            {
                throw new System.InvalidOperationException("FieldSetupComponent が見つかりません。Inspector で設定してください。");
            }

            // FieldSetupComponentから検証済みの設定を取得
            var settings = _fieldSetupComponent.GetValidatedFieldSetupSettings(participantCount);
            var dependencies = _fieldSetupComponent.CreateFieldSetupDependencies();

            return new FieldSetupManager(settings, dependencies);
        }

        /// <summary>
        /// PlayerId を ActorNumber に強制マップする（オンライン時の統一）。
        /// オフライン/未接続時は入力をそのまま返す。
        /// </summary>
        private List<PlayerInfo> RemapPlayerInfosToActorNumbers(List<PlayerInfo> input)
        {
            if (!PhotonNetwork.IsConnectedAndReady) return input;
            var remapped = new List<PlayerInfo>(input.Count);
            // ここでは単純に順番どおりにActorNumberを割り当てる例。実際はルーム参加者列挙で対応。
            // 注意: 本実装は最小例です。実運用では PhotonNetwork.PlayerList を参照してください。
            var actors = PhotonNetwork.PlayerList; // 並び順はJoin順。必要に応じてソート。
            for (int i = 0; i < input.Count && i < actors.Length; i++)
            {
                var src = input[i];
                src.Id = new PlayerId(actors[i].ActorNumber);
                remapped.Add(src);
            }
            // 余りはそのまま（オフライン想定）
            for (int i = remapped.Count; i < input.Count; i++) remapped.Add(input[i]);
            return remapped;
        }

        private void EventSubscribe()
        {
            _dealer.GameEnd += OnGameEnd;
        }

        private void EventUnsubscribe()
        {
            if (_dealer != null)
            {
                _dealer.GameEnd -= OnGameEnd;
            }
        }


        #endregion

        #region ゲーム制御メソッド
        public async UniTask StartGame()
        {
            if (!_isInitialized)
            {
                Debug.LogError("GameManager: 初期化されていません");
                return;
            }

            if (_isGameRunning)
            {
                Debug.LogWarning("GameManager: ゲームが既に実行中です");
                return;
            }

            try
            {
                _isGameRunning = true;
                _gameCts = new CancellationTokenSource();

                // ホストの場合は初期宣言（GameStarted）を発行（簡易：DeckId=1/仮）
                if (PhotonNetwork.IsMasterClient && _networkInitialized)
                {
                    var started = new GameStartedEvent
                    {
                        deckId = InGameConsts.DEFAULT_DECK_ID,
                        suitOrder = new byte[] { 0, 1, 2, 3 },
                        minNumber = 1,
                        maxNumber = 13,
                        playerActorNumbers = null,
                    };
                    _netCtl.GetBroadcaster()?.Raise(EventCode.GameStarted, started);
                    await _dealer.StartGameAsync(0f, _gameCts.Token);   // ゲーム開始（ホスト）
                }

                Debug.Log("GameManager: ゲームが正常に終了しました");
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("GameManager: ゲームがキャンセルされました");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameManager: ゲーム実行中にエラーが発生: {ex.Message}");
                throw;
            }
            finally
            {
                _isGameRunning = false;
            }
        }

        #endregion

        #region イベントハンドラ

        private void OnGameEnd()
        {
            Debug.Log("GameManager: ゲーム終了イベントを受信");
            _isGameRunning = false;
            EventUnsubscribe();
        }

        #endregion

        #region デバッグ描画用

        public void DrawDebugGUI()
        {

        }

        #endregion

        #region Unity固有メソッド
        private void Update()
        {
            // 必要に応じてフェーズ管理のUpdate処理
        }

        /// <summary>
        /// ゲームを停止してGameManagerをリセットする
        /// </summary>
        [ContextMenu("Stop and Reset")]
        public void StopAndReset()
        {
            Debug.Log("GameManager: 停止とリセットを実行");

            // ゲーム停止
            _gameCts?.Cancel();

            // イベント購読解除
            EventUnsubscribe();

            // ネットワーク停止
            if (_networkInitialized)
            {
                _netCtl?.Stop();
                // 所有オブジェクトの明示破棄
                if (_netCtl is System.IDisposable d)
                {
                    d.Dispose();
                }
                _netCtl = null;
                _networkInitialized = false;
            }

            // リソース破棄
            _gameCts?.Dispose();
            _gameCts = null;

            // 状態リセット
            _dealer = null;
            _fieldSetupManager = null;
            _isInitialized = false;
            _isGameRunning = false;

            Debug.Log("GameManager: 停止とリセット完了");
        }

        /// <summary>
        /// StopAndResetの別名（後方互換性）
        /// </summary>
        [ContextMenu("Reset")]
        public void Reset() => StopAndReset();

        /// <summary>
        /// StopGameの実装をStopAndResetに統一
        /// </summary>
        public void StopGame() => StopAndReset();

        public void OnDestroy()
        {
            Debug.Log("GameManager: OnDestroy実行");
            StopAndReset();
        }

        private void OnDisable()
        {
            // ライフサイクル連携: 無効化時も確実に停止・解除
            StopAndReset();
        }

        #endregion

        #region ネットワーク初期化/受信ハンドラ
        private IdRegistry<CardId, Card> _cardRegistry;
        private IdRegistry<PileId, CardPile> _pileRegistry;
        private IdRegistry<PlayerId, Player> _playerRegistry;

        private void InitializeNetworking()
        {
            if (_networkInitialized) return;

            // レジストリの生成（登録はフィールド構築側で行う想定）
            _cardRegistry = new IdRegistry<CardId, Card>();
            _pileRegistry = new IdRegistry<PileId, CardPile>();
            _playerRegistry = new IdRegistry<PlayerId, Player>();

            _netCtl = new GameplayNetworkController();
            _netCtl.Initialize(
                PhotonNetwork.IsMasterClient,
                _pileRegistry,
                _cardRegistry,
                _playerRegistry,
                onActionRequestedHost: OnActionRequestedReceived,
                onGameStartedOptional: OnGameStartedReceived
            );
            _netCtl.Start();
            _networkInitialized = true;
        }

        private void OnGameStartedReceived(GameStartedEvent e) { Debug.Log($"GameManager: GameStarted {e.deckId}"); }
        private void OnTurnStartedReceived(TurnStartedEvent e) { Debug.Log($"TurnStarted seq={e.sequence} player={e.currentPlayerActorNumber}"); }
        private void OnActionRequestedReceived(ActionRequestedEvent e) { /* 旧ハンドラは廃止。Controller/Handlerに委譲 */ }
        #endregion
    }
}