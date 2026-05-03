using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Network.Gameplay;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクション管理・実行制御を担当するクラス（ActionType専用版）
    /// Singleton パターンで実装
    /// </summary>
    public class ActionManager
    {
        private static ActionManager _instance;
        private static readonly object _lock = new object();

        public static ActionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ActionManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private ActionPanelController _actionPanelController;

        private readonly Dictionary<ActionType, Func<IPlayer, IGameContext, IAction>> _actionFactories;
        private IGameContext _gameContextProvider;
        private IRoundManager _roundManager;
        private ActionAwaiter _actionAwaiter; // ActionAwaiterを追加
        private INetworkActionContext _networkCtx;

        /// <summary>
        /// アクション実行前イベント
        /// </summary>
        public event Action<IAction, IActionContext> OnActionStarted;

        /// <summary>
        /// アクション実行完了イベント
        /// </summary>
        public event Action<IAction, IActionContext, ActionResult> ActionCompleted;

        private ActionManager()
        {
            _actionFactories = new Dictionary<ActionType, Func<IPlayer, IGameContext, IAction>>();
        }

        /// <summary>
        /// ゲームコンテキストプロバイダーを設定
        /// </summary>
        public void SetGameContextProvider(IGameContext provider)
        {
            _gameContextProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public IGameContext GetGameContextProvider()
        {
            if (_gameContextProvider == null)
            {
                Debug.LogError("GameContextProviderが設定されていません");
                return null;
            }
            return _gameContextProvider;
        }

        public void SetRoundManager(IRoundManager roundManager)
        {
            _roundManager = roundManager ?? throw new ArgumentNullException(nameof(roundManager));
        }

        public IRoundManager GetRoundManager()
        {
            if (_roundManager == null)
            {
                Debug.LogError("RoundManagerが設定されていません");
                return null;
            }
            return _roundManager;
        }

        /// <summary>
        /// ActionAwaiterを設定
        /// </summary>
        public void SetActionAwaiter(ActionAwaiter actionAwaiter)
        {
            _actionAwaiter = actionAwaiter;
        }

        public ActionAwaiter GetActionAwaiter()
        {
            return _actionAwaiter;
        }

        /// <summary>
        /// ネットワークアクションコンテキストを設定
        /// </summary>
        public void SetNetworkActionContext(INetworkActionContext networkCtx)
        {
            _networkCtx = networkCtx;
        }

        /// <summary>
        /// アクションファクトリを登録（ActionType版）
        /// </summary>
        public void RegisterActionFactory(ActionType actionType, Func<IPlayer, IGameContext, IAction> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _actionFactories[actionType] = factory;
            // Debug.Log($"アクションファクトリを登録しました: {actionType}");
        }

        /// <summary>
        /// アクションファクトリを登録（string版 - 後方互換性のため）
        /// </summary>
        public void RegisterActionFactory(string actionId, Func<IPlayer, IGameContext, IAction> factory)
        {
            if (string.IsNullOrEmpty(actionId))
                throw new ArgumentException("アクションIDは空にできません", nameof(actionId));

            if (!actionId.IsValidActionType())
                throw new ArgumentException($"無効なActionType: {actionId}", nameof(actionId));

            var actionType = actionId.ToActionType();
            RegisterActionFactory(actionType, factory);
        }

        /// <summary>
        /// 指定されたアクションを作成（ActionType版）
        /// </summary>
        public IAction CreateAction(ActionType actionType, IPlayer requester)
        {
            if (!_actionFactories.ContainsKey(actionType))
            {
                throw new ArgumentException($"未登録のアクションタイプ: {actionType}");
            }

            if (_gameContextProvider == null)
            {
                throw new InvalidOperationException("GameContextProviderが設定されていません");
            }

            return _actionFactories[actionType](requester, _gameContextProvider);
        }

        /// <summary>
        /// 指定されたアクションを作成（string版 - 後方互換性のため）
        /// </summary>
        public IAction CreateAction(string actionId, IPlayer requester)
        {
            if (string.IsNullOrEmpty(actionId))
                throw new ArgumentException("アクションIDは空にできません", nameof(actionId));

            if (!actionId.IsValidActionType())
                throw new ArgumentException($"無効なActionType: {actionId}", nameof(actionId));

            var actionType = actionId.ToActionType();
            return CreateAction(actionType, requester);
        }

        /// <summary>
        /// アクションが実行可能かチェック（ActionType版）
        /// </summary>
        public bool CanExecuteAction(ActionType actionType, IPlayer requester)
        {
            try
            {
                var action = CreateAction(actionType, requester);
                var context = new ActionContext(requester, _gameContextProvider, _actionAwaiter, _networkCtx);
                return action.CanExecute(context);
            }
            catch (Exception ex)
            {
                Debug.LogError($"アクション実行可能性チェックでエラー: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// アクションを実行（ActionType版）
        /// </summary>
        public async UniTask<ActionResult> ExecuteActionAsync(ActionType actionType, IPlayer requester)
        {
            try
            {
                var action = CreateAction(actionType, requester);
                var context = new ActionContext(requester, _gameContextProvider, _actionAwaiter, _networkCtx);

                // イベント発火
                OnActionStarted?.Invoke(action, context);

                // アクション実行
                var result = await action.ExecuteAsync(context);

                // イベント発火
                ActionCompleted?.Invoke(action, context, result);

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"アクション実行でエラー: {ex.Message}");
                return ActionResult.Failure($"アクション実行エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 登録済みのActionType一覧を取得
        /// </summary>
        public IReadOnlyList<ActionType> GetRegisteredActionTypes()
        {
            return _actionFactories.Keys.ToList();
        }

        /// <summary>
        /// 登録済みのアクションID一覧を取得（string版 - 後方互換性のため）
        /// </summary>
        public IReadOnlyList<string> GetRegisteredActionIds()
        {
            return _actionFactories.Keys.Select(actionType => actionType.ToActionId()).ToList();
        }

        /// <summary>
        /// プレイヤーが実行可能なActionType一覧を取得
        /// </summary>
        public IReadOnlyList<ActionType> GetAvailableActionTypes(IPlayer requester)
        {
            return _actionFactories.Keys
                .Where(actionType => CanExecuteAction(actionType, requester))
                .ToList();
        }


    }
}