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

        public void EmitScanStart(int userPlayerActorNumber, int[] playerActorNumbers)
        {
            var e = new StartScanPhaseEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
                userPlayerActorNumber = userPlayerActorNumber,
                playerActorNumbers = playerActorNumbers,
            };
            _broadcaster.Raise(EventCode.StartScanPhase, e);
        }

        public void EmitScanEnd()
        {
            var e = new EndScanPhaseEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
            };
            _broadcaster.Raise(EventCode.EndScanPhase, e);
        }

        public void EmitFinishingGame(int[] winnerActorNumbers)
        {
            var e = new FinishingGameEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
                winnerActorNumbers = winnerActorNumbers,
            };
            _broadcaster.Raise(EventCode.FinishingGame, e);
        }

        public void EmitGameEnded(int[] winnerActorNumbers)
        {
            var e = new GameEndedEvent
            {
                sequence = _seq.NextSequence(),
                stateVersion = _seq.NextStateVersion(),
                winnerActorNumbers = winnerActorNumbers,
            };
            _broadcaster.Raise(EventCode.GameEnded, e);
        }
    }
}


