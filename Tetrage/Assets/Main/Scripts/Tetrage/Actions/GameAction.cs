using Tetrage.Models;

namespace Tetrage.Actions
{
    /// <summary>
    /// ゲームにおけるアクションの基底クラスです。
    /// すべての具体的なアクションはこのクラスを継承して定義します。
    /// </summary>
    public abstract class GameAction
    {
        /// <summary>
        /// アクションが指定されたプレイヤーおよび状況で有効かどうかを検証します。
        /// </summary>
        /// <param name="player">アクションを実行しようとしているプレイヤー。</param>
        /// <returns>アクションが実行可能であれば true、それ以外は false。</returns>
        public abstract bool Validate(Player player);

        /// <summary>
        /// アクションを実際に実行します。
        /// </summary>
        /// <param name="player">アクションを実行するプレイヤー。</param>
        public abstract void Execute(Player player);
    }
}
