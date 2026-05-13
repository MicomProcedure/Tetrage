using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Actions;
using Tetrage.Network.Gameplay;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Core;
using R3;
using Tetrage.Core.Constants;
using DomainEvents = Tetrage.Core.Events;

/// <summary>
/// ゲームのディーラークラス。カードの配布、ターン管理、勝敗判定を行う。
/// </summary>
namespace Tetrage.Managers
{
    public class Dealer : IRoundManager
    {


        #region フィールド


        /// <summary>
        /// ディーラー戦略
        /// </summary>
        private IDealerPlanner _dealerPlanner;
        public IDealerPlanner DealerPlanner { get { return _dealerPlanner; } set { _dealerPlanner = value; } }

        /// <summary>
        /// ディーラー戦略のイベント発行用メッセンジャー
        /// </summary>
        private DealerNetworkMessenger _messenger;
        public DealerNetworkMessenger Messenger => _messenger;

        private TurnGate _turnGate; // Hostのみ使用
        private IGameContext _gameContext; // 読み取り専用のコンテキスト
        private INetworkContext _networkContext; // ネットワーク状態の抽象化
        private IPlayerIdMapper _playerIdMapper;
        private IPlayer _initialTurnPlayer;
        private readonly CompositeDisposable _disposables = new();

        private const float ScanSelectionTimeoutSeconds = SettingConsts.SCAN_SELECTION_TIMEOUT_SECONDS;

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
        public Dealer(
            IGameContext gameContext,
            IDealerPlanner dealerPlanner,
            INetworkContext networkContext,
            IPlayerIdMapper playerIdMapper)
        {
            if (gameContext == null) throw new ArgumentNullException(nameof(gameContext));
            if (dealerPlanner == null) throw new ArgumentNullException(nameof(dealerPlanner));

            _gameContext = gameContext;
            _dealerPlanner = dealerPlanner;
            _networkContext = networkContext;
            _playerIdMapper = playerIdMapper;
            _roundCount = 0; // 初期化
            _turnCount = 0; // 初期化

            InitializeActionSystem();
            SubscribeDomainEvent();
            // GameContext は GameManager 側で生成後に注入されるため、ActionSystem 初期化は後段で行う
            Debug.Log("Dealer: インスタンスが作成されました（戦略パターン対応）");
        }

        #endregion

        #region 設定メソッド

        /// <summary>
        /// 後からMessengerを差し替える（GameManagerのネットワーク初期化完了後に注入）。
        /// Hostのみ設定。Guestはnullのまま。
        /// </summary>
        public void SetMessenger(DealerNetworkMessenger messenger)
        {
            if (!_networkContext.IsHost) return;
            _messenger = messenger;
        }

