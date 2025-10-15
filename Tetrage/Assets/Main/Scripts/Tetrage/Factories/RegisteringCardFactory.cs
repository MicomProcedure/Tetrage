using System.Collections.Generic;
using Tetrage.Core.Contracts;
using Tetrage.Core.Ids;
using Tetrage.Core.Enums;
using Tetrage.Models;

namespace Tetrage.Factories
{
    /// <summary>
    /// 生成された Card を必ずレジストリに登録するデコレーターファクトリ。
    /// </summary>
    public class RegisteringCardFactory : ICardFactory
    {
        private readonly ICardFactory _innerFactory;
        private readonly IdRegistry<CardId, Card> _registry;

        public RegisteringCardFactory(ICardFactory innerFactory, IdRegistry<CardId, Card> registry)
        {
            _innerFactory = innerFactory;
            _registry = registry;
        }

        public Card CreateCard(Suit suit, int number)
        {
            var card = _innerFactory.CreateCard(suit, number);
            _registry.Register(card);
            return card;
        }

        public List<Card> CreateCards(Suit[] suits, int countPerSuit)
        {
            var cards = _innerFactory.CreateCards(suits, countPerSuit);
            foreach (var card in cards)
            {
                _registry.Register(card);
            }
            return cards;
        }
    }
}


