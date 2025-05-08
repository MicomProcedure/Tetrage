using Tetrage.Models;

namespace Tetrage.Factories
{
    /// <summary>
    /// カード山(Pile)生成機能を定義するインターフェース
    /// </summary>
    public interface ICardPileFactory
    {
        /// <summary>
        /// 新しいカード山(Pile)を生成します。
        /// </summary>
        CardPile CreatePile(string name, int maxCount);
    }
} 