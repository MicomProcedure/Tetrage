using System.Collections.Generic;
using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// スキャンフェーズ開始イベント
    /// </summary>
    public sealed class ScanPhaseStartedEvent : DomainEventBase
    {
        public PlayerId UserPlayerId { get; }
        public IReadOnlyList<PlayerId> PlayerIds { get; }

        public ScanPhaseStartedEvent(
            int sequence,
            PlayerId userPlayerId,
            IReadOnlyList<PlayerId> playerIds,
            int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            UserPlayerId = userPlayerId;
            PlayerIds = playerIds;
        }
    }
}

