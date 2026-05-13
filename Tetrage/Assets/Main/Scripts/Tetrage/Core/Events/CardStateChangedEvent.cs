using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    public enum CardStateType : byte
    {
        Unknown = 0,
        FaceUp = 1,
        IsSuitVisible = 2,
        IsHighlighted = 3,
    }

    /// <summary>
    /// カード状態変更イベント
    /// </summary>
    public sealed class CardStateChangedEvent : DomainEventBase
    {
        public CardId CardId { get; }
        public bool IsFaceUp { get; }
        public CardStateType StateType { get; }
        public bool StateValue { get; }

        public CardStateChangedEvent(
            int sequence,
            CardId cardId,
            bool isFaceUp,
            CardStateType stateType = CardStateType.FaceUp,
            bool stateValue = false,
            int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            CardId = cardId;
            IsFaceUp = isFaceUp;
            StateType = stateType;
            StateValue = stateValue;
        }
    }
}

