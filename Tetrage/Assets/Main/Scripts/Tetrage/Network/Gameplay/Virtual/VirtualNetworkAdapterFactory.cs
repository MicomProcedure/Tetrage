namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// 仮想輸送ネットワークアダプタを生成するファクトリ実装。
    /// 同一プロセス内でネットワーク通信を模倣するBroadcaster/Receiverを生成する。
    /// </summary>
    public sealed class VirtualNetworkAdapterFactory : INetworkAdapterFactory
    {
        #region Fields

        private readonly VirtualTransportHub _hub;
        private readonly bool _useMppmSharedTransport;
        private readonly MppmSharedVirtualTransportStore _mppmStore;
        private readonly int _mppmActorNumber;

        #endregion

        #region Constructor

        /// <summary>
        /// 実行環境に応じて通常VirtualTransportまたはMPPM共有VirtualTransportを生成する。
        /// </summary>
        public VirtualNetworkAdapterFactory()
            : this(MppmSharedVirtualTransportStore.IsMppmProcess())
        {
        }

        /// <summary>
        /// テスト用にMPPM共有VirtualTransportの使用有無を指定して生成する。
        /// </summary>
        public VirtualNetworkAdapterFactory(bool useMppmSharedTransport)
            : this(new VirtualTransportHub(), useMppmSharedTransport)
        {
        }

        /// <summary>
        /// 指定Hubを使って通常VirtualTransportを生成する。
        /// </summary>
        public VirtualNetworkAdapterFactory(VirtualTransportHub hub)
            : this(hub, false)
        {
        }

        private VirtualNetworkAdapterFactory(VirtualTransportHub hub, bool useMppmSharedTransport)
        {
            _hub = hub;
            _useMppmSharedTransport = useMppmSharedTransport;

            if (!_useMppmSharedTransport)
            {
                return;
            }

            if (!MppmSharedVirtualTransportStore.TryResolveMppmActorNumber(out _mppmActorNumber))
            {
                _mppmActorNumber = 1;
            }

            _mppmStore = new MppmSharedVirtualTransportStore(MppmSharedVirtualTransportStore.CreateDefaultEventsPath());
            if (MppmSharedVirtualTransportStore.IsMppmHostProcess())
            {
                _mppmStore.ClearOnceForHost();
            }
        }

        #endregion

        #region INetworkAdapterFactory

        /// <summary>
        /// VirtualBroadcasterを生成する
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>VirtualBroadcaster</returns>
        public INetworkBroadcaster CreateBroadcaster(ISerializer serializer)
        {
            if (_useMppmSharedTransport)
            {
                return new MppmSharedVirtualBroadcaster(serializer, _mppmStore, _mppmActorNumber);
            }

            return new VirtualBroadcaster(serializer, _hub);
        }

        /// <summary>
        /// VirtualReceiverを生成する
        /// </summary>
        /// <param name="serializer">シリアライザ</param>
        /// <returns>VirtualReceiver</returns>
        public INetworkReceiver CreateReceiver(ISerializer serializer)
        {
            if (_useMppmSharedTransport)
            {
                return new MppmSharedVirtualReceiver(serializer, _mppmStore, _mppmActorNumber);
            }

            return new VirtualReceiver(serializer, _hub);
        }

        #endregion
    }
}
