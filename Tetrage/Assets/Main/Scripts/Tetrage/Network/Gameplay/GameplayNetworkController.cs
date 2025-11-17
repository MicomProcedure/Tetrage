using System;
using Tetrage.Core.Ids;
using Tetrage.Models;
using Tetrage.Core;
using Tetrage.Core.Events;
using Tetrage.Network.Contracts;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ネットワーク初期化・受信・送信の集約。GameManager から分離。
    /// </summary>
    public sealed class GameplayNetworkController : IGameplayNetworkController, IDisposable
    {
        private readonly ISerializer _serializer;
        private INetworkBroadcaster _broadcaster;
        public INetworkBroadcaster Broadcaster => _broadcaster;
        private INetworkReceiver _receiver;
        private IGameplayEventBus _bus;
        private TurnGate _turnGate;
        private GameContext _gameContext;
        private SequenceService _sequence;
        private readonly bool _isHost;
        private IHostActionProcessor _hostActionProcessor;
        private NetworkEventApplier _applier;
        private GameplayDomainEventHandler _domainEventHandler;
        private readonly IPlayerIdMapper _playerIdMapper;
        private bool _started;
        private bool _disposed;

        /// <summary>
        /// GameplayNetworkControllerのコンストラクタ
        /// </summary>
        /// <param name="isHost">ホストかどうか</param>
        /// <param name="pileRegistry">カードパイルレジストリ</param>
        /// <param name="cardRegistry">カードレジストリ</param>
        /// <param name="playerRegistry">プレイヤーレジストリ</param>
        /// <param name="onActionRequestedHost">ホスト側のアクション要求ハンドラ</param>
        /// <param name="onGameStartedOptional">ゲーム開始時のオプショナルハンドラ</param>
        /// <param name="playerIdMapper">PlayerId/ActorNumberマッピング</param>
        public GameplayNetworkController(
            bool isHost,
            IdRegistry<PileId, CardPile> pileRegistry,
            IdRegistry<CardId, Card> cardRegistry,
            IdRegistry<PlayerId, Player> playerRegistry,
            IPlayerIdMapper playerIdMapper = null)
        {
            _serializer = new PhotonJsonSerializer();
            _isHost = isHost;
            _playerIdMapper = playerIdMapper;

            // R3EventBusを使用
            _bus = new R3EventBus();
            _turnGate = new TurnGate();
            _sequence = new SequenceService();

            // DomainEventConverter作成
            var converter = new DomainEventConverter(_playerIdMapper);

            // NetworkEventApplier作成（変換専用）
            _applier = new NetworkEventApplier(_bus, converter);

            // GameplayDomainEventHandler作成（ドメインロジック実行）
            _domainEventHandler = new GameplayDomainEventHandler(
                cardRegistry,
                pileRegistry,
                playerRegistry,
                _turnGate,
                _gameContext,
                _bus);

            _hostActionProcessor = new DefaultHostActionProcessor(this);
            _broadcaster = new PhotonBroadcaster(_serializer);
            _receiver = new PhotonReceiver(_serializer);

            _receiver.On<GameStartedEvent>(EventCode.GameStarted, e =>
            {
                _applier.Apply(e);
            });
            _receiver.On<TurnStartedEvent>(EventCode.TurnStarted, e =>
            {
                _applier.Apply(e);
            });
            _receiver.On<TurnEndedEvent>(EventCode.TurnEnded, e =>
            {
                _applier.Apply(e);
            });
            _receiver.On<ListOrderDeclaredEvent>(EventCode.ListOrderDeclared, e => _applier.Apply(e));
            _receiver.On<CardMovedEvent>(EventCode.CardMoved, e => _applier.Apply(e));
            _receiver.On<CardVisibilityChangedEvent>(EventCode.CardVisibilityChanged, e => _applier.Apply(e));
            _receiver.On<StartScanPhaseEvent>(EventCode.StartScanPhase, e => _applier.Apply(e));
            _receiver.On<EndScanPhaseEvent>(EventCode.EndScanPhase, e => _applier.Apply(e));
            _receiver.On<FinishingGameEvent>(EventCode.FinishingGame, e => _applier.Apply(e));
            _receiver.On<GameEndedEvent>(EventCode.GameEnded, e => _applier.Apply(e));
            _receiver.On<PileShuffledWithSeedEvent>(EventCode.PileShuffledWithSeed, e => _applier.Apply(e));
            _receiver.On<ActionResultEvent>(EventCode.ActionResult, e => _applier.Apply(e));
            _receiver.On<ActionRequestedEvent>(EventCode.ActionRequested, e =>
            {
                if (_isHost)
                {
                    // まずは既定プロセッサで即時処理（後でDealer検証に差し替え可）
                    _hostActionProcessor.Process(e);
                }
                else
                {
                    // ゲスト側は現状通知不要。必要になればUI通知デリゲートを追加する。
                }
            });
        }

        public TurnGate TurnGate => _turnGate;
        public IGameplayEventBus EventBus => _bus;
        public SequenceService Sequence => _sequence;

        public void AttachGameContext(Tetrage.Core.GameContext ctx)
        {
            _gameContext = ctx;
            // GameContextはコンストラクタで既にGameplayDomainEventHandlerに渡されている
        }

        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GameplayNetworkController));
            if (_started) return;
            _receiver.Start();
            _started = true;
        }

        public void Stop()
        {
            if (!_started) return;
            _receiver.Stop();
            _started = false;
        }

        #region IDisposable
        /// <summary>
        /// 受信停止を保証して破棄します。
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                Stop();
                _domainEventHandler?.Dispose();
            }
            finally
            {
                if (_receiver is IDisposable d) d.Dispose();
                if (_bus is R3EventBus r3Bus) r3Bus.Dispose();
                _receiver = null;
                _broadcaster = null;
                _hostActionProcessor = null;
                _domainEventHandler = null;
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
        #endregion
    }


}


