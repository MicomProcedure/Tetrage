using System.Collections.Generic;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// アクション要求イベント
    /// </summary>
    public sealed class ActionRequestedEvent : DomainEventBase
    {
        public int ClientSequence { get; }
        public PlayerId ActorPlayerId { get; }
        public ActionType ActionType { get; }
        public IReadOnlyList<CardId> TargetCardIds { get; }
        public int ActionStatusInt { get; }

        public ActionRequestedEvent(
            int sequence,
            int clientSequence,
            PlayerId actorPlayerId,
            ActionType actionType,
            IReadOnlyList<CardId> targetCardIds,
            int actionStatusInt,
            int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            ClientSequence = clientSequence;
            ActorPlayerId = actorPlayerId;
            ActionType = actionType;
            TargetCardIds = targetCardIds;
            ActionStatusInt = actionStatusInt;
        }
    }
}

