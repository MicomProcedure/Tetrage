using System;
using Tetrage.Core.Ids;
using Tetrage.Models;
using Tetrage.Core;
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
        private bool _isHost;
        private IHostActionProcessor _hostActionProcessor;
        private NetworkEventApplier _applier;
        private bool _started;
        private bool _disposed;

        public GameplayNetworkController()
        {
            _serializer = new PhotonJsonSerializer();
        }

        public void Initialize(
            bool isHost,
            IdRegistry<PileId, CardPile> pileRegistry,
            IdRegistry<CardId, Card> cardRegistry,
            IdRegistry<PlayerId, Player> playerRegistry,
            Action<ActionRequestedEvent> onActionRequestedHost,
            Action<GameStartedEvent> onGameStartedOptional = null)
        {
            _isHost = isHost;
            _bus = new SimpleGameplayEventBus();
            _turnGate = new TurnGate();
            _sequence = new SequenceService();
            _applier = new NetworkEventApplier(pileRegistry, cardRegistry, playerRegistry, _bus, _turnGate, _gameContext);
            _hostActionProcessor = new DefaultHostActionProcessor(this);
            _broadcaster = new PhotonBroadcaster(_serializer);
            _receiver = new PhotonReceiver(_serializer);

            _receiver.On<GameStartedEvent>(EventCode.GameStarted, e =>
            {
                onGameStartedOptional?.Invoke(e);
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
                    onActionRequestedHost?.Invoke(e);
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
            _applier?.AttachContext(ctx);
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
            }
            finally
            {
                if (_receiver is IDisposable d) d.Dispose();
                _receiver = null;
                _broadcaster = null;
                _hostActionProcessor = null;
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
        #endregion
    }


}


