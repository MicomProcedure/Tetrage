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
using Tetrage.Core;
using Tetrage.Network.Contracts;

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
        [SerializeField] private InGameUIManager _inGameUIManager;

        private GameContext _gameContext;
        public GameContext GameContext => _gameContext;
		private List<PlayerInfo> _participantInfos;
        private INetworkContext _networkContext;
        private IPlayerIdMapper _playerIdMapper;

        #endregion

        #region レジストリ
        private IdRegistry<CardId, Card> _cardRegistry = new IdRegistry<CardId, Card>();
        private IdRegistry<PileId, CardPile> _pileRegistry = new IdRegistry<PileId, CardPile>();
        private IdRegistry<PlayerId, Player> _playerRegistry = new IdRegistry<PlayerId, Player>();

        #endregion

        #region 状態管理
        private bool _isInitialized = false;
        private bool _isGameRunning = false;
        private CancellationTokenSource _gameCts;
        [SerializeField] private bool _networkInitialized = false;
        private bool _remoteGameEnded = false;

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
        /// <param name="userPlayerInfo">ユーザープレイヤー情報</param>
        /// <param name="networkContext">ネットワーク状態の抽象化（ApplicationManagerから提供）</param>
        /// <param name="playerIdMapper">PlayerId/ActorNumberマッピング（ApplicationManagerから提供）</param>
        public void Initialize(List<PlayerInfo> participantInfoList, PlayerInfo userPlayerInfo, INetworkContext networkContext = null, IPlayerIdMapper playerIdMapper = null)
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
                ValidatePlayerInfos(participantInfoList);

				// 2. フィールドのセットアップ
				_fieldSetupManager.SetupField(participantInfoList);

                // 2.5 参加者情報の保持（UIへローカルで提供）
                _participantInfos = new List<PlayerInfo>(participantInfoList);

                // 3. ネットワーク受信・適用の初期化（ホスト/ゲスト共通）
                _netCtl = InitializeNetworking();
                
                // 3.5 NetworkContextの設定（ApplicationManagerから提供、未提供の場合はPhoton実装を生成）
                _networkContext = networkContext ?? new PhotonNetworkContext();
                
                // 3.6 PlayerIdMapperの設定（ApplicationManagerから提供）
                _playerIdMapper = playerIdMapper;

                // 3.5 ユーザープレイヤーの特定
                if (!TryGetPlayerById(userPlayerInfo.Id, out var userPlayer))
                {
                    Debug.LogWarning($"GameManager: ユーザープレイヤーが見つかりません。PlayerId={userPlayerInfo.Id}");
                    return;
                }

                // 4. GameContextの生成（PUNのLocalPlayerから IPlayer を解決）
                _gameContext = new Tetrage.Core.GameContext(
                    _fieldSetupManager.Stage,
                    _fieldSetupManager.Players,
                    userPlayer,
                    _netCtl.EventBus
                );
                Debug.Log($"GameManager: GameContext created, userPlayerId: {userPlayer.PlayerId}, PhotonActorId: {PhotonNetwork.LocalPlayer.ActorNumber}");
                _netCtl.AttachGameContext(_gameContext);

                // 5. Dealerの生成と初期化（ブロードキャスタを注入）
                _dealer = DealerFactory.CreateDealer(
                    _gameMode,
                    _gameContext,
                    _netCtl?.Broadcaster
                );

                // 5.5 ActionManager に NetworkActionContext を注入（ActionSystemはDealerのコンストラクタで初期化されている）
                var actionMgr = Tetrage.Core.Actions.ActionSystemInitializer.GetActionManager();
                if (actionMgr != null && _netCtl != null)
                {
                    var networkCtx = new Tetrage.Network.Gameplay.PhotonActionContext(_netCtl.Broadcaster, _netCtl.Sequence);
                    actionMgr.SetNetworkActionContext(networkCtx);
                }

                EventSubscribe();

                // 5.6 InGameUIManager 初期化（Bus購読開始）
                if (_inGameUIManager != null)
                {
                    _inGameUIManager.Initialize(_gameContext);
                }

                // 6. Dealerへ NetworkContext/Broadcaster/Sequence/TurnGate を提供（Hostのみ）
                _dealer.SetNetworkContext(_networkContext);
                if (_networkContext.IsHost)
                {
                    var bc = _netCtl?.Broadcaster;
                    if (bc == null) throw new System.InvalidOperationException("Broadcaster が見つかりません。");
                    var seq = _netCtl.Sequence;
                    _dealer.SetEmitter(new DealerPlanEmitter(bc, seq));
                    _dealer.SetTurnGate(_netCtl.TurnGate);
                    _dealer.SetLifecycleEmitter(new GameLifecycleEmitter(bc, seq));
                    // DealerにPlayerIdMapperを注入（PlayerId→ActorNumber変換用）
                    _dealer.SetPlayerIdMapper(_playerIdMapper);
                }

                _isInitialized = true;

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
            // Registry注入版の依存性を使用
            var dependencies = _fieldSetupComponent.CreateFieldSetupDependencies(_pileRegistry, _cardRegistry, _playerRegistry);

            return new FieldSetupManager(settings, dependencies);
        }

        /// <summary>
        /// PlayerInfo の Id が PUN の ActorNumber と対応しているか検証する。
        /// オフライン/未接続時は検証せず入力をそのまま返す。
        /// </summary>
        private void ValidatePlayerInfos(List<PlayerInfo> input)
        {

            var actors = PhotonNetwork.PlayerList;

            // ActorNumber の集合を構築
            var actorNumbers = new HashSet<int>();
            for (int i = 0; i < actors.Length; i++)
            {
                actorNumbers.Add(actors[i].ActorNumber);
            }

            // 入力の重複と存在を検証
            var seen = new HashSet<int>();
            for (int i = 0; i < input.Count; i++)
            {
                var idValue = input[i].Id.Value;
                if (!actorNumbers.Contains(idValue))
                {
                    throw new System.InvalidOperationException($"PlayerInfo.Id={idValue} が現在の ActorNumber 一覧に存在しません。");
                }
                if (!seen.Add(idValue))
                {
                    throw new System.InvalidOperationException($"PlayerInfo.Id={idValue} が重複しています。");
                }
            }

            // 参考: 数が合わない場合は警告（観戦や未参加者の可能性）。
            if (input.Count != actors.Length)
            {
                UnityEngine.Debug.LogWarning($"GameManager: 参加者数({input.Count})と PUN 参加者数({actors.Length}) に差異があります。");
            }

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

                // ホスト: 初期宣言を送信してDealerを実行／ゲスト: 終了まで待機
                if (PhotonNetwork.IsMasterClient && _networkInitialized)
                {
                    PublishGameStarted();  // ゲーム開始イベントを送信

                    await _dealer.StartGameAsync(0f, _gameCts.Token);
                }
                else
                {
                    await WaitForGameEndAsync();
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

        private async UniTask WaitForGameEndAsync()
        {
            if (PhotonNetwork.IsMasterClient) return;
            _remoteGameEnded = false;
            await UniTask.WaitUntil(() => _remoteGameEnded || !PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom);
        }

        private void PublishGameStarted(){
                    var started = new GameStartedEvent
                    {
                        // 一旦FieldSetupComponentの設定を使用するため実質使わない
                        // TODO: 将来的には設定されたルールに応じて適切な値を設定する
                        deckId = InGameConsts.DEFAULT_DECK_ID,
                        suitOrder = new byte[] { 0, 1, 2, 3 },
                        minNumber = 1,
                        maxNumber = 13,
                        playerActorNumbers = BuildInitialPlayerOrder(),
                    };
                    _netCtl.Broadcaster.Raise(EventCode.GameStarted, started);
        }
        #endregion

        #region イベントハンドラ

        private void OnGameEnd()
        {
            Debug.Log("GameManager: ゲーム終了イベントを受信");
            _isGameRunning = false;
            _remoteGameEnded = true;
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

            // UI購読解除
            if (_inGameUIManager != null)
            {
                _inGameUIManager.Teardown();
            }

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


        private IGameplayNetworkController InitializeNetworking()
        {
            if (_networkInitialized)
            {
                return _netCtl;
            }

            var netCtl = new GameplayNetworkController(
                _networkContext?.IsHost ?? PhotonNetwork.IsMasterClient,
                _pileRegistry,
                _cardRegistry,
                _playerRegistry,
                _playerIdMapper
            );
            netCtl.Start();
            _networkInitialized = true;
            return netCtl;
        }

        private int[] BuildInitialPlayerOrder()
        {
            if (_playerRegistry == null) return null;
            var list = new List<int>();
            // 現状は登録順序を採用。必要なら座席順や任意の順序に変更可。
            foreach (var kv in _playerRegistry.Entries)
            {
                list.Add(kv.Key.Value);
            }
            return list.ToArray();
        }

        // NetPlayerInfo の送受信は撤廃

        private void OnActionRequestedReceived(ActionRequestedEvent e) { /* 旧ハンドラは廃止。Controller/Handlerに委譲 */ }
        #endregion

        #region プレイヤーヘルパーメソッド

        public bool TryGetPlayerById(PlayerId id, out Player player)
        {
            return _playerRegistry.TryGet(id, out player);
        }

        #endregion
    }
}