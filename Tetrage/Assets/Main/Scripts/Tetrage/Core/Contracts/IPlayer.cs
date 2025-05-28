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
        /// プレイヤーのユーザーID。
        /// </summary>
        public string UserId { get; }

        /// <summary>
        /// プレイヤーの一意な識別子。
        /// </summary>
        public int PlayerId { get; }

        /// <summary>
        /// プレイヤーの最初の一枚(本来のカード)
        /// </summary>
        CardPile Target { get; }

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
