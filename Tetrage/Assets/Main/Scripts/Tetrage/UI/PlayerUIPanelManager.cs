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
        [SerializeField] private PlayerUI panelPrefab;        // PlayerDisplayPanelプレハブ
        [SerializeField] private Transform panelContainer;    // パネルを配置する親
        
        [Header("Manager References")]
        [SerializeField] private GameManager gameManager;     // GameManager参照
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        [Header("Position Settings")]
        [Tooltip("固定位置を使用する場合はチェック")]
        [SerializeField] private bool useFixedPositions = true;
        
        [Header("Direct Position Settings (UI Coordinates)")]
        [Tooltip("UI座標で直接指定（推奨）")]
        [SerializeField] private Vector2 position0 = new Vector2(-400, 250);
        [SerializeField] private Vector2 position1 = new Vector2(400, 250);
        [SerializeField] private Vector2 position2 = new Vector2(-400, -250);
        [SerializeField] private Vector2 position3 = new Vector2(400, -250);
        
        [Header("Panel Size Settings")]
        [Tooltip("パネルのサイズを強制的に変更する（通常はPrefab側で設定を推奨）")]
        [SerializeField] private bool overridePanelSize = false;
        [SerializeField] private Vector2 panelSize = new Vector2(150, 100);
        
        #endregion

        #region Private Fields
        
        private List<PlayerUI> _activePanels = new List<PlayerUI>();
        private List<PlayerInfo> _playerInfoList = new List<PlayerInfo>(); // PlayerIdとパネルの対応用
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// PlayerInfoリストからパネルを生成
        /// </summary>
    public void SetupPanels(List<PlayerInfo> playerInfoList)
    {
        ClearPanels();
        
        if (playerInfoList == null || playerInfoList.Count == 0)
        {
            Debug.LogWarning("PlayerUIPanelManager: プレイヤー情報リストが空です");
            return;
        }
        
        // panelContainerのLayoutGroupを無効化
        DisableLayoutGroups();
        
        _playerInfoList = new List<PlayerInfo>(playerInfoList);
        
        foreach (var playerInfo in playerInfoList)
        {
            var panel = CreatePanel();
            panel.SetPlayerInfo(playerInfo);
        }
        
        // 固定位置を適用
        if (useFixedPositions)
        {
            ApplyFixedPositions();
            // 次のフレームでも再度適用（LayoutSystemの影響を避けるため）
            StartCoroutine(ApplyFixedPositionsDelayed());
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"PlayerUIPanelManager: {_activePanels.Count}個のパネルを生成しました");
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
        }
        
        /// <summary>
        /// 指定したPlayerIdのパネルをハイライト（現在の手番プレイヤーを表示）
        /// </summary>
        public void SetCurrentPlayer(int playerId)
        {
            // PlayerIdとパネルの対応付け
            for (int i = 0; i < _activePanels.Count && i < _playerInfoList.Count; i++)
            {
                bool isCurrentPlayer = _playerInfoList[i].Id.Value == playerId;
                _activePanels[i].SetCurrentPlayer(isCurrentPlayer);
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"PlayerUIPanelManager: 現在のプレイヤーをPlayerId={playerId}に設定");
            }
        }

        /// <summary>
        /// panelContainerのLayoutGroupを無効化
        /// </summary>
        private void DisableLayoutGroups()
        {
            if (panelContainer == null) return;
            
            // HorizontalLayoutGroupを無効化
            var horizontalLayout = panelContainer.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
            {
                horizontalLayout.enabled = false;
                if (enableDebugLog)
                {
                    Debug.Log("PlayerUIPanelManager: HorizontalLayoutGroupを無効化しました");
                }
            }
            
            // VerticalLayoutGroupを無効化
            var verticalLayout = panelContainer.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                verticalLayout.enabled = false;
                if (enableDebugLog)
                {
                    Debug.Log("PlayerUIPanelManager: VerticalLayoutGroupを無効化しました");
                }
            }
            
            // GridLayoutGroupを無効化
            var gridLayout = panelContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.enabled = false;
                if (enableDebugLog)
                {
                    Debug.Log("PlayerUIPanelManager: GridLayoutGroupを無効化しました");
                }
            }
        }
        
        /// <summary>
        /// パネルを固定位置に配置（UI座標で直接指定）
        /// </summary>
        private void ApplyFixedPositions()
        {
            Vector2[] positions = { position0, position1, position2, position3 };
            
            if (enableDebugLog)
            {
                Debug.Log($"ApplyFixedPositions開始: _activePanels.Count={_activePanels.Count}, positions.Length={positions.Length}");
            }
            
            // panelContainerの状態をデバッグ
            if (enableDebugLog && panelContainer != null)
            {
                RectTransform containerRect = panelContainer.GetComponent<RectTransform>();
                if (containerRect != null)
                {
                    Debug.Log($"PanelContainer: anchoredPos={containerRect.anchoredPosition}, " +
                             $"sizeDelta={containerRect.sizeDelta}, " +
                             $"anchorMin={containerRect.anchorMin}, anchorMax={containerRect.anchorMax}");
                }
            }
            
            for (int i = 0; i < _activePanels.Count && i < positions.Length; i++)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"ループ開始: Panel {i}");
                }
                
                if (_activePanels[i] == null)
                {
                    if (enableDebugLog)
                    {
                        Debug.LogWarning($"PlayerUIPanelManager: Panel {i} がnullです");
                    }
                    continue;
                }
                
                // PlayerUIのGameObjectからRectTransformを取得
                GameObject panelObj = _activePanels[i].gameObject;
                RectTransform panelRect = panelObj.GetComponent<RectTransform>();
                
                if (enableDebugLog)
                {
                    Debug.Log($"Panel {i}: GameObject={panelObj.name}, " +
                             $"Transform type={panelObj.transform.GetType().Name}, " +
                             $"RectTransformは{(panelRect != null ? "存在" : "null")}");
                }
                
                // RectTransformがない場合は追加を試みる（Canvas子要素の場合）
                if (panelRect == null)
                {
                    // TransformをRectTransformに変換
                    Transform oldTransform = panelObj.transform;
                    if (oldTransform.parent != null && oldTransform.parent.GetComponentInParent<Canvas>() != null)
                    {
                        // Canvas配下のオブジェクトは自動的にRectTransformになるべき
                        Debug.LogWarning($"Panel {i} がRectTransformを持っていません。Prefabを確認してください。");
                    }
                    continue;
                }
                
                if (panelRect != null)
                {
                    // 親のRectTransformを取得してアンカーをフルに設定
                    RectTransform parentRect = panelRect.parent as RectTransform;
                    
                    // アンカーを中央に設定（Canvas中心基準）
                    panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    panelRect.pivot = new Vector2(0.5f, 0.5f);
                    
                    // UI座標を設定
                    panelRect.anchoredPosition = positions[i];
                    
                    // スケールとローテーションをリセット
                    panelRect.localScale = Vector3.one;
                    panelRect.localRotation = Quaternion.identity;
                    
                    // サイズを強制的に変更する場合のみ
                    if (overridePanelSize)
                    {
                        panelRect.sizeDelta = panelSize;
                        
                        // ContentSizeFitterを無効化
                        var contentSizeFitter = panelRect.GetComponent<ContentSizeFitter>();
                        if (contentSizeFitter != null)
                        {
                            contentSizeFitter.enabled = false;
                        }
                    }
                    
                    if (enableDebugLog)
                    {
                        Debug.Log($"Panel {i}: 設定位置={positions[i]}, " +
                                 $"実際位置={panelRect.anchoredPosition}, " +
                                 $"ワールド位置={panelRect.position}, " +
                                 $"サイズ={panelRect.sizeDelta}, " +
                                 $"子要素数={panelRect.childCount}, " +
                                 $"親={parentRect?.name}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 次のフレームで位置を再適用（コルーチン）
        /// </summary>
        private System.Collections.IEnumerator ApplyFixedPositionsDelayed()
        {
            yield return null; // 1フレーム待つ
            ApplyFixedPositions();
            
            if (enableDebugLog)
            {
                Debug.Log("PlayerUIPanelManager: 次のフレームで位置を再適用しました");
            }
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// パネルを生成
        /// </summary>
        private PlayerUI CreatePanel()
        {
            if (panelPrefab == null)
            {
                Debug.LogError("PlayerUIPanelManager: panelPrefabが設定されていません");
                return null;
            }
            
            if (panelContainer == null)
            {
                Debug.LogError("PlayerUIPanelManager: panelContainerが設定されていません");
                return null;
            }
            
            var panelInstance = Instantiate(panelPrefab, panelContainer);
            _activePanels.Add(panelInstance);
            
            if (enableDebugLog)
            {
                Debug.Log($"PlayerUIPanelManager: Panel生成 #{_activePanels.Count - 1}, GameObject={panelInstance?.gameObject.name}");
            }
            
            return panelInstance;
        }
        
        #endregion

        #region Context Menu (Debug)
        
        [ContextMenu("パネルをクリア")]
        private void TestClearPanels()
        {
            ClearPanels();
        }

        /// <summary>
        /// パネルの位置を手動で更新
        /// </summary>
        [ContextMenu("パネル位置を更新")]
        public void UpdatePanelPositions()
        {
            if (!useFixedPositions)
            {
                Debug.LogWarning("PlayerUIPanelManager: 固定位置モードが無効です");
                return;
            }
            
            ApplyFixedPositions();
            
            if (enableDebugLog)
            {
                Debug.Log("PlayerUIPanelManager: パネルの位置を更新しました");
            }
        }
        
        #endregion
    }
}