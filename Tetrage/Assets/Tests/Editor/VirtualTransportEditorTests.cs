using NUnit.Framework;
using Tetrage.Network.Gameplay;

namespace Tetrage.Tests.Editor
{
    public class VirtualTransportEditorTests
    {
        [Test]
        public void SameFactory_Raise_ReachesRegisteredReceiver()
        {
            var serializer = new PhotonJsonSerializer();
            var factory = new VirtualNetworkAdapterFactory();
            var broadcaster = factory.CreateBroadcaster(serializer);
            var receiver = (VirtualReceiver)factory.CreateReceiver(serializer);
            var received = false;

            receiver.On<TurnStartedEvent>(EventCode.TurnStarted, _ => received = true);
            receiver.Start();

            broadcaster.Raise(EventCode.TurnStarted, new TurnStartedEvent
            {
                sequence = 1,
                stateVersion = 1,
                currentPlayerActorNumber = 1
            });

            Assert.IsTrue(received);
            receiver.Dispose();
        }

        [Test]
        public void DifferentFactoryInstances_AreIsolated()
        {
            var serializer = new PhotonJsonSerializer();
            var factoryA = new VirtualNetworkAdapterFactory();
            var receiverA = (VirtualReceiver)factoryA.CreateReceiver(serializer);
            var reachedA = false;
            receiverA.On<TurnStartedEvent>(EventCode.TurnStarted, _ => reachedA = true);
            receiverA.Start();

            var factoryB = new VirtualNetworkAdapterFactory();
            var broadcasterB = factoryB.CreateBroadcaster(serializer);
            broadcasterB.Raise(EventCode.TurnStarted, new TurnStartedEvent
            {
                sequence = 1,
                stateVersion = 1,
                currentPlayerActorNumber = 1
            });

            Assert.IsFalse(reachedA, "異なるFactory間でHub状態が共有されています");
            receiverA.Dispose();
        }
    }
}
