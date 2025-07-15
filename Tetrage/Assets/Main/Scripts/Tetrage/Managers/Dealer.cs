using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Tetrage.Models;
using Tetrage.Core.Contracts;
using Tetrage.Core.DTO;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Actions;
using Tetrage.Factories;
using Tetrage.Services;
using System.Threading.Tasks;


/// <summary>
/// ゲームのディーラークラス。カードの配布、ターン管理、勝敗判定を行う。
/// </summary>
namespace Tetrage.Managers
{
    public class Dealer : IGameContextProvider, IRoundManager
    {

        #region イベント
        public event Action RoundStart;
        public event Action RoundEnd;

        #endregion

        #region フィールド
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
        /// ラウンド数
        /// </summary>
        private int _roundCount;
        public int RoundCount { get { return _roundCount; } }

        /// <summary>
        /// 最大ラウンド数（デフォルト: 10）
        /// </summary>
        private int _maxRounds = 10;
        public int MaxRounds { get { return _maxRounds; } }

        // プレイヤーアクション待機用
        private ActionAwaiter _actionAwaiter;   // ActionAwaiter に責任を委譲
        private ActionManager _actionManager;

        // タイムアウト処理用
        private ITimeoutHandler _timeoutHandler; // ActionAwaiter へ委譲予定

        // ゲーム終了フラグ
        private bool _gameFinished;

        // ゲームが途中中断されたかどうか
        private bool _gameInterrupted;

        #endregion

        #region コンストラクタ

        /// <summary>
        /// 戦略パターン対応のデフォルトコンストラクタ
        /// </summary>
        public Dealer(Stage stage, List<IPlayer> players, IDealerStrategy dealerStrategy)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            if (players == null || players.Count == 0) throw new ArgumentNullException(nameof(players));
            if (dealerStrategy == null) throw new ArgumentNullException(nameof(dealerStrategy));

            _stage = stage;
            _players = players;
            _dealerStrategy = dealerStrategy;
            _roundCount = 0; // 初期化
            _timeoutHandler = CreateDefaultTimeoutHandler();
            InitializeActionSystem();
            Debug.Log("Dealer: インスタンスが作成されました（戦略パターン対応）");
        }

        /// <summary>
        /// デフォルトのタイムアウトハンドラーを作成
        /// </summary>
        private ITimeoutHandler CreateDefaultTimeoutHandler()
        {
            // ファクトリーを使用してデフォルトハンドラーを作成
            return TimeoutHandlerFactory.CreateAutoPass();
        }

