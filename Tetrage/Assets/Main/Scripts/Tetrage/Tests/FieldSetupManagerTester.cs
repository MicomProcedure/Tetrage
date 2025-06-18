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
    /// シーン上のFieldSetupComponentから必要なプレハブや設定を受け取り、フィールドセットアップの動作を確認します
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
        /// フィールドセットアップを実行します（FieldSetupComponentのデータを使用）
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

                // FieldSetupSettingsとDependenciesを作成
                var settings = CreateFieldSetupSettings();
                var dependencies = CreateFieldSetupDependencies();

                // 参加者情報リストを作成
                var participantInfoList = CreateParticipantInfoList();

                // FieldSetupManagerを作成してセットアップ実行
                _fieldSetupManager = new FieldSetupManager(settings, dependencies);
                _fieldSetupManager.SetupField(participantInfoList);

                Debug.Log($"FieldSetupManagerTester: セットアップが完了しました。プレイヤー数: {_fieldSetupManager.Players.Count}");

                // 結果をログ出力
                LogSetupResults();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FieldSetupManagerTester: セットアップ中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                _isSetupInProgress = false;
            }
        }

        /// <summary>
        /// 参加者情報リストを作成
        /// </summary>
        private List<PlayerInfo> CreateParticipantInfoList()
        {
            var participantInfoList = new List<PlayerInfo>();
            foreach (var testPlayer in testPlayerList)
            {
                participantInfoList.Add(new PlayerInfo
                {
                    UserId = testPlayer.userId,
                    PlayerType = testPlayer.playerType
                });
            }
            return participantInfoList;
        }

        /// <summary>
        /// 必要なコンポーネントが設定されているかを検証
        /// </summary>
        private bool ValidateRequiredComponents()
        {
            bool isValid = true;

            if (fieldSetupComponent == null)
            {
                Debug.LogError("FieldSetupComponentが設定されていません");
                isValid = false;
                return isValid;
            }

            // FieldSetupComponentの設定を検証
            try
            {
                fieldSetupComponent.ValidateConfigurationInEditor();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FieldSetupComponentの設定エラー: {e.Message}");
                isValid = false;
            }

            // プレイヤー位置の数とプレイヤー数の整合性チェック
            var playerLocations = fieldSetupComponent.GetPlayerLocations();
            if (playerLocations.Count != testPlayerList.Count)
            {
                Debug.LogError($"プレイヤー位置の数({playerLocations.Count})とプレイヤー数({testPlayerList.Count})が一致しません");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// FieldSetupDependenciesを作成
        /// </summary>
        private FieldSetupDependencies CreateFieldSetupDependencies()
        {
            // 依存性オブジェクトを作成
            ICardFactory cardModelFactory = new CardModelFactory();
            ICardPileFactory cardPileFactory = new CardPileFactory();
            IStageFactory stageModelFactory = new StageModelFactory(cardPileFactory, cardModelFactory);
            IPlayerFactory playerModelFactory = new PlayerModelFactory(cardPileFactory, cardModelFactory);

            return new FieldSetupDependencies(
                cardModelFactory,
                stageModelFactory,
                playerModelFactory,
                cardPileFactory
            );
        }

        /// <summary>
        /// FieldSetupSettingsを作成（FieldSetupComponentから取得）
        /// </summary>
        private FieldSetupSettings CreateFieldSetupSettings()
        {
            // FieldSetupSettingsBuilderを使用してFieldSetupSettingsを構築
            return FieldSetupSettingsBuilder.BuildSettings(
                fieldSetupComponent.PrefabConfig,
                fieldSetupComponent.StageRoot,
                fieldSetupComponent.PlayerRoot,
                fieldSetupComponent.StageSpawnPositionConfig,
                fieldSetupComponent.PlayerLocationsConfig,
                fieldSetupComponent.HandsPileLayoutAsset,
                fieldSetupComponent.TmpPileLayoutAsset,
                fieldSetupComponent.TargetPileLayoutAsset,
                fieldSetupComponent.TrashPileLayoutAsset,
                fieldSetupComponent.StackPileLayoutAsset
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
            Debug.Log("設定ソース: FieldSetupComponent");

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

            _fieldSetupManager = null;
            Debug.Log("フィールドがクリアされました");
        }

        /// <summary>
        /// Transform の子オブジェクトをクリア
        /// </summary>
        private void ClearTransformChildren(Transform parent)
        {
            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 現在の設定状況を表示（デバッグ用）
        /// </summary>
        [ContextMenu("設定状況を表示")]
        public void ShowCurrentSettings()
        {
            Debug.Log("=== FieldSetupManagerTester 設定状況 ===");
            
            if (fieldSetupComponent != null)
            {
                Debug.Log($"使用予定の設定ソース: FieldSetupComponent ({fieldSetupComponent.name})");
                Debug.Log($"StageRoot: {fieldSetupComponent.StageRoot?.name ?? "未設定"}");
                Debug.Log($"PlayerRoot: {fieldSetupComponent.PlayerRoot?.name ?? "未設定"}");
                Debug.Log($"プレイヤー位置数: {fieldSetupComponent.GetPlayerLocations().Count}");
            }
            else
            {
                Debug.Log("使用予定の設定ソース: 未設定（FieldSetupComponentが設定されていません）");
            }
            
            Debug.Log($"テストプレイヤー数: {testPlayerList.Count}");
            foreach (var player in testPlayerList)
            {
                Debug.Log($"  - {player.userId} ({player.playerType})");
            }
            Debug.Log("================================");
        }
    }
}