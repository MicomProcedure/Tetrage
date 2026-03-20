namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// 仮想輸送ネットワークアダプタを生成するファクトリ実装。
    /// 同一プロセス内でネットワーク通信を模倣するBroadcaster/Receiverを生成する。
    /// </summary>
    public sealed class VirtualNetworkAdapterFactory : INetworkAdapterFactory
    {
        private readonly VirtualTransportHub _hub;

        public VirtualNetworkAdapterFactory()
            : this(new VirtualTransportHub())
        {
        }

        public VirtualNetworkAdapterFactory(VirtualTransportHub hub)
        {
            _hub = hub;
        }

        /// <summary>
        /// VirtualBroadcasterを生成する
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>VirtualBroadcaster</returns>
        public INetworkBroadcaster CreateBroadcaster(ISerializer serializer)
        {
            return new VirtualBroadcaster(serializer, _hub);
        }

        /// <summary>
        /// VirtualReceiverを生成する
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>VirtualReceiver</returns>
        public INetworkReceiver CreateReceiver(ISerializer serializer)
        {
            return new VirtualReceiver(serializer, _hub);
        }
    }
}
