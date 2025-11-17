using System;
using System.Threading;
using UnityEngine;
using Tetrage.Core.Contracts;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Actions;
using Tetrage.Network.Gameplay;
using Tetrage.Core.DTO;
using R3;
using DomainEvents = Tetrage.Core.Events;

/// <summary>
/// ゲームのディーラークラス。カードの配布、ターン管理、勝敗判定を行う。
/// </summary>
namespace Tetrage.Managers
{
    public class Dealer : IRoundManager
    {

        #region イベント
        public event Action TurnStart;
        public event Action TurnEnd;
        public event Action RoundStart;
        public event Action RoundEnd;
        public event Action GameStart;
        public event Action GameEnd;

        #endregion

        #region フィールド


        /// <summary>
        /// ディーラー戦略
        /// </summary>
        private IDealerPlanner _dealerPlanner;
        public IDealerPlanner DealerPlanner { get { return _dealerPlanner; } }

        /// <summary>
        /// ディーラー戦略のイベント発行用
        /// </summary>
        private IEventEmitter<DealerPlan> _dealerPlanEmitter;
        public IEventEmitter<DealerPlan> DealerPlanEmitter => _dealerPlanEmitter;
        private TurnGate _turnGate; // Hostのみ使用
        private IEventEmitter<TurnStartedEvent> _lifecycleEmitter; // Hostのみ使用
        private IGameContextProvider _gameContext; // 読み取り専用のコンテキスト
        private INetworkContext _networkContext; // ネットワーク状態の抽象化
        private IPlayerIdMapper _playerIdMapper; // PlayerId/ActorNumber変換用

        /// <summary>
        /// ラウンド数
        /// </summary>
        private int _roundCount = 0;
        public int RoundCount { get { return _roundCount; } }

        /// <summary>
        /// ターン数
        /// </summary>
        private int _turnCount = 0;
        public int TurnCount { get { return _turnCount; } }

        /// <summary>
        /// 最大ラウンド数（デフォルト: 10）
        /// </summary>
        private int _maxRounds = 100;
        public int MaxRounds { get { return _maxRounds; } }

        // プレイヤーアクション待機用
        private ActionAwaiter _actionAwaiter;   // ActionAwaiter に責任を委譲
        private ActionManager _actionManager;

        // ゲーム終了フラグ
        private bool _isGameFinished = false;
        public bool IsGameFinished { get { return _isGameFinished; } }

        // ゲームが途中中断されたかどうか
        private bool _isGameInterrupted = false;
        public bool IsGameInterrupted { get { return _isGameInterrupted; } }

        #endregion

        #region コンストラクタ

        /// <summary>
        /// 戦略パターン対応のデフォルトコンストラクタ
        /// </summary>
        public Dealer(IGameContextProvider gameContext, IDealerPlanner dealerPlanner, IEventEmitter<DealerPlan> dealerPlanEmitter)
        {
            if (gameContext == null) throw new ArgumentNullException(nameof(gameContext));
            if (dealerPlanner == null) throw new ArgumentNullException(nameof(dealerPlanner));

            _gameContext = gameContext;
            _dealerPlanner = dealerPlanner;
            _dealerPlanEmitter = dealerPlanEmitter;
            _roundCount = 0; // 初期化
            _turnCount = 0; // 初期化

            InitializeActionSystem();
            // GameContext は GameManager 側で生成後に注入されるため、ActionSystem 初期化は後段で行う
            Debug.Log("Dealer: インスタンスが作成されました（戦略パターン対応）");
        }

        #endregion

        #region 設定メソッド

        /// <summary>
        /// 後からEmitterを差し替える（GameManagerのネットワーク初期化完了後に注入）。
        /// Hostのみ設定。Guestはnullのまま。
        /// </summary>
        public void SetEmitter(IEventEmitter<DealerPlan> emitter)
        {
            _dealerPlanEmitter = emitter;
        }

