using UnityEngine;
using Tetrage.Core.Contracts;
using System.Collections.Generic;
using Tetrage.Core.DTO;

namespace Tetrage.Components
{
    /// <summary>
    /// FieldSetup関連の静的設定検証を担当するクラス
    /// 単一責任: 静的設定の妥当性検証のみ（動的な参加者情報は除く）
    /// </summary>
    public static class FieldSetupConfigurationValidator
    {
        /// <summary>
        /// 全静的設定の妥当性を検証
        /// </summary>
        public static bool ValidateAllConfigurations(
            FieldSetupPrefabConfig prefabConfig,
            Transform stageRoot,
            Transform playerRoot,
            IPositionConfig stageSpawnPositionConfig,
            IPositionConfig playerLocationsConfig,
            CardPileLayoutAsset handsPileLayoutAsset,
            CardPileLayoutAsset tmpPileLayoutAsset,
            CardPileLayoutAsset targetPileLayoutAsset,
            CardPileLayoutAsset trashPileLayoutAsset,
            CardPileLayoutAsset stackPileLayoutAsset,
            MonoBehaviour context)
        {
            bool isValid = true;

            // 基本設定の検証
            isValid &= ValidateBasicConfiguration(prefabConfig, stageRoot, playerRoot, context);
            
            // PrefabConfigの詳細検証
            if (prefabConfig != null)
            {
                isValid &= ValidatePrefabConfig(prefabConfig, context);
            }

            // PositionConfigの検証
            isValid &= ValidatePositionConfigs(stageSpawnPositionConfig, playerLocationsConfig, context);

            // レイアウトアセットの検証
            isValid &= ValidatePileLayoutAssets(handsPileLayoutAsset, tmpPileLayoutAsset, targetPileLayoutAsset, trashPileLayoutAsset, stackPileLayoutAsset, context);

            return isValid;
        }

