namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ネットワークアダプタを使用したアクションコンテキスト実装。Actionがこのクラスを介してネットワークを通じてHostにアクション確定リクエストを送る。
    /// </summary>
    public sealed class GeneralNetworkActionContext : INetworkActionContext
    {
        private readonly INetworkBroadcaster _broadcaster;
        private readonly SequenceService _seq;
        private readonly IPlayerIdMapper _playerIdMapper;

        public GeneralNetworkActionContext(INetworkBroadcaster broadcaster, SequenceService seq, IPlayerIdMapper playerIdMapper)
        {
            _broadcaster = broadcaster;
            _seq = seq;
            _playerIdMapper = playerIdMapper;
        }

        public void Request(ActionRequestedEvent request)
        {
            // DescriptorのactorPlayerIdはPlayerId.Value前提。送信DTO境界ではActorNumberに変換する。
            if (!_playerIdMapper.TryGetActorNumber(new Core.Ids.PlayerId(request.actorPlayerId), out var actorNumber))
            {
                UnityEngine.Debug.LogError($"GeneralNetworkActionContext: PlayerId {request.actorPlayerId} のActorNumber変換に失敗しました");
                return;
            }

            request.actorPlayerId = actorNumber;
            _broadcaster.Raise(EventCode.ActionRequested, request);
        }

        public int NextClientSequence()
        {
            return _seq.NextSequence();
        }
    }
}

