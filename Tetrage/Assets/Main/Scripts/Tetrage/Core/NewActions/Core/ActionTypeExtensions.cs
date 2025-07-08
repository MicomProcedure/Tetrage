using System;
using Tetrage.Core.Enums;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// ActionType列挙型の拡張メソッド
    /// stringとの相互変換を提供
    /// </summary>
    public static class ActionTypeExtensions
    {
        /// <summary>
        /// ActionTypeをstringに変換
        /// </summary>
        public static string ToActionId(this ActionType actionType)
        {
            return actionType.ToString();
        }
        
        /// <summary>
        /// stringをActionTypeに変換
        /// </summary>
        public static ActionType ToActionType(this string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                throw new ArgumentException("ActionIdが空です", nameof(actionId));
            
            if (!Enum.TryParse<ActionType>(actionId, true, out var actionType))
                throw new ArgumentException($"無効なActionId: {actionId}", nameof(actionId));
            
            return actionType;
        }
        
        /// <summary>
        /// ActionTypeが有効かチェック
        /// </summary>
        public static bool IsValidActionType(this string actionId)
        {
            return !string.IsNullOrEmpty(actionId) && 
                   Enum.TryParse<ActionType>(actionId, true, out _);
        }
        
        /// <summary>
        /// 全てのActionTypeを取得
        /// </summary>
        public static ActionType[] GetAllActionTypes()
        {
            return (ActionType[])Enum.GetValues(typeof(ActionType));
        }
        
        /// <summary>
        /// 全てのActionIdを取得
        /// </summary>
        public static string[] GetAllActionIds()
        {
            var actionTypes = GetAllActionTypes();
            var actionIds = new string[actionTypes.Length];
            
            for (int i = 0; i < actionTypes.Length; i++)
            {
                actionIds[i] = actionTypes[i].ToActionId();
            }
            
            return actionIds;
        }
    }
} 