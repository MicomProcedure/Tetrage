using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Tetrage.Core.DTO;
using Tetrage.Managers;

namespace Tetrage.UI
{
    /// <summary>
    /// ゲーム中の全プレイヤー情報パネルを管理
    /// </summary>
    public class PlayerUIPanelManager : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Panel Settings")]
        [SerializeField] private PlayerUI panelPrefab;
        [SerializeField] private Transform panelContainer;
        
        [Header("Position Settings")]
        [SerializeField] private bool useFixedPositions = true;
        [SerializeField] private System.Collections.Generic.List<Vector2> fixedPositions = new System.Collections.Generic.List<Vector2>
        {
            new Vector2(-400, 250),
            new Vector2( 400, 250),
            new Vector2(-400,-250),
            new Vector2( 400,-250),
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

        #region Public Methods
        
        /// <summary>
        /// PlayerInfoリストからパネルを生成
        /// </summary>
        public void SetupPanels(List<PlayerInfo> playerInfoList)
        {
            Debug.Log($"PlayerUIPanelManager: SetupPanels, playerInfoList: {playerInfoList.Count}");
            ClearPanels();
            
            if (playerInfoList == null || playerInfoList.Count == 0)
            {
                Debug.LogWarning("PlayerUIPanelManager: プレイヤー情報リストが空です");
                return;
            }
            
            DisableLayoutGroups();
            
            _playerInfoList = new List<PlayerInfo>(playerInfoList);
            
            foreach (var playerInfo in playerInfoList)
            {
                var panel = CreatePanel();
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
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// panelContainerのLayoutGroupを無効化
        /// </summary>
        private void DisableLayoutGroups()
        {
            if (panelContainer == null) return;
            
            var horizontalLayout = panelContainer.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null) horizontalLayout.enabled = false;
            
            var verticalLayout = panelContainer.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null) verticalLayout.enabled = false;
            
            var gridLayout = panelContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null) gridLayout.enabled = false;
        }
        
        /// <summary>
        /// パネルを固定位置に配置
        /// </summary>
        private void ApplyFixedPositions()
        {
            var positions = fixedPositions;
            for (int i = 0; i < _activePanels.Count && i < positions.Count; i++)
            {
                if (_activePanels[i] == null) continue;
                
                RectTransform panelRect = _activePanels[i].GetComponent<RectTransform>();
                if (panelRect == null) continue;
                
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = positions[i];
                panelRect.localScale = Vector3.one;
                panelRect.localRotation = Quaternion.identity;
            }
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
        private PlayerUI CreatePanel()
        {
            if (panelPrefab == null || panelContainer == null)
            {
                Debug.LogError("PlayerUIPanelManager: panelPrefabまたはpanelContainerが設定されていません");
                return null;
            }
            
            var panelInstance = Instantiate(panelPrefab, panelContainer);
            _activePanels.Add(panelInstance);
            
            return panelInstance;
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugPositions || !useFixedPositions) return;
            if (panelContainer == null) return;

            // UIローカル空間で描画
            Gizmos.matrix = panelContainer.localToWorldMatrix;
            for (int i = 0; i < fixedPositions.Count; i++)
            {
                var pos = fixedPositions[i];
                var col = debugColors != null && debugColors.Count > 0 ? debugColors[i % debugColors.Count] : Color.yellow;
                Gizmos.color = col;
                Gizmos.DrawWireCube(new Vector3(pos.x, pos.y, 0f), new Vector3(debugGizmoSize, debugGizmoSize * 0.6f, 1f));
#if UNITY_EDITOR
                UnityEditor.Handles.color = col;
                UnityEditor.Handles.Label(new Vector3(pos.x, pos.y, 0f), $"Pos{i}");
#endif
            }
        }
        
        #endregion
    }
}