using NUnit.Framework;
using Tetrage.Core.Ids;
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
            var mapper = new PlayerIdMapper();
            mapper.Register(new PlayerId(1), 10);
            var ctx = new GeneralNetworkActionContext(spy, new SequenceService(), mapper);
            var request = new ActionRequestedEvent
            {
                sequence = 1,
                clientSequence = 2,
                actorPlayerId = 1
            };

            ctx.Request(request);

            Assert.AreEqual(EventCode.ActionRequested, spy.LastCode);
            Assert.IsInstanceOf<ActionRequestedEvent>(spy.LastPayload);
            var dto = (ActionRequestedEvent)spy.LastPayload;
            Assert.AreEqual(10, dto.actorPlayerId);
        }

        [Test]
        public void NextClientSequence_UsesSequenceService()
        {
            var mapper = new PlayerIdMapper();
            mapper.Register(new PlayerId(1), 10);
            var ctx = new GeneralNetworkActionContext(new BroadcasterSpy(), new SequenceService(), mapper);

            Assert.AreEqual(1, ctx.NextClientSequence());
            Assert.AreEqual(2, ctx.NextClientSequence());
        }
    }
}
