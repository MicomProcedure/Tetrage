using Cysharp.Threading.Tasks;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクション実行処理を抽象化するインターフェース
    /// </summary>
    public interface IActionExecutor
    {
        /// <summary>
        /// アクションの実行処理を行う
        /// </summary>
        /// <param name="context">実行コンテキスト</param>
        /// <returns>実行結果</returns>
        UniTask<ActionResult> ExecuteAsync(IActionContext context);
    }
} 