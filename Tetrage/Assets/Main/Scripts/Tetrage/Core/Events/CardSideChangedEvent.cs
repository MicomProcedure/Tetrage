using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// カードの表裏変更イベント
    /// </summary>
    public sealed class CardSideChangedEvent : DomainEventBase
    {
        public CardId CardId { get; }
        public bool IsFaceUp { get; }

        public CardSideChangedEvent(int sequence, CardId cardId, bool isFaceUp, int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            CardId = cardId;
            IsFaceUp = isFaceUp;
        }
    }
}

