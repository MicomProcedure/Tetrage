namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// Photonネットワークを使用したアクションコンテキスト実装
    /// </summary>
    public sealed class PhotonActionContext : INetworkActionContext
    {
        private readonly INetworkBroadcaster _broadcaster;
        private readonly SequenceService _seq;

        public PhotonActionContext(INetworkBroadcaster broadcaster, SequenceService seq)
        {
            _broadcaster = broadcaster;
            _seq = seq;
        }

        public void Request(ActionRequestedEvent request)
        {
            _broadcaster.Raise(EventCode.ActionRequested, request);
        }

        public int NextClientSequence()
        {
            return _seq.NextSequence();
        }
    }
}

