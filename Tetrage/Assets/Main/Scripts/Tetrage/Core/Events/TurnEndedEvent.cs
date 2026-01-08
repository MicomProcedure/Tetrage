using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// ターン終了イベント
    /// </summary>
    public sealed class TurnEndedEvent : DomainEventBase
    {
        public PlayerId PreviousPlayerId { get; }

        public TurnEndedEvent(int sequence, PlayerId previousPlayerId, int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            PreviousPlayerId = previousPlayerId;
        }
    }
}

