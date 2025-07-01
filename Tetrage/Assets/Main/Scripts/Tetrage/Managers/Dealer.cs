using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Tetrage.Models;
using Tetrage.Core.Contracts;
using Tetrage.Core.DTO;
using Tetrage.Core.Constants;
using Tetrage.Managers.DealerStrategies;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Actions;
using Tetrage.Core.Enums;
using System.Threading.Tasks;

/// <summary>
/// ゲームのディーラークラス。カードの配布、ターン管理、勝敗判定を行う。
/// </summary>
namespace Tetrage.Managers
{
    public class Dealer : IGameContextProvider, IRoundManager
    {

        #region <---- イベント ---->
        public event Action RoundStart;
        public event Action RoundEnd;

        #endregion

        #region <---- フィールド ---->
        /// <summary>
        /// 現在のステージ情報。
        /// </summary>
        private Stage _stage;
        public Stage Stage { get { return _stage; } }

        /// <summary>
        /// ゲームに参加しているプレイヤーのリスト。
        /// </summary>
        private List<IPlayer> _players;
        public IReadOnlyList<IPlayer> Players => _players;

        /// <summary>
        /// 現在のプレイヤー。
        /// </summary>
        private IPlayer _currentPlayer;
        public IPlayer CurrentPlayer { get { return _currentPlayer; } }

        /// <summary>
        /// ディーラー戦略
        /// </summary>
        private IDealerStrategy _dealerStrategy;
        public IDealerStrategy DealerStrategy { get { return _dealerStrategy; } }

        /// <summary>
        /// 参加者情報リスト
        /// </summary>
        private List<PlayerInfo> _participantInfoList;
        public List<PlayerInfo> ParticipantInfoList { get { return _participantInfoList; } }

        /// <summary>
        /// ラウンド数
        /// </summary>
        private int _roundCount;
        public int RoundCount { get { return _roundCount; } }

        // プレイヤーアクション待機用
        private UniTaskCompletionSource<ActionResult> _actionCompletionSource;
        private CancellationTokenSource _actionCancellationTokenSource;
        private ActionManager _actionManager;

        #endregion

        #region <---- コンストラクタ ---->
        /// <summary>
        /// Dealerクラスのコンストラクタ
        /// </summary>
        public Dealer(List<PlayerInfo> participantInfoList)
        {
            _players = new List<IPlayer>();
            _participantInfoList = participantInfoList;
            _roundCount = 0; // 初期化
            InitializeActionSystem();
            Debug.Log("Dealer: インスタンスが作成されました");
        }

        /// <summary>
        /// 戦略パターン対応コンストラクタ
        /// </summary>
        public Dealer(IDealerStrategy dealerStrategy, List<PlayerInfo> participantInfoList)
        {
            _dealerStrategy = dealerStrategy;
            _participantInfoList = participantInfoList;
            _roundCount = 0; // 初期化
            InitializeActionSystem();
        }
        #endregion

        #region <---- 初期化メソッド ---->
        /// <summary>
        /// Stageを設定する（初期化用）
        /// </summary>
        public void SetStage(Stage stage)
        {
            _stage = stage;
        }

        /// <summary>
        /// プレイヤーリストを設定する（初期化用）
        /// </summary>
        public void SetPlayers(List<IPlayer> players)
        {
            _players = players ?? new List<IPlayer>();
        }

        /// <summary>
        /// 現在のプレイヤーを設定する
        /// </summary>
        public void SetCurrentPlayer(IPlayer player)
        {
            _currentPlayer = player;
        }

        #endregion

        #region <---- ラウンド管理 ---->

        public void StartGame()
        {
            // 前提条件を検証
            ValidateStartGame();
            ValidateStrategy();

            FirstDeal();

            StartRound().Forget();
            Debug.Log("Dealer: ラウンドを開始しました");
        }

