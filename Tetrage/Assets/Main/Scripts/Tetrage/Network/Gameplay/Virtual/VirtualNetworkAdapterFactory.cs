namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// 仮想輸送ネットワークアダプタを生成するファクトリ実装。
    /// 同一プロセス内でネットワーク通信を模倣するBroadcaster/Receiverを生成する。
    /// </summary>
    public sealed class VirtualNetworkAdapterFactory : INetworkAdapterFactory
    {
        /// <summary>
        /// VirtualBroadcasterを生成する
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>VirtualBroadcaster</returns>
        public INetworkBroadcaster CreateBroadcaster(ISerializer serializer)
        {
            return new VirtualBroadcaster(serializer);
        }

        /// <summary>
        /// VirtualReceiverを生成する
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>VirtualReceiver</returns>
        public INetworkReceiver CreateReceiver(ISerializer serializer)
        {
            return new VirtualReceiver(serializer);
        }
    }
}
