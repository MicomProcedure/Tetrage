using System;
using Tetrage.Models;
using Tetrage.Core.Contracts;
namespace Tetrage.Factories
{
    /// <summary>
    /// カード固有の山札(Pile)生成用Factory
    /// </summary>
    public class CardPileFactory : ICardPileFactory
    {
        /// <summary>
        /// 新しいカード山(Pile)を生成します。Modelのみ生成します。
        /// </summary>
        public CardPile CreatePile(string name, int maxCount)
        {
            // カード山(Pile)モデル生成
            var pileModel = new CardPile(name, maxCount);

            return pileModel;
        }
    }
} 