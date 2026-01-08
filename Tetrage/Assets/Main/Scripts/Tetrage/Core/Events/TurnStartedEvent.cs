using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// ターン開始イベント
    /// </summary>
    public sealed class TurnStartedEvent : DomainEventBase
    {
        public PlayerId CurrentPlayerId { get; }

        public TurnStartedEvent(int sequence, PlayerId currentPlayerId, int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            CurrentPlayerId = currentPlayerId;
        }
    }
}

