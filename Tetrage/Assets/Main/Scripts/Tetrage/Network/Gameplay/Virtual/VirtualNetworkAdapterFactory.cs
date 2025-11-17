using System;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// 仮想輸送ネットワークアダプタを生成するファクトリ実装。
    /// 同一プロセス内でネットワーク通信を模倣するBroadcaster/Receiverを生成する。
    /// 
    /// 【注意】Phase 5で完全実装予定。現在はスタブ実装。
    /// </summary>
    public sealed class VirtualNetworkAdapterFactory : INetworkAdapterFactory
    {
        /// <summary>
        /// VirtualBroadcasterを生成する（Phase 5で実装予定）
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>VirtualBroadcaster</returns>
        /// <exception cref="NotImplementedException">Phase 5で実装予定</exception>
        public INetworkBroadcaster CreateBroadcaster(ISerializer serializer)
        {
            throw new NotImplementedException("VirtualBroadcaster is not implemented yet. This will be completed in Phase 5.");
        }

        /// <summary>
        /// VirtualReceiverを生成する（Phase 5で実装予定）
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>VirtualReceiver</returns>
        /// <exception cref="NotImplementedException">Phase 5で実装予定</exception>
        public INetworkReceiver CreateReceiver(ISerializer serializer)
        {
            throw new NotImplementedException("VirtualReceiver is not implemented yet. This will be completed in Phase 5.");
        }
    }
}

