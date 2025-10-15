using System;
using System.Collections.Generic;
using UnityEngine;
using Tetrage.Core.Contracts;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.Factories;
using Tetrage.UI;
using Tetrage.Managers;

namespace Tetrage.Components
{
    /// <summary>
    /// FieldSetupManagerに必要な依存性と静的設定を提供するMonoBehaviourコンポーネント
    /// 責務: 静的フィールド設定の提供、設定検証、FieldSetupSettings構築
    /// 動的な参加者情報はDealerが管理、セットアップ実行は上位モジュールが担当
    /// </summary>
    public class FieldSetupComponent : MonoBehaviour
    {
        #region Inspector設定
        [Header("Configuration")]
        [SerializeField] private FieldSetupPrefabConfig prefabConfig;

        [Header("Scene Dependencies")]
        [SerializeField] private Transform stageRoot;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private PositionConfig stageSpawnPositionPrefab;
        [SerializeField] private PositionConfig playerLocationsPrefab;

        [Header("カードパイルレイアウト設定")]
        [SerializeField] private CardPileLayoutAsset handsPileLayoutAsset;
        [SerializeField] private CardPileLayoutAsset tmpPileLayoutAsset;
        [SerializeField] private CardPileLayoutAsset targetPileLayoutAsset;
        [SerializeField] private CardPileLayoutAsset trashPileLayoutAsset;
        [SerializeField] private CardPileLayoutAsset stackPileLayoutAsset;
        #endregion

        #region 内部キャッシュ
        // キャッシュされた設定（一度構築したらキャッシュ）
        private FieldSetupSettings _cachedSettings;
        private bool _isValidated = false;
        #endregion

        #region プロパティ
        /// <summary>
        /// PrefabConfigを取得
        /// </summary>
        public FieldSetupPrefabConfig PrefabConfig => prefabConfig;

        /// <summary>
        /// StageRootを取得
        /// </summary>
        public Transform StageRoot => stageRoot;

        /// <summary>
        /// PlayerRootを取得
        /// </summary>
        public Transform PlayerRoot => playerRoot;

        /// <summary>
        /// StageSpawnPositionConfigをIPositionConfigとして取得
        /// </summary>
        public IPositionConfig StageSpawnPositionConfig => stageSpawnPositionPrefab;

        /// <summary>
        /// PlayerLocationsConfigをIPositionConfigとして取得
        /// </summary>
        public IPositionConfig PlayerLocationsConfig => playerLocationsPrefab;

        /// <summary>
        /// HandsPileLayoutAssetを取得
        /// </summary>
        public CardPileLayoutAsset HandsPileLayoutAsset => handsPileLayoutAsset;

        /// <summary>
        /// TmpPileLayoutAssetを取得
        /// </summary>
        public CardPileLayoutAsset TmpPileLayoutAsset => tmpPileLayoutAsset;

        /// <summary>
        /// TargetPileLayoutAssetを取得
        /// </summary>
        public CardPileLayoutAsset TargetPileLayoutAsset => targetPileLayoutAsset;

        /// <summary>
        /// TrashPileLayoutAssetを取得
        /// </summary>
        public CardPileLayoutAsset TrashPileLayoutAsset => trashPileLayoutAsset;

        /// <summary>
        /// StackPileLayoutAssetを取得
        /// </summary>
        public CardPileLayoutAsset StackPileLayoutAsset => stackPileLayoutAsset;
        #endregion

        #region 統合設定取得・検証API
        /// <summary>
        /// 設定検証済みのFieldSetupSettingsを取得
        /// 上位モジュールはこのメソッドのみを呼び出すだけで完全な設定を取得可能
        /// </summary>
        /// <returns>検証済みのFieldSetupSettings</returns>
        /// <exception cref="InvalidOperationException">設定に問題がある場合</exception>
        public FieldSetupSettings GetValidatedFieldSetupSettings()
        {
            // キャッシュがある場合はそれを返す
            if (_cachedSettings != null && _isValidated)
            {
                return _cachedSettings;
            }

            // 設定の検証を実行
            if (!ValidateAllConfigurations())
            {
                throw new InvalidOperationException("FieldSetupComponent: 設定検証に失敗しました");
            }

            // FieldSetupSettingsを構築
            _cachedSettings = BuildFieldSetupSettings();
            _isValidated = true;

            Debug.Log("FieldSetupComponent: FieldSetupSettingsを正常に構築しました");
            return _cachedSettings;
        }

