using System.Collections.Generic;
using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// ゲーム開始イベント
    /// </summary>
    public sealed class GameStartedEvent : DomainEventBase
    {
        public DeckId DeckId { get; }
        public byte[] SuitOrder { get; }
        public int MinNumber { get; }
        public int MaxNumber { get; }
        public IReadOnlyList<PlayerId> PlayerIds { get; } // ターン順/座席順

        public GameStartedEvent(
            int sequence,
            DeckId deckId,
            byte[] suitOrder,
            int minNumber,
            int maxNumber,
            IReadOnlyList<PlayerId> playerIds,
            int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            DeckId = deckId;
            SuitOrder = suitOrder;
            MinNumber = minNumber;
            MaxNumber = maxNumber;
            PlayerIds = playerIds;
        }
    }
}

