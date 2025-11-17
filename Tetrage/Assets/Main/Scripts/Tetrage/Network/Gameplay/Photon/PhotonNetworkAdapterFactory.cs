namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// Photonネットワークアダプタを生成するファクトリ実装。
    /// 実際のPhoton通信を使用するBroadcaster/Receiverを生成する。
    /// </summary>
    public sealed class PhotonNetworkAdapterFactory : INetworkAdapterFactory
    {
        /// <summary>
        /// PhotonBroadcasterを生成する
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>PhotonBroadcaster</returns>
        public INetworkBroadcaster CreateBroadcaster(ISerializer serializer)
        {
            return new PhotonBroadcaster(serializer);
        }

        /// <summary>
        /// PhotonReceiverを生成する
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>PhotonReceiver</returns>
        public INetworkReceiver CreateReceiver(ISerializer serializer)
        {
            return new PhotonReceiver(serializer);
        }
    }
}

