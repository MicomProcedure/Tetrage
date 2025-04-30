using System.Collections;
using Tetrage.Models;
using Unity.VisualScripting;

namespace Tetrage.Actions
{
    /// <summary>
    /// ゲームにおけるアクションの基底クラスです。
    /// すべての具体的なアクションはこのクラスを継承して定義します
    /// </summary>
    public abstract class GameAction
    {
        protected readonly Player _requester;

        protected GameAction(Player requester)
        {
            _requester = requester;
        }


        /// <summary>
        /// アクションが指定されたプレイヤーおよび状況で有効かどうかを検証します。
        /// </summary>
        /// <param name="player">アクションを実行しようとしているプレイヤー。</param>
        /// <returns>アクションが実行可能であれば true、それ以外は false。</returns>
        public abstract bool Validate(); //abstractにした関数の中身は継承先で実装する

        /// <summary>
        /// アクションを実際に実行します。
        /// </summary>
        public virtual void Execute() {
            _requester.StartCoroutine(Run());
        }

        protected abstract IEnumerator Run(); // 派生クラスで実装する
    }
}
