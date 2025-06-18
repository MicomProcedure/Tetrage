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
    /// 責務: 静的フィールド設定の提供のみ（セットアップ実行は行わない）
    /// 動的な参加者情報はDealerが管理、セットアップ実行は上位モジュールが担当
    /// </summary>
    public class FieldSetupComponent : MonoBehaviour
    {
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
        /// 設定の検証
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

        #if UNITY_EDITOR
        /// <summary>
        /// Inspector用の検証ボタン
        /// </summary>
        [ContextMenu("設定を検証")]
        public void ValidateConfigurationInEditor()
        {
            try
            {
                ValidateConfiguration();
                Debug.Log("FieldSetupComponent: 設定の検証が完了しました");
            }
            catch (Exception e)
            {
                Debug.LogError($"FieldSetupComponent: 設定エラー - {e.Message}");
            }
        }
        #endif
    }
}