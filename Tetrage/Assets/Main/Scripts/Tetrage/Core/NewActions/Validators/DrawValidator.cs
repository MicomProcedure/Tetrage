using System.Linq;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// Draw アクションの実行条件を検証するクラス
    /// </summary>
    public class DrawValidator : IActionValidator
    {
        public ValidationResult Validate(IActionContext context)
        {
            // TODO: 具体的な条件判定ロジックを実装
            // 例: Stackに十分なカードがあるか、プレイヤーのTmpが空かなど
            
            // 基本チェック: リクエスターがターン中のプレイヤーか
            if (!ReferenceEquals(context.RequesterPlayer, context.CurrentTurnPlayer))
            {
                return ValidationResult.Invalid("自分のターンではありません");
            }
            
            // 基本チェック: Stackにカードが存在するか
            if (context.CurrentStage?.Stack == null || context.CurrentStage.Stack.Count < 2)
            {
                return ValidationResult.Invalid("Stackに十分なカードがありません");
            }
            
            // 基本チェック: プレイヤーのTmpが空か
            if (context.RequesterPlayer.Tmp.Count > 0)
            {
                return ValidationResult.Invalid("Tmpが空ではありません");
            }

            if(context.RequesterPlayer.IsReach)
            {
                return ValidationResult.Invalid("リーチをしているプレイヤーはDrawできません");
            }
            
            return ValidationResult.Valid();
        }
    }
} 