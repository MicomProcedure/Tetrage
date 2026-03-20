using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// カードの可視性変更イベント
    /// </summary>
    public sealed class CardVisibilityChangedEvent : DomainEventBase
    {
        public CardId CardId { get; }
        public bool IsVisible { get; }

        public CardVisibilityChangedEvent(int sequence, CardId cardId, bool isVisible, int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            CardId = cardId;
            IsVisible = isVisible;
        }
    }
}

