using UnityEngine;
using System.Collections.Generic;
using Tetrage.Managers;
using Tetrage.Core.Enums;
using Tetrage.Core.DTO;
using Tetrage.UI;
using Tetrage.Factories;
using Tetrage.Components;
using Tetrage.Core.Contracts;

namespace Tetrage.Tests
{
    /// <summary>
    /// FieldSetupManagerのプレイモードテストクラス
    /// Inspectorから必要なプレハブや設定を受け取り、フィールドセットアップの動作を確認します
    /// </summary>
    public class FieldSetupManagerTester : MonoBehaviour
    {
        [Header("プレハブ設定")]
        [SerializeField] private CardView cardViewPrefab;
        [SerializeField] private StageView stageViewPrefab;
        [SerializeField] private BasicPlayerView localPlayerViewPrefab;
        [SerializeField] private BasicPlayerView remotePlayerViewPrefab;
        [SerializeField] private BasicPlayerView botPlayerViewPrefab;
        
        [Header("カードパイルビュープレハブ")]
        [SerializeField] private BasicCardPileView basicCardPileViewPrefab;
        [SerializeField] private BasicCardPileView handsCardPileViewPrefab;
        [SerializeField] private BasicCardPileView tmpCardPileViewPrefab;
        [SerializeField] private BasicCardPileView stackCardPileViewPrefab;
        [SerializeField] private BasicCardPileView trashCardPileViewPrefab;
        
        [Header("シーン内の参照")]
        [SerializeField] private Transform stageRoot;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private PositionMarker positionMarker;
        
        [Header("ステージ設定")]
        [SerializeField] private Vector3 stageSpawnPosition = Vector3.zero;
        
        [Header("プレイヤー設定")]
        [SerializeField] private List<TestPlayerData> testPlayerList = new List<TestPlayerData>
        {
            new TestPlayerData { userId = "LocalPlayer1", playerType = PlayerType.LocalPlayer },
            new TestPlayerData { userId = "RemotePlayer1", playerType = PlayerType.RemotePlayer }
        };
        
        [Header("レイアウト設定")]
        [SerializeField] private CardPileLayoutSettingsData handsLayoutSettings = new CardPileLayoutSettingsData
        {
            pileWidth = 15f,
            minSpacing = 1f,
            maxSpacing = 3f,
            positionOffset = Vector3.zero
        };
        
        [SerializeField] private CardPileLayoutSettingsData tmpLayoutSettings = new CardPileLayoutSettingsData
        {
            pileWidth = 8f,
            minSpacing = 0.5f,
            maxSpacing = 2f,
            positionOffset = Vector3.zero
        };
        
        [SerializeField] private CardPileLayoutSettingsData targetLayoutSettings = new CardPileLayoutSettingsData
        {
            pileWidth = 5f,
            minSpacing = 0f,
            maxSpacing = 1f,
            positionOffset = Vector3.zero
        };
        
        [SerializeField] private CardPileLayoutSettingsData stackLayoutSettings = new CardPileLayoutSettingsData
        {
            pileWidth = 5f,
            minSpacing = 0f,
            maxSpacing = 0f,
            positionOffset = new Vector3(-5f, 0f, 0f)
        };
        
        [SerializeField] private CardPileLayoutSettingsData trashLayoutSettings = new CardPileLayoutSettingsData
        {
            pileWidth = 5f,
            minSpacing = 0f,
            maxSpacing = 0f,
            positionOffset = new Vector3(5f, 0f, 0f)
        };
        
        [Header("テスト設定")]
        [SerializeField] private bool autoSetupOnStart = false;
        
        // セットアップ後のインスタンス
        private FieldSetupManager _fieldSetupManager;
        
        /// <summary>
        /// テスト用プレイヤーデータ構造体
        /// </summary>
        [System.Serializable]
        public struct TestPlayerData
        {
            public string userId;
            public PlayerType playerType;
        }
        
