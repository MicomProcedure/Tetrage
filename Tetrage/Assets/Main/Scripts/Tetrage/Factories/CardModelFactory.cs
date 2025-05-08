using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.Core.Enums;
using Tetrage.Core.Contracts;


namespace Tetrage.Factories
{
    /// <summary>
    /// モデルのみを生成するカードファクトリ実装
    /// </summary>
    public class CardModelFactory : ICardFactory
    {
        /// <inheritdoc/>
        public Card CreateCard(Suit suit, int number)
        {
            var card = new Card();
            // 初期表示は裏向き(false)とする
            card.Initialize(suit, number, isVisible: false);
            return card;
        }

        /// <inheritdoc/>
        public List<Card> CreateCards(Suit[] suits, int countPerSuit)
        {
            var list = new List<Card>();
            if (suits == null || suits.Length == 0 || countPerSuit <= 0)
            {
                return list;
            }

            foreach (var suit in suits)
            {
                for (int num = 1; num <= countPerSuit; num++)
                {
                    list.Add(CreateCard(suit, num));
                }
            }

            return list;
        }
    }
} 