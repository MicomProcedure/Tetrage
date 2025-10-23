using System.Linq;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// TetrageSolo アクションの実行条件を検証するクラス
    /// </summary>
    public class TetrageSoloValidator : IActionValidator
    {
        public ValidationResult Validate(IActionContext context)
        {
            // ネットワーク前提: ローカルユーザーのみが自身のアクションをリクエスト可能
            if (!ReferenceEquals(context.RequesterPlayer, context.GameContext?.UserPlayer))
            {
                return ValidationResult.Invalid("ローカルプレイヤー以外は操作できません");
            }
            // 基本チェック: リクエスターがターン中のプレイヤーか
            if (!ReferenceEquals(context.RequesterPlayer, context.CurrentTurnPlayer))
            {
                return ValidationResult.Invalid("自分のターンではありません");
            }
            
            // TODO: プレイヤーが自分のスートと同じスートを手札に揃えているかどうかの判定
            var hands = context.RequesterPlayer.Hands;
            
            // 手札が空でないかチェック
            if (!hands.Any())
            {
                return ValidationResult.Invalid("手札にカードがありません");
            }
            
            // 全てのカードのスートが一致していて、そのスートと自分のターゲットスートが一致しているかチェック
            var firstSuit = hands.First().Suit;
            var allSuitsSame = hands.All(card => card.Suit == firstSuit);
            var targetSuit = context.RequesterPlayer.Target.First().Suit;
            var isTargetSuit = allSuitsSame && firstSuit == targetSuit;
            
            if (!isTargetSuit)
            {
                return ValidationResult.Invalid("手札のスートが全て一致していません");
            }
            
            return ValidationResult.Valid();
        }
    }
} 