        /// <summary>
        /// タイムアウトハンドラーを設定する
        /// </summary>
        public void SetTimeoutHandler(ITimeoutHandler timeoutHandler)
        {
            _timeoutHandler = timeoutHandler ?? CreateDefaultTimeoutHandler();
            _actionAwaiter?.SetTimeoutHandler(_timeoutHandler);
            Debug.Log($"Dealer: タイムアウトハンドラーを変更しました - {_timeoutHandler.GetType().Name}");
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

        #region 初期化メソッド


        #endregion

        #region ラウンド管理

        public async UniTask StartGameAsync(float timeoutSeconds = 0)
        {
            // 前提条件を検証
            ValidateStartGame();
            ValidateStrategy();

            FirstDeal();

            Debug.Log("Dealer: ラウンドを開始します");

            _gameFinished = false;

            await StartRoundLoopAsync(timeoutSeconds);

            Debug.Log("Dealer: ゲームが終了します");

            EndGame();
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



        /// <summary>
        /// 勝敗が決まるまでラウンドを繰り返すメインループ
        /// </summary>
        /// <param name="cancellationToken">外部からゲーム全体をキャンセルしたい場合のトークン</param>
        public async UniTask StartRoundLoopAsync(float timeoutSeconds = 0, CancellationToken cancellationToken = default)
        {
            while (!_gameFinished && !cancellationToken.IsCancellationRequested)
            {

                try
                {
                    await StartSingleRoundAsync(timeoutSeconds, cancellationToken);
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    Debug.Log($"Dealer: ラウンドループがキャンセルされました: {ex.Message}");
                    _gameInterrupted = true;
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Dealer: ラウンドループ中に致命的エラーが発生: {ex.Message}");
                    _gameInterrupted = true;
                    _gameFinished = true; // 強制終了
                }
            }
        }

        /// <summary>
        /// 単一ラウンドを処理
        /// </summary>
        public async UniTask StartSingleRoundAsync(float timeoutSeconds = 0, CancellationToken cancellationToken = default)
        {
            OnRoundStart();

            try
            {
                // 現在のプレイヤーが設定されているかどうかを検証
                if (!ValidateCurrentPlayer()) return;

                // プレイヤーのアクションを待つ
                // タイムアウト時は自動で次のプレイヤーに移行する
                var actionResult = await _actionAwaiter.WaitForPlayerActionAsync(_currentPlayer, timeoutSeconds: timeoutSeconds);

                if (!actionResult.IsSuccess)
                {
                    Debug.LogWarning($"Dealer: プレイヤーアクション失敗 - {actionResult.ErrorMessage}");
                }

                actionResult.Log("Dealer: プレイヤーアクション結果");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Debug.Log($"Dealer: プレイヤー {_currentPlayer.PlayerId} のアクションがキャンセルされました");
                _gameFinished = true;
            }

            // 勝利条件チェック
            if (CheckWinCondition())
            {
                _gameFinished = true;
            }

            OnRoundEnd();

            // 次のプレイヤーへ
            if (!_gameFinished && _currentPlayer != null)
            {
                _currentPlayer = _dealerStrategy.GetNextPlayer(_currentPlayer, _players);
                Debug.Log($"Dealer: 次のターンは Player {_currentPlayer.PlayerId}");
            }
        }

        public void EndGame()
        {
            // 進行中のプレイヤーアクションをキャンセル
            CancelCurrentPlayerAction();

            // ラウンド終了イベントを通知
            OnRoundEnd();

            // 状態をリセット
            _currentPlayer = null;
            _dealerStrategy?.ResetTurnOrder(_players);

            // ActionAwaiter を破棄
            _actionAwaiter?.Dispose();

            if (_gameInterrupted)
            {
                Debug.Log("Dealer: ゲームが途中中断されました");
            }

            // ゲームが途中中断されたかどうかをリセット
            _gameInterrupted = false;

            Debug.Log("Dealer: ゲームを終了しました");
        }

        public void OnRoundStart()
        {
            _roundCount++;
            Debug.Log($"Dealer: ラウンド {_roundCount} を開始します");
            RoundStart?.Invoke();
        }

        public void OnRoundEnd()
        {
            Debug.Log($"Dealer: ラウンド {_roundCount} を終了します");
            RoundEnd?.Invoke();
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
        public async UniTask StartRoundLoopAsync()
        {
            await StartRoundLoopAsync(default);
        }

        public async UniTask StartSingleRoundAsync()
        {
            await StartSingleRoundAsync(default);
        }

        #endregion


        #region プレイヤーアクション待機システム

        /// <summary>
        /// ActionSystemを初期化する
        /// </summary>
        private void InitializeActionSystem()
        {
            // 新しいActionSystemInitializerを使用
            ActionSystemInitializer.InitializeActionSystem(this);

            _actionManager = ActionSystemInitializer.GetActionManager();
            if (_actionManager != null)
            {
                // ActionAwaiter を構築
                _actionAwaiter = new ActionAwaiter(_actionManager, _timeoutHandler);

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

        private bool ValidateCurrentPlayer()
        {
            if (_currentPlayer == null)
            {
                Debug.LogError("Dealer: 現在のプレイヤーが設定されていません");
                return false;
            }
            return true;
        }

        #endregion
    }
}