using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// ScanPhase でゲストが選択した偵察対象をホストへ通知するイベント。
    /// </summary>
    public sealed class ScanTargetSelectedEvent : DomainEventBase
    {
        public PlayerId ActorPlayerId { get; }
        public PlayerId SelectedTargetPlayerId { get; }

        public ScanTargetSelectedEvent(int sequence, PlayerId actorPlayerId, PlayerId selectedTargetPlayerId, int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            ActorPlayerId = actorPlayerId;
            SelectedTargetPlayerId = selectedTargetPlayerId;
        }
    }
}
