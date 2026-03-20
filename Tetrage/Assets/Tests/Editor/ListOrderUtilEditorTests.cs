using System.Collections.Generic;
using NUnit.Framework;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;

namespace Tetrage.Tests.Editor
{
    public class ListOrderUtilEditorTests
    {
        private sealed class PlayerItem
        {
            public PlayerId Id { get; }

            public PlayerItem(int id)
            {
                Id = new PlayerId(id);
            }
        }

        [Test]
        public void ApplyOrderByPlayerId_SortsByOrderedIds()
        {
            var items = new List<PlayerItem> { new(1), new(2), new(3) };
            var orderedIds = new[] { 3, 1, 2 };

            ListOrderUtil.ApplyOrderByPlayerId(items, orderedIds, x => x.Id);

            Assert.AreEqual(3, items[0].Id.Value);
            Assert.AreEqual(1, items[1].Id.Value);
            Assert.AreEqual(2, items[2].Id.Value);
        }

        [Test]
        public void ApplyOrderInPlace_NullArgs_NoThrow()
        {
            Assert.DoesNotThrow(() => ListOrderUtil.ApplyOrderInPlace<int, int>(null, null, null, null));
        }
    }
}
