using Tetrage.Core.DTO;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// DealerPlan を受け取り、個別のネットワークDTOに分解して発行する汎用エミッタ。
    /// </summary>
    public sealed class DealerPlanEmitter : IEventEmitter<DealerPlan>
    {
        private readonly INetworkBroadcaster _broadcaster;
        public INetworkBroadcaster Broadcaster => _broadcaster;
        private int _sequence;
        private int _stateVersion;

        public DealerPlanEmitter(INetworkBroadcaster broadcaster)
        {
            _broadcaster = broadcaster;
            _sequence = 0;
            _stateVersion = 0;
        }

        public void Emit(DealerPlan plan)
        {
            // まず決定論シャッフルのSeed配布を処理
            if (plan.ShuffleSeeds != null)
            {
                for (int i = 0; i < plan.ShuffleSeeds.Count; i++)
                {
                    var s = plan.ShuffleSeeds[i];
                    EmitPileShuffleSeed(s.PileId.Value, s.Seed);
                }
            }



            // 続いて Moves/Visibility を処理
            if (plan.Moves != null)
            {
                for (int i = 0; i < plan.Moves.Count; i++)
                {
                    var m = plan.Moves[i];
                    var dto = new CardMovedEvent
                    {
                        sequence = ++_sequence,
                        stateVersion = ++_stateVersion,
                        cardId = m.CardId.Value,
                        fromPileId = m.FromPileId.Value,
                        toPileId = m.ToPileId.Value,
                    };
                    _broadcaster.Raise(EventCode.CardMoved, dto);
                }
            }

            if (plan.Visibility != null)
            {
                for (int i = 0; i < plan.Visibility.Count; i++)
                {
                    var v = plan.Visibility[i];
                    var dto = new CardVisibilityChangedEvent
                    {
                        sequence = ++_sequence,
                        stateVersion = ++_stateVersion,
                        cardId = v.CardId.Value,
                        isVisible = v.IsVisible,
                    };
                    _broadcaster.Raise(EventCode.CardVisibilityChanged, dto);
                }
            }
        }

        public void EmitPileShuffleSeed(int pileId, int seed)
        {
            var dto = new PileShuffledWithSeedEvent
            {
                sequence = ++_sequence,
                stateVersion = ++_stateVersion,
                pileId = pileId,
                seed = seed,
            };
            _broadcaster.Raise(EventCode.PileShuffledWithSeed, dto);
        }
    }
}


