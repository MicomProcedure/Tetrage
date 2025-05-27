using Cysharp.Threading.Tasks; // UniTask を使うための using
using Tetrage.Models;

namespace Tetrage.Actions
{
    /// <summary>
    /// ゲームにおけるアクションの基底クラスです。
    /// すべての具体的なアクションはこのクラスを継承して定義します
    /// </summary>
    public abstract class GameAction
    {
        protected readonly IPlayer _requester;

        protected GameAction(IPlayer requester)
        {
            _requester = requester;
        }

        /// <summary>
        /// アクションが指定されたプレイヤーおよび状況で有効かどうかを検証します。
        /// </summary>
        /// <param name="player">アクションを実行しようとしているプレイヤー。</param>
        /// <returns>アクションが実行可能であれば true、それ以外は false。</returns>
        public abstract bool Validate(); // abstractにした関数の中身は継承先で実装する

        /// <summary>
        /// アクションを実際に実行します。
        /// UniTask を返すように変更し、非同期処理を待ちます。
        /// </summary>
        public virtual async UniTask Execute()
        {
            await Run(); // Run メソッドの UniTask を待つ
        }

        /// <summary>
        /// 派生クラスで実装する非同期処理の本体。
        /// UniTask を返すように変更します。
        /// </summary>
        protected abstract UniTask Run(); //abstractで実装されているのでGameActionを継承している他のクラスで中身を実装できる．
    }
}