using System;
using System.Collections.Generic;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクション生成ファクトリクラス（ActionType専用版）
    /// ActionRegistryを使用した設定ベースのアプローチ
    /// </summary>
    public static class ActionFactory
    {
        private static readonly ActionRegistry _registry = new ActionRegistry();

        /// <summary>
        /// デフォルトのActionRegistryを取得
        /// </summary>
        public static ActionRegistry Registry => _registry;

        /// <summary>
        /// 静的初期化：デフォルトのアクションを登録
        /// </summary>
        static ActionFactory()
        {
            RegisterDefaultActions();
        }

        /// <summary>
        /// デフォルトのアクションを登録
        /// </summary>
        private static void RegisterDefaultActions()
        {
            // ActionType版での登録
            _registry.RegisterAction<DrawValidator, DrawExecutor>(ActionType.Draw);
            _registry.RegisterAction<OpenValidator, OpenExecutor>(ActionType.Open);
            _registry.RegisterAction<ReachValidator, ReachExecutor>(ActionType.Reach);
            _registry.RegisterAction<CheckValidator, CheckExecutor>(ActionType.Check);
            _registry.RegisterAction<PassValidator, PassExecutor>(ActionType.Pass);
            _registry.RegisterAction<TetrageSoloValidator, TetrageSoloExecutor>(ActionType.TetrageSolo);
            _registry.RegisterAction<TetrageMultiValidator, TetrageMultiExecutor>(ActionType.TetrageMulti);

            Debug.Log("デフォルトアクションの登録完了");
        }

        /// <summary>
        /// アクションを作成する
        /// </summary>
        /// <param name="actionType">アクションタイプ</param>
        /// <param name="requester">実行プレイヤー</param>
        /// <param name="gameContextProvider">ゲームコンテキストプロバイダー（未使用、互換性のため保持）</param>
        /// <returns>作成されたアクション</returns>
        public static IAction CreateAction(ActionType actionType, IPlayer requester, IGameContext gameContextProvider = null)
        {
            if (requester == null)
                throw new ArgumentNullException(nameof(requester));

            try
            {
                return _registry.CreateAction(actionType, requester);
            }
            catch (Exception ex)
            {
                Debug.LogError($"アクション作成中にエラーが発生: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 新しいアクションを登録する（型指定版）
        /// </summary>
        public static void RegisterAction<TValidator, TExecutor>(ActionType actionType)
            where TValidator : IActionValidator, new()
            where TExecutor : IActionExecutor, new()
        {
            _registry.RegisterAction<TValidator, TExecutor>(actionType);
        }

        /// <summary>
        /// 新しいアクションを登録する（ファクトリ関数版）
        /// </summary>
        public static void RegisterAction(
            ActionType actionType,
            Func<IActionValidator> validatorFactory,
            Func<IActionExecutor> executorFactory)
        {
            _registry.RegisterAction(actionType, validatorFactory, executorFactory);
        }

        /// <summary>
        /// 指定されたアクションタイプがサポートされているかチェック
        /// </summary>
        public static bool IsActionSupported(ActionType actionType)
        {
            return _registry.IsActionRegistered(actionType);
        }

        /// <summary>
        /// 利用可能なActionType一覧を取得
        /// </summary>
        public static IReadOnlyList<ActionType> GetAvailableActionTypes()
        {
            return _registry.GetRegisteredActionTypes();
        }

        /// <summary>
        /// ActionManagerにすべてのアクションファクトリを登録する
        /// </summary>
        public static void RegisterAllActions(ActionManager actionManager)
        {
            if (actionManager == null)
                throw new ArgumentNullException(nameof(actionManager));

            var actionTypes = _registry.GetRegisteredActionTypes();
            foreach (var actionType in actionTypes)
            {
                var actionId = actionType.ToActionId();
                // ActionManagerの既存インターフェースに合わせてファクトリ関数を登録
                actionManager.RegisterActionFactory(actionId, (requester, gameContextProvider) =>
                    _registry.CreateAction(actionType, requester));
            }

            Debug.Log($"ActionManagerに登録完了: {string.Join(", ", actionTypes)}");
        }

        /// <summary>
        /// 全てのアクション定義をクリア（テスト用）
        /// </summary>
        public static void ClearAllActions()
        {
            _registry.Clear();
        }

        /// <summary>
        /// デフォルトアクションを再登録（テスト後のリセット用）
        /// </summary>
        public static void ResetToDefaults()
        {
            _registry.Clear();
            RegisterDefaultActions();
        }
    }
}