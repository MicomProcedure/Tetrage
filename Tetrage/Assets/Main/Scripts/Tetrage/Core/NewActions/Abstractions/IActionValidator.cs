namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクションの実行条件検証を抽象化するインターフェース
    /// </summary>
    public interface IActionValidator
    {
        /// <summary>
        /// 検証処理を実行する
        /// </summary>
        /// <param name="context">実行コンテキスト</param>
        /// <returns>検証結果</returns>
        ValidationResult Validate(IActionContext context);
    }
} 