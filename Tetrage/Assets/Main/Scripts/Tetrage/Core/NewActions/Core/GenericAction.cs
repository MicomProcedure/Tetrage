using System;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// 汎用的なActionクラス
    /// Validator/Executorを外部から注入することで任意のActionを表現
    /// </summary>
    public class GenericAction : ActionBase
    {
        public override string ActionId { get; }
        
        public GenericAction(
            string actionId,
            IPlayer requester,
            IActionValidator validator,
            IActionExecutor executor) 
            : base(requester, validator, executor)
        {
            ActionId = actionId ?? throw new ArgumentNullException(nameof(actionId));
        }
        
        /// <summary>
        /// ファクトリメソッド（後方互換性のため）
        /// </summary>
        public static GenericAction Create(
            string actionId,
            IPlayer requester,
            IActionValidator validator,
            IActionExecutor executor)
        {
            return new GenericAction(actionId, requester, validator, executor);
        }
    }
} 