        /// <summary>
        /// TurnGate を注入（Hostのみ）。GameplayNetworkController から取得して渡す想定。
        /// </summary>
        public void SetTurnGate(TurnGate gate)
        {
            _turnGate = gate;
        }

        /// <summary>
        /// 進行イベント用のEmitterを注入（Hostのみ）。
        /// </summary>
        public void SetLifecycleEmitter(IEventEmitter<TurnStartedEvent> emitter)
        {
            _lifecycleEmitter = emitter;
        }

        /// <summary>
        /// NetworkContextを注入。ホスト判定等に使用する。
        /// </summary>
        public void SetNetworkContext(INetworkContext networkContext)
        {
            _networkContext = networkContext;
        }

        /// <summary>
        /// PlayerIdMapperを注入。PlayerId→ActorNumber変換に使用する。
        /// </summary>
        public void SetPlayerIdMapper(IPlayerIdMapper playerIdMapper)
        {
            _playerIdMapper = playerIdMapper;
        }



        /// <summary>
        /// 最大ラウンド数を設定する
        /// </summary>
        /// <param name="maxRounds">最大ラウンド数（1以上の値）</param>
        public void SetMaxRounds(int maxRounds)
        {
            if (maxRounds < 1)
            {
                Debug.LogWarning($"Dealer: 無効な最大ラウンド数: {maxRounds}. 最小値1に設定します");
                _maxRounds = 1;
            }
            else
            {
                _maxRounds = maxRounds;
                Debug.Log($"Dealer: 最大ラウンド数を{_maxRounds}に設定しました");
            }
        }

        #endregion

        #region ラウンド管理

        public async UniTask StartGameAsync(float timeoutSeconds = 0, CancellationToken gameCts = default)
        {
            if (!IsHost())
            {
                Debug.Log("Dealer: StartGameAsync はホストのみ実行します。無視しました");
                return;
            }
            // 前提条件を検証
            ValidateStartGame();
            ValidateStrategy();

            // 初めてラウンドを開始する際の処理（山札シャッフル、ターゲットカード設定、ターン順序初期化、最初のプレイヤーを決定）
            FirstDeal();

            // ゲーム開始イベントを通知
            OnGameStart();

            // ゲーム終了フラグをリセット
            _isGameFinished = false;

            // 初回のTurnStartedが適用されるまで待機（CurrentPlayerが設定されるまで）
            if (_turnGate != null)
            {
                await _turnGate.WaitNextAsync();
            }

            await StartTurnLoopAsync(timeoutSeconds, gameCts);

            Debug.Log("Dealer: ゲームが終了します");

            EndGame();
        }

        /// <summary>
        /// 初めてラウンドを開始する際の処理（山札シャッフル、ターゲットカード設定、ターン順序初期化、最初のプレイヤーを決定）
        /// </summary>
        public void FirstDeal()
        {
            if (!IsHost())
            {
                Debug.Log("Dealer: FirstDeal はホストのみ実行します。無視しました");
                return;
            }
            // デッキ準備 & 配布（副作用なしプラン → イベント発行 → 受信適用）
            // 1) 山札シャッフル（決定論で構築される前提のため、原則空プラン）
            var shufflePlan = _dealerPlanner.PlanShuffleDeck(_gameContext.Stage.Stack);
            _dealerPlanEmitter?.Emit(shufflePlan);

            // 2) 初期ターゲット設定
            var targetPlan = _dealerPlanner.PlanTargetSetup(_gameContext.Players, _gameContext.Stage.Stack);
            _dealerPlanEmitter?.Emit(targetPlan);

            // 3) ターン順序初期化（プランにTurnOrderを含め、Emitterで送信）
            var orderPlan = _dealerPlanner.PlanResetTurnOrder(_gameContext.Players);
            _dealerPlanEmitter?.Emit(orderPlan);

            // 4) 最初のプレイヤーを決定
            var firstPlayer = _dealerPlanner.DecideFirstPlayer(_gameContext.Players);
            Debug.Log($"Dealer: ゲーム開始 - 最初のプレイヤーは Player {firstPlayer.Id}");
            // 最初の手番を宣言（適用はApplierが行い、CurrentPlayerを設定）
            // PlayerId→ActorNumber変換を実行してから送信
            if (_playerIdMapper != null && _playerIdMapper.TryGetActorNumber(firstPlayer.Id, out var actorNumber))
            {
                _lifecycleEmitter?.Emit(new TurnStartedEvent { currentPlayerActorNumber = actorNumber });
            }
            else
            {
                Debug.LogWarning($"Dealer: PlayerId {firstPlayer.Id} のマッピングが見つかりません");
            }


        }



