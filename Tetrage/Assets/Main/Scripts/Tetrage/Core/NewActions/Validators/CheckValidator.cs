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
        
            if(!context.RequesterPlayer.IsReach)
            {
                return ValidationResult.Invalid("リーチをしていないプレイヤーはCheckできません");
            }
            
            var hands = context.RequesterPlayer.Hands;
            
            // 手札が空でないかチェック
            if (!hands.Any())
            {
                return ValidationResult.Invalid("手札にカードがありません");
            }
            
            
            
            return ValidationResult.Valid();
        }
    }
} 