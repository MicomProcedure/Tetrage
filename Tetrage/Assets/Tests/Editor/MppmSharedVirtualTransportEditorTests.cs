using NUnit.Framework;
using Tetrage.Network.Gameplay;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// VirtualTransportのMPPM対応ファクトリを検証する。
    /// </summary>
    public class MppmSharedVirtualTransportEditorTests
    {
        #region Tests

        [Test]
        public void VirtualNetworkAdapterFactory_UsesInMemoryTransportByDefault()
        {
            var factory = new VirtualNetworkAdapterFactory(useMppmSharedTransport: false);
            var serializer = new PhotonJsonSerializer();

            Assert.IsInstanceOf<VirtualBroadcaster>(factory.CreateBroadcaster(serializer));
            Assert.IsInstanceOf<VirtualReceiver>(factory.CreateReceiver(serializer));
        }

        [Test]
        public void VirtualNetworkAdapterFactory_UsesSharedTransportWhenRequested()
        {
            var factory = new VirtualNetworkAdapterFactory(useMppmSharedTransport: true);
            var serializer = new PhotonJsonSerializer();

            Assert.IsInstanceOf<MppmSharedVirtualBroadcaster>(factory.CreateBroadcaster(serializer));
            Assert.IsInstanceOf<MppmSharedVirtualReceiver>(factory.CreateReceiver(serializer));
        }

        #endregion
    }
}
