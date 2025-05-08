using Tetrage.Models;

namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// カード山(Pile)生成機能を定義するインターフェース
    /// </summary>
    public interface ICardPileFactory
    {
        /// <summary>
        /// 新しいカード山(Pile)を生成します。
        /// </summary>
        /// <param name="name">カード山の名前</param>
        /// <param name="maxCount">カード山の最大枚数</param>
        CardPile CreatePile(string name, int maxCount);
    }
} 