using Tetrage.Core.Contracts;
using System.Collections.Generic;
using Tetrage.Factories;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.UI;
using UnityEngine;

namespace Tetrage.Managers
{
    public class FieldSetupSettings
    {
        /* -------- 1. 必須パラメータ -------- */
        public List<PlayerInfo> ParticipantInfoList { get; }    // 参加者情報リスト
        public Dictionary<PlayerType, BasicPlayerView> PlayerViewPrefabDict { get; }    // プレイヤー表示用ビューのディクショナリ
        public Dictionary<CardPileType, BasicCardPileView> PileViewPrefabDict { get; }    // カードパイル表示用ビューのディクショナリ
        public CardView CardViewPrefab { get; }    // カード表示用ビューのプレハブ
        public StageView StageViewPrefab { get; }    // ステージ表示用ビューのプレハブ
        public Vector3 StageSpawnPosition { get; }    // ステージ表示用ビューの生成位置
        public Transform StageRoot { get; }    // ステージのルート
        public Transform PlayerRoot { get; }    // プレイヤーのルート
        public List<Vector3> PlayerLocations { get; }    // プレイヤーの位置リスト
        public Dictionary<PlayerType, CardPileLayoutSettings> PlayerPilesLayoutSettings { get; }    // プレイヤーのカードパイルのレイアウト設定
        public CardPileLayoutSettings TrashPileLayoutSettings { get; }    // トラッシュのカードパイルのレイアウト設定
        public CardPileLayoutSettings StackPileLayoutSettings { get; }    // スタックのカードパイルのレイアウト設定

        /* -------- 2. コンストラクタ -------- */
        /// <summary>
        /// フィールドのセットアップ設定を作成します。
        /// </summary>
        /// <param name="participantInfoList">参加者情報リスト</param>
        /// <param name="playerViewPrefabDict">プレイヤー表示用ビューのディクショナリ</param>
        /// <param name="pileViewPrefabDict">カードパイル表示用ビューのディクショナリ</param>
        /// <param name="cardViewPrefab">カード表示用ビューのプレハブ</param>
        /// <param name="stageViewPrefab">ステージ表示用ビューのプレハブ</param>
        /// <param name="stageSpawnPosition">ステージ表示用ビューの生成位置</param>
        /// <param name="stageRoot">ステージのルート</param>
        /// <param name="playerRoot">プレイヤーのルート</param>
        /// <param name="playerLocations">プレイヤーの位置リスト</param>
        /// <param name="playerPilesLayoutSettings">プレイヤーのカードパイルのレイアウト設定</param>
        /// <param name="trashPileLayoutSettings">トラッシュのカードパイルのレイアウト設定</param>
        /// <param name="stackPileLayoutSettings">スタックのカードパイルのレイアウト設定</param>        
        public FieldSetupSettings(
            List<PlayerInfo> participantInfoList,
            Dictionary<PlayerType, BasicPlayerView> playerViewPrefabDict,
            Dictionary<CardPileType, BasicCardPileView> pileViewPrefabDict,
            CardView cardViewPrefab,
            StageView stageViewPrefab,
            Vector3 stageSpawnPosition,
            Transform stageRoot,
            Transform playerRoot,
            List<Vector3> playerLocations,
            Dictionary<PlayerType, CardPileLayoutSettings> playerPilesLayoutSettings,
            CardPileLayoutSettings trashPileLayoutSettings,
            CardPileLayoutSettings stackPileLayoutSettings
        )
        {
            ParticipantInfoList = participantInfoList;
            PlayerViewPrefabDict = playerViewPrefabDict;
            PileViewPrefabDict = pileViewPrefabDict;
            CardViewPrefab = cardViewPrefab;
            StageViewPrefab = stageViewPrefab;
            StageSpawnPosition = stageSpawnPosition;
            StageRoot = stageRoot;
            PlayerRoot = playerRoot;
            PlayerLocations = playerLocations;
            PlayerPilesLayoutSettings = playerPilesLayoutSettings;
            TrashPileLayoutSettings = trashPileLayoutSettings;
            StackPileLayoutSettings = stackPileLayoutSettings;
        }
    }
}