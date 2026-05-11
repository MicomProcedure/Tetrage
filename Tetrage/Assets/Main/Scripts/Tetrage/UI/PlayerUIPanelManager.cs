using UnityEngine;
using System.Collections.Generic;
using Tetrage.Components;
using Tetrage.Core.Contracts;
using Tetrage.Extentions;
using System.Linq;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Ids;
using Tetrage.Core.Enums;
using Tetrage.Core.Events;

namespace Tetrage.UI
{
    /// <summary>
    /// ゲーム中の全プレイヤー情報パネルを管理
    /// </summary>
    public class PlayerUIPanelManager : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Panel Settings")]
        [SerializeField] private GameObject panelPrefab;
        [SerializeField] private Transform panelParent; // パネルを生成する親Transform（通常はCanvas）
        
        [Header("Position Settings")]
        [SerializeField] private bool useFixedPositions = true;
        [SerializeField] private RectPositionConfig playerSlotPositionsPrefab;

        [Header("Debug Draw")]
        [SerializeField] private bool drawDebugPositions = true;
        [SerializeField] private List<Color> debugColors = new List<Color>
        {
            new Color(0.2f, 0.8f, 1f, 1f),
            new Color(1f, 0.6f, 0.2f, 1f),
            new Color(0.6f, 1f, 0.2f, 1f),
            new Color(1f, 0.2f, 0.6f, 1f),
            new Color(0.9f, 0.9f, 0.2f, 1f),
        };
        [SerializeField] private float debugGizmoSize = 60f;
        
        #endregion

        #region Private Fields
        
        private List<PlayerUI> _activePanels = new List<PlayerUI>();
        private readonly Dictionary<PlayerId, PlayerUI> _playerIdToPanel = new Dictionary<PlayerId, PlayerUI>();  // PlayerIdをキーにしたパネル席順の辞書
        private readonly Dictionary<PlayerId, TargetSyncUIPileView> _playerIdToTargetPileView = new Dictionary<PlayerId, TargetSyncUIPileView>();
        private IPositionConfig PlayerSlotPositionsConfig => playerSlotPositionsPrefab;