        /// <summary>
        /// 勝敗が決まるまでラウンドを繰り返すメインループ
        /// </summary>
        /// <param name="gameCts">外部からゲーム全体をキャンセルしたい場合のトークン</param>
        public async UniTask StartTurnLoopAsync(float timeoutSeconds = 0, CancellationToken gameCts = default)
        {
            if (!IsHost())
            {
                Debug.Log("Dealer: StartTurnLoopAsync はホストのみ実行します。無視しました");
                return;
            }
            // ゲーム終了かキャンセルされるまでラウンドのループを繰り返す
            while (!_isGameFinished && !gameCts.IsCancellationRequested)
            {
                // 単一ラウンドを開始
                try
                {
                    await StartSingleTurnAsync(timeoutSeconds, gameCts);
                }
                catch (OperationCanceledException ex) when (gameCts.IsCancellationRequested)
                {
                    Debug.Log($"Dealer: ラウンドループがキャンセルされました: {ex.Message}");
                    _isGameInterrupted = true;
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Dealer: ラウンドループ中に致命的エラーが発生: {ex.Message}");
                    _isGameInterrupted = true;
                    _isGameFinished = true; // 強制終了
                }
            }
        }

        /// <summary>
        /// 単一ラウンドを処理
        /// </summary>
        public async UniTask StartSingleTurnAsync(float timeoutSeconds = 0, CancellationToken gameCts = default)
        {
            if (!IsHost())
            {
                Debug.Log("Dealer: StartSingleTurnAsync はホストのみ実行します。無視しました");
                return;
            }
            // ラウンド開始イベントを通知
            OnTurnStart();

            try
            {
                // 現在のプレイヤーが設定されているかどうかを検証
                if (!ValidateCurrentPlayer()) return;

                // プレイヤーのアクションを待つ（現在手番のプレイヤー）
                // Hostの自手番は ActionAwaiter、Guest手番はネットのActionResultを待機
                var isLocalTurn = _gameContext.UserPlayer != null && ReferenceEquals(_gameContext.CurrentPlayer, _gameContext.UserPlayer);
                ActionResult actionResult;
                if (isLocalTurn)
                {
                    actionResult = await _actionAwaiter.WaitForPlayerActionAsync(_gameContext.CurrentPlayer);
                }
                else
                {
                    actionResult = await WaitActionResultFromNetworkAsync(_gameContext.CurrentPlayer, gameCts);
                }

                if (!actionResult.IsSuccess)
                {
                    Debug.LogWarning($"Dealer: プレイヤーアクション失敗 - {actionResult.ErrorMessage}");
                }

                actionResult.Log("Dealer: プレイヤーアクション結果");
            }
            catch (OperationCanceledException) when (gameCts.IsCancellationRequested)
            {
                Debug.Log($"Dealer: プレイヤー {_gameContext.CurrentPlayer?.PlayerId} のアクションがキャンセルされました");
                _isGameFinished = true;
            }

            // 勝利条件チェック
            if (CheckWinCondition())
            {
                _isGameFinished = true;
            }

            OnTurnEnd();


            // 次のプレイヤーへ
            if (!_isGameFinished && _gameContext.CurrentPlayer != null)
            {
                // 次手番を決定し、TurnStarted を発行（Emitter経由）し、適用完了を待つ
                var next = _dealerPlanner.GetNextPlayer(_gameContext.CurrentPlayer, _gameContext.Players);
                if (next != null && IsHost())
                {
                    // PlayerId→ActorNumber変換を実行してから送信
                    if (_playerIdMapper != null && _playerIdMapper.TryGetActorNumber(next.Id, out var actorNumber))
                    {
                        _lifecycleEmitter?.Emit(new TurnStartedEvent { currentPlayerActorNumber = actorNumber });
                    }
                    else
                    {
                        Debug.LogWarning($"Dealer: PlayerId {next.Id} のマッピングが見つかりません");
                    }
                    if (_turnGate != null)
                    {
                        // TurnGate は PlayerId を返す
                        var receivedPlayerId = await _turnGate.WaitNextAsync();
                        Debug.Log($"Dealer: TurnGate解放 - 受信PlayerId={receivedPlayerId}, 期待値={next.Id}");
                    }
                    Debug.Log($"Dealer: 次のターンは Player {next.Id}");
                }
            }
        }

