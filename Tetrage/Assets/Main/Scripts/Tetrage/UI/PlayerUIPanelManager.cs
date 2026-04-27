using UnityEngine;
using System.Collections.Generic;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;

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
        
        // スロット番号と位置のマッピング
        [SerializeField] private List<PlayerSlotPositionMapping> playerSlotPositionMappings = new List<PlayerSlotPositionMapping>
        {
            new PlayerSlotPositionMapping { slotIndex = 0, rect = new Rect( 0, -250, 0, 0) },
            new PlayerSlotPositionMapping { slotIndex = 1, rect = new Rect( -400, 0, 0, 0) },
            new PlayerSlotPositionMapping { slotIndex = 2, rect = new Rect( 0, 250, 0, 0) },
            new PlayerSlotPositionMapping { slotIndex = 3, rect = new Rect( 400, 0, 0, 0) },
        };

        [Header("Debug Draw")]
        [SerializeField] private bool drawDebugPositions = true;
        [SerializeField] private System.Collections.Generic.List<Color> debugColors = new System.Collections.Generic.List<Color>
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
        private List<PlayerInfo> _playerInfoList = new List<PlayerInfo>();
        private readonly Dictionary<int, PlayerUI> _playerIdToPanel = new Dictionary<int, PlayerUI>();
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateSerializedReferences();


        }

        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// PlayerInfoリストからパネルを生成
        /// </summary>
        public void SetupPanels(List<PlayerInfo> playerInfoList)
        {
            if (!ValidateSetupPanels(playerInfoList))
            {
                return;
            }

            ClearPanels();

            // 渡されたPlayerInfoの内容をログ出力
            for (int i = 0; i < playerInfoList.Count; i++)
            {
                var info = playerInfoList[i];
                Debug.Log($"[PlayerUIPanelManager] PlayerInfo[{i}]: Id={info.Id.Value}, UserId={info.UserId}, IconIndex={info.PlayerIconIndex}");
            }

            _playerInfoList = new List<PlayerInfo>(playerInfoList);
            
            var displaySeatOrderedPlayers = BuildDisplaySeatOrderedPlayers(playerInfoList);
            foreach (var playerInfo in displaySeatOrderedPlayers)
            {
                var panel = CreatePanel(playerInfo.Id.Value);
                if (panel == null)
                {
                    Debug.LogError($"PlayerUIPanelManager: パネル生成に失敗しました (PlayerId={playerInfo.Id.Value})");
                    continue;
                }
                panel.SetPlayerInfo(playerInfo);
                _playerIdToPanel[playerInfo.Id.Value] = panel;
            }
            
            if (useFixedPositions)
            {
                ApplyFixedPositions();
                StartCoroutine(ApplyFixedPositionsDelayed());
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
            _playerInfoList.Clear();
            _playerIdToPanel.Clear();
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
        public void SetCurrentPlayer(int playerId)
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
        /// 現在の位置マッピングをログ出力（デバッグ用）
        /// </summary>
        [ContextMenu("Log Current Positions")]
        public void LogCurrentPositions()
        {
            Debug.Log("=== Current Player Positions ===");
            foreach (var mapping in playerSlotPositionMappings)
            {
                Debug.Log($"SlotIndex={mapping.slotIndex}: Rect={mapping.rect}");
            }
        }
        
        #endregion

        #region Private Methods

        /// <summary>
        /// パネル生成に必要な入力と参照が揃っているか検証する。
        /// </summary>
        private bool ValidateSetupPanels(List<PlayerInfo> playerInfoList)
        {
            if (playerInfoList == null || playerInfoList.Count == 0)
            {
                Debug.LogWarning("PlayerUIPanelManager: プレイヤー情報リストが空です");
                return false;
            }

            for (int i = 0; i < playerInfoList.Count; i++)
            {
                if (playerInfoList[i] == null)
                {
                    Debug.LogError($"PlayerUIPanelManager: PlayerInfo[{i}] が null です");
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

            if (useFixedPositions && (playerSlotPositionMappings == null || playerSlotPositionMappings.Count == 0))
            {
                Debug.LogError("PlayerUIPanelManager: 固定配置を使う場合は playerSlotPositionMappings を設定してください", this);
                isValid = false;
            }

            return isValid;
        }
        
        /// <summary>
        /// パネルを固定位置に配置（スロット番号ベースのマッピング使用）
        /// </summary>
        private void ApplyFixedPositions()
        {
            for (int i = 0; i < _activePanels.Count; i++)
            {
                PlayerUI panel = _activePanels[i];
                if (panel == null) continue;

                // スロット番号に対応する位置を検索
                var mapping = playerSlotPositionMappings.Find(m => m.slotIndex == i);
                if (mapping == null)
                {
                    Debug.LogWarning($"PlayerUIPanelManager: SlotIndex={i} の位置マッピングが見つかりません");
                    continue;
                }
                
                RectTransform panelRect = panel.GetComponent<RectTransform>();
                if (panelRect == null) continue;
                
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = mapping.rect.position;
                if (mapping.rect.width > 0f && mapping.rect.height > 0f)
                {
                    // Rectのサイズ指定がある場合のみUIサイズを上書きする
                    panelRect.sizeDelta = new Vector2(mapping.rect.width, mapping.rect.height);
                }
                panelRect.localScale = Vector3.one;
                panelRect.localRotation = Quaternion.identity;
                
                Debug.Log($"PlayerUIPanelManager: SlotIndex={i} を Rect {mapping.rect} に配置しました");
            }
        }

        /// <summary>
        /// 表示用の席順を構築する。ローカルプレイヤーを先頭にし、それ以外は元順序を維持する。
        /// </summary>
        private static List<PlayerInfo> BuildDisplaySeatOrderedPlayers(List<PlayerInfo> playerInfoList)
        {
            var orderedPlayers = new List<PlayerInfo>(playerInfoList.Count);
            PlayerInfo localPlayer = null;

            for (int i = 0; i < playerInfoList.Count; i++)
            {
                var playerInfo = playerInfoList[i];
                if (playerInfo != null && playerInfo.PlayerType == PlayerType.Local && localPlayer == null)
                {
                    localPlayer = playerInfo;
                }
            }

            if (localPlayer != null)
            {
                orderedPlayers.Add(localPlayer);
            }

            for (int i = 0; i < playerInfoList.Count; i++)
            {
                var playerInfo = playerInfoList[i];
                if (playerInfo == null)
                {
                    continue;
                }

                if (ReferenceEquals(playerInfo, localPlayer))
                {
                    continue;
                }

                orderedPlayers.Add(playerInfo);
            }

            return orderedPlayers;
        }
        
        /// <summary>
        /// 次のフレームで位置を再適用
        /// </summary>
        private System.Collections.IEnumerator ApplyFixedPositionsDelayed()
        {
            yield return null;
            ApplyFixedPositions();
        }
        
        /// <summary>
        /// パネルを生成
        /// </summary>
        private PlayerUI CreatePanel(int playerId)
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

            panelInstance.name = $"PlayerUI_P{playerId}";
            panelInstance.gameObject.SetActive(true);
            _activePanels.Add(panelInstance);
            
            return panelInstance;
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugPositions || !useFixedPositions) return;
            if (panelParent == null) return;
            if (playerSlotPositionMappings == null) return;

            // UIローカル空間で描画
            Gizmos.matrix = panelParent.localToWorldMatrix;
            for (int i = 0; i < playerSlotPositionMappings.Count; i++)
            {
                var mapping = playerSlotPositionMappings[i];
                var col = debugColors != null && debugColors.Count > 0 ? debugColors[i % debugColors.Count] : Color.yellow;
                Gizmos.color = col;
                var debugWidth = mapping.rect.width > 0f ? mapping.rect.width : debugGizmoSize;
                var debugHeight = mapping.rect.height > 0f ? mapping.rect.height : debugGizmoSize * 0.6f;
                var center = new Vector3(mapping.rect.x + debugWidth * 0.5f, mapping.rect.y + debugHeight * 0.5f, 0f);
                Gizmos.DrawWireCube(center, new Vector3(debugWidth, debugHeight, 1f));
#if UNITY_EDITOR
                UnityEditor.Handles.color = col;
                UnityEditor.Handles.Label(center, $"S{mapping.slotIndex}");
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
    
    /// <summary>
    /// スロット番号と位置のマッピング（インスペクター編集用）
    /// </summary>
    [System.Serializable]
    public class PlayerSlotPositionMapping
    {
        [Tooltip("スロット番号（0始まり）")]
        public int slotIndex = 0;
        
        [Tooltip("Rect(x,y,width,height): x,yはanchoredPosition、width,heightは0より大きい時のみsizeDeltaに反映")]
        public Rect rect = new Rect(0f, 0f, 0f, 0f);
    }
}