        /// <summary>
        /// Inspector用のレイアウト設定データ構造体
        /// </summary>
        [System.Serializable]
        public struct CardPileLayoutSettingsData
        {
            public float pileWidth;
            public float minSpacing;
            public float maxSpacing;
            public Vector3 positionOffset;
            
            /// <summary>
            /// CardPileLayoutSettingsに変換
            /// </summary>
            public CardPileLayoutSettings ToCardPileLayoutSettings()
            {
                return new CardPileLayoutSettings(pileWidth, minSpacing, maxSpacing, positionOffset);
            }
        }
        
        void Start()
        {
            if (autoSetupOnStart)
            {
                SetupField();
            }
        }
        
        /// <summary>
        /// フィールドセットアップを実行します（パブリックメソッド、Inspectorボタンから呼び出し可能）
        /// </summary>
        [ContextMenu("フィールドセットアップを実行")]
        public void SetupField()
        {
            try
            {
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
                
                // FieldSetupManagerを作成してセットアップ実行
                _fieldSetupManager = new FieldSetupManager(settings, dependencies);
                _fieldSetupManager.SetupField();
                
                Debug.Log($"FieldSetupManagerTester: セットアップが完了しました。プレイヤー数: {_fieldSetupManager.Players.Count}");
                
                // 結果をログ出力
                LogSetupResults();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FieldSetupManagerTester: セットアップ中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// 必要なコンポーネントが設定されているかを検証
        /// </summary>
        private bool ValidateRequiredComponents()
        {
            bool isValid = true;
            
            if (cardViewPrefab == null)
            {
                Debug.LogError("CardViewPrefabが設定されていません");
                isValid = false;
            }
            
            if (stageViewPrefab == null)
            {
                Debug.LogError("StageViewPrefabが設定されていません");
                isValid = false;
            }
            
            if (stageRoot == null)
            {
                Debug.LogError("StageRootが設定されていません");
                isValid = false;
            }
            
            if (playerRoot == null)
            {
                Debug.LogError("PlayerRootが設定されていません");
                isValid = false;
            }
            
            if (positionMarker == null)
            {
                Debug.LogError("PositionMarkerが設定されていません");
                isValid = false;
            }
            
            // プレイヤービュープレハブの検証
            if (localPlayerViewPrefab == null || remotePlayerViewPrefab == null || botPlayerViewPrefab == null)
            {
                Debug.LogError("プレイヤービュープレハブが不足しています");
                isValid = false;
            }
            
            // カードパイルビュープレハブの検証
            if (basicCardPileViewPrefab == null || handsCardPileViewPrefab == null || 
                tmpCardPileViewPrefab == null || stackCardPileViewPrefab == null || 
                trashCardPileViewPrefab == null)
            {
                Debug.LogError("カードパイルビュープレハブが不足しています");
                isValid = false;
            }
            
            // プレイヤー位置の数とプレイヤー数の整合性チェック
            if (!positionMarker.ValidateConfig(testPlayerList.Count))
            {
                Debug.LogError($"PositionMarkerの位置数({positionMarker.Position.Count})とプレイヤー数({testPlayerList.Count})が一致しません");
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
        /// FieldSetupSettingsを作成
        /// </summary>
        private FieldSetupSettings CreateFieldSetupSettings()
        {
            // 参加者情報リストを作成
            var participantInfoList = new List<PlayerInfo>();
            foreach (var testPlayer in testPlayerList)
            {
                participantInfoList.Add(new PlayerInfo
                {
                    UserId = testPlayer.userId,
                    PlayerType = testPlayer.playerType
                });
            }
            
            // プレイヤービュープレハブ辞書を作成
            var playerViewPrefabDict = new Dictionary<PlayerType, BasicPlayerView>
            {
                { PlayerType.LocalPlayer, localPlayerViewPrefab },
                { PlayerType.RemotePlayer, remotePlayerViewPrefab },
                { PlayerType.Bot, botPlayerViewPrefab }
            };
            
            // カードパイルビュープレハブ辞書を作成
            var pileViewPrefabDict = new Dictionary<CardPileType, BasicCardPileView>
            {
                { CardPileType.Basic, basicCardPileViewPrefab },
                { CardPileType.Hands, handsCardPileViewPrefab },
                { CardPileType.Tmp, tmpCardPileViewPrefab },
                { CardPileType.Target, basicCardPileViewPrefab }, // Targetは基本ビューを使用
                { CardPileType.Stack, stackCardPileViewPrefab },
                { CardPileType.Trash, trashCardPileViewPrefab }
            };
            
            // プレイヤー位置リストを作成
            var playerLocations = positionMarker.Position;
            
            // プレイヤータイプ別レイアウト設定辞書を作成
            var playerPilesLayoutSettings = new Dictionary<PlayerType, CardPileLayoutSettings>();
            
            // 全プレイヤータイプに対して同じレイアウト設定を適用
            // 実際の運用では、プレイヤータイプごとに異なる設定も可能
            var handsLayout = handsLayoutSettings.ToCardPileLayoutSettings();
            var tmpLayout = tmpLayoutSettings.ToCardPileLayoutSettings();
            var targetLayout = targetLayoutSettings.ToCardPileLayoutSettings();
            
            foreach (PlayerType playerType in System.Enum.GetValues(typeof(PlayerType)))
            {
                playerPilesLayoutSettings[playerType] = handsLayout; // 簡単のため、全プレイヤーに同じ設定を適用
            }
            
            return new FieldSetupSettings(
                participantInfoList,
                playerViewPrefabDict,
                pileViewPrefabDict,
                cardViewPrefab,
                stageViewPrefab,
                stageSpawnPosition,
                stageRoot,
                playerRoot,
                playerLocations,
                playerPilesLayoutSettings,
                trashLayoutSettings.ToCardPileLayoutSettings(),
                stackLayoutSettings.ToCardPileLayoutSettings()
            );
        }
        
        /// <summary>
        /// セットアップ結果をログ出力
        /// </summary>
        private void LogSetupResults()
        {
            if (_fieldSetupManager == null) return;
            
            Debug.Log("=== FieldSetupManager セットアップ結果 ===");
            
            // プレイヤー情報
            var players = _fieldSetupManager.Players;
            Debug.Log($"生成されたプレイヤー数: {players.Count}");
            
            // PositionMarkerの位置情報も表示
            var positions = positionMarker.Position;
            Debug.Log($"PositionMarkerの位置数: {positions.Count}");
            
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                Debug.Log($"プレイヤー{i + 1}: UserID={player.UserId}, " +
                         $"Hands={player.Hands.Cards.Count}枚, " +
                         $"Tmp={player.Tmp.Cards.Count}枚, " +
                         $"Target={player.Target.Cards.Count}枚");
                         
                // プレイヤーの想定位置も表示
                if (i < positions.Count)
                {
                    Debug.Log($"  想定位置: {positions[i]}");
                }
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
            if (playerRoot != null)
            {
                for (int i = playerRoot.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(playerRoot.GetChild(i).gameObject);
                }
            }
            
            // ステージの子オブジェクトを削除
            if (stageRoot != null)
            {
                for (int i = stageRoot.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(stageRoot.GetChild(i).gameObject);
                }
            }
            
            _fieldSetupManager = null;
            Debug.Log("フィールドがクリアされました");
        }
        
        /// <summary>
        /// Inspector用のテストボタン群
        /// </summary>
        void OnValidate()
        {
            // Inspectorでの値変更時の検証処理
            if (testPlayerList.Count == 0)
            {
                testPlayerList.Add(new TestPlayerData { userId = "TestPlayer1", playerType = PlayerType.LocalPlayer });
            }
        }
    }
} 