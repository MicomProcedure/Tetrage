using System;
using Tetrage.Core.Ids;
using Tetrage.Models;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ネットワーク初期化・受信・送信の集約。GameManager から分離。
    /// </summary>
    public sealed class GameplayNetworkController : IGameplayNetworkController, IDisposable
    {
        private readonly ISerializer _serializer;
        private INetworkBroadcaster _broadcaster;
        private INetworkReceiver _receiver;
        private IGameplayEventHandler _handler;
        private bool _isHost;
        private IHostActionProcessor _hostActionProcessor;
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
            var applier = new NetworkEventApplier(pileRegistry, cardRegistry, playerRegistry);
            _handler = new DefaultGameplayEventHandler(
                applier,
                isHost,
                onActionRequestedHost,
                onGameStartedOptional,
                e => UnityEngine.Debug.Log($"TurnStarted seq={e.sequence} player={e.currentPlayerActorNumber}")
            );
            _hostActionProcessor = new DefaultHostActionProcessor(this);
            _broadcaster = new PhotonBroadcaster(_serializer);
            _receiver = new PhotonReceiver(_serializer);

            _receiver.On<GameStartedEvent>(EventCode.GameStarted, e => _handler.OnGameStarted(e));
            _receiver.On<TurnStartedEvent>(EventCode.TurnStarted, e => _handler.OnTurnStarted(e));
            _receiver.On<CardMovedEvent>(EventCode.CardMoved, e => _handler.OnCardMoved(e));
            _receiver.On<CardVisibilityChangedEvent>(EventCode.CardVisibilityChanged, e => _handler.OnCardVisibilityChanged(e));
            _receiver.On<ActionResultEvent>(EventCode.ActionResult, e => _handler.OnActionResult(e));
            _receiver.On<ActionRequestedEvent>(EventCode.ActionRequested, e =>
            {
                if (_isHost)
                {
                    // まずは既定プロセッサで即時処理（後でDealer検証に差し替え可）
                    _hostActionProcessor.Process(e);
                }
                else
                {
                    _handler.OnActionRequested(e);
                }
            });
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

        public void BroadcastGameStarted(GameStartedEvent e)
        {
            _broadcaster.Raise(EventCode.GameStarted, e);
        }

        public void BroadcastActionResult(ActionResultEvent e)
        {
            _broadcaster.Raise(EventCode.ActionResult, e);
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
                _handler = null;
                _hostActionProcessor = null;
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
        #endregion
    }

}