        #region ネット待機ヘルパー
        /// <summary>
        /// ネットからの ActionResult を待機する（Guest手番向け）。
        /// </summary>
        private async UniTask<ActionResult> WaitActionResultFromNetworkAsync(IPlayer waitingPlayer, CancellationToken token)
        {
            if (waitingPlayer == null || _gameContext?.Events == null)
            {
                return ActionResult.Failure("待機対象またはイベントバスが無効です");
            }

            try
            {
                // R3のObservableで該当プレイヤーのActionResultを待機
                // FirstAsync()はTask<T>を返すので、直接awaitする
                var result = await _gameContext.Events.ActionResult
                    .FirstAsync(e => e.ActorPlayerId == waitingPlayer.PlayerId, token);

                // 成否はネット結果に合わせる
                return result.Accepted ? ActionResult.Success() : ActionResult.Failure(result.Reason);
            }
            catch (OperationCanceledException)
            {
                return ActionResult.Failure("ActionResult待機がキャンセルされました");
            }
        }
        #endregion

        public void EndGame()
        {
            // 進行中のプレイヤーアクションをキャンセル
            CancelCurrentPlayerAction();

            // ターン終了イベントを通知
            OnTurnEnd();

            // 状態をリセット
            // 手番や順序の最終状態はApplier/Contextが保持するため、ここでは直接変更しない

            // ActionAwaiter を破棄
            _actionAwaiter?.Dispose();

            if (_isGameInterrupted)
            {
                Debug.Log("Dealer: ゲームが途中中断されました");
            }

            // ゲームが途中中断されたかどうかをリセット
            _isGameInterrupted = false;

            OnGameEnd(); // ゲーム終了イベントを通知

            Debug.Log("Dealer: ゲームを終了しました");
        }





        /// <summary>
        /// 勝利条件をチェックする（仮実装）
        /// </summary>
        private bool CheckWinCondition()
        {
            // 設定された最大ラウンド数で勝利とする
            if (_roundCount >= _maxRounds)
            {
                Debug.Log($"Dealer: 最大ラウンド数({_maxRounds})に到達しました。ゲームを終了します。");
                return true;
            }

            return false;
        }

        // 既存インターフェース互換のオーバーロード
        public async UniTask StartTurnLoopAsync()
        {
            await StartTurnLoopAsync(default);
        }

        public async UniTask StartSingleTurnAsync()
        {
            await StartSingleTurnAsync(default);
        }

        #endregion