        /// <summary>
        /// 初めてラウンドを開始する際の処理（山札シャッフル、ターゲットカード設定、ターン順序初期化、最初のプレイヤーを決定）
        /// </summary>
        public void FirstDeal()
        {
            #region デッキ準備 & 配布
            // 山札シャッフル
            _dealerStrategy.ShuffleDeck(_stage.Stack);

            // 初期ターゲットカード設定
            _dealerStrategy.SetupInitialTargets(_players, _stage.Stack);
            #endregion

            #region ターン順序初期化
            _dealerStrategy.ResetTurnOrder(_players);

            // 最初のプレイヤーを決定
            _currentPlayer = _dealerStrategy.DecideFirstPlayer(_players);
            Debug.Log($"Dealer: ゲーム開始 - 最初のプレイヤーは Player {_currentPlayer.PlayerId}");
            #endregion

        }

        public async UniTaskVoid StartRound()
        {
            OnRoundStart();

            // プレイヤーのアクションを待つ
            var actionResult = await WaitForPlayerActionAsync();

            if (actionResult.IsSuccess)
            {
                Debug.Log($"Dealer: プレイヤー {_currentPlayer.PlayerId} のアクション完了 - {actionResult.AdditionalData}");

                // アクション完了後の処理（次のターンへ進むなど）
                await ProcessActionCompletionAsync(actionResult);
            }
            else
            {
                Debug.LogWarning($"Dealer: プレイヤーアクション失敗またはキャンセル - {actionResult.ErrorMessage}");
            }
        }


        public void EndGame()
        {
            // 進行中のプレイヤーアクションをキャンセル
            CancelCurrentPlayerAction();

            // ActionManagerのイベント購読を解除
            if (_actionManager != null)
            {
                _actionManager.OnActionCompleted -= OnPlayerActionCompleted;
            }

            // ラウンド終了イベントを通知
            OnRoundEnd();

            // 状態をリセット
            _currentPlayer = null;
            _dealerStrategy?.ResetTurnOrder(_players);

            Debug.Log("Dealer: ゲームを終了しました");
        }

        public void OnRoundStart()
        {
            _roundCount++;
            RoundStart?.Invoke();
        }

        public void OnRoundEnd()
        {
            RoundEnd?.Invoke();
        }

        #endregion

        #region <---- プレイヤーアクション待機システム ---->

        /// <summary>
        /// ActionSystemを初期化する
        /// </summary>
        private void InitializeActionSystem()
        {
            _actionManager = ActionManager.Instance;
            if (_actionManager != null)
            {
                _actionManager.SetGameContextProvider(this);
                ActionFactory.RegisterAllActions(_actionManager);

                // アクション完了イベントを購読
                _actionManager.OnActionCompleted += OnPlayerActionCompleted;

                Debug.Log("Dealer: ActionSystemが初期化されました");
            }
        }

        /// <summary>
        /// プレイヤーのアクション完了を待機する
        /// </summary>
        /// <param name="timeoutSeconds">タイムアウト時間（秒）、0で無制限</param>
        /// <returns>アクション実行結果</returns>
        public async UniTask<ActionResult> WaitForPlayerActionAsync(float timeoutSeconds = 0)
        {
            if (_currentPlayer == null)
            {
                return ActionResult.Failure("現在のプレイヤーが設定されていません");
            }

            // 既存の待機をキャンセル
            CancelCurrentPlayerAction();

            // 新しい待機を開始
            _actionCompletionSource = new UniTaskCompletionSource<ActionResult>();
            _actionCancellationTokenSource = new CancellationTokenSource();

            try
            {
                Debug.Log($"Dealer: プレイヤー {_currentPlayer.PlayerId} のアクションを待機中...");

                UniTask<ActionResult> waitTask = _actionCompletionSource.Task;

                // タイムアウト設定
                if (timeoutSeconds > 0)
                {
                    var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_actionCancellationTokenSource.Token);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                    try
                    {
                        return await waitTask.AttachExternalCancellation(timeoutCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !_actionCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return ActionResult.Failure($"プレイヤーアクションがタイムアウトしました（{timeoutSeconds}秒）");
                    }
                    finally
                    {
                        timeoutCts?.Dispose();
                    }
                }

                return await waitTask;
            }
            catch (OperationCanceledException)
            {
                return ActionResult.Failure("プレイヤーアクションがキャンセルされました");
            }
            finally
            {
                CleanupActionWaiting();
            }
        }

