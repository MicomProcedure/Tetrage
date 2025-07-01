namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Pass アクションの実行条件を検証するクラス
    /// 何もしないアクションなので、基本的に常に実行可能
    /// </summary>
    public class PassValidator : IActionValidator
    {
        public ValidationResult Validate(IActionContext context)
        {
            // 基本チェック: リクエスターがターン中のプレイヤーか
            if (!ReferenceEquals(context.RequesterPlayer, context.CurrentTurnPlayer))
            {
                return ValidationResult.Invalid("自分のターンではありません");
            }

            // Passアクションは何もしないアクションなので、
            // 基本的な条件さえ満たしていれば常に実行可能
            return ValidationResult.Valid();
        }
    }
}