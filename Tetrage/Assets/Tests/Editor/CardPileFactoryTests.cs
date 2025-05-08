using NUnit.Framework;
using System;
using Tetrage.Factories;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// CardPileFactory の振る舞いを検証するテストクラス
    /// </summary>
    public class CardPileFactoryTests
    {
        private CardPileFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new CardPileFactory();
        }

        [Test]
        public void CreatePile_ThrowsNotImplementedException()
        {
            // 未実装状態では NotImplementedException を投げる
            Assert.Throws<NotImplementedException>(() => _factory.CreatePile());
        }

        // 将来的に実装が完了した場合は、返却される CardPile のプロパティを検証するテストを追加してください。
    }
} 