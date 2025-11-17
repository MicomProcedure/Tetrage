namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// INetworkBroadcasterとINetworkReceiverのペアを生成するファクトリインターフェース。
    /// NetworkModeに応じた実装（Photon/Virtual）を返す。
    /// </summary>
    public interface INetworkAdapterFactory
    {
        /// <summary>
        /// ネットワークブロードキャスタを生成する
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>INetworkBroadcaster実装</returns>
        INetworkBroadcaster CreateBroadcaster(ISerializer serializer);

        /// <summary>
        /// ネットワークレシーバを生成する
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>INetworkReceiver実装</returns>
        INetworkReceiver CreateReceiver(ISerializer serializer);
    }
}

