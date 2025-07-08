using System.Linq;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Check アクションの実行条件を検証するクラス
    /// </summary>
    public class CheckValidator : IActionValidator
    {
        public ValidationResult Validate(IActionContext context)
        {
            // 基本チェック: リクエスターがターン中のプレイヤーか
            if (!ReferenceEquals(context.RequesterPlayer, context.CurrentTurnPlayer))
            {
                return ValidationResult.Invalid("自分のターンではありません");
            }
            
            // TODO: プレイヤーがReach状態かどうかの判定
            // 現在のモデルにReach状態のフラグがないため、手札の状態から推測
            var hands = context.RequesterPlayer.Hands;
            
            // 手札が空でないかチェック
            if (!hands.Any())
            {
                return ValidationResult.Invalid("手札にカードがありません");
            }
            
            // 全てのカードのスートが一致していて、全て表向きか（Reach状態の推定）
            var firstSuit = hands.First().Suit;
            var allSuitsSame = hands.All(card => card.Suit == firstSuit);
            var allVisible = hands.All(card => card.IsVisible);
            
            if (!allSuitsSame || !allVisible)
            {
                return ValidationResult.Invalid("Reach状態ではありません");
            }
            
            return ValidationResult.Valid();
        }
    }
} 