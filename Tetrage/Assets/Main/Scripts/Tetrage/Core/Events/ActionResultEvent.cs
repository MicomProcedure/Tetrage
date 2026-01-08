using System.Collections.Generic;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// アクション結果イベント
    /// </summary>
    public sealed class ActionResultEvent : DomainEventBase
    {
        public int ClientSequence { get; }
        public PlayerId ActorPlayerId { get; }
        public ActionType ActionType { get; }
        public bool Accepted { get; }
        public string Reason { get; }
        public IReadOnlyList<CardId> TargetCardIds { get; }
        public int ActionStatusInt { get; }

        public ActionResultEvent(
            int sequence,
            int clientSequence,
            PlayerId actorPlayerId,
            ActionType actionType,
            bool accepted,
            string reason,
            IReadOnlyList<CardId> targetCardIds,
            int actionStatusInt,
            int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            ClientSequence = clientSequence;
            ActorPlayerId = actorPlayerId;
            ActionType = actionType;
            Accepted = accepted;
            Reason = reason;
            TargetCardIds = targetCardIds;
            ActionStatusInt = actionStatusInt;
        }
    }
}

