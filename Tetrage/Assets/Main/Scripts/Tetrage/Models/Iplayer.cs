using Tetrage.Actions;
using Tetrage.Core.Enums;

namespace Tetrage.Models
{
    /// <summary>
    /// プレイヤーの公開インターフェース。(プレイヤーができることを指定する)
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        /// プレイヤーの一意な識別子。
        /// </summary>
        int PlayerID { get; set; }

        /// <summary>
        /// プレイヤーの最初の一枚(本来のカード)
        /// </summary>
        Card Target { get; set; }

        /// <summary>
        /// プレイヤーが所持している手札の一覧。
        /// </summary>
        CardPile Hands { get; }

        /// <summary>
        /// 一時的に保持しているカードの一覧。
        /// </summary>
        CardPile Tmp { get; }

        /// <summary>
        /// 指定されたアクションを実行します。
        /// </summary>
        /// <param name="action">実行するゲームアクション。</param>
        void PerformAction(GameAction action);
    }
}
