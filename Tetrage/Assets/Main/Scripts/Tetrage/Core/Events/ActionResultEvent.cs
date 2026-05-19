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
        /// <summary>Draw 等: CardId。TetrageMulti では未使用。</summary>
        public IReadOnlyList<CardId> TargetCardIds { get; }
        /// <summary>TetrageMulti ResponseRequested: 提出参加者 ActorNumber。</summary>
        public IReadOnlyList<int> ParticipantActorNumbers { get; }
        /// <summary>TetrageMulti 最終結果: 勝者 ActorNumber。</summary>
        public IReadOnlyList<int> WinnerActorNumbers { get; }
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
            IReadOnlyList<int> participantActorNumbers = null,
            IReadOnlyList<int> winnerActorNumbers = null,
            int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            ClientSequence          = clientSequence;
            ActorPlayerId           = actorPlayerId;
            ActionType              = actionType;
            Accepted                = accepted;
            Reason                  = reason;
            TargetCardIds           = targetCardIds;
            ActionStatusInt         = actionStatusInt;
            ParticipantActorNumbers = participantActorNumbers ?? new List<int>();
            WinnerActorNumbers      = winnerActorNumbers ?? new List<int>();
        }
    }
}