        #region イベント通知
        public void OnTurnStart()
        {
            _turnCount++;
            // PlayerId→ActorNumber変換を実行してから送信
            if (_playerIdMapper != null && _playerIdMapper.TryGetActorNumber(_gameContext.CurrentPlayer.Id, out var actorNumber))
            {
                _lifecycleEmitter?.Emit(new TurnStartedEvent { currentPlayerActorNumber = actorNumber });
            }
            else
            {
                Debug.LogWarning($"Dealer: PlayerId {_gameContext.CurrentPlayer.Id} のマッピングが見つかりません");
            }
            Debug.Log($"Dealer: ターン {_turnCount} を開始します");
            TurnStart?.Invoke();
        }
        public void OnTurnEnd()
        {            // 終了イベントのネットワーク送信（任意）
            if (_lifecycleEmitter is GameLifecycleEmitter gle && IsHost() && _gameContext.CurrentPlayer != null)
            {
                // PlayerId→ActorNumber変換を実行してから送信
                if (_playerIdMapper != null && _playerIdMapper.TryGetActorNumber(_gameContext.CurrentPlayer.Id, out var actorNumber))
                {
                    gle.EmitEnded(actorNumber);
                }
                else
                {
                    Debug.LogWarning($"Dealer: PlayerId {_gameContext.CurrentPlayer.Id} のマッピングが見つかりません");
                }
            }
            TurnEnd?.Invoke();
        }

        public void OnRoundStart()
        {
            _roundCount++;
            Debug.Log($"Dealer: ラウンド {_roundCount} を開始します");
            RoundStart?.Invoke();
        }
        public void OnRoundEnd()
        {
            RoundEnd?.Invoke();
        }

        public void OnGameStart()
        {
            GameStart?.Invoke();
        }

        public void OnGameEnd()
        {
            GameEnd?.Invoke();
        }

        #endregion

        #region アクション待機システム

        /// <summary>
        /// ActionSystemを初期化する
        /// </summary>
        private void InitializeActionSystem()
        {
            if (_gameContext == null)
            {
                Debug.LogWarning("Dealer: GameContext が未設定のため ActionSystem を初期化できませんでした");
                return;
            }
            // 新しいActionSystemInitializerを使用（コンテキストと自身のIRoundManagerを渡す）
            ActionSystemInitializer.InitializeActionSystem(_gameContext, this);

            _actionManager = ActionSystemInitializer.GetActionManager();
            if (_actionManager != null)
            {
                // ActionAwaiter を構築（タイムアウトハンドラーなし）
                _actionAwaiter = new ActionAwaiter(_actionManager);

                // ActionManagerにActionAwaiterを設定
                _actionManager.SetActionAwaiter(_actionAwaiter);

                Debug.Log("Dealer: ActionSystemが初期化されました (ActionSystemInitializer使用)");
            }
            else
            {
                Debug.LogError("Dealer: ActionSystemInitializerからActionManagerを取得できませんでした");
            }
        }


        /// <summary>
        /// 現在のプレイヤーアクション待機をキャンセルする
        /// </summary>
        public void CancelCurrentPlayerAction()
        {
            _actionAwaiter?.CancelWaiting();
        }

        #endregion

        #region バリデーションメソッド

        /// <summary>
        /// ゲーム開始前の前提条件を検証する
        /// </summary>
        /// <exception cref="InvalidOperationException">条件を満たさない場合</exception>
        private void ValidateStartGame()
        {
            if (_gameContext.Players == null || _gameContext.Players.Count == 0)
                throw new InvalidOperationException("Dealer: プレイヤーが存在しません");

            if (_gameContext.Stage == null)
                throw new InvalidOperationException("Dealer: Stage が未設定です");
        }

        /// <summary>
        /// 戦略の存在を検証する
        /// </summary>
        private void ValidateStrategy()
        {
            if (_dealerPlanner == null)
                throw new InvalidOperationException("Dealer: ディーラー戦略が設定されていません");
        }

        private bool ValidateCurrentPlayer()
        {
            if (_gameContext.UserPlayer == null)
            {
                Debug.LogError("Dealer: 現在のプレイヤーが設定されていません");
                return false;
            }
            return true;
        }

        private bool IsHost()
        {
            if (_networkContext == null)
            {
                Debug.LogWarning("Dealer: NetworkContextが設定されていません。falseを返します。");
                return false;
            }
            return _networkContext.IsHost;
        }

        #endregion
    }
}