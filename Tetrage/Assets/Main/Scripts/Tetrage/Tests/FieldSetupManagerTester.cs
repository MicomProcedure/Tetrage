using UnityEngine;
using System.Collections.Generic;
using Tetrage.Managers;
using Tetrage.Core.Enums;
using Tetrage.Core.DTO;
using Tetrage.UI;
using Tetrage.Factories;
using Tetrage.Components;
using Tetrage.Core.Contracts;
using Tetrage.Services;
using Tetrage.Models;

namespace Tetrage.Tests
{
    /// <summary>
    /// FieldSetupManagerのプレイモードテストクラス
    /// シーン上のFieldSetupComponentから設定を取得し、フィールドセットアップの動作を確認します
    /// </summary>
    public class FieldSetupManagerTester : MonoBehaviour
    {
        [Header("FieldSetupComponent参照")]
        [SerializeField] private FieldSetupComponent fieldSetupComponent;

        [Header("プレイヤー設定")]
        [SerializeField]
        private List<TestPlayerData> testPlayerList = new List<TestPlayerData>
        {
            new TestPlayerData { userId = "LocalPlayer1", playerType = PlayerType.Local },
            new TestPlayerData { userId = "RemotePlayer1", playerType = PlayerType.Remote }
        };

        [Header("テスト設定")]
        [SerializeField] private bool autoSetupOnStart = false;
        [SerializeField] private bool preventDuplicateSetup = true;

        // セットアップ後のインスタンス
        private FieldSetupManager _fieldSetupManager;
        private bool _isSetupInProgress = false;

        /// <summary>
        /// テスト用プレイヤーデータ構造体
        /// </summary>
        [System.Serializable]
        public struct TestPlayerData
        {
            public string userId;
            public PlayerType playerType;
        }

        void Start()
        {
            if (autoSetupOnStart)
            {
                SetupField();
            }
        }

        /// <summary>
        /// フィールドセットアップを実行します（FieldSetupComponentの統合メソッドを使用）
        /// </summary>
        [ContextMenu("フィールドセットアップを実行")]
        public void SetupField()
        {
            // 重複セットアップの防止
            if (preventDuplicateSetup && _isSetupInProgress)
            {
                Debug.LogWarning("FieldSetupManagerTester: セットアップが既に実行中です。重複実行をスキップします。");
                return;
            }

            if (preventDuplicateSetup && _fieldSetupManager != null)
            {
                Debug.LogWarning("FieldSetupManagerTester: フィールドは既にセットアップ済みです。再セットアップする場合は先にクリアしてください。");
                return;
            }

            try
            {
                _isSetupInProgress = true;
                Debug.Log("FieldSetupManagerTester: フィールドセットアップを開始します");

                // 必要なコンポーネントの検証
                if (!ValidateRequiredComponents())
                {
                    Debug.LogError("FieldSetupManagerTester: 必要なコンポーネントが不足しています");
                    return;
                }

                // 参加者情報リストを作成
                var participantInfoList = CreateParticipantInfoList();

                // FieldSetupComponentから検証済みの設定を取得（参加者数チェック付き）
                var settings = fieldSetupComponent.GetValidatedFieldSetupSettings(participantInfoList.Count);
                var dependencies = CreateFieldSetupDependencies();

                // FieldSetupManagerを作成してセットアップ実行
                _fieldSetupManager = new FieldSetupManager(settings, dependencies);
                _fieldSetupManager.SetupField(participantInfoList);

                Debug.Log("FieldSetupManagerTester: フィールドセットアップが完了しました");
                LogSetupResults();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FieldSetupManagerTester: セットアップに失敗しました - {e.Message}");
            }
            finally
            {
                _isSetupInProgress = false;
            }
        }

