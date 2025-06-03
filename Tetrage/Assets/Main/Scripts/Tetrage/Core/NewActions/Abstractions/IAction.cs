using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// 全てのActionが実装すべき基本インターフェース
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// アクションの一意識別子
        /// </summary>
        string ActionId { get; }
        
        /// <summary>
        /// アクションを実行するプレイヤー
        /// </summary>
        IPlayer Requester { get; }
        
        /// <summary>
        /// アクションの実行条件を検証する
        /// </summary>
        /// <param name="context">実行コンテキスト</param>
        /// <returns>実行可能な場合true</returns>
        bool CanExecute(IActionContext context);
        
        /// <summary>
        /// アクションを実行する
        /// </summary>
        /// <param name="context">実行コンテキスト</param>
        /// <returns>実行結果</returns>
        UniTask<ActionResult> ExecuteAsync(IActionContext context);
    }
} 