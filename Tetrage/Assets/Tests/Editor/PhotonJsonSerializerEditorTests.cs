using NUnit.Framework;
using Tetrage.Network.Gameplay;

namespace Tetrage.Tests.Editor
{
    public class PhotonJsonSerializerEditorTests
    {
        [Test]
        public void SerializeDeserialize_RoundTrip_MaintainsDtoFields()
        {
            var serializer = new PhotonJsonSerializer();
            var dto = new TurnEndedEvent
            {
                sequence = 7,
                stateVersion = 3,
                previousPlayerActorNumber = 22
            };

            var bytes = serializer.Serialize(dto);
            var restored = serializer.Deserialize<TurnEndedEvent>(bytes);

            Assert.AreEqual(7, restored.sequence);
            Assert.AreEqual(3, restored.stateVersion);
            Assert.AreEqual(22, restored.previousPlayerActorNumber);
        }

        [Test]
        public void Deserialize_MissingFields_ReturnsDefaultStructValues()
        {
            var serializer = new PhotonJsonSerializer();
            var bytes = System.Text.Encoding.UTF8.GetBytes("{}");

            var restored = serializer.Deserialize<TurnEndedEvent>(bytes);

            Assert.AreEqual(0, restored.sequence);
            Assert.AreEqual(0, restored.stateVersion);
            Assert.AreEqual(0, restored.previousPlayerActorNumber);
        }
    }
}