        /// <summary>
        /// TurnGate を注入（Hostのみ）。GameplayNetworkController から取得して渡す想定。
        /// </summary>
        public void SetTurnGate(TurnGate gate)
        {
            if (!_networkContext.IsHost) return;
            _turnGate = gate;
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


            // ゲーム終了フラグをリセット
            _isGameFinished = false;

            // ScanPhase（FirstDeal後・最初のターン開始前）
            await ExecuteScanPhaseAsync(gameCts);

            if (_initialTurnPlayer != null && IsHost())
            {
                _messenger?.PublishTurnStarted(_initialTurnPlayer.Id);
            }

            // 初回のTurnStartedが適用されるまで待機（CurrentPlayerが設定されるまで）
            if (_turnGate != null && _initialTurnPlayer != null)
            {
                await _turnGate.WaitNextAsync();
            }

            await StartTurnLoopAsync(timeoutSeconds, gameCts);

            Debug.Log("Dealer: ゲームが終了します");
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
            _messenger?.PublishDealerPlan(shufflePlan);

            // 2) 初期ターゲット設定
            var targetPlan = _dealerPlanner.PlanTargetSetup(_gameContext.Players, _gameContext.Stage.Stack);
            _messenger?.PublishDealerPlan(targetPlan);

            // 3) ターン順序初期化（プランにTurnOrderを含め、Emitterで送信）
            var orderPlan = _dealerPlanner.PlanResetTurnOrder(_gameContext.Players);
            _messenger?.PublishDealerPlan(orderPlan);

            // 4) 最初のプレイヤーを決定
            var firstPlayer = _dealerPlanner.DecideFirstPlayer(_gameContext.Players);
            _initialTurnPlayer = firstPlayer;
            Debug.Log($"Dealer: ゲーム開始 - 最初のプレイヤーは Player {firstPlayer.Id}");
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
            PublishTurnStart();

            ActionResult actionResult = ActionResult.Failure("アクションが実行されませんでした");
            try
            {
                // 現在のプレイヤーが設定されているかどうかを検証
                if (!ValidateCurrentPlayer()) return;

                // プレイヤーのアクションを待つ（現在手番のプレイヤー）
                // Hostの自手番は ActionAwaiter、Guest手番はネットのActionResultを待機
                var isLocalTurn = _gameContext.UserPlayer != null && ReferenceEquals(_gameContext.CurrentPlayer, _gameContext.UserPlayer);
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
            catch (Exception ex)
            {
                Debug.LogError($"Dealer: アクション実行中に致命的エラーが発生: {ex.Message}");
                _isGameInterrupted = true;
                _isGameFinished = true; // 強制終了
            }

            // TetrageSoloかTetrageMultiの場合のみゲーム終了を判定し、ループを抜ける
            if (HandleGameEndingByAction(actionResult))
            {
                return;
            }

            PublishTurnEnd();


            // 次のプレイヤーへ
            if (!_isGameFinished && _gameContext.CurrentPlayer != null)
            {
                // 次手番を決定し、TurnStarted を発行（Emitter経由）し、適用完了を待つ
                var next = _dealerPlanner.GetNextPlayer(_gameContext.CurrentPlayer, _gameContext.Players);
                if (next != null && IsHost())
                {
                    // 次手番を宣言
                    _messenger?.PublishTurnStarted(next.Id);

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
                // ResponseRequested は参加応答収集の中間通知のため除外し、最終結果のみを受け取る
                var result = await _gameContext.Events.ActionResult
                    .FirstAsync(e => e.ActorPlayerId    == waitingPlayer.PlayerId
                                  && e.ActionStatusInt  != Core.Constants.InGameConsts.TetrageMultiStatus.ResponseRequested,
                                token);

                var descriptor = new ActionRequestDescriptorPacket
                {
                    actionType = result.ActionType,
                    actorPlayerId = result.ActorPlayerId.Value,
                    targetCardIds = result.TargetCardIds?.Select(id => id).ToArray(),
                    actionStatusInt = result.ActionStatusInt
                };

                // 成否はネット結果に合わせる
                return result.Accepted
                    ? ActionResult.Success(descriptor)
                    : ActionResult.Failure(result.Reason, descriptor);
            }
            catch (OperationCanceledException)
            {
                return ActionResult.Failure("ActionResult待機がキャンセルされました");
            }
        }
        #endregion

        #region ScanPhase

        private async UniTask ExecuteScanPhaseAsync(CancellationToken token)
        {
            if (!IsHost() || _messenger == null || _gameContext?.Players == null || _gameContext.Players.Count <= 1)
            {
                return;
            }

            // HostがScanPhaseを開始
            var playerIds = _gameContext.Players.Select(player => player.Id).ToList();
            _messenger.PublishScanPhaseStart(_gameContext.UserPlayer.Id, playerIds);    // 全プレイヤーにScanPhaseStartを通知

            var allSelections = await CollectAllPlayerScanSelectionsAsync(ScanSelectionTimeoutSeconds, token); // 全プレイヤーのScanTargetSelectedを集める
            foreach (var pair in allSelections)
            {
                if (!TryResolvePlayerById(pair.Value, out var selectedTarget)) continue;
                var targetCard = selectedTarget.Target.FirstOrDefault();
                if (targetCard == null) continue;

                _messenger.PublishScanResultToActor(
                    receiverPlayerId: pair.Key,
                    targetPlayerId: pair.Value,
                    targetSuit: targetCard.Suit);
            }

            _messenger.PublishScanPhaseEnd();
        }

        /// <summary>
        /// 全席分の ScanTargetSelected を集める。Host/Guest とも UI から送信されたイベントを待つ。
        /// </summary>
        private async UniTask<Dictionary<PlayerId, PlayerId>> CollectAllPlayerScanSelectionsAsync(float timeoutSeconds, CancellationToken token)
        {
            var selections = new Dictionary<PlayerId, PlayerId>();
            var allPlayers = _gameContext.Players.ToList();
            if (allPlayers.Count == 0)
            {
                return selections;
            }

            var playerIdSet = new HashSet<PlayerId>(allPlayers.Select(p => p.Id));

            using var disposables = new CompositeDisposable();
            _gameContext.Events.ScanTargetSelected
                .Subscribe(e =>
                {
                    // アクターのプレイヤーIDが有効かどうかを確認
                    if (!playerIdSet.Contains(e.ActorPlayerId)) return;
                    // 選択されたターゲットのプレイヤーIDが有効かどうかを確認
                    if (!playerIdSet.Contains(e.SelectedTargetPlayerId)) return;
                    // 自分自身をターゲットに選択していないかを確認
                    if (e.SelectedTargetPlayerId == e.ActorPlayerId) return;
                    // ターゲットのプレイヤーIDが実際に存在しているかを確認
                    if (!TryResolvePlayerById(e.SelectedTargetPlayerId, out _)) return;
   

                    selections[e.ActorPlayerId] = e.SelectedTargetPlayerId;
                    Debug.Log($"Dealer: ScanTargetSelected 受信 actor={e.ActorPlayerId}, target={e.SelectedTargetPlayerId}");
                })
                .AddTo(disposables);

            var deadline = Time.realtimeSinceStartup + Mathf.Max(0f, timeoutSeconds);   // タイムアウト時間を設定
            while (!token.IsCancellationRequested && selections.Count < allPlayers.Count)   // 全プレイヤーの選択が完了するまで待機
            {
                if (Time.realtimeSinceStartup >= deadline)   // タイムアウト時間を超えた場合
                {
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);   // 更新ループを待機
            }

            if (token.IsCancellationRequested)   // キャンセルされた場合
            {
                return selections;
            }

            foreach (var player in allPlayers)   // 全プレイヤーに対してデフォルトのターゲットを適用
            {
                if (selections.ContainsKey(player.Id)) continue;
                var fallbackTarget = SelectDefaultScanTarget(player.Id);   // デフォルトのターゲットを選択
                if (fallbackTarget == null) continue;
                selections[player.Id] = fallbackTarget.Id;
                Debug.LogWarning($"Dealer: ScanPhase選択タイムアウトのため、Player {player.Id} にデフォルト対象 {fallbackTarget.Id} を適用しました");
            }

            return selections;   // 全プレイヤーの選択結果を返す
        }

        private IPlayer SelectDefaultScanTarget(PlayerId actorPlayerId)
        {
            return _gameContext.Players.FirstOrDefault(player => player.Id != actorPlayerId);
        }

        #endregion

        #region Game Ending

        /// <summary>
        /// アクション結果によってゲーム終了を判定する
        /// </summary>
        /// <param name="actionResult">アクション結果</param>
        /// <returns>ゲーム終了フラグ</returns>
        private bool HandleGameEndingByAction(ActionResult actionResult)
        {
            if (!TryGetActionDescriptor(actionResult, out var descriptor))
            {
                return false;
            }

            // TetrageSoloかTetrageMultiの場合のみゲーム終了を判定する
            if (descriptor.actionType != ActionType.TetrageSolo && descriptor.actionType != ActionType.TetrageMulti)
            {
                return false;
            }

            var winners = BuildWinnersFromActionResult(descriptor);
            _messenger?.PublishFinishingGame(winners);
            _messenger?.PublishGameEnded(winners);
            return true;
        }

        private bool TryGetActionDescriptor(ActionResult actionResult, out ActionRequestDescriptorPacket descriptor)
        {
            if (actionResult?.AdditionalData is ActionRequestDescriptorPacket typed)
            {
                descriptor = typed;
                return true;
            }

            descriptor = default;
            return false;
        }

        private List<PlayerId> BuildWinnersFromActionResult(ActionRequestDescriptorPacket descriptor)
        {
            if (descriptor.actionType == ActionType.TetrageSolo)
            {
                return EvaluateSoloWinners(descriptor.actorPlayerId);
            }

            if (descriptor.actionType == ActionType.TetrageMulti)
            {
                return EvaluateMultiWinners(descriptor.actorPlayerId, descriptor.targetCardIds, descriptor.actionStatusInt);
            }

            return new List<PlayerId>();
        }

        private List<PlayerId> EvaluateSoloWinners(int actorPlayerId)
        {
            var actorId = new PlayerId(actorPlayerId);
            if (!TryResolvePlayerById(actorId, out var actorPlayer))
            {
                return new List<PlayerId>();
            }

            var actorCard = actorPlayer.Target.FirstOrDefault();
            if (actorCard == null)
            {
                return new List<PlayerId>();
            }

            var matchedPlayerIds = _gameContext.Players
                .Where(player => player.Id != actorId)
                .Where(player =>
                {
                    var card = player.Target.FirstOrDefault();
                    return card != null && card.Suit == actorCard.Suit;
                })
                .Select(player => player.Id)
                .ToList();

            if (matchedPlayerIds.Count == 0)
            {
                return new List<PlayerId> { actorId };
            }

            var losers = new HashSet<PlayerId>(matchedPlayerIds) { actorId };
            return _gameContext.Players
                .Where(player => !losers.Contains(player.Id))
                .Select(player => player.Id)
                .ToList();
        }

        /// <summary>
        /// TetrageMulti の勝者を判定する。
        /// openedTargetCardIds: Host が最終 ActionResult に載せた「出した参加者」の Target カード ID。
        /// actionStatusInt 1 = 成功（参加者全員同スート）、0 = 失敗。
        /// </summary>
        private List<PlayerId> EvaluateMultiWinners(int actorPlayerId, CardId[] openedTargetCardIds, int actionStatusInt)
        {
            var actorId = new PlayerId(actorPlayerId);
            if (!TryResolvePlayerById(actorId, out var actorPlayer))
                return new List<PlayerId>();

            var actorCard = actorPlayer.Target.FirstOrDefault();
            if (actorCard == null)
                return new List<PlayerId>();

            // 「出した参加者」を cardId セットで解決する
            var openCardIdSet  = openedTargetCardIds != null ? new HashSet<CardId>(openedTargetCardIds) : new HashSet<CardId>();
            var openPlayerIds  = _gameContext.Players
                .Where(p => p.Id != actorId)
                .Where(p =>
                {
                    var c = p.Target.FirstOrDefault();
                    return c != null && openCardIdSet.Contains(c.Id);
                })
                .Select(p => p.Id)
                .ToHashSet();

            // 成功: 参加者（宣言者 + 出したプレイヤー）全員が勝者
            if (actionStatusInt == 1)
            {
                var winners = new List<PlayerId> { actorId };
                winners.AddRange(openPlayerIds);
                return winners;
            }

            // 失敗時の勝者判定
            var openSuits = _gameContext.Players
                .Where(p => openPlayerIds.Contains(p.Id) || p.Id == actorId)
                .Select(p => p.Target.FirstOrDefault()?.Suit)
                .Where(s => s.HasValue)
                .Select(s => s.Value)
                .ToHashSet();

            var result = new List<PlayerId>();
            foreach (var player in _gameContext.Players)
            {
                var card = player.Target.FirstOrDefault();
                if (card == null) continue;

                if (openPlayerIds.Contains(player.Id))
                {
                    // 出した参加者: 宣言者とスートが違う場合のみ勝者
                    if (card.Suit != actorCard.Suit)
                        result.Add(player.Id);
                }
                else if (player.Id != actorId)
                {
                    // 出さなかったプレイヤー: 全 open 参加者のスートと異なる場合のみ勝者
                    if (!openSuits.Contains(card.Suit))
                        result.Add(player.Id);
                }
                // 宣言者は失敗時には勝者にならない
            }

            return result.Distinct().ToList();
        }

        #endregion

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

        /// <summary>
        /// ターンカウントをインクリメントする。
        /// TurnStartedのネットワーク送信はFirstDeal()または前ターン末尾で行われるため、
        /// ここではカウント管理のみ行う。
        /// </summary>
        public void PublishTurnStart()
        {
            _turnCount++;
            Debug.Log($"Dealer: ターン {_turnCount} を開始します (CurrentPlayer: {_gameContext.CurrentPlayer?.PlayerId})");
        }
        public void PublishTurnEnd()
        {            // 終了イベントのネットワーク送信（任意）
            if (_messenger != null && IsHost() && _gameContext.CurrentPlayer != null)
            {
                // ターン終了イベントを送信
                _messenger.PublishTurnEnded(_gameContext.CurrentPlayer.Id);
            }
        }

        public void OnRoundStart()
        {
            _roundCount++;
            Debug.Log($"Dealer: ラウンド {_roundCount} を開始します");
        }
        
        public void OnRoundEnd()
        {
        }


        #endregion

        #region イベント購読

        private void SubscribeDomainEvent()
        {
            _gameContext.Events.GameEnded
                .Subscribe(OnGameEnded)
                .AddTo(_disposables);
        }

        private void OnGameEnded(DomainEvents.GameEndedEvent e)
        {
            if (_isGameFinished) return;

            // GameEnded受信を終了状態への遷移点として、Dealerの後始末もここで完了する。
            _isGameFinished = true; // ゲーム終了フラグをセット
            CancelCurrentPlayerAction();
            PublishTurnEnd();

            // 手番や順序の最終状態はApplier/Contextが保持するため、ここでは直接変更しない。
            _actionAwaiter?.Dispose();

            if (_isGameInterrupted)
            {
                Debug.Log("Dealer: ゲームが途中中断されました");
            }

            _isGameInterrupted = false; // ゲーム中断フラグをリセット
            Debug.Log("Dealer: ゲームを終了しました");
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

        private bool TryResolvePlayerById(PlayerId playerId, out IPlayer player)
        {
            player = _gameContext.Players.FirstOrDefault(p => p.Id == playerId);
            return player != null;
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