namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ターン開始/終了などゲーム進行イベントの送信を担当。
    /// </summary>
    public sealed class GameLifecycleEmitter : IEventEmitter<TurnStartedEvent>
    {
        private readonly INetworkBroadcaster _broadcaster;
        private readonly SequenceService _seq;

        public GameLifecycleEmitter(INetworkBroadcaster broadcaster)
            : this(broadcaster, new SequenceService())
        {
        }

        public GameLifecycleEmitter(INetworkBroadcaster broadcaster, SequenceService sequenceService)
        {
            _broadcaster = broadcaster;
            _seq = sequenceService ?? new SequenceService();
        }

        public INetworkBroadcaster Broadcaster => _broadcaster;

        public void Emit(TurnStartedEvent e)
        {
            e.sequence = _seq.NextSequence();
            e.stateVersion = _seq.NextStateVersion();
            _broadcaster.Raise(EventCode.TurnStarted, e);
        }

        public void EmitEnded(int previousPlayerActorNumber)
        {
            var e = new TurnEndedEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
                previousPlayerActorNumber = previousPlayerActorNumber,
            };
            _broadcaster.Raise(EventCode.TurnEnded, e);
        }
    }
}


