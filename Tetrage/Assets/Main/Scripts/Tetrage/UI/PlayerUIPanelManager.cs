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
        [SerializeField] private Vector2 position0 = new Vector2(-400, 250);
        [SerializeField] private Vector2 position1 = new Vector2(400, 250);
        [SerializeField] private Vector2 position2 = new Vector2(-400, -250);
        [SerializeField] private Vector2 position3 = new Vector2(400, -250);
        
        #endregion

        #region Private Fields
        
        private List<PlayerUI> _activePanels = new List<PlayerUI>();
        private List<PlayerInfo> _playerInfoList = new List<PlayerInfo>();
        
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
            
            DisableLayoutGroups();
            
            _playerInfoList = new List<PlayerInfo>(playerInfoList);
            
            foreach (var playerInfo in playerInfoList)
            {
                var panel = CreatePanel();
                panel.SetPlayerInfo(playerInfo);
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
        }
        
        /// <summary>
        /// 指定したPlayerIdのパネルをハイライト
        /// </summary>
        public void SetCurrentPlayer(int playerId)
        {
            for (int i = 0; i < _activePanels.Count && i < _playerInfoList.Count; i++)
            {
                bool isCurrentPlayer = _playerInfoList[i].Id.Value == playerId;
                _activePanels[i].SetCurrentPlayer(isCurrentPlayer);
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
            Vector2[] positions = { position0, position1, position2, position3 };
            
            for (int i = 0; i < _activePanels.Count && i < positions.Length; i++)
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
        
        #endregion
    }
}