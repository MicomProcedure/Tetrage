using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// カード移動イベント
    /// </summary>
    public sealed class CardMovedEvent : DomainEventBase
    {
        public CardId CardId { get; }
        public PileId FromPileId { get; }
        public PileId ToPileId { get; }

        public CardMovedEvent(int sequence, CardId cardId, PileId fromPileId, PileId toPileId, int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            CardId = cardId;
            FromPileId = fromPileId;
            ToPileId = toPileId;
        }
    }
}

