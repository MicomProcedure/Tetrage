using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクション管理・実行制御を担当するクラス（ActionType専用版）
    /// Singleton パターンで実装
    /// </summary>
    public class ActionManager : MonoBehaviour
    {
        private static ActionManager _instance;
        public static ActionManager Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    var gameObject = new GameObject("ActionManager");
                    _instance = gameObject.AddComponent<ActionManager>();
                    DontDestroyOnLoad(gameObject);
                }
                return _instance;
            }
        }
        
        private readonly Dictionary<ActionType, Func<IPlayer, IGameContextProvider, IAction>> _actionFactories;
        private IGameContextProvider _gameContextProvider;
        
        /// <summary>
        /// アクション実行前イベント
        /// </summary>
        public event Action<IAction, IActionContext> OnActionStarted;
        
        /// <summary>
        /// アクション実行完了イベント
        /// </summary>
        public event Action<IAction, IActionContext, ActionResult> OnActionCompleted;
        
        public ActionManager()
        {
            _actionFactories = new Dictionary<ActionType, Func<IPlayer, IGameContextProvider, IAction>>();
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        /// <summary>
        /// ゲームコンテキストプロバイダーを設定
        /// </summary>
        public void SetGameContextProvider(IGameContextProvider provider)
        {
            _gameContextProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }
        
        /// <summary>
        /// アクションファクトリを登録（ActionType版）
        /// </summary>
        public void RegisterActionFactory(ActionType actionType, Func<IPlayer, IGameContextProvider, IAction> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            _actionFactories[actionType] = factory;
            Debug.Log($"アクションファクトリを登録しました: {actionType}");
        }
        
        /// <summary>
        /// アクションファクトリを登録（string版 - 後方互換性のため）
        /// </summary>
        public void RegisterActionFactory(string actionId, Func<IPlayer, IGameContextProvider, IAction> factory)
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
                var context = new ActionContext(requester, _gameContextProvider);
                return action.CanExecute(context);
            }
            catch (Exception ex)
            {
                Debug.LogError($"アクション実行可能性チェックでエラー: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// アクションが実行可能かチェック（string版 - 後方互換性のため）
        /// </summary>
        public bool CanExecuteAction(string actionId, IPlayer requester)
        {
            if (string.IsNullOrEmpty(actionId) || !actionId.IsValidActionType())
                return false;
            
            var actionType = actionId.ToActionType();
            return CanExecuteAction(actionType, requester);
        }
        
        /// <summary>
        /// アクションを実行（ActionType版）
        /// </summary>
        public async UniTask<ActionResult> ExecuteActionAsync(ActionType actionType, IPlayer requester)
        {
            try
            {
                var action = CreateAction(actionType, requester);
                var context = new ActionContext(requester, _gameContextProvider);
                
                // イベント発火
                OnActionStarted?.Invoke(action, context);
                
                // アクション実行
                var result = await action.ExecuteAsync(context);
                
                // イベント発火
                OnActionCompleted?.Invoke(action, context, result);
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"アクション実行でエラー: {ex.Message}");
                return ActionResult.Failure($"アクション実行エラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// アクションを実行（string版 - 後方互換性のため）
        /// </summary>
        public async UniTask<ActionResult> ExecuteActionAsync(string actionId, IPlayer requester)
        {
            if (string.IsNullOrEmpty(actionId))
                return ActionResult.Failure("アクションIDが空です");
            
            if (!actionId.IsValidActionType())
                return ActionResult.Failure($"無効なActionType: {actionId}");
            
            var actionType = actionId.ToActionType();
            return await ExecuteActionAsync(actionType, requester);
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
        
        /// <summary>
        /// プレイヤーが実行可能なアクション一覧を取得（string版 - 後方互換性のため）
        /// </summary>
        public IReadOnlyList<string> GetAvailableActions(IPlayer requester)
        {
            return GetAvailableActionTypes(requester)
                .Select(actionType => actionType.ToActionId())
                .ToList();
        }
    }
} 