using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.Core.Enums;

namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// カード生成機能を定義するインターフェース
    /// </summary>
    public interface ICardFactory
    {
        /// <summary>
        /// 指定したスートセットと各スートあたりの枚数に応じてカードを一括生成します。
        /// </summary>
        /// <param name="suits">生成対象のスート配列</param>
        /// <param name="countPerSuit">各スートあたりの枚数</param>
        /// <returns>生成されたカードモデルのリスト</returns>
        List<Card> CreateCards(Suit[] suits, int countPerSuit);

        /// <summary>
        /// 指定したスートと番号を持つ単一のカードを生成します。
        /// </summary>
        /// <param name="suit">生成するカードのスート</param>
        /// <param name="number">生成するカードの番号</param>
        /// <returns>生成されたカードモデル</returns>
        Card CreateCard(Suit suit, int number);
    }
} 