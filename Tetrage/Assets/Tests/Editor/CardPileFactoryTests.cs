using NUnit.Framework;
using System;
using Tetrage.Factories;
using Tetrage.Models;
using Tetrage.Core.Contracts;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// CardPileFactory の振る舞いを検証するテストクラス
    /// </summary>
    public class CardPileFactoryTests
    {
        private ICardPileFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new CardPileFactory();
        }

        [Test]
        public void CreatePile_ReturnsEmptyCardPile()
        {
            var pile = _factory.CreatePile("test", 10);
            Assert.NotNull(pile);
            Assert.AreEqual(0, pile.Count, "新規生成時のCountは0であるべき");
        }

        [Test]
        public void CreatePile_MultipleCalls_ReturnsDistinctInstances()
        {
            var pile1 = _factory.CreatePile("test1", 10);
            var pile2 = _factory.CreatePile("test2", 10);
            Assert.AreNotSame(pile1, pile2, "複数呼び出しで異なるインスタンスを返すべき");
        }

        // 将来的に実装が拡張された場合は、Name や OwnerType などのプロパティ検証テストを追加してください。
    }
} 