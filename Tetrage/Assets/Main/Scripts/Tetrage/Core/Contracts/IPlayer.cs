using Tetrage.Actions;
using Tetrage.Models;
using Cysharp.Threading.Tasks;

namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// プレイヤーの公開インターフェース。(プレイヤーができることを指定する)
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        /// プレイヤーの一意な識別子。
        /// </summary>
        public int PlayerID { get; }

        /// <summary>
        /// プレイヤーの最初の一枚(本来のカード)
        /// </summary>
        Card Target { get; }

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
        UniTask PerformAction(GameAction action);
    }
}
