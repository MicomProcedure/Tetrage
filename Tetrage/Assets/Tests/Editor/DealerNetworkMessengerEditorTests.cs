using System.Collections.Generic;
using NUnit.Framework;
using Tetrage.Core.DTO;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;

namespace Tetrage.Tests.Editor
{
    public class DealerNetworkMessengerEditorTests
    {
        private sealed class BroadcasterSpy : INetworkBroadcaster
        {
            public List<(EventCode code, object payload)> Calls { get; } = new();

            public void Raise<T>(EventCode code, T payload)
            {
                Calls.Add((code, payload));
            }

            public void RaiseToActors<T>(EventCode code, T payload, int[] targetActorNumbers)
            {
            }

            public void RaiseToActor<T>(EventCode code, T payload, int targetActorNumber)
            {
            }
        }

        [Test]
        public void PublishTurnStarted_ConvertsPlayerIdToActorNumber()
        {
            var spy = new BroadcasterSpy();
            var mapper = new PlayerIdMapper();
            mapper.Register(new PlayerId(1), 10);
            var messenger = new DealerNetworkMessenger(spy, new SequenceService(), mapper);

            messenger.PublishTurnStarted(new PlayerId(1));

            Assert.AreEqual(1, spy.Calls.Count);
            Assert.AreEqual(EventCode.TurnStarted, spy.Calls[0].code);
            var dto = (TurnStartedEvent)spy.Calls[0].payload;
            Assert.AreEqual(10, dto.currentPlayerActorNumber);
        }

        [Test]
        public void PublishDealerPlan_RaisesExpectedEvents()
        {
            var spy = new BroadcasterSpy();
            var mapper = new PlayerIdMapper();
            mapper.Register(new PlayerId(1), 1);
            var messenger = new DealerNetworkMessenger(spy, new SequenceService(), mapper);
            var plan = new DealerPlan
            {
                ShuffleSeeds = new List<PileShuffleSeedEffect>
                {
                    new() { PileId = new PileId(100), Seed = 42 }
                },
                TurnOrder = new List<TurnOrderEffect>
                {
                    new() { PlayerId = new PlayerId(1), Order = 0 }
                },
                Moves = new List<CardMoveEffect>
                {
                    new() { CardId = new CardId(1), FromPileId = new PileId(100), ToPileId = new PileId(200) }
                },
                Visibility = new List<VisibilityEffect>
                {
                    new() { CardId = new CardId(1), IsVisible = true }
                }
            };

            messenger.PublishDealerPlan(plan);

            Assert.AreEqual(4, spy.Calls.Count);
            Assert.AreEqual(EventCode.PileShuffledWithSeed, spy.Calls[0].code);
            Assert.AreEqual(EventCode.ListOrderDeclared, spy.Calls[1].code);
            Assert.AreEqual(EventCode.CardMoved, spy.Calls[2].code);
            Assert.AreEqual(EventCode.CardVisibilityChanged, spy.Calls[3].code);
        }
    }
}
