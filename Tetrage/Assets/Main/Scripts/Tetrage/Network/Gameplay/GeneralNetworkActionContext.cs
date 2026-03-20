namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ネットワークアダプタを使用したアクションコンテキスト実装。Actionがこのクラスを介してネットワークを通じてHostにアクション確定リクエストを送る。
    /// </summary>
    public sealed class GeneralNetworkActionContext : INetworkActionContext
    {
        private readonly INetworkBroadcaster _broadcaster;
        private readonly SequenceService _seq;

        public GeneralNetworkActionContext(INetworkBroadcaster broadcaster, SequenceService seq)
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

