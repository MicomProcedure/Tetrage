using NUnit.Framework;
using System.Collections.Generic;
using Tetrage.Factories;
using Tetrage.Core.Enums;
using Tetrage.Models;
using Tetrage.Core.Contracts;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// CardModelFactory の振る舞いを検証するテストクラス
    /// </summary>
    public class CardModelFactoryTests
    {
        private ICardFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new CardModelFactory();
        }

        [Test]
        public void CreateCard_WithValidSuitAndNumber_ReturnsInitializedCard()
        {
            var suit = Suit.Heart;
            var number = 5;

            var card = _factory.CreateCard(suit, number);

            Assert.NotNull(card);
            Assert.AreEqual(suit, card.Suit);
            Assert.AreEqual(number, card.Number);
            Assert.IsFalse(card.IsVisible, "初期表示は裏向き(false)であるべき");
        }

        [Test]
        public void CreateCards_WithMultipleSuitsAndCount_ReturnsCorrectSequence()
        {
            var suits = new[] { Suit.Spade, Suit.Diamond };
            var countPerSuit = 2;

            var cards = _factory.CreateCards(suits, countPerSuit);

            Assert.NotNull(cards);
            Assert.AreEqual(suits.Length * countPerSuit, cards.Count);

            var expected = new List<(Suit suit, int number)>
            {
                (Suit.Spade, 1),
                (Suit.Spade, 2),
                (Suit.Diamond, 1),
                (Suit.Diamond, 2)
            };

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i].suit, cards[i].Suit);
                Assert.AreEqual(expected[i].number, cards[i].Number);
                Assert.IsFalse(cards[i].IsVisible, "生成されたカードはすべて裏向き(false)であるべき");
            }
        }

        [Test]
        public void CreateCards_WithInvalidParameters_ReturnsEmptyList()
        {
            // null suits
            Assert.IsEmpty(_factory.CreateCards(null, 5));
            // 空のスート配列
            Assert.IsEmpty(_factory.CreateCards(new Suit[0], 5));
            // 0 枚
            Assert.IsEmpty(_factory.CreateCards(new[] { Suit.Club }, 0));
            // 負の枚数
            Assert.IsEmpty(_factory.CreateCards(new[] { Suit.Heart }, -1));
        }
    }
} 