        /// <summary>
        /// 参加者数との互換性を検証済みのFieldSetupSettingsを取得
        /// </summary>
        /// <param name="participantCount">参加者数</param>
        /// <returns>参加者数との互換性検証済みのFieldSetupSettings</returns>
        /// <exception cref="InvalidOperationException">参加者数と設定が互換性がない場合</exception>
        public FieldSetupSettings GetValidatedFieldSetupSettings(int participantCount)
        {
            var settings = GetValidatedFieldSetupSettings();

            // 参加者数との互換性検証
            if (!ValidateParticipantCompatibility(participantCount))
            {
                throw new InvalidOperationException($"FieldSetupComponent: 参加者数({participantCount})と設定が互換性がありません");
            }

            return settings;
        }

        /// <summary>
        /// FieldSetupDependenciesを生成する
        /// </summary>
        public FieldSetupDependencies CreateFieldSetupDependencies()
        {
            // 各種ファクトリーを生成（依存関係順）（TODO: ここをRegistry付きのファクトリに変更する）
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
        /// 設定をリセット（キャッシュクリア）
        /// Inspector設定変更時などに呼び出し
        /// </summary>
        [ContextMenu("設定をリセット")]
        public void ResetSettings()
        {
            _cachedSettings = null;
            _isValidated = false;
            Debug.Log("FieldSetupComponent: 設定をリセットしました");
        }
        #endregion

        #region 内部実装: 検証・ビルド
        /// <summary>
        /// 全設定の検証を実行
        /// </summary>
        /// <returns>検証結果</returns>
        private bool ValidateAllConfigurations()
        {
            try
            {
                // FieldSetupConfigurationValidatorを使用して検証
                bool isValid = FieldSetupConfigurationValidator.ValidateAllConfigurations(
                    prefabConfig,
                    stageRoot,
                    playerRoot,
                    stageSpawnPositionPrefab,
                    playerLocationsPrefab,
                    handsPileLayoutAsset,
                    tmpPileLayoutAsset,
                    targetPileLayoutAsset,
                    trashPileLayoutAsset,
                    stackPileLayoutAsset,
                    this // contextとしてthisを渡す
                );

                if (!isValid)
                {
                    Debug.LogError("FieldSetupComponent: 設定検証に失敗しました");
                }

                return isValid;
            }
            catch (Exception e)
            {
                Debug.LogError($"FieldSetupComponent: 設定検証中にエラーが発生しました - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 参加者数との互換性を検証
        /// </summary>
        /// <param name="participantCount">参加者数</param>
        /// <returns>互換性検証結果</returns>
        private bool ValidateParticipantCompatibility(int participantCount)
        {
            try
            {
                // 参加者数からダミーの参加者情報リストを作成
                var dummyParticipantList = new List<PlayerInfo>();
                for (int i = 0; i < participantCount; i++)
                {
                    dummyParticipantList.Add(new PlayerInfo
                    {
                        UserId = $"DummyPlayer{i + 1}",
                        PlayerType = PlayerType.Local
                    });
                }

                return FieldSetupConfigurationValidator.ValidateParticipantCompatibility(
                    dummyParticipantList,
                    playerLocationsPrefab,
                    this // contextとしてthisを渡す
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"FieldSetupComponent: 参加者互換性検証中にエラーが発生しました - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// FieldSetupSettingsを構築
        /// </summary>
        /// <returns>構築されたFieldSetupSettings</returns>
        private FieldSetupSettings BuildFieldSetupSettings()
        {
            // FieldSetupSettingsBuilderを使用してFieldSetupSettingsを構築
            return FieldSetupSettingsBuilder.BuildSettings(
                prefabConfig,
                stageRoot,
                playerRoot,
                stageSpawnPositionPrefab,
                playerLocationsPrefab,
                handsPileLayoutAsset,
                tmpPileLayoutAsset,
                targetPileLayoutAsset,
                trashPileLayoutAsset,
                stackPileLayoutAsset
            );
        }
        #endregion

        #region 互換性維持API（従来）
        /// <summary>
        /// カードパイルレイアウト設定の辞書を取得
        /// </summary>
        public Dictionary<CardPileType, CardPileLayoutSettings> GetCardPileLayoutSettings()
        {
            return CreateCardPileLayoutSettingsDictionary();
        }

        /// <summary>
        /// プレイヤー位置情報を取得
        /// </summary>
        public List<Vector3> GetPlayerLocations()
        {
            return PlayerLocationsConfig.Position;
        }

        /// <summary>
        /// ステージスポーン位置を取得
        /// </summary>
        public Vector3 GetStageSpawnPosition()
        {
            return StageSpawnPositionConfig.Position[0]; // ステージは1つのみ
        }

        /// <summary>
        /// カードパイルレイアウト設定の辞書を作成
        /// </summary>
        private Dictionary<CardPileType, CardPileLayoutSettings> CreateCardPileLayoutSettingsDictionary()
        {
            return new Dictionary<CardPileType, CardPileLayoutSettings>
            {
                { CardPileType.Hands, handsPileLayoutAsset.ToLayoutSettings() },
                { CardPileType.Tmp, tmpPileLayoutAsset.ToLayoutSettings() },
                { CardPileType.Target, targetPileLayoutAsset.ToLayoutSettings() },
                { CardPileType.Trash, trashPileLayoutAsset.ToLayoutSettings() },
                { CardPileType.Stack, stackPileLayoutAsset.ToLayoutSettings() }
            };
        }

        /// <summary>
        /// 設定の検証（従来版）
        /// </summary>
        private void ValidateConfiguration()
        {
            if (prefabConfig == null)
                throw new InvalidOperationException("PrefabConfigが設定されていません");

            if (stageRoot == null)
                throw new InvalidOperationException("StageRootが設定されていません");

            if (playerRoot == null)
                throw new InvalidOperationException("PlayerRootが設定されていません");

            if (stageSpawnPositionPrefab == null)
                throw new InvalidOperationException("StageSpawnPositionPrefabが設定されていません");

            if (playerLocationsPrefab == null)
                throw new InvalidOperationException("PlayerLocationsPrefabが設定されていません");

            // カードパイルレイアウト設定の検証
            ValidateCardPileLayoutAsset(handsPileLayoutAsset, "HandsPileLayoutAsset");
            ValidateCardPileLayoutAsset(tmpPileLayoutAsset, "TmpPileLayoutAsset");
            ValidateCardPileLayoutAsset(targetPileLayoutAsset, "TargetPileLayoutAsset");
            ValidateCardPileLayoutAsset(trashPileLayoutAsset, "TrashPileLayoutAsset");
            ValidateCardPileLayoutAsset(stackPileLayoutAsset, "StackPileLayoutAsset");
        }

        /// <summary>
        /// CardPileLayoutAssetの検証
        /// </summary>
        private void ValidateCardPileLayoutAsset(CardPileLayoutAsset asset, string assetName)
        {
            if (asset == null)
                throw new InvalidOperationException($"{assetName}が設定されていません");
        }
        #endregion

        #region UnityEditor用メソッド
#if UNITY_EDITOR
        /// <summary>
        /// Inspector用の設定検証ボタン
        /// </summary>
        [ContextMenu("設定を検証")]
        public void ValidateConfigurationInEditor()
        {
            try
            {
                if (ValidateAllConfigurations())
                {
                    Debug.Log("FieldSetupComponent: 設定の検証が完了しました");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"FieldSetupComponent: 設定エラー - {e.Message}");
            }
        }

        /// <summary>
        /// Inspector用のFieldSetupSettings構築テストボタン
        /// </summary>
        [ContextMenu("FieldSetupSettingsを構築テスト")]
        public void TestBuildFieldSetupSettings()
        {
            try
            {
                var settings = GetValidatedFieldSetupSettings();
                Debug.Log("FieldSetupComponent: FieldSetupSettingsの構築テストが成功しました");
            }
            catch (Exception e)
            {
                Debug.LogError($"FieldSetupComponent: FieldSetupSettings構築テストに失敗しました - {e.Message}");
            }
        }
#endif
        #endregion

        #region Unityイベント
        private void OnValidate()
        {
            // Inspector設定変更時にキャッシュをクリア
            ResetSettings();
        }
        #endregion
    }
}