using System.Collections.Generic;
using NUnit.Framework;
using Tetrage.Core.Actions;
using Tetrage.Core;
using Tetrage.Core.Contracts;
using Tetrage.Core.DTO;
using Tetrage.Core.Ids;
using Tetrage.Managers;
using Tetrage.Models;
using Tetrage.Network.Gameplay;
using DomainEvents = Tetrage.Core.Events;

namespace Tetrage.Tests.Editor
{
    public class DealerGameEndedEventEditorTests
    {
        [SetUp]
        public void SetUp()
        {
            ActionSystemInitializer.ResetActionSystem();
        }

        [TearDown]
        public void TearDown()
        {
            ActionSystemInitializer.ResetActionSystem();
        }

        private sealed class FakeDealerPlanner : IDealerPlanner
        {
            public IPlayer DecideFirstPlayer(IReadOnlyList<IPlayer> players) => players.Count > 0 ? players[0] : null;
            public IPlayer GetNextPlayer(IPlayer currentPlayer, IReadOnlyList<IPlayer> players) => currentPlayer;
            public IPlayer GetNextPlayerWithConditions(IPlayer currentPlayer, IReadOnlyList<IPlayer> players, TurnSkipConditions skipConditions = null) => currentPlayer;
            public DealerPlan PlanResetTurnOrder(IReadOnlyList<IPlayer> players) => new();
            public DealerPlan PlanShuffleDeck(CardPile stack) => new();
            public DealerPlan PlanTargetSetup(IReadOnlyList<IPlayer> players, CardPile stack) => new();
            public DealerPlan PlanDistribution(IReadOnlyList<IPlayer> players, CardPile stack, int cardsPerPlayer) => new();
        }

        private sealed class FakeNetworkContext : INetworkContext
        {
            public bool IsHost => true;
            public int UserActorNumber => 1;
            public bool IsReady => true;
            public bool IsInRoom => true;
            public int PlayerCount => 1;
            public IReadOnlyList<int> GetActorNumbers() => new[] { 1 };
        }

        [Test]
        public void GameEndedEvent_SetsGameFinished()
        {
            var eventBus = new R3EventBus();
            var context = new GameContext(
                stage: null,
                players: new List<IPlayer>(),
                userPlayer: null,
                events: eventBus);
            var dealer = new Dealer(context, new FakeDealerPlanner(), new FakeNetworkContext(), new PlayerIdMapper());

            Assert.IsFalse(dealer.IsGameFinished);

            eventBus.Publish(new DomainEvents.GameEndedEvent(1, new List<PlayerId>()));

            Assert.IsTrue(dealer.IsGameFinished);
        }
    }
}
