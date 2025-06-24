using UnityEngine;
using Tetrage.Core.Enums;
using Tetrage.Core.DTO;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.Components;
using Tetrage.Factories;
using System.Collections.Generic;
using System.Linq;

namespace Tetrage.Managers
{
    public class GameManager : MonoBehaviour
    {
        /* -------- 1. フェーズ管理（既存） -------- */
        [SerializeField]
        private GamePhase currentPhase;

        /// <summary>
        /// 現在のフェーズを外部から参照するためのプロパティ（読み取り専用）
        /// </summary>
        public GamePhase CurrentPhase
        {
            get { return currentPhase; }
            set { currentPhase = value; }
        }

        /* -------- 2. 設定コンポーネント -------- */
        [Header("Field Setup Configuration")]
        [SerializeField] private FieldSetupComponent fieldSetupComponent;

        /* -------- 3. 管理対象インスタンス -------- */
        private FieldSetupManager _fieldSetupManager;
        private Dealer _dealer;

        /* -------- 4. 状態管理 -------- */
        private bool _isInitialized = false;

        /* -------- 5. プロパティ -------- */
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

        /// <summary>セットアップ済みのStage</summary>
        public Stage Stage => _fieldSetupManager?.Stage;
        
        /// <summary>セットアップ済みのPlayers</summary>
        public IReadOnlyList<IPlayer> Players => _fieldSetupManager?.Players;

        /* -------- 6. 初期化メソッド -------- */
        /// <summary>
        /// GameManagerの初期化
        /// </summary>
        /// <param name="participantInfoList">参加者情報リスト</param>
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
                _dealer = CreateDealer();
                
                // 4. Dealerにフィールド情報を設定
                SetupDealer();
                
                // 5. 初期フェーズの設定
                currentPhase = GamePhase.Starting;
                
                _isInitialized = true;
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
            if (fieldSetupComponent == null)
            {
                // FieldSetupComponentが設定されていない場合、シーンから検索
                fieldSetupComponent = FindObjectOfType<FieldSetupComponent>();
                if (fieldSetupComponent == null)
                {
                    throw new System.InvalidOperationException("FieldSetupComponent が見つかりません。シーンに配置するか、Inspector で設定してください。");
                }
            }

            // FieldSetupComponentから検証済みの設定を取得
            var settings = fieldSetupComponent.GetValidatedFieldSetupSettings(participantCount);
            var dependencies = CreateFieldSetupDependencies();

            return new FieldSetupManager(settings, dependencies);
        }

        /// <summary>
        /// FieldSetupDependenciesを生成する
        /// </summary>
        private FieldSetupDependencies CreateFieldSetupDependencies()
        {
            // 各種ファクトリーを生成（依存関係順）
            var cardFactory = new CardModelFactory();
            var cardPileFactory = new CardPileFactory();
            var stageFactory = new StageModelFactory(cardPileFactory, cardFactory);
            var playerFactory = new PlayerModelFactory(cardPileFactory, cardFactory);

            return new FieldSetupDependencies(
                cardFactory,
                stageFactory,
                playerFactory,
                cardPileFactory
            );
        }

        /// <summary>
        /// Dealerインスタンスを生成する
        /// 戦略パターンの注入もここで行う
        /// </summary>
        private Dealer CreateDealer()
        {
            // 戦略パターンの設定が可能
            // var turnStrategy = new ClockwiseTurnStrategy();
            // var firstPlayerStrategy = new RandomFirstPlayerStrategy();
            // return new Dealer(turnStrategy, firstPlayerStrategy);
            
            return new Dealer();
        }

        /// <summary>
        /// Dealerにフィールド情報を設定する
        /// </summary>
        private void SetupDealer()
        {
            // フィールド情報をDealerに設定
            _dealer.SetStage(_fieldSetupManager.Stage);
            _dealer.SetPlayers(_fieldSetupManager.Players.ToList());
            
            // 初期プレイヤーの決定
            if (_fieldSetupManager.Players?.Count > 0)
            {
                _dealer.DecideFirstPlayer();
            }
            
            Debug.Log("GameManager: Dealerの設定が完了しました");
        }

        /* -------- 7. ゲーム制御メソッド（拡張） -------- */
        void StartGame()
        {
            if (!_isInitialized)
            {
                Debug.LogError("GameManager: 初期化されていません");
                return;
            }

            currentPhase = GamePhase.Playing;
            _dealer.OnRoundStart();
            Debug.Log("GameManager: ゲームが開始されました");
        }

        void StartRound()
        {
            if (!_isInitialized)
            {
                Debug.LogError("GameManager: 初期化されていません");
                return;
            }

            currentPhase = GamePhase.Playing;
            _dealer.OnRoundStart();
            Debug.Log("GameManager: ラウンドが開始されました");
        }

        void EndRound()
        {
            if (!_isInitialized)
            {
                Debug.LogError("GameManager: 初期化されていません");
                return;
            }

            _dealer.OnRoundEnd();
            currentPhase = GamePhase.Ending;
            Debug.Log("GameManager: ラウンドが終了しました");
        }

        void TransitionPhase()
        {
            // フェーズ遷移ロジック
            switch (currentPhase)
            {
                case GamePhase.Starting:
                    StartGame();
                    break;
                case GamePhase.Playing:
                    EndRound();
                    break;
                case GamePhase.Ending:
                    DisplayResults();
                    break;
            }
        }

        void DisplayResults()
        {
            currentPhase = GamePhase.Ending;
            Debug.Log("GameManager: 結果を表示しています");
        }

        void RestartOrQuit()
        {
            // リセット処理
            _dealer = null;
            _fieldSetupManager = null;
            _isInitialized = false;
            currentPhase = GamePhase.Starting;
            Debug.Log("GameManager: リセットされました");
        }

        /* -------- 8. Unity固有メソッド -------- */
        private void Update()
        {
            // 必要に応じてフェーズ管理のUpdate処理
        }

        /// <summary>
        /// GameManagerをリセットする（テスト用）
        /// </summary>
        public void Reset()
        {
            RestartOrQuit();
        }
    }
}