        /// <summary>
        /// 動的な参加者情報と静的設定の互換性を検証
        /// </summary>
        public static bool ValidateParticipantCompatibility(
            List<PlayerInfo> participantInfoList,
            IPositionConfig playerLocationsConfig,
            MonoBehaviour context)
        {
            bool isValid = true;

            if (participantInfoList == null || participantInfoList.Count == 0)
            {
                Debug.LogError("参加者情報リストが設定されていません", context);
                return false;
            }

            if (playerLocationsConfig == null)
            {
                Debug.LogError("PlayerLocationsConfigが設定されていません", context);
                return false;
            }

            int requiredPlayerCount = participantInfoList.Count;
            if (!playerLocationsConfig.ValidateConfig(requiredPlayerCount))
            {
                Debug.LogError($"PlayerLocationsConfigの位置数({playerLocationsConfig.Position?.Count ?? 0})が参加者数({requiredPlayerCount})と一致しません", context);
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// 基本設定の検証
        /// </summary>
        private static bool ValidateBasicConfiguration(
            FieldSetupPrefabConfig prefabConfig,
            Transform stageRoot,
            Transform playerRoot,
            MonoBehaviour context)
        {
            bool isValid = true;

            if (prefabConfig == null)
            {
                Debug.LogError("FieldSetupPrefabConfigが設定されていません", context);
                isValid = false;
            }

            if (stageRoot == null)
            {
                Debug.LogError("StageRootが設定されていません", context);
                isValid = false;
            }

            if (playerRoot == null)
            {
                Debug.LogError("PlayerRootが設定されていません", context);
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// PrefabConfigの詳細検証
        /// </summary>
        private static bool ValidatePrefabConfig(FieldSetupPrefabConfig prefabConfig, MonoBehaviour context)
        {
            bool isValid = true;

            // Player View Prefabs
            if (prefabConfig.LocalPlayerViewPrefab == null)
            {
                Debug.LogError("PrefabConfig.LocalPlayerViewPrefabが設定されていません", context);
                isValid = false;
            }

            if (prefabConfig.RemotePlayerViewPrefab == null)
            {
                Debug.LogError("PrefabConfig.RemotePlayerViewPrefabが設定されていません", context);
                isValid = false;
            }

            if (prefabConfig.BotPlayerViewPrefab == null)
            {
                Debug.LogError("PrefabConfig.BotPlayerViewPrefabが設定されていません", context);
                isValid = false;
            }

            // Card and Stage View Prefabs
            if (prefabConfig.CardViewPrefab == null)
            {
                Debug.LogError("PrefabConfig.CardViewPrefabが設定されていません。Factory作成時にエラーが発生します", context);
                isValid = false;
            }

            if (prefabConfig.StageViewPrefab == null)
            {
                Debug.LogError("PrefabConfig.StageViewPrefabが設定されていません", context);
                isValid = false;
            }

            // Card Pile View Prefabs
            if (prefabConfig.BasicCardPileViewPrefab == null)
            {
                Debug.LogError("PrefabConfig.BasicCardPileViewPrefabが設定されていません", context);
                isValid = false;
            }

            if (prefabConfig.HandsCardPileViewPrefab == null)
            {
                Debug.LogError("PrefabConfig.HandsCardPileViewPrefabが設定されていません", context);
                isValid = false;
            }

            if (prefabConfig.TmpCardPileViewPrefab == null)
            {
                Debug.LogError("PrefabConfig.TmpCardPileViewPrefabが設定されていません", context);
                isValid = false;
            }

            if (prefabConfig.StackCardPileViewPrefab == null)
            {
                Debug.LogError("PrefabConfig.StackCardPileViewPrefabが設定されていません", context);
                isValid = false;
            }

            if (prefabConfig.TrashCardPileViewPrefab == null)
            {
                Debug.LogError("PrefabConfig.TrashCardPileViewPrefabが設定されていません", context);
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// PositionConfig検証
        /// </summary>
        private static bool ValidatePositionConfigs(
            IPositionConfig stageSpawnPositionConfig,
            IPositionConfig playerLocationsConfig,
            MonoBehaviour context)
        {
            bool isValid = true;

            // Stage Position の検証
            if (stageSpawnPositionConfig == null)
            {
                Debug.LogError("StageSpawnPositionConfigが設定されていません", context);
                isValid = false;
            }
            else if (!stageSpawnPositionConfig.ValidateConfig(1))
            {
                Debug.LogWarning("StageSpawnPositionConfigの位置設定が不正です。最低1つの位置が必要です。", context);
                isValid = false;
            }

            // Player Locations の検証（基本チェックのみ）
            if (playerLocationsConfig == null)
            {
                Debug.LogError("PlayerLocationsConfigが設定されていません", context);
                isValid = false;
            }
            else
            {
                var positions = playerLocationsConfig.Position;
                if (positions == null || positions.Count == 0)
                {
                    Debug.LogError("PlayerLocationsConfigに位置が設定されていません", context);
                    isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// レイアウトアセット検証
        /// </summary>
        private static bool ValidatePileLayoutAssets(
            CardPileLayoutAsset handsPileLayoutAsset,
            CardPileLayoutAsset tmpPileLayoutAsset,
            CardPileLayoutAsset targetPileLayoutAsset,
            CardPileLayoutAsset trashPileLayoutAsset,
            CardPileLayoutAsset stackPileLayoutAsset,
            MonoBehaviour context)
        {
            bool isValid = true;

            isValid &= ValidateLayoutAsset("HandsPileLayoutAsset", handsPileLayoutAsset, context);
            isValid &= ValidateLayoutAsset("TmpPileLayoutAsset", tmpPileLayoutAsset, context);
            isValid &= ValidateLayoutAsset("TargetPileLayoutAsset", targetPileLayoutAsset, context);
            isValid &= ValidateLayoutAsset("TrashPileLayoutAsset", trashPileLayoutAsset, context);
            isValid &= ValidateLayoutAsset("StackPileLayoutAsset", stackPileLayoutAsset, context);

            return isValid;
        }

        private static bool ValidateLayoutAsset(string assetName, CardPileLayoutAsset asset, MonoBehaviour context)
        {
            if (asset == null)
            {
                Debug.LogError($"PrefabConfig.{assetName}が設定されていません。カードパイルの表示に影響します", context);
                return false;
            }
            return true;
        }
    }
} 