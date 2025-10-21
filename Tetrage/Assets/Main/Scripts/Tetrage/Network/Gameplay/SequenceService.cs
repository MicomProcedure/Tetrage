namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// sequence/stateVersion を一元管理する単純なサービス。
    /// </summary>
    public sealed class SequenceService
    {
        private int _sequence;
        private int _stateVersion;

        public SequenceService()
        {
            _sequence = 0;
            _stateVersion = 0;
        }

        public int NextSequence() => ++_sequence;
        public int NextStateVersion() => ++_stateVersion;

        public void Reset()
        {
            _sequence = 0;
            _stateVersion = 0;
        }
    }
}


