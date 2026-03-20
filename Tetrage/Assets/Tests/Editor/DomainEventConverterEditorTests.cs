using System;
using NUnit.Framework;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;
using CoreEvents = Tetrage.Core.Events;
using NetworkDto = Tetrage.Network.Gameplay;

namespace Tetrage.Tests.Editor
{
    public class DomainEventConverterEditorTests
    {
        [Test]
        public void ToDomain_TurnStarted_ConvertsActorNumberToPlayerId()
        {
            var mapper = new PlayerIdMapper();
            mapper.Register(new PlayerId(1), 10);
            var converter = new CoreEvents.DomainEventConverter(mapper);
            var dto = new NetworkDto.TurnStartedEvent
            {
                sequence = 5,
                stateVersion = 2,
                currentPlayerActorNumber = 10
            };

            var domain = converter.ToDomain(dto);

            Assert.AreEqual(5, domain.Sequence);
            Assert.AreEqual(new PlayerId(1), domain.CurrentPlayerId);
            Assert.AreEqual(2, domain.StateVersion);
        }

        [Test]
        public void ToDomain_TurnStarted_ThrowsWhenMappingMissing()
        {
            var converter = new CoreEvents.DomainEventConverter(new PlayerIdMapper());
            var dto = new NetworkDto.TurnStartedEvent
            {
                sequence = 1,
                stateVersion = 1,
                currentPlayerActorNumber = 99
            };

            Assert.Throws<InvalidOperationException>(() => converter.ToDomain(dto));
        }

        [Test]
        public void ToDto_TurnEnded_ThrowsWhenActorMappingMissing()
        {
            var converter = new CoreEvents.DomainEventConverter(new PlayerIdMapper());
            var domain = new CoreEvents.TurnEndedEvent(sequence: 1, previousPlayerId: new PlayerId(99), stateVersion: 0);

            Assert.Throws<InvalidOperationException>(() => converter.ToDto(domain));
        }
    }
}