        /// <summary>
        /// 必要なコンポーネントの検証
        /// </summary>
        private bool ValidateRequiredComponents()
        {
            if (fieldSetupComponent == null)
            {
                Debug.LogError("FieldSetupManagerTester: FieldSetupComponentが設定されていません");
                return false;
            }

            if (testPlayerList == null || testPlayerList.Count == 0)
            {
                Debug.LogError("FieldSetupManagerTester: テストプレイヤーリストが設定されていません");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 参加者情報リストを作成
        /// </summary>
        private List<PlayerInfo> CreateParticipantInfoList()
        {
            var participantInfoList = new List<PlayerInfo>();

            foreach (var testPlayer in testPlayerList)
            {
                var playerInfo = new PlayerInfo
                {
                    UserId = testPlayer.userId,
                    PlayerType = testPlayer.playerType
                };
                participantInfoList.Add(playerInfo);
            }

            Debug.Log($"FieldSetupManagerTester: 作成された参加者情報数: {participantInfoList.Count}");
            return participantInfoList;
        }

        /// <summary>
        /// FieldSetupDependenciesを作成
        /// </summary>
        private FieldSetupDependencies CreateFieldSetupDependencies()
        {
            // 必要なファクトリを作成
            var cardModelFactory = new CardModelFactory();
            var cardPileFactory = new CardPileFactory();
            var stageModelFactory = new StageModelFactory(cardPileFactory, cardModelFactory);
            var playerModelFactory = new PlayerModelFactory(cardPileFactory, cardModelFactory);

            return new FieldSetupDependencies(
                cardModelFactory,
                stageModelFactory,
                playerModelFactory,
                cardPileFactory
            );
        }

        /// <summary>
        /// セットアップ結果をログ出力
        /// </summary>
        private void LogSetupResults()
        {
            if (_fieldSetupManager == null) 
            {
                Debug.LogWarning("FieldSetupManagerTester: FieldSetupManagerが初期化されていません");
                return;
            }

            Debug.Log("=== FieldSetupManager セットアップ結果 ===");
            Debug.Log("設定ソース: FieldSetupComponent（統合メソッド使用）");

            // プレイヤー情報
            var players = _fieldSetupManager.Players;
            Debug.Log($"生成されたプレイヤー数: {players.Count}");

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                Debug.Log($"プレイヤー{i + 1}: UserID={player.UserId}, " +
                         $"Hands={player.Hands.Cards.Count}枚, " +
                         $"Tmp={player.Tmp.Cards.Count}枚, " +
                         $"Target={player.Target.Cards.Count}枚");
            }

            // ステージ情報
            var stage = _fieldSetupManager.Stage;
            Debug.Log($"ステージ - Stack: {stage.Stack.Cards.Count}枚, Trash: {stage.Trash.Cards.Count}枚");

            Debug.Log("=== セットアップ完了 ===");
        }

        /// <summary>
        /// セットアップ済みのManagerインスタンスを取得（テスト用）
        /// </summary>
        public FieldSetupManager GetFieldSetupManager()
        {
            return _fieldSetupManager;
        }

        /// <summary>
        /// フィールドをクリア（テストの後始末用）
        /// </summary>
        [ContextMenu("フィールドをクリア")]
        public void ClearField()
        {
            // プレイヤーの子オブジェクトを削除
            if (fieldSetupComponent?.PlayerRoot != null)
            {
                for (int i = fieldSetupComponent.PlayerRoot.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(fieldSetupComponent.PlayerRoot.GetChild(i).gameObject);
                }
            }

            // ステージの子オブジェクトを削除
            if (fieldSetupComponent?.StageRoot != null)
            {
                for (int i = fieldSetupComponent.StageRoot.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(fieldSetupComponent.StageRoot.GetChild(i).gameObject);
                }
            }

            // 内部状態をリセット
            _fieldSetupManager = null;
            _isSetupInProgress = false;

            Debug.Log("FieldSetupManagerTester: フィールドをクリアしました");
        }

        /// <summary>
        /// 設定のみをテスト（セットアップは実行しない）
        /// </summary>
        [ContextMenu("設定検証のみ実行")]
        public void TestConfigurationOnly()
        {
            try
            {
                if (fieldSetupComponent == null)
                {
                    Debug.LogError("FieldSetupManagerTester: FieldSetupComponentが設定されていません");
                    return;
                }

                var participantCount = testPlayerList?.Count ?? 0;
                Debug.Log($"FieldSetupManagerTester: 参加者数({participantCount})での設定検証を開始します");

                // 設定検証のみ実行
                var settings = fieldSetupComponent.GetValidatedFieldSetupSettings(participantCount);
                
                Debug.Log("FieldSetupManagerTester: 設定検証が正常に完了しました");
                Debug.Log($"プレイヤー位置数: {settings.PlayerLocations.Count}");
                Debug.Log($"ステージスポーン位置: {settings.StageSpawnPosition}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FieldSetupManagerTester: 設定検証に失敗しました - {e.Message}");
            }
        }
    }
}