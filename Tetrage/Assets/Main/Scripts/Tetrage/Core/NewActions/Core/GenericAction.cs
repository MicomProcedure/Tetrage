using System;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// 汎用的なActionクラス
    /// Validator/Executorを外部から注入することで任意のActionを表現
    /// </summary>
    public class GenericAction : NetworkActionBase
    {
        public override ActionType ActionType { get; }

        public GenericAction(
            ActionType actionType,
            IPlayer requester,
            IActionValidator validator,
            IActionExecutor executor)
            : base(requester, validator, executor)
        {
            ActionType = actionType;
        }
    }
}