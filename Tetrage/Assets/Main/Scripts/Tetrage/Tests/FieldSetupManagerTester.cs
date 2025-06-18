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
    /// シーン上のFieldSetupComponentからデータを受け取ってテストを実行します
    /// </summary>
    public class FieldSetupManagerTester : MonoBehaviour
    {
        [Header("FieldSetupComponent参照")]
        [SerializeField] private FieldSetupComponent fieldSetupComponent;

        [Header("テスト設定")]
        [SerializeField] private bool autoSetupOnStart = false;
        [SerializeField] private bool logDetailedResults = true;
        [SerializeField] private bool clearRegistryBeforeSetup = true;
        [SerializeField] private bool preventDuplicateSetup = true;

        [Header("デバッグ用参加者情報")]
        [SerializeField] private bool useCustomParticipantList = true;
        [SerializeField] private List<PlayerInfo> debugParticipantInfoList = new List<PlayerInfo>
        {
            new PlayerInfo { UserId = "DebugPlayer1", PlayerType = PlayerType.Local },
            new PlayerInfo { UserId = "DebugPlayer2", PlayerType = PlayerType.Local },
            new PlayerInfo { UserId = "DebugPlayer3", PlayerType = PlayerType.Bot },
            new PlayerInfo { UserId = "DebugPlayer4", PlayerType = PlayerType.Remote }
        };

        // セットアップ後のインスタンス
        private FieldSetupManager _fieldSetupManager;
        
        // 重複セットアップ防止フラグ
        private bool _isSetupInProgress = false;

        void Start()
        {
            // FieldSetupComponentが設定されていない場合、自動検索
            if (fieldSetupComponent == null)
            {
                fieldSetupComponent = FindObjectOfType<FieldSetupComponent>();
                if (fieldSetupComponent != null)
                {
                    Debug.Log("FieldSetupManagerTester: FieldSetupComponentを自動検出しました");
                }
            }

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

                // FieldSetupComponentの検証
                if (!ValidateFieldSetupComponent())
                {
                    Debug.LogError("FieldSetupManagerTester: FieldSetupComponentが利用できません");
                    return;
                }

                // セットアップ前のクリーンアップ
                if (clearRegistryBeforeSetup)
                {
                    PerformPreSetupCleanup();
                }

                // 参加者情報を取得
                List<PlayerInfo> participantInfoList = GetParticipantInfoList();
                
                // FieldSetupComponentから設定を取得
                var settings = CreateFieldSetupSettings(participantInfoList);
                var dependencies = CreateFieldSetupDependencies();
                
                // FieldSetupManagerを直接作成・実行
                _fieldSetupManager = new FieldSetupManager(settings, dependencies);
                _fieldSetupManager.SetupField(participantInfoList);
                
                // セットアップ完了後処理
                SetupDealer();
                
                // セットアップ完了ログ
                Debug.Log($"FieldSetupManagerTester: フィールドセットアップが完了しました（参加者数: {participantInfoList.Count}人）");

                if (_fieldSetupManager != null)
                {
                    Debug.Log($"FieldSetupManagerTester: セットアップが完了しました。プレイヤー数: {_fieldSetupManager.Players.Count}");
                    
                    if (logDetailedResults)
                    {
                        LogSetupResults();
                    }
                }
                else
                {
                    Debug.LogError("FieldSetupManagerTester: FieldSetupManagerの取得に失敗しました");
                }
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
        /// FieldSetupComponentが使用可能かを検証
        /// </summary>
        private bool ValidateFieldSetupComponent()
        {
            if (fieldSetupComponent == null)
            {
                Debug.LogError("FieldSetupComponentが設定されていません。シーンに配置するか、Inspectorで参照を設定してください。");
                return false;
            }

            // FieldSetupComponentの基本的な設定も検証できる
            Debug.Log("FieldSetupManagerTester: FieldSetupComponentの検証が完了しました");
            return true;
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
        /// FieldSetupComponentの取得（外部からのアクセス用）
        /// </summary>
        public FieldSetupComponent GetFieldSetupComponent()
        {
            return fieldSetupComponent;
        }

        /// <summary>
        /// フィールドをクリア（テストの後始末用）
        /// </summary>
        [ContextMenu("フィールドをクリア")]
        public void ClearField()
        {
            if (fieldSetupComponent == null)
            {
                Debug.LogWarning("FieldSetupComponentが設定されていないため、手動でクリアを実行できません");
                return;
            }

            Debug.Log("FieldSetupManagerTester: フィールドクリアを開始します");

            // セットアップ中の場合は警告
            if (_isSetupInProgress)
            {
                Debug.LogWarning("セットアップ実行中ですが、強制的にクリアします");
                _isSetupInProgress = false;
            }

            // CardViewRegistryをクリア
            CardViewRegistry.Clear();
            
            // 既存のフィールドオブジェクトをクリア
            ClearExistingFieldObjects();

            _fieldSetupManager = null;
            Debug.Log("FieldSetupManagerTester: フィールドクリアが完了しました");
        }

        /// <summary>
        /// FieldSetupComponentの設定をInspectorに表示（デバッグ用）
        /// </summary>
        [ContextMenu("FieldSetupComponentの設定を表示")]
        public void ShowFieldSetupComponentSettings()
        {
            if (fieldSetupComponent == null)
            {
                Debug.LogWarning("FieldSetupComponentが設定されていません");
                return;
            }

            Debug.Log("=== FieldSetupComponent 設定情報 ===");
            
            // リフレクションを使用して設定を表示
            var stageRoot = GetStageRootFromComponent();
            var playerRoot = GetPlayerRootFromComponent();
            
            Debug.Log($"StageRoot: {(stageRoot != null ? stageRoot.name : "未設定")}");
            Debug.Log($"PlayerRoot: {(playerRoot != null ? playerRoot.name : "未設定")}");
            
            Debug.Log("=== 設定表示完了 ===");
        }

        /// <summary>
        /// FieldSetupComponentからStageRootを取得（リフレクション使用）
        /// </summary>
        private Transform GetStageRootFromComponent()
        {
            if (fieldSetupComponent == null) return null;
            
            var field = typeof(FieldSetupComponent).GetField("stageRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(fieldSetupComponent) as Transform;
        }

        /// <summary>
        /// FieldSetupComponentからPlayerRootを取得（リフレクション使用）
        /// </summary>
        private Transform GetPlayerRootFromComponent()
        {
            if (fieldSetupComponent == null) return null;
            
            var field = typeof(FieldSetupComponent).GetField("playerRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(fieldSetupComponent) as Transform;
        }

        /// <summary>
        /// FieldSetupSettingsを作成
        /// </summary>
        private FieldSetupSettings CreateFieldSetupSettings(List<PlayerInfo> participantInfoList)
        {
            // FieldSetupComponentから設定情報を取得
            var playerViewPrefabDict = new Dictionary<PlayerType, BasicPlayerView>();
            var pileViewPrefabDict = new Dictionary<CardPileType, BasicCardPileView>();
            
            // PrefabConfigから情報を取得
            var prefabConfig = fieldSetupComponent.PrefabConfig;
            
            // 必要な辞書を構築
            playerViewPrefabDict[PlayerType.Local] = prefabConfig.LocalPlayerViewPrefab;
            playerViewPrefabDict[PlayerType.Remote] = prefabConfig.RemotePlayerViewPrefab;
            playerViewPrefabDict[PlayerType.Bot] = prefabConfig.BotPlayerViewPrefab;
            
            pileViewPrefabDict[CardPileType.Hands] = prefabConfig.HandsCardPileViewPrefab;
            pileViewPrefabDict[CardPileType.Tmp] = prefabConfig.TmpCardPileViewPrefab;
            pileViewPrefabDict[CardPileType.Target] = prefabConfig.BasicCardPileViewPrefab;
            pileViewPrefabDict[CardPileType.Trash] = prefabConfig.TrashCardPileViewPrefab;
            pileViewPrefabDict[CardPileType.Stack] = prefabConfig.StackCardPileViewPrefab;
            
            var cardPileLayoutSettings = fieldSetupComponent.GetCardPileLayoutSettings();
            
            return new FieldSetupSettings(
                playerViewPrefabDict,
                pileViewPrefabDict,
                prefabConfig.CardViewPrefab,
                prefabConfig.StageViewPrefab,
                fieldSetupComponent.GetStageSpawnPosition(),
                fieldSetupComponent.StageRoot,
                fieldSetupComponent.PlayerRoot,
                fieldSetupComponent.GetPlayerLocations(),
                cardPileLayoutSettings,
                cardPileLayoutSettings[CardPileType.Trash],
                cardPileLayoutSettings[CardPileType.Stack]
            );
        }

        /// <summary>
        /// FieldSetupDependenciesを作成
        /// </summary>
        private FieldSetupDependencies CreateFieldSetupDependencies()
        {
            var prefabConfig = fieldSetupComponent.PrefabConfig;
            
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
        /// セットアップ前のクリーンアップを実行
        /// </summary>
        private void PerformPreSetupCleanup()
        {
            Debug.Log("FieldSetupManagerTester: セットアップ前のクリーンアップを実行します");
            
            // CardViewRegistryをクリア（重複登録エラーを防ぐ）
            CardViewRegistry.Clear();
            
            // 既存のフィールドオブジェクトをクリア
            if (fieldSetupComponent != null)
            {
                ClearExistingFieldObjects();
            }
            
            // FieldSetupManagerの参照をクリア
            _fieldSetupManager = null;
            
            Debug.Log("FieldSetupManagerTester: クリーンアップが完了しました");
        }

        /// <summary>
        /// 既存のフィールドオブジェクトをクリア
        /// </summary>
        private void ClearExistingFieldObjects()
        {
            var stageRoot = GetStageRootFromComponent();
            var playerRoot = GetPlayerRootFromComponent();

            // プレイヤーの子オブジェクトを削除
            if (playerRoot != null)
            {
                int playerChildCount = playerRoot.childCount;
                for (int i = playerChildCount - 1; i >= 0; i--)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(playerRoot.GetChild(i).gameObject);
                    }
                    else
                    {
                        DestroyImmediate(playerRoot.GetChild(i).gameObject);
                    }
                }
                if (playerChildCount > 0)
                {
                    Debug.Log($"PlayerRootの子オブジェクトを{playerChildCount}個削除しました");
                }
            }

            // ステージの子オブジェクトを削除
            if (stageRoot != null)
            {
                int stageChildCount = stageRoot.childCount;
                for (int i = stageChildCount - 1; i >= 0; i--)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(stageRoot.GetChild(i).gameObject);
                    }
                    else
                    {
                        DestroyImmediate(stageRoot.GetChild(i).gameObject);
                    }
                }
                if (stageChildCount > 0)
                {
                    Debug.Log($"StageRootの子オブジェクトを{stageChildCount}個削除しました");
                }
            }
        }

        /// <summary>
        /// 参加者情報リストを取得（デバッグ用カスタムリストまたはデフォルト）
        /// </summary>
        private List<PlayerInfo> GetParticipantInfoList()
        {
            if (useCustomParticipantList && debugParticipantInfoList != null && debugParticipantInfoList.Count > 0)
            {
                Debug.Log($"FieldSetupManagerTester: デバッグ用参加者リストを使用します（{debugParticipantInfoList.Count}人）");
                return new List<PlayerInfo>(debugParticipantInfoList);
            }

            // フォールバック: デフォルトの参加者情報
            var defaultList = new List<PlayerInfo>
            {
                new PlayerInfo { UserId = "DefaultPlayer1", PlayerType = PlayerType.Local },
                new PlayerInfo { UserId = "DefaultPlayer2", PlayerType = PlayerType.Local }
            };
            
            Debug.Log($"FieldSetupManagerTester: デフォルト参加者リストを使用します（{defaultList.Count}人）");
            return defaultList;
        }

        /// <summary>
        /// 参加者情報リストをランダムに生成（デバッグ用）
        /// </summary>
        [ContextMenu("ランダム参加者リストを生成")]
        public void GenerateRandomParticipantList()
        {
            debugParticipantInfoList.Clear();
            
            int playerCount = Random.Range(2, 5); // 2-4人のプレイヤー
            var playerTypes = System.Enum.GetValues(typeof(PlayerType)) as PlayerType[];
            
            for (int i = 0; i < playerCount; i++)
            {
                var randomType = playerTypes[Random.Range(0, playerTypes.Length)];
                debugParticipantInfoList.Add(new PlayerInfo 
                { 
                    UserId = $"RandomPlayer{i + 1}", 
                    PlayerType = randomType 
                });
            }
            
            Debug.Log($"ランダム参加者リストを生成しました（{playerCount}人）");
        }

        /// <summary>
        /// 現在の参加者リストをログに表示
        /// </summary>
        [ContextMenu("参加者リストを表示")]
        public void LogParticipantList()
        {
            var participantList = GetParticipantInfoList();
            Debug.Log("=== 現在の参加者リスト ===");
            
            for (int i = 0; i < participantList.Count; i++)
            {
                var participant = participantList[i];
                Debug.Log($"参加者{i + 1}: UserID={participant.UserId}, PlayerType={participant.PlayerType}");
            }
            
            Debug.Log("=== 参加者リスト表示完了 ===");
        }

        /// <summary>
        /// Inspector用のテストボタン - FieldSetupComponentの検証
        /// </summary>
        [ContextMenu("FieldSetupComponentを検証")]
        public void ValidateFieldSetupComponentMenu()
        {
            if (ValidateFieldSetupComponent())
            {
                Debug.Log("FieldSetupComponentの検証が完了しました");
            }
        }
    }
}