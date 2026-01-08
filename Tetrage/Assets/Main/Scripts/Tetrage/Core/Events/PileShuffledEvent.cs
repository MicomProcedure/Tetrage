using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// パイルシャッフルイベント
    /// </summary>
    public sealed class PileShuffledEvent : DomainEventBase
    {
        public PileId PileId { get; }
        public int Seed { get; }

        public PileShuffledEvent(int sequence, PileId pileId, int seed, int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            PileId = pileId;
            Seed = seed;
        }
    }
}

