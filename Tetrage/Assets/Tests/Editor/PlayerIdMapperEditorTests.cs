using NUnit.Framework;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;

namespace Tetrage.Tests.Editor
{
    public class PlayerIdMapperEditorTests
    {
        [Test]
        public void Register_ThenTryGet_BidirectionalMappingWorks()
        {
            var mapper = new PlayerIdMapper();
            var playerId = new PlayerId(1);
            mapper.Register(playerId, 10);

            Assert.IsTrue(mapper.TryGetPlayerId(10, out var actualPlayerId));
            Assert.AreEqual(playerId, actualPlayerId);
            Assert.IsTrue(mapper.TryGetActorNumber(playerId, out var actorNumber));
            Assert.AreEqual(10, actorNumber);
        }

        [Test]
        public void UnknownKey_ReturnsFalse()
        {
            var mapper = new PlayerIdMapper();

            Assert.IsFalse(mapper.TryGetPlayerId(999, out _));
            Assert.IsFalse(mapper.TryGetActorNumber(new PlayerId(999), out _));
        }
    }
}
