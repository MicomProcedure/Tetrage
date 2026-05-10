using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.Core.Enums;
using Tetrage.Core.Contracts;
using Tetrage.Core.Ids;


namespace Tetrage.Factories
{
    /// <summary>
    /// モデルのみを生成するカードファクトリ実装
    /// </summary>
    public class CardModelFactory : ICardFactory
    {
        // 単一デッキ前提の既定 DeckId。複数デッキ対応時は差し替え/DI する。
        private static readonly DeckId DefaultDeckId = new DeckId(1);

        /// <inheritdoc/>
        public Card CreateCard(Suit suit, int number) //1枚のカードオブジェクトを生成.
        {
            // CardId を DeckId×suitIndex×number で決定論的に合成して生成
            var cardId = CardIdComposer.Compose(DefaultDeckId, (int)suit, number);
            return new Card(cardId, suit, number, isFaceUp: false);
        }

        /// <inheritdoc/>
        public List<Card> CreateCards(Suit[] suits, int countPerSuit) //指定された条件に基づいて複数のカードを生成し、リストとして返す．
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