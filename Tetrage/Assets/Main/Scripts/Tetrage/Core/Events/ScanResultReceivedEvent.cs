using Tetrage.Core.Enums;
using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// ScanPhase の偵察結果を受信したことを表すイベント。
    /// </summary>
    public sealed class ScanResultReceivedEvent : DomainEventBase
    {
        public PlayerId TargetPlayerId { get; }
        public Suit TargetSuit { get; }

        public ScanResultReceivedEvent(int sequence, PlayerId targetPlayerId, Suit targetSuit, int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            TargetPlayerId = targetPlayerId;
            TargetSuit = targetSuit;
        }
    }
}
