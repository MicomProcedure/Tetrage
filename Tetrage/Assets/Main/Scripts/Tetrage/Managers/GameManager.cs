using UnityEngine;
using Tetrage.Core.Enums;
using Tetrage.Core.DTO;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.Components;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

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


        /// <summary>セットアップ済みのDealerStrategy</summary>
        public IDealerStrategy DealerStrategy { get; private set; }

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


        #endregion

        #region ゲーム制御メソッド
        public async UniTask StartGame()
        {
            if (!_isInitialized)
            {
                Debug.LogError("GameManager: 初期化されていません");
                return;
            }

            try
            {
                await _dealer.StartGameAsync();
                Debug.Log("GameManager: ゲームが開始されました");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameManager: ゲーム開始中にエラーが発生: {ex.Message}");
            }
        }


        #endregion

        #region Unity固有メソッド
        private void Update()
        {
            // 必要に応じてフェーズ管理のUpdate処理
        }

        /// <summary>
        /// GameManagerをリセットする（テスト用）
        /// </summary>
        [ContextMenu("Reset")]
        public void Reset()
        {
            _dealer = null;
            _fieldSetupManager = null;
            _isInitialized = false;
        }
        #endregion
    }
}