        private IGameContext _gameContext;
        private bool _isInitialized = false;
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateSerializedReferences();


        }

        #endregion

        #region Initialization

        public void Initialize(IGameContext gameContext)
        {
            _gameContext = gameContext;
            _isInitialized = true;
        }

        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// PlayerInfoリストからパネルを生成
        /// </summary>
        public void SetupPanels()
        {
            if (!_isInitialized)
            {
                Debug.LogError("PlayerUIPanelManager: 初期化されていないためパネルのセットアップをスキップします");
                return;
            }

            if (!ValidateSetupPanels())
            {
                return;
            }

            ClearPanels();

            // GameContextから渡されたPlayerの内容をログ出力
            for (int i = 0; i < _gameContext.Players.Count; i++)
            {
                var player = _gameContext.Players[i];
                Debug.Log($"[PlayerUIPanelManager] Player[{i}]: Id={player.Id}, UserId={player.UserId}, IconIndex={player.IconIndex}");
            }

            var displaySeatOrderedPlayers = BuildDisplaySeatOrderedPlayers(); // UserPlayerを先頭にし、それ以外は元順序を維持する。
            for (int i = 0; i < displaySeatOrderedPlayers.Count; i++)   // displaySeatOrderedPlayersはUserPlayerを先頭にし、それ以外は元順序を維持する。
            {
                var player = displaySeatOrderedPlayers[i];
                var playerNumber = _gameContext.Players.IndexOf(player)+1;  // プレイヤー番号（ターン順）を取得する。UserPlayerは1番目なので+1する。
                var panel = CreatePanel(playerNumber);
                if (panel == null)
                {
                    Debug.LogError($"PlayerUIPanelManager: パネル生成に失敗しました (player={player})");
                    continue;
                }
                panel.SetPlayerProfileData(player, playerNumber);  // プレイヤー番号を設定する。
                _playerIdToPanel[player.Id] = panel;
            }
            
            if (useFixedPositions)
            {
                ApplyFixedPositions();
            }

            // パネルの位置に合わせてTargetMarkerを同期する。
            foreach (var playerUI in _playerIdToPanel.Values)
            {
                if (playerUI != null)
                {
                    playerUI.TryAlignTargetMarkerTransform(Camera.main, playerUI.transform.position, -Camera.main.transform.forward);
                }
            }

            HideAllTurnMarker();    // 初期化時は全パネルのTurnMarkerを非表示にする
        }
        
        /// <summary>
        /// 全パネルをクリア
        /// </summary>
        public void ClearPanels()
        {
            foreach (var panel in _activePanels)
            {
                if (panel != null)
                {
                    Destroy(panel.gameObject);
                }
            }
            _activePanels.Clear();
            _playerIdToPanel.Clear();
            _playerIdToTargetPileView.Clear();
        }

        #region Visibility

        /// <summary>
        /// <see cref="panelParent"/> 以下に生成したプレイヤーパネルの表示切替。
        /// マネージャと panelParent が別オブジェクトのとき、マネージャだけ SetActive してもアイコンが残るため利用する。
        /// </summary>
        public void SetPanelsContainerVisible(bool visible)
        {
            if (panelParent != null)
            {
                panelParent.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// InGamePlayerUI ルート（PanelContainer・テンプレ用プレハブなど兄弟含む）の表示切替。
        /// </summary>
        public void SetPlayerHudRootVisible(bool visible)
        {
            Transform hudRoot = transform.parent;
            if (hudRoot != null)
            {
                hudRoot.gameObject.SetActive(visible);
                return;
            }

            SetPanelsContainerVisible(visible);
            gameObject.SetActive(visible);
        }

        #endregion
        
        /// <summary>
        /// 指定したPlayerIdのパネルをハイライト
        /// </summary>
        /// <param name="playerId">ハイライトするプレイヤーのPlayerId</param>
        public void SetCurrentPlayer(PlayerId playerId)
        {
            // まず全パネルのハイライトをオフ
            for (int i = 0; i < _activePanels.Count; i++)
            {
                var panel = _activePanels[i];
                if (panel != null)
                {
                    panel.SetCurrentPlayer(false);
                }
            }

            // 対象プレイヤーのパネルのみオン
            if (_playerIdToPanel.TryGetValue(playerId, out var currentPanel) && currentPanel != null)
            {
                currentPanel.SetCurrentPlayer(true);
            }
            else
            {
                Debug.LogWarning($"PlayerUIPanelManager: 指定のPlayerIdに対応するパネルが見つかりませんでした (PlayerId={playerId})");
            }
        }   

        /// <summary>
        /// PlayerIdに対応するPlayerUIを取得する
        /// </summary>
        public bool TryGetPlayerUI(PlayerId playerId, out PlayerUI playerUI)
        {
            return _playerIdToPanel.TryGetValue(playerId, out playerUI) && playerUI != null;
        }

        /// <summary>
        /// PlayerIdに対応するTargetPile同期用マーカーを取得する
        /// </summary>
        public bool TryGetTargetPileMarker(PlayerId playerId, out Transform marker)
        {
            marker = null;
            if (!TryGetPlayerUI(playerId, out var playerUI))
            {
                return false;
            }

            return playerUI.TryGetTargetPileMarker(out marker);
        }

        /// <summary>
        /// PlayerIdに対応するTargetSyncUIPileViewを登録する
        /// </summary>
        public void RegisterTargetPileView(PlayerId playerId, TargetSyncUIPileView targetSyncUIPileView)
        {
            if (targetSyncUIPileView == null)
            {
                Debug.LogWarning($"PlayerUIPanelManager: 登録対象のTargetSyncUIPileViewがnullです (PlayerId={playerId})");
                return;
            }

            _playerIdToTargetPileView[playerId] = targetSyncUIPileView;
        }

        /// <summary>
        /// 指定PlayerIdのTargetPileビュー位置を同期する
        /// </summary>
        public void RefreshTargetPileViewPosition(PlayerId playerId)
        {
            if (!_playerIdToTargetPileView.TryGetValue(playerId, out var targetSyncUIPileView) || targetSyncUIPileView == null)
            {
                return;
            }

            // PlayerUI: Rect → targetMarkerTransform を更新。山側はワールド座標の複製のみ（TargetSyncUIPileView.RefreshPosition）。
            if (!TryGetTargetPileMarker(playerId, out var marker))
            {
                return;
            }

            targetSyncUIPileView.RefreshPosition(marker);
        }

        /// <summary>
        /// 登録済みTargetPileビュー位置を一括同期する
        /// </summary>
        public void RefreshAllTargetPileViewPositions()
        {
            foreach (var playerId in _playerIdToTargetPileView.Keys.ToList())
            {
                RefreshTargetPileViewPosition(playerId);
                Debug.Log("<color=red>PlayerUIPanelManager: playerId=" + playerId + " targetPileView=" + _playerIdToTargetPileView[playerId] + " ownerPlayerId=" + _playerIdToTargetPileView[playerId].OwnerPlayerId);
            }
        }
        
        /// <summary>
        /// 現在の位置マッピングをログ出力（デバッグ用）
        /// </summary>
        [ContextMenu("Log Current Positions")]
        public void LogCurrentPositions()
        {
            Debug.Log("=== Current Player Positions ===");
            if (PlayerSlotPositionsConfig?.Position == null)
            {
                Debug.LogWarning("PlayerUIPanelManager: PlayerSlotPositionsConfig が未設定です");
                return;
            }

            for (int i = 0; i < PlayerSlotPositionsConfig.Position.Count; i++)
            {
                Debug.Log($"SlotIndex={i}: Position={PlayerSlotPositionsConfig.Position[i]}");
            }
        }
        
        #endregion

        #region Private Methods

        /// <summary>
        /// パネル生成に必要な入力と参照が揃っているか検証する。
        /// </summary>
        private bool ValidateSetupPanels()
        {
            if (_gameContext.Players == null || _gameContext.Players.Count == 0)
            {
                Debug.LogWarning("PlayerUIPanelManager: プレイヤーが存在しません");
                return false;
            }

            for (int i = 0; i < _gameContext.Players.Count; i++)
            {
                if (_gameContext.Players[i] == null)
                {
                    Debug.LogError($"PlayerUIPanelManager: Player[{i}] が null です");
                    return false;
                }
            }

            return ValidateSerializedReferences();
        }

        /// <summary>
        /// Inspector で設定する参照が揃っているか検証する。
        /// </summary>
        private bool ValidateSerializedReferences()
        {
            bool isValid = true;

            if (panelPrefab == null)
            {
                Debug.LogError("PlayerUIPanelManager: panelPrefab が設定されていません", this);
                isValid = false;
            }
            else if (panelPrefab.GetComponent<PlayerUI>() == null)
            {
                Debug.LogError("PlayerUIPanelManager: panelPrefab に PlayerUI コンポーネントがありません", this);
                isValid = false;
            }

            if (panelParent == null)
            {
                Debug.LogError("PlayerUIPanelManager: panelParent が設定されていません", this);
                isValid = false;
            }

            if (useFixedPositions && PlayerSlotPositionsConfig == null)
            {
                Debug.LogError("PlayerUIPanelManager: 固定配置を使う場合は playerSlotPositionsPrefab を設定してください", this);
                isValid = false;
            }

            return isValid;
        }
        
        /// <summary>
        /// パネルを固定位置に配置（スロット番号ベースのマッピング使用）
        /// </summary>
        private void ApplyFixedPositions()
        {
            if (PlayerSlotPositionsConfig?.Position == null)
            {
                Debug.LogWarning("PlayerUIPanelManager: PlayerSlotPositionsConfig が未設定のため固定配置を適用できません");
                return;
            }

            var positions = PlayerSlotPositionsConfig.Position;
            for (int i = 0; i < _activePanels.Count; i++)
            {
                PlayerUI panel = _activePanels[i];
                if (panel == null) continue;

                if (i >= positions.Count)
                {
                    Debug.LogWarning($"PlayerUIPanelManager: SlotIndex={i} の位置設定が不足しています");
                    continue;
                }
                
                RectTransform panelRect = panel.GetComponent<RectTransform>();
                if (panelRect == null) continue;
                
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = new Vector2(positions[i].x, positions[i].y);
                panelRect.localScale = Vector3.one;
                panelRect.localRotation = Quaternion.identity;
                
                Debug.Log($"PlayerUIPanelManager: SlotIndex={i} を Position {positions[i]} に配置しました");
            }
        }

        /// <summary>
        /// 表示用の席順を構築する。UserPlayerを先頭にし、それ以外は元順序を維持する。
        /// </summary>
        private List<IPlayer> BuildDisplaySeatOrderedPlayers()
        {
            var players = _gameContext?.Players;
            var userPlayer = _gameContext?.UserPlayer;

            if (players == null || players.Count == 0 || userPlayer == null)
            {
                return new List<IPlayer>();
            }

            // 参照不一致に備えて Id で位置を取る
            var index = players.ToList().FindIndex(p => p != null && p.Id.Equals(userPlayer.Id));
            if (index < 0)
            {
                Debug.LogWarning("PlayerUIPanelManager: UserPlayer が Players に存在しないため、元順序を使用します。");
                return players.ToList();
            }

            return players.RotateFromIndex(index);
        }
        
        
        /// <summary>
        /// パネルを生成
        /// </summary>
        private PlayerUI CreatePanel(int playerNumber)
        {
            if (!ValidateSerializedReferences())
            {
                return null;
            }

            var panelObject = Instantiate(panelPrefab, panelParent);
            var panelInstance = panelObject.GetComponent<PlayerUI>();
            if (panelInstance == null)
            {
                Debug.LogError("PlayerUIPanelManager: 生成した panelPrefab に PlayerUI がありません", this);
                Destroy(panelObject);
                return null;
            }

            panelInstance.name = $"PlayerUI_P{playerNumber}";
            panelInstance.gameObject.SetActive(true);
            _activePanels.Add(panelInstance);
            
            return panelInstance;
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugPositions || !useFixedPositions) return;
            if (panelParent == null) return;
            if (PlayerSlotPositionsConfig?.Position == null) return;

            // UIローカル空間で描画
            Gizmos.matrix = panelParent.localToWorldMatrix;
            var positions = PlayerSlotPositionsConfig.Position;
            for (int i = 0; i < positions.Count; i++)
            {
                var col = debugColors != null && debugColors.Count > 0 ? debugColors[i % debugColors.Count] : Color.yellow;
                Gizmos.color = col;
                var center = new Vector3(positions[i].x, positions[i].y, 0f);
                Gizmos.DrawWireCube(center, new Vector3(debugGizmoSize, debugGizmoSize * 0.6f, 1f));
#if UNITY_EDITOR
                UnityEditor.Handles.color = col;
                UnityEditor.Handles.Label(center, $"S{i}");
#endif
            }
        }

        /// <summary>
        /// 全パネルのTurnMarkerを非表示にする
        /// </summary>
        private void HideAllTurnMarker()
        {
            foreach (var panel in _activePanels)
            {
                if (panel != null)
                {
                    panel.HideAllTurnMarker();
                }
            }
        }
        #endregion
    }
}