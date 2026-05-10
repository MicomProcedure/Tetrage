using System.Linq;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Open アクションの実行条件を検証するクラス
    /// </summary>
    public class OpenValidator : IActionValidator
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
                return ValidationResult.Invalid("リーチをしているプレイヤーはOpenできません");
            }
            
            // 相手プレイヤーの手札に裏向きのカードが存在するかチェック
            var hasHiddenCards = context.OtherPlayers
                .Any(player => player.Hands.Any(card => !card.IsFaceUp));
            
            if (!hasHiddenCards)
            {
                return ValidationResult.Invalid("相手の手札に裏向きのカードがありません");
            }
            
            return ValidationResult.Valid();
        }
    }
} 