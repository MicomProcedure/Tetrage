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
    /// 優先的にシーン上のFieldSetupComponentを利用し、存在しない場合のみInspector設定を使用します
    /// </summary>
    public class FieldSetupManagerTester : MonoBehaviour
    {
        [Header("FieldSetupComponent参照")]
        [SerializeField] private FieldSetupComponent fieldSetupComponent;
        [Tooltip("nullの場合、シーン内のFieldSetupComponentを自動検索します")]

        [Header("フォールバック用プレハブ設定")]
        [SerializeField] private CardView cardViewPrefab;
        [SerializeField] private StageView stageViewPrefab;
        [SerializeField] private BasicPlayerView localPlayerViewPrefab;
        [SerializeField] private BasicPlayerView remotePlayerViewPrefab;
        [SerializeField] private BasicPlayerView botPlayerViewPrefab;

        [Header("フォールバック用カードパイルビュープレハブ")]
        [SerializeField] private BasicCardPileView basicCardPileViewPrefab;
        [SerializeField] private BasicCardPileView handsCardPileViewPrefab;
        [SerializeField] private BasicCardPileView tmpCardPileViewPrefab;
        [SerializeField] private BasicCardPileView stackCardPileViewPrefab;
        [SerializeField] private BasicCardPileView trashCardPileViewPrefab;

        [Header("フォールバック用シーン内の参照")]
        [SerializeField] private Transform stageRoot;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private PositionConfig playerPositionConfig;
        [SerializeField] private PositionConfig stagePositionConfig;

        [Header("フォールバック用プレイヤー設定")]
        [SerializeField]
        private List<TestPlayerData> testPlayerList = new List<TestPlayerData>
        {
            new TestPlayerData { userId = "LocalPlayer1", playerType = PlayerType.Local },
            new TestPlayerData { userId = "RemotePlayer1", playerType = PlayerType.Remote }
        };

        [Header("フォールバック用レイアウト設定")]
        [SerializeField]
        private CardPileLayoutSettingsData handsLayoutSettings = new CardPileLayoutSettingsData
        {
            pileWidth = 15f,
            minSpacing = 1f,
            maxSpacing = 3f,
            positionOffset = Vector3.zero
        };

        [SerializeField]
        private CardPileLayoutSettingsData tmpLayoutSettings = new CardPileLayoutSettingsData
        {
            pileWidth = 8f,
            minSpacing = 0.5f,
            maxSpacing = 2f,
            positionOffset = Vector3.zero
        };

        [SerializeField]
        private CardPileLayoutSettingsData targetLayoutSettings = new CardPileLayoutSettingsData
        {
            pileWidth = 5f,
            minSpacing = 0f,
            maxSpacing = 1f,
            positionOffset = Vector3.zero
        };

        [SerializeField]
        private CardPileLayoutSettingsData stackLayoutSettings = new CardPileLayoutSettingsData
        {
            pileWidth = 5f,
            minSpacing = 0f,
            maxSpacing = 0f,
            positionOffset = new Vector3(-5f, 0f, 0f)
        };

        [SerializeField]
        private CardPileLayoutSettingsData trashLayoutSettings = new CardPileLayoutSettingsData
        {
            pileWidth = 5f,
            minSpacing = 0f,
            maxSpacing = 0f,
            positionOffset = new Vector3(5f, 0f, 0f)
        };

        [Header("テスト設定")]
        [SerializeField] private bool autoSetupOnStart = false;
        [SerializeField] private bool forceUseInspectorSettings = false;
        [Tooltip("trueの場合、FieldSetupComponentが存在してもInspector設定を強制使用")]

        // セットアップ後のインスタンス
        private FieldSetupManager _fieldSetupManager;
        private bool _usingFieldSetupComponent = false;

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

                // FieldSetupComponentの取得を試行
                var targetFieldSetupComponent = GetTargetFieldSetupComponent();

                if (targetFieldSetupComponent != null && !forceUseInspectorSettings)
                {
                    // FieldSetupComponentを使用してセットアップ
                    SetupUsingFieldSetupComponent(targetFieldSetupComponent);
                }
                else
                {
                    // フォールバック: Inspector設定を使用してセットアップ
                    SetupUsingInspectorSettings();
                }

                Debug.Log($"FieldSetupManagerTester: セットアップが完了しました。プレイヤー数: {_fieldSetupManager.Players.Count}");
                Debug.Log($"使用した設定ソース: {(_usingFieldSetupComponent ? "FieldSetupComponent" : "Inspector設定")}");

                // 結果をログ出力
                LogSetupResults();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FieldSetupManagerTester: セットアップ中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 使用するFieldSetupComponentを取得
        /// </summary>
        private FieldSetupComponent GetTargetFieldSetupComponent()
        {
            // 1. Inspector設定を優先
            if (fieldSetupComponent != null)
            {
                Debug.Log("Inspector設定のFieldSetupComponentを使用します");
                return fieldSetupComponent;
            }

            // 2. シーン内を検索
            var foundComponent = FindObjectOfType<FieldSetupComponent>();
            if (foundComponent != null)
            {
                Debug.Log("シーン内で見つかったFieldSetupComponentを使用します");
                return foundComponent;
            }

            Debug.Log("FieldSetupComponentが見つかりませんでした。Inspector設定を使用します");
            return null;
        }

        /// <summary>
        /// FieldSetupComponentを使用したセットアップ
        /// </summary>
        private void SetupUsingFieldSetupComponent(FieldSetupComponent component)
        {
            Debug.Log("FieldSetupComponentを使用してセットアップを実行します");

            // FieldSetupComponentのSetupFieldを実行
            component.SetupField();

            // セットアップされたManagerを取得
            _fieldSetupManager = component.FieldSetupManager;
            _usingFieldSetupComponent = true;

            if (_fieldSetupManager == null)
            {
                throw new System.Exception("FieldSetupComponentからFieldSetupManagerを取得できませんでした");
            }
        }

        /// <summary>
        /// Inspector設定を使用したセットアップ（従来の方式）
        /// </summary>
        private void SetupUsingInspectorSettings()
        {
            Debug.Log("Inspector設定を使用してセットアップを実行します");

            // 必要なコンポーネントの検証
            if (!ValidateRequiredComponents())
            {
                Debug.LogError("FieldSetupManagerTester: 必要なコンポーネントが不足しています");
                return;
            }

            // FieldSetupSettingsとDependenciesを作成
            var settings = CreateFieldSetupSettingsFromInspector();
            var dependencies = CreateFieldSetupDependenciesFromInspector();

            // FieldSetupManagerを作成してセットアップ実行
            _fieldSetupManager = new FieldSetupManager(settings, dependencies);
            _fieldSetupManager.SetupField();
            _usingFieldSetupComponent = false;
        }

        /// <summary>
        /// 必要なコンポーネントが設定されているかを検証（Inspector設定用）
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

            // カードパイルビュープレハブの検証
            if (basicCardPileViewPrefab == null || handsCardPileViewPrefab == null ||
                tmpCardPileViewPrefab == null || stackCardPileViewPrefab == null ||
                trashCardPileViewPrefab == null)
            {
                Debug.LogError("カードパイルビュープレハブが不足しています");
                isValid = false;
            }

            // プレイヤー位置の数とプレイヤー数の整合性チェック
            if (playerPositionConfig != null && !playerPositionConfig.ValidateConfig(testPlayerList.Count))
            {
                Debug.LogError($"PositionMarkerの位置数({playerPositionConfig.Position.Count})とプレイヤー数({testPlayerList.Count})が一致しません");
                isValid = false;
            }

            if (stagePositionConfig != null && !stagePositionConfig.ValidateConfig(1))
            {
                Debug.LogError($"StagePositionConfigの位置数({stagePositionConfig.Position.Count})が1ではありません");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// FieldSetupDependenciesを作成（Inspector設定用）
        /// </summary>
        private FieldSetupDependencies CreateFieldSetupDependenciesFromInspector()
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
        /// FieldSetupSettingsを作成（Inspector設定用）
        /// </summary>
        private FieldSetupSettings CreateFieldSetupSettingsFromInspector()
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
                { PlayerType.Local, localPlayerViewPrefab },
                { PlayerType.Remote, remotePlayerViewPrefab },
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
            var playerLocations = playerPositionConfig != null ? playerPositionConfig.Position : new List<Vector3>();

            // カードパイルタイプごとのレイアウト設定辞書を作成
            var cardPileLayoutSettingsDict = new Dictionary<CardPileType, CardPileLayoutSettings>
            {
                { CardPileType.Hands, handsLayoutSettings.ToCardPileLayoutSettings() },
                { CardPileType.Tmp, tmpLayoutSettings.ToCardPileLayoutSettings() },
                { CardPileType.Target, targetLayoutSettings.ToCardPileLayoutSettings() },
                { CardPileType.Stack, stackLayoutSettings.ToCardPileLayoutSettings() },
                { CardPileType.Trash, trashLayoutSettings.ToCardPileLayoutSettings() },
                { CardPileType.Basic, CardPileLayoutSettings.Default }
            };

            var stageSpawnPosition = stagePositionConfig != null && stagePositionConfig.Position.Count > 0
                ? stagePositionConfig.Position[0]
                : Vector3.zero;

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
                cardPileLayoutSettingsDict
            );
        }

        /// <summary>
        /// セットアップ結果をログ出力
        /// </summary>
        private void LogSetupResults()
        {
            if (_fieldSetupManager == null) return;

            Debug.Log("=== FieldSetupManager セットアップ結果 ===");
            Debug.Log($"設定ソース: {(_usingFieldSetupComponent ? "FieldSetupComponent" : "Inspector設定")}");

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
        /// 使用している設定ソースを取得
        /// </summary>
        public bool IsUsingFieldSetupComponent()
        {
            return _usingFieldSetupComponent;
        }

        /// <summary>
        /// フィールドをクリア（テストの後始末用）
        /// </summary>
        [ContextMenu("フィールドをクリア")]
        public void ClearField()
        {
            if (_usingFieldSetupComponent && fieldSetupComponent != null)
            {
                // FieldSetupComponentを使用している場合は、そちらの参照先をクリア
                var targetStageRoot = fieldSetupComponent.transform.Find("StageRoot");
                var targetPlayerRoot = fieldSetupComponent.transform.Find("PlayerRoot");

                ClearTransformChildren(targetStageRoot);
                ClearTransformChildren(targetPlayerRoot);
            }
            else
            {
                // Inspector設定を使用している場合は、従来通りの処理
                ClearTransformChildren(stageRoot);
                ClearTransformChildren(playerRoot);
            }

            _fieldSetupManager = null;
            _usingFieldSetupComponent = false;
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
        /// Inspector用のテストボタン群
        /// </summary>
        void OnValidate()
        {
            // Inspectorでの値変更時の検証処理
            if (testPlayerList.Count == 0)
            {
                testPlayerList.Add(new TestPlayerData { userId = "TestPlayer1", playerType = PlayerType.Local });
            }
        }

        /// <summary>
        /// 現在の設定状況を表示（デバッグ用）
        /// </summary>
        [ContextMenu("設定状況を表示")]
        public void ShowCurrentSettings()
        {
            Debug.Log("=== FieldSetupManagerTester 設定状況 ===");

            var targetComponent = GetTargetFieldSetupComponent();
            if (targetComponent != null && !forceUseInspectorSettings)
            {
                Debug.Log($"使用予定の設定ソース: FieldSetupComponent ({targetComponent.name})");
            }
            else
            {
                Debug.Log("使用予定の設定ソース: Inspector設定");
                Debug.Log($"テストプレイヤー数: {testPlayerList.Count}");
                foreach (var player in testPlayerList)
                {
                    Debug.Log($"  - {player.userId} ({player.playerType})");
                }
            }

            if (forceUseInspectorSettings)
            {
                Debug.Log("注意: Inspector設定の強制使用が有効です");
            }

            Debug.Log("================================");
        }
    }
}