using UnityEngine;
using Tetrage.Core.Enums;
using Tetrage.Core.DTO;
using Tetrage.Components;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Tetrage.Managers
{
    public class GameManager : MonoBehaviour
    {



        #region 設定コンポーネント
        [Header("Field Setup Configuration")]
        [SerializeField] private FieldSetupComponent _fieldSetupComponent;
        [SerializeField] private GameMode _gameMode = GameMode.Debug;

        #endregion

        #region 管理対象インスタンス
        private FieldSetupManager _fieldSetupManager;
        private Dealer _dealer;

        #endregion

        #region 状態管理
        private bool _isInitialized = false;
        private bool _isGameRunning = false;
        private CancellationTokenSource _gameCts;

        #endregion

        #region プロパティ
        /// <summary>セットアップが完了したDealer</summary>
        public Dealer Dealer
        {
            get
            {
                if (!_isInitialized)
                {
                    throw new System.InvalidOperationException("GameManager が初期化されていません。");
                }
                return _dealer;
            }
        }

        /// <summary>ゲーム実行中かどうか</summary>
        public bool IsGameRunning => _isGameRunning;

        /// <summary>初期化済みかどうか</summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region ライフサイクル


        #endregion

        #region 初期化メソッド
        /// <summary>
        /// GameManagerの初期化
        /// </summary>
        /// <param name="participantInfoList">参加者情報リスト</param>
        /// <param name="dealerStrategy">DealerStrategy</param>
        public void Initialize(List<PlayerInfo> participantInfoList)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("GameManager: 既に初期化済みです");
                return;
            }

            try
            {
                // 1. FieldSetupManagerの生成
                _fieldSetupManager = CreateFieldSetupManager(participantInfoList.Count);

                // 2. フィールドのセットアップ
                _fieldSetupManager.SetupField(participantInfoList);

                // 3. Dealerの生成と初期化
                _dealer = DealerFactory.CreateDealer(_fieldSetupManager, _gameMode);

                _isInitialized = true;

                EventSubscribe();

                Debug.Log("GameManager: 初期化が完了しました");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameManager: 初期化中にエラーが発生: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// FieldSetupManagerを生成する
        /// </summary>
        /// <param name="participantCount">参加者数</param>
        private FieldSetupManager CreateFieldSetupManager(int participantCount)
        {
            if (_fieldSetupComponent == null)
            {
                throw new System.InvalidOperationException("FieldSetupComponent が見つかりません。Inspector で設定してください。");
            }

            // FieldSetupComponentから検証済みの設定を取得
            var settings = _fieldSetupComponent.GetValidatedFieldSetupSettings(participantCount);
            var dependencies = _fieldSetupComponent.CreateFieldSetupDependencies();

            return new FieldSetupManager(settings, dependencies);
        }

        private void EventSubscribe()
        {
            _dealer.GameEnd += OnGameEnd;
        }

        private void EventUnsubscribe()
        {
            if (_dealer != null)
            {
                _dealer.GameEnd -= OnGameEnd;
            }
        }


        #endregion

        #region ゲーム制御メソッド
        public async UniTask StartGame()
        {
            if (!_isInitialized)
            {
                Debug.LogError("GameManager: 初期化されていません");
                return;
            }

            if (_isGameRunning)
            {
                Debug.LogWarning("GameManager: ゲームが既に実行中です");
                return;
            }

            try
            {
                _isGameRunning = true;
                _gameCts = new CancellationTokenSource();
                
                await _dealer.StartGameAsync(0f, _gameCts.Token);
                Debug.Log("GameManager: ゲームが正常に終了しました");
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("GameManager: ゲームがキャンセルされました");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameManager: ゲーム実行中にエラーが発生: {ex.Message}");
                throw;
            }
            finally
            {
                _isGameRunning = false;
            }
        }

        #endregion

        #region イベントハンドラ

        private void OnGameEnd()
        {
            Debug.Log("GameManager: ゲーム終了イベントを受信");
            _isGameRunning = false;
            EventUnsubscribe();
        }

        #endregion

        #region Unity固有メソッド
        private void Update()
        {
            // 必要に応じてフェーズ管理のUpdate処理
        }

        /// <summary>
        /// ゲームを停止してGameManagerをリセットする
        /// </summary>
        [ContextMenu("Stop and Reset")]
        public void StopAndReset()
        {
            Debug.Log("GameManager: 停止とリセットを実行");
            
            // ゲーム停止
            _gameCts?.Cancel();
            
            // イベント購読解除
            EventUnsubscribe();
            
            // リソース破棄
            _gameCts?.Dispose();
            _gameCts = null;
            
            // 状態リセット
            _dealer = null;
            _fieldSetupManager = null;
            _isInitialized = false;
            _isGameRunning = false;
            
            Debug.Log("GameManager: 停止とリセット完了");
        }

        /// <summary>
        /// StopAndResetの別名（後方互換性）
        /// </summary>
        [ContextMenu("Reset")]
        public void Reset() => StopAndReset();

        /// <summary>
        /// StopGameの実装をStopAndResetに統一
        /// </summary>
        public void StopGame() => StopAndReset();

        public void OnDestroy()
        {
            Debug.Log("GameManager: OnDestroy実行");
            StopAndReset();
        }

        #endregion
    }
}