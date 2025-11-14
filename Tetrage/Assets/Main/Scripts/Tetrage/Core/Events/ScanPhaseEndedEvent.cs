namespace Tetrage.Core.Events
{
    /// <summary>
    /// スキャンフェーズ終了イベント
    /// </summary>
    public sealed class ScanPhaseEndedEvent : DomainEventBase
    {
        public ScanPhaseEndedEvent(int sequence, int stateVersion = 0)
            : base(sequence, stateVersion)
        {
        }
    }
}

