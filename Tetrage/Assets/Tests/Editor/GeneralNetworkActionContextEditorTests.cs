using NUnit.Framework;
using Tetrage.Network.Gameplay;

namespace Tetrage.Tests.Editor
{
    public class GeneralNetworkActionContextEditorTests
    {
        private sealed class BroadcasterSpy : INetworkBroadcaster
        {
            public EventCode? LastCode { get; private set; }
            public object LastPayload { get; private set; }

            public void Raise<T>(EventCode code, T payload)
            {
                LastCode = code;
                LastPayload = payload;
            }

            public void RaiseToActors<T>(EventCode code, T payload, int[] targetActorNumbers)
            {
            }

            public void RaiseToActor<T>(EventCode code, T payload, int targetActorNumber)
            {
            }
        }

        [Test]
        public void Request_RaisesActionRequestedEvent()
        {
            var spy = new BroadcasterSpy();
            var ctx = new GeneralNetworkActionContext(spy, new SequenceService());
            var request = new ActionRequestedEvent
            {
                sequence = 1,
                clientSequence = 2,
                actorPlayerId = 1
            };

            ctx.Request(request);

            Assert.AreEqual(EventCode.ActionRequested, spy.LastCode);
            Assert.IsInstanceOf<ActionRequestedEvent>(spy.LastPayload);
        }

        [Test]
        public void NextClientSequence_UsesSequenceService()
        {
            var ctx = new GeneralNetworkActionContext(new BroadcasterSpy(), new SequenceService());

            Assert.AreEqual(1, ctx.NextClientSequence());
            Assert.AreEqual(2, ctx.NextClientSequence());
        }
    }
}