        /// <summary>
        /// プレイヤーアクション完了時のイベントハンドラー
        /// </summary>
        private void OnPlayerActionCompleted(IAction action, IActionContext context, ActionResult result)
        {
            // 現在のプレイヤーのアクションかチェック
            if (_actionCompletionSource != null &&
                ReferenceEquals(context.RequesterPlayer, _currentPlayer))
            {
                Debug.Log($"Dealer: プレイヤー {_currentPlayer.PlayerId} のアクション {action.ActionType} が完了");
                _actionCompletionSource.TrySetResult(result);
            }
        }

        /// <summary>
        /// アクション完了後の処理
        /// </summary>
        private async UniTask ProcessActionCompletionAsync(ActionResult actionResult)
        {
            // アクション完了後のゲームロジック
            // 例：勝利条件チェック、次のターンへの移行など

            await UniTask.Delay(500); // UI更新の時間を確保

            // 勝利条件チェック（仮実装）
            if (CheckWinCondition())
            {
                EndGame();
                return;
            }

            // 次のターンへ進む
            NextTurn();

            // 次のプレイヤーのラウンドを開始
            if (_currentPlayer != null)
            {
                StartRound().Forget();
            }
        }

        /// <summary>
        /// 現在のプレイヤーアクション待機をキャンセルする
        /// </summary>
        public void CancelCurrentPlayerAction()
        {
            if (_actionCancellationTokenSource != null && !_actionCancellationTokenSource.Token.IsCancellationRequested)
            {
                _actionCancellationTokenSource.Cancel();
            }

            if (_actionCompletionSource != null)
            {
                _actionCompletionSource.TrySetCanceled();
            }

            CleanupActionWaiting();
        }

        /// <summary>
        /// アクション待機の後処理
        /// </summary>
        private void CleanupActionWaiting()
        {
            _actionCompletionSource = null;
            _actionCancellationTokenSource?.Dispose();
            _actionCancellationTokenSource = null;
        }

        /// <summary>
        /// 勝利条件をチェックする（仮実装）
        /// </summary>
        private bool CheckWinCondition()
        {
            // TODO: 実際の勝利条件ロジックを実装
            return false;
        }

        #endregion

        #region <---- ゲームロジック ---->


        /// <summary>
        /// 次のプレイヤーのターンに移行する。
        /// </summary>
        public void NextTurn()
        {
            try
            {
                ValidateStrategy();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError(e.Message);
                return;
            }

            if (_currentPlayer == null)
            {
                Debug.LogWarning("Dealer: 現在のプレイヤーが未設定のため、StartGame() を呼び出します");
                StartGame();
                return;
            }

            // 次のプレイヤーを取得
            _currentPlayer = _dealerStrategy.GetNextPlayer(_currentPlayer, _players);
            Debug.Log($"Dealer: 次のターンは Player {_currentPlayer.PlayerId}");
        }
        #endregion

        #region <---- バリデーションメソッド ---->

        /// <summary>
        /// ゲーム開始前の前提条件を検証する
        /// </summary>
        /// <exception cref="InvalidOperationException">条件を満たさない場合</exception>
        private void ValidateStartGame()
        {
            if (_players == null || _players.Count == 0)
                throw new InvalidOperationException("Dealer: プレイヤーが存在しません");

            if (_stage == null)
                throw new InvalidOperationException("Dealer: Stage が未設定です");
        }

        /// <summary>
        /// 戦略の存在を検証する
        /// </summary>
        private void ValidateStrategy()
        {
            if (_dealerStrategy == null)
                throw new InvalidOperationException("Dealer: ディーラー戦略が設定されていません");
        }

        #endregion
    }
}