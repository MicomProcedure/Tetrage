using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Tetrage.Models;
using Tetrage.Core.Contracts;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Actions;


/// <summary>
/// ゲームのディーラークラス。カードの配布、ターン管理、勝敗判定を行う。
/// </summary>
namespace Tetrage.Managers
{
    public class Dealer : IGameContextProvider, IRoundManager
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
        public Dealer(Stage stage, List<IPlayer> players, IDealerStrategy dealerStrategy)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            if (players == null || players.Count == 0) throw new ArgumentNullException(nameof(players));
            if (dealerStrategy == null) throw new ArgumentNullException(nameof(dealerStrategy));

            _stage = stage;
            _players = players;
            _dealerStrategy = dealerStrategy;

            _roundCount = 0; // 初期化
            _turnCount = 0; // 初期化
            InitializeActionSystem();
            Debug.Log("Dealer: インスタンスが作成されました（戦略パターン対応）");
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

        public async UniTask StartGameAsync(float timeoutSeconds = 0)
        {
            // 前提条件を検証
            ValidateStartGame();
            ValidateStrategy();

            // 初めてラウンドを開始する際の処理（山札シャッフル、ターゲットカード設定、ターン順序初期化、最初のプレイヤーを決定）
            FirstDeal();

            // ゲーム開始イベントを通知
            OnGameStart();

            // ゲーム終了フラグをリセット
            _isGameFinished = false;

            await StartTurnLoopAsync(timeoutSeconds);

            Debug.Log("Dealer: ゲームが終了します");

            EndGame();
        }

        /// <summary>
        /// 初めてラウンドを開始する際の処理（山札シャッフル、ターゲットカード設定、ターン順序初期化、最初のプレイヤーを決定）
        /// </summary>
        public void FirstDeal()
        {
            // デッキ準備 & 配布
            // 山札シャッフル
            _dealerStrategy.ShuffleDeck(_stage.Stack);

            // 初期ターゲットカード設定
            _dealerStrategy.SetupInitialTargets(_players, _stage.Stack);

            // ターン順序初期化
            _dealerStrategy.ResetTurnOrder(_players);

            // 最初のプレイヤーを決定
            _currentPlayer = _dealerStrategy.DecideFirstPlayer(_players);
            Debug.Log($"Dealer: ゲーム開始 - 最初のプレイヤーは Player {_currentPlayer.PlayerId}");


        }



        /// <summary>
        /// 勝敗が決まるまでラウンドを繰り返すメインループ
        /// </summary>
        /// <param name="gameCts">外部からゲーム全体をキャンセルしたい場合のトークン</param>
        public async UniTask StartTurnLoopAsync(float timeoutSeconds = 0, CancellationToken gameCts = default)
        {
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
            // ラウンド開始イベントを通知
            OnTurnStart();

            try
            {
                // 現在のプレイヤーが設定されているかどうかを検証
                if (!ValidateCurrentPlayer()) return;

                // プレイヤーのアクションを待つ
                var actionResult = await _actionAwaiter.WaitForPlayerActionAsync(_currentPlayer);

                if (!actionResult.IsSuccess)
                {
                    Debug.LogWarning($"Dealer: プレイヤーアクション失敗 - {actionResult.ErrorMessage}");
                }

                actionResult.Log("Dealer: プレイヤーアクション結果");
            }
            catch (OperationCanceledException) when (gameCts.IsCancellationRequested)
            {
                Debug.Log($"Dealer: プレイヤー {_currentPlayer.PlayerId} のアクションがキャンセルされました");
                _isGameFinished = true;
            }

            // 勝利条件チェック
            if (CheckWinCondition())
            {
                _isGameFinished = true;
            }

            OnTurnEnd();

            // 次のプレイヤーへ
            if (!_isGameFinished && _currentPlayer != null)
            {
                _currentPlayer = _dealerStrategy.GetNextPlayer(_currentPlayer, _players);
                Debug.Log($"Dealer: 次のターンは Player {_currentPlayer.PlayerId}");
            }
        }

        public void EndGame()
        {
            // 進行中のプレイヤーアクションをキャンセル
            CancelCurrentPlayerAction();

            // ターン終了イベントを通知
            OnTurnEnd();

            // 状態をリセット
            _currentPlayer = null;
            _dealerStrategy?.ResetTurnOrder(_players);

            // ActionAwaiter を破棄
            _actionAwaiter?.Dispose();

            if (_isGameInterrupted)
            {
                Debug.Log("Dealer: ゲームが途中中断されました");
            }

            // ゲームが途中中断されたかどうかをリセット
            _isGameInterrupted = false;

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
            Debug.Log($"Dealer: ターン {_turnCount} を開始します");
            TurnStart?.Invoke();
        }
        public void OnTurnEnd()
        {
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
            // 新しいActionSystemInitializerを使用
            ActionSystemInitializer.InitializeActionSystem(this);

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