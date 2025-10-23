using System.Linq;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Reach アクションの実行条件を検証するクラス
    /// </summary>
    public class ReachValidator : IActionValidator
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

            if(context.RequesterPlayer.IsReach)
            {
                return ValidationResult.Invalid("リーチをしています");
            }
            
            var hands = context.RequesterPlayer.Hands;
            
            // 手札が満杯かチェック（容量は定数から取得する想定）
            // TODO: プレイヤーの手札容量を定数から取得
            var maxHandCapacity = 3; // 仮の値
            if (hands.Count < maxHandCapacity)
            {
                return ValidationResult.Invalid("手札が満杯ではありません");
            }
            
            // 手札が空でないかチェック
            if (!hands.Any())
            {
                return ValidationResult.Invalid("手札にカードがありません");
            }
            
            // 全てのカードのスートが一致しているかチェック
            var firstSuit = hands.First().Suit;
            var allSuitsSame = hands.All(card => card.Suit == firstSuit);
            
            if (!allSuitsSame)
            {
                return ValidationResult.Invalid("手札のスートが全て一致していません");
            }
            
            return ValidationResult.Valid();
        }
    }
} 