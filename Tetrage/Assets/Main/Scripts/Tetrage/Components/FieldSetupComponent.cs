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
    /// FieldSetupManagerに必要な依存性と設定を準備・提供するMonoBehaviourコンポーネント
    /// ScriptableObjectとシーン固有設定を組み合わせて統合的なセットアップを実現
    /// </summary>
    public class FieldSetupComponent : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private FieldSetupPrefabConfig prefabConfig;

        [Header("Scene Dependencies")]
        [SerializeField] private Transform stageRoot;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Vector3 stageSpawnPosition;
        [SerializeField] private List<Vector3> playerLocations;

        [Header("Game Settings")]
        [SerializeField] private List<PlayerInfo> participantInfoList;

        [Header("Card Pile Layout Settings")]
        [SerializeField] private CardPileLayoutConfig handsLayoutConfig;
        [SerializeField] private CardPileLayoutConfig tmpLayoutConfig;
        [SerializeField] private CardPileLayoutConfig targetLayoutConfig;
        [SerializeField] private CardPileLayoutConfig trashPileLayoutConfig;
        [SerializeField] private CardPileLayoutConfig stackPileLayoutConfig;

        private FieldSetupManager _fieldSetupManager;

        /// <summary>
        /// セットアップされたFieldSetupManagerを取得
        /// </summary>
        public FieldSetupManager FieldSetupManager => _fieldSetupManager;

        private void Awake()
        {
            ValidateConfiguration();
        }

        /// <summary>
        /// フィールドセットアップを実行
        /// </summary>
        [ContextMenu("Setup Field")]
        public void SetupField()
        {
            try
            {
                var dependencies = CreateFieldSetupDependencies();
                var settings = CreateFieldSetupSettings();

                _fieldSetupManager = new FieldSetupManager(settings, dependencies);
                _fieldSetupManager.SetupField();

                // セットアップ完了後、DealerにStageとPlayersを設定
                SetupDealer();

                Debug.Log("フィールドセットアップが完了しました");
            }
            catch (Exception ex)
            {
                Debug.LogError($"フィールドセットアップに失敗しました: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// FieldSetupDependenciesを作成（直接インスタンス化）
        /// </summary>
        private FieldSetupDependencies CreateFieldSetupDependencies()
        {
            // 基本となるモデル用Factoryを作成
            var cardModelFactory = new CardWithViewFactory(new CardModelFactory(), prefabConfig.CardViewPrefab);
            var cardPileFactory = new CardPileFactory();

            // 依存関係のあるFactoryを作成
            var stageModelFactory = new StageModelFactory(cardPileFactory, cardModelFactory);
            var playerModelFactory = new PlayerModelFactory(cardPileFactory, cardModelFactory);

            return new FieldSetupDependencies(
                cardModelFactory,
                stageModelFactory,
                playerModelFactory,
                cardPileFactory);
        }

        /// <summary>
        /// FieldSetupSettingsを作成
        /// </summary>
        private FieldSetupSettings CreateFieldSetupSettings()
        {
            Debug.Log("FieldSetupSettingsの作成を開始します", this);

            try
            {
                Debug.Log("プレイヤービューPrefabディクショナリを作成中...", this);
                var playerViewPrefabDict = CreatePlayerViewPrefabDict();

                Debug.Log("カードパイルビューPrefabディクショナリを作成中...", this);
                var pileViewPrefabDict = CreatePileViewPrefabDict();

                Debug.Log("カードパイルレイアウト設定ディクショナリを作成中...", this);
                var cardPileLayoutSettingsDict = CreateCardPileLayoutSettingsDict();

                Debug.Log("FieldSetupSettingsインスタンスを作成中...", this);

                // 各パラメータのnullチェック
                if (participantInfoList == null)
                {
                    Debug.LogError("participantInfoListがnullです", this);
                    throw new System.NullReferenceException("participantInfoList is null");
                }

                if (playerLocations == null)
                {
                    Debug.LogError("playerLocationsがnullです", this);
                    throw new System.NullReferenceException("playerLocations is null");
                }

                if (stageRoot == null)
                {
                    Debug.LogError("stageRootがnullです", this);
                    throw new System.NullReferenceException("stageRoot is null");
                }

                if (playerRoot == null)
                {
                    Debug.LogError("playerRootがnullです", this);
                    throw new System.NullReferenceException("playerRoot is null");
                }

                Debug.Log($"参加者数: {participantInfoList.Count}, プレイヤー位置数: {playerLocations.Count}", this);

                return new FieldSetupSettings(
                    participantInfoList,
                    playerViewPrefabDict,
                    pileViewPrefabDict,
                    prefabConfig.CardViewPrefab,
                    prefabConfig.StageViewPrefab,
                    stageSpawnPosition,
                    stageRoot,
                    playerRoot,
                    playerLocations,
                    cardPileLayoutSettingsDict);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"FieldSetupSettings作成中にエラーが発生しました: {ex.Message}", this);
                throw;
            }
        }

        /// <summary>
        /// プレイヤービューPrefabディクショナリを作成
        /// </summary>
        private Dictionary<PlayerType, BasicPlayerView> CreatePlayerViewPrefabDict()
        {
            // デバッグ: prefabConfigのnullチェック
            if (prefabConfig == null)
            {
                Debug.LogError("prefabConfigがnullです。FieldSetupPrefabConfigを設定してください。", this);
                throw new System.NullReferenceException("prefabConfig is null");
            }

            // デバッグ: 各プレイヤービューPrefabのnullチェック
            if (prefabConfig.LocalPlayerViewPrefab == null)
            {
                Debug.LogError("prefabConfig.LocalPlayerViewPrefabがnullです。", this);
                throw new System.NullReferenceException("LocalPlayerViewPrefab is null");
            }

            if (prefabConfig.RemotePlayerViewPrefab == null)
            {
                Debug.LogError("prefabConfig.RemotePlayerViewPrefabがnullです。", this);
                throw new System.NullReferenceException("RemotePlayerViewPrefab is null");
            }

            if (prefabConfig.BotPlayerViewPrefab == null)
            {
                Debug.LogError("prefabConfig.BotPlayerViewPrefabがnullです。", this);
                throw new System.NullReferenceException("BotPlayerViewPrefab is null");
            }

            Debug.Log("プレイヤービューPrefabディクショナリを作成します", this);
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
        private Dictionary<CardPileType, BasicCardPileView> CreatePileViewPrefabDict()
        {
            return new Dictionary<CardPileType, BasicCardPileView>
            {
                { CardPileType.Basic, prefabConfig.BasicCardPileViewPrefab },
                { CardPileType.Hands, prefabConfig.HandsCardPileViewPrefab },
                { CardPileType.Tmp, prefabConfig.TmpCardPileViewPrefab },
                { CardPileType.Stack, prefabConfig.StackCardPileViewPrefab },
                { CardPileType.Trash, prefabConfig.TrashCardPileViewPrefab },
                { CardPileType.Target, prefabConfig.BasicCardPileViewPrefab } // Targetは基本ビューを使用
            };
        }

        /// <summary>
        /// カードパイルタイプごとのレイアウト設定ディクショナリを作成
        /// </summary>
        private Dictionary<CardPileType, CardPileLayoutSettings> CreateCardPileLayoutSettingsDict()
        {
            return new Dictionary<CardPileType, CardPileLayoutSettings>
            {
                { CardPileType.Hands, ConvertToLayoutSettings(handsLayoutConfig) },
                { CardPileType.Tmp, ConvertToLayoutSettings(tmpLayoutConfig) },
                { CardPileType.Target, ConvertToLayoutSettings(targetLayoutConfig) },
                { CardPileType.Stack, ConvertToLayoutSettings(stackPileLayoutConfig) },
                { CardPileType.Trash, ConvertToLayoutSettings(trashPileLayoutConfig) },
                { CardPileType.Basic, CardPileLayoutSettings.Default } // Basicはデフォルト設定
            };
        }

        /// <summary>
        /// CardPileLayoutConfigをCardPileLayoutSettingsに変換
        /// </summary>
        private CardPileLayoutSettings ConvertToLayoutSettings(CardPileLayoutConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("CardPileLayoutConfigがnullです。デフォルト設定を使用します。");
                return CardPileLayoutSettings.Default;
            }

            return new CardPileLayoutSettings(
                config.PileWidth,
                config.MinSpacing,
                config.MaxSpacing,
                config.PositionOffset);
        }

        /// <summary>
        /// DealerにStageとPlayersを設定
        /// </summary>
        private void SetupDealer()
        {
            if (_fieldSetupManager == null)
            {
                Debug.LogError("FieldSetupManagerが初期化されていません");
                return;
            }

            var dealer = Dealer.Instance;
            dealer.SetStage(_fieldSetupManager.Stage);
            dealer.SetPlayers(_fieldSetupManager.Players);

            Debug.Log("DealerにStageとPlayersを設定しました");
        }

        /// <summary>
        /// 設定の妥当性を検証
        /// </summary>
        private void ValidateConfiguration()
        {
            if (prefabConfig == null)
                Debug.LogError("FieldSetupPrefabConfigが設定されていません", this);

            if (stageRoot == null)
                Debug.LogError("StageRootが設定されていません", this);

            if (playerRoot == null)
                Debug.LogError("PlayerRootが設定されていません", this);

            if (participantInfoList == null || participantInfoList.Count == 0)
                Debug.LogError("ParticipantInfoListが設定されていません", this);

            if (playerLocations == null || playerLocations.Count == 0)
                Debug.LogError("PlayerLocationsが設定されていません", this);

            // Layout Config の検証
            ValidateLayoutConfig("HandsLayoutConfig", handsLayoutConfig);
            ValidateLayoutConfig("TmpLayoutConfig", tmpLayoutConfig);
            ValidateLayoutConfig("TargetLayoutConfig", targetLayoutConfig);
            ValidateLayoutConfig("TrashPileLayoutConfig", trashPileLayoutConfig);
            ValidateLayoutConfig("StackPileLayoutConfig", stackPileLayoutConfig);
        }

        /// <summary>
        /// レイアウト設定の妥当性を検証
        /// </summary>
        private void ValidateLayoutConfig(string configName, CardPileLayoutConfig config)
        {
            if (config == null)
                Debug.LogWarning($"{configName}が設定されていません。デフォルト設定を使用します。", this);
        }

        /// <summary>
        /// Inspectorでのテスト用メソッド
        /// </summary>
        [ContextMenu("Validate Configuration")]
        private void ValidateConfigurationMenu()
        {
            ValidateConfiguration();
            Debug.Log("設定の妥当性検証が完了しました");
        }

        /// <summary>
        /// Factory依存関係作成テスト用メソッド
        /// </summary>
        [ContextMenu("Test Dependencies Creation")]
        private void TestDependenciesCreation()
        {
            try
            {
                var dependencies = CreateFieldSetupDependencies();
                Debug.Log("全てのFactory依存関係の作成に成功しました");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Factory依存関係作成テストに失敗しました: {ex.Message}");
            }
        }
    }
}