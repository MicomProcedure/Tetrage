using System;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクション定義を管理するレジストリクラス（ActionType専用版）
    /// 設定ベースでValidator/Executorの組み合わせを管理
    /// </summary>
    public class ActionRegistry
    {
        private readonly Dictionary<ActionType, ActionDefinition> _definitions = new();

        /// <summary>
        /// アクションを登録する（型指定版）
        /// </summary>
        public void RegisterAction<TValidator, TExecutor>(ActionType actionType)
            where TValidator : IActionValidator, new()
            where TExecutor : IActionExecutor, new()
        {
            _definitions[actionType] = new ActionDefinition
            {
                ActionType = actionType,
                ValidatorFactory = () => new TValidator(),
                ExecutorFactory = () => new TExecutor()
            };

            // Debug.Log($"Action登録完了: {actionType} -> {typeof(TValidator).Name}/{typeof(TExecutor).Name}");
        }

        /// <summary>
        /// アクションを登録する（ファクトリ関数版）
        /// </summary>
        public void RegisterAction(
            ActionType actionType,
            Func<IActionValidator> validatorFactory,
            Func<IActionExecutor> executorFactory)
        {
            if (validatorFactory == null)
                throw new ArgumentNullException(nameof(validatorFactory));

            if (executorFactory == null)
                throw new ArgumentNullException(nameof(executorFactory));

            _definitions[actionType] = new ActionDefinition
            {
                ActionType = actionType,
                ValidatorFactory = validatorFactory,
                ExecutorFactory = executorFactory
            };

            Debug.Log($"Action登録完了: {actionType}");
        }

        /// <summary>
        /// 指定されたActionTypeのActionを作成
        /// </summary>
        public IAction CreateAction(ActionType actionType, IPlayer requester)
        {
            if (!_definitions.TryGetValue(actionType, out var definition))
                throw new ArgumentException($"未登録のアクションタイプ: {actionType}", nameof(actionType));

            if (requester == null)
                throw new ArgumentNullException(nameof(requester));

            try
            {
                var validator = definition.ValidatorFactory();
                var executor = definition.ExecutorFactory();

                return new GenericAction(actionType, requester, validator, executor);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Action作成中にエラー: {actionType} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 登録済みのActionType一覧を取得
        /// </summary>
        public IReadOnlyList<ActionType> GetRegisteredActionTypes()
        {
            return _definitions.Keys.ToList();
        }

        /// <summary>
        /// 指定されたActionTypeが登録されているかチェック
        /// </summary>
        public bool IsActionRegistered(ActionType actionType)
        {
            return _definitions.ContainsKey(actionType);
        }

        /// <summary>
        /// アクション定義を取得（デバッグ用）
        /// </summary>
        public ActionDefinition GetActionDefinition(ActionType actionType)
        {
            return _definitions.TryGetValue(actionType, out var definition) ? definition : null;
        }

        /// <summary>
        /// 全てのアクション定義をクリア
        /// </summary>
        public void Clear()
        {
            _definitions.Clear();
            Debug.Log("全てのAction定義をクリアしました");
        }
    }

    /// <summary>
    /// アクション定義クラス（ActionType専用版）
    /// Validator/Executorのファクトリ関数を保持
    /// </summary>
    public class ActionDefinition
    {
        public ActionType ActionType { get; set; }
        public Func<IActionValidator> ValidatorFactory { get; set; }
        public Func<IActionExecutor> ExecutorFactory { get; set; }

        /// <summary>
        /// ActionIdプロパティ（ログ出力用）
        /// </summary>
        public string ActionId => ActionType.ToActionId();

        public override string ToString()
        {
            return $"ActionDefinition[{ActionType}]";
        }
    }
}