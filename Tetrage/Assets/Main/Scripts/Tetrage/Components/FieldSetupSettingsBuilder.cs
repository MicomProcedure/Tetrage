using System.Collections.Generic;
using UnityEngine;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.UI;
using Tetrage.Managers;
using Tetrage.Core.Contracts;

namespace Tetrage.Components
{
    /// <summary>
    /// FieldSetupSettings構築を担当するクラス
    /// 単一責任: 静的設定データの構築・変換のみ
    /// </summary>
    public static class FieldSetupSettingsBuilder
    {
        /// <summary>
        /// FieldSetupSettings（静的設定のみ）を構築
        /// 参加者情報は含まれません
        /// </summary>
        public static FieldSetupSettings BuildSettings(
            FieldSetupPrefabConfig prefabConfig,
            Transform stageRoot,
            Transform playerRoot,
            IPositionConfig stageSpawnPositionConfig,
            IPositionConfig playerLocationsConfig,
            CardPileLayoutAsset handsPileLayoutAsset,
            CardPileLayoutAsset tmpPileLayoutAsset,
            CardPileLayoutAsset targetPileLayoutAsset,
            CardPileLayoutAsset trashPileLayoutAsset,
            CardPileLayoutAsset stackPileLayoutAsset)
        {
            var playerViewPrefabDict = CreatePlayerViewPrefabDict(prefabConfig);
            var pileViewPrefabDict = CreatePileViewPrefabDict(prefabConfig);
            var playerPilesLayoutSettings = CreatePlayerPilesLayoutSettings(handsPileLayoutAsset, tmpPileLayoutAsset, targetPileLayoutAsset);

            var stageSpawnPosition = GetStageSpawnPosition(stageSpawnPositionConfig);
            var playerLocations = GetPlayerLocations(playerLocationsConfig);

            return new FieldSetupSettings(
                playerViewPrefabDict,
                pileViewPrefabDict,
                prefabConfig.CardViewPrefab,
                prefabConfig.StageViewPrefab,
                stageSpawnPosition,
                stageRoot,
                playerRoot,
                playerLocations,
                playerPilesLayoutSettings,
                trashPileLayoutAsset.ToLayoutSettings(),
                stackPileLayoutAsset.ToLayoutSettings());
        }

        /// <summary>
        /// プレイヤービューPrefabディクショナリを作成
        /// </summary>
        private static Dictionary<PlayerType, BasicPlayerView> CreatePlayerViewPrefabDict(FieldSetupPrefabConfig prefabConfig)
        {
            return new Dictionary<PlayerType, BasicPlayerView>
            {
                { PlayerType.Local, prefabConfig.LocalPlayerViewPrefab },
                { PlayerType.Remote, prefabConfig.RemotePlayerViewPrefab },
                { PlayerType.Bot, prefabConfig.BotPlayerViewPrefab }
            };
        }

        /// <summary>
        /// カードパイルビューPrefabディクショナリを作成
        /// </summary>
        private static Dictionary<CardPileType, BasicCardPileView> CreatePileViewPrefabDict(FieldSetupPrefabConfig prefabConfig)
        {
            return new Dictionary<CardPileType, BasicCardPileView>
            {
                { CardPileType.Hands, prefabConfig.HandsCardPileViewPrefab },
                { CardPileType.Tmp, prefabConfig.TmpCardPileViewPrefab },
                { CardPileType.Target, prefabConfig.BasicCardPileViewPrefab },
                { CardPileType.Trash, prefabConfig.TrashCardPileViewPrefab },
                { CardPileType.Stack, prefabConfig.StackCardPileViewPrefab }
            };
        }

        /// <summary>
        /// プレイヤーカードパイルレイアウト設定を作成
        /// </summary>
        private static Dictionary<CardPileType, CardPileLayoutSettings> CreatePlayerPilesLayoutSettings(
            CardPileLayoutAsset handsPileLayoutAsset,
            CardPileLayoutAsset tmpPileLayoutAsset,
            CardPileLayoutAsset targetPileLayoutAsset)
        {
            return new Dictionary<CardPileType, CardPileLayoutSettings>
            {
                { CardPileType.Hands, handsPileLayoutAsset.ToLayoutSettings() },
                { CardPileType.Tmp, tmpPileLayoutAsset.ToLayoutSettings() },
                { CardPileType.Target, targetPileLayoutAsset.ToLayoutSettings() }
            };
        }

        /// <summary>
        /// ステージスポーン位置を取得
        /// </summary>
        private static Vector3 GetStageSpawnPosition(IPositionConfig stageSpawnPositionConfig)
        {
            return stageSpawnPositionConfig?.Position?[0] ?? Vector3.zero;
        }

        /// <summary>
        /// プレイヤー位置リストを取得
        /// </summary>
        private static List<Vector3> GetPlayerLocations(IPositionConfig playerLocationsConfig)
        {
            return playerLocationsConfig?.Position ?? new List<Vector3>();
        }
    }
} 