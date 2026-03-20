using NUnit.Framework;
using Tetrage.Network.Gameplay;

namespace Tetrage.Tests.Editor
{
    public class SequenceServiceEditorTests
    {
        [Test]
        public void Next_IncrementsSequenceAndStateVersion()
        {
            var seq = new SequenceService();

            Assert.AreEqual(1, seq.NextSequence());
            Assert.AreEqual(2, seq.NextSequence());
            Assert.AreEqual(1, seq.NextStateVersion());
            Assert.AreEqual(2, seq.NextStateVersion());
        }

        [Test]
        public void Reset_RestartsCounters()
        {
            var seq = new SequenceService();
            seq.NextSequence();
            seq.NextStateVersion();

            seq.Reset();

            Assert.AreEqual(1, seq.NextSequence());
            Assert.AreEqual(1, seq.NextStateVersion());
        }
    }
}
