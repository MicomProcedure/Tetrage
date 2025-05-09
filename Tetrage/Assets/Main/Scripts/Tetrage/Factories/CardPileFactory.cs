using System;
using System.Collections.Generic;
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

        /// <summary>
        /// 新しいカード山(Pile)を生成します（初期カード指定付き）。
        /// </summary>
        /// <param name="name">カード山の名前</param>
        /// <param name="initialCards">生成時に含めるカードのコレクション</param>
        /// <param name="maxCount">カード山の最大枚数</param>
        public CardPile CreatePile(string name, IEnumerable<Card> initialCards, int maxCount)
        {
            // 初期カード付きの CardPile を生成
            var pileModel = new CardPile(name, initialCards, maxCount);
            return pileModel;
        }
    }
} 