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
        [SerializeField] private Transform panelParent; // パネルを生成する親Transform（通常はCanvas）
        
        [Header("Position Settings")]
        [SerializeField] private bool useFixedPositions = true;
        
        // プレイヤーIDと位置のマッピング
        [SerializeField] private List<PlayerPositionMapping> playerPositionMappings = new List<PlayerPositionMapping>
        {
            new PlayerPositionMapping { playerId = 1, position = new Vector2(-400, 250) },
            new PlayerPositionMapping { playerId = 2, position = new Vector2( 400, 250) },
            new PlayerPositionMapping { playerId = 3, position = new Vector2(-400,-250) },
            new PlayerPositionMapping { playerId = 4, position = new Vector2( 400,-250) },
        };

        [Header("Persistence Settings")]
        [SerializeField] private bool useSavedPositions = true; // 保存された位置を使用するか
        [SerializeField] private bool autoSaveOnDrag = true; // ドラッグ時に自動保存するか
        [SerializeField] private SaveMethod saveMethod = SaveMethod.PlayerPrefs; // 保存方法
        
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
        
        private const string SAVE_KEY = "PlayerUI_Positions";
        private const string SAVE_FILE_NAME = "PlayerUIPositions.json";
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // 保存された位置を読み込み
            if (useSavedPositions)
            {
                LoadPositions();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// PlayerInfoリストからパネルを生成
        /// </summary>
        public void SetupPanels(List<PlayerInfo> playerInfoList)
        {
            Debug.Log($"[PlayerUIPanelManager] SetupPanels 呼び出し回数: {System.Environment.StackTrace}");
            Debug.Log($"PlayerUIPanelManager: SetupPanels, playerInfoList: {playerInfoList.Count}");
            
            // 渡されたPlayerInfoの内容をログ出力
            for (int i = 0; i < playerInfoList.Count; i++)
            {
                var info = playerInfoList[i];
                Debug.Log($"[PlayerUIPanelManager] PlayerInfo[{i}]: Id={info.Id.Value}, UserId={info.UserId}, IconIndex={info.PlayerIconIndex}");
            }
            
            ClearPanels();
            
            if (playerInfoList == null || playerInfoList.Count == 0)
            {
                Debug.LogWarning("PlayerUIPanelManager: プレイヤー情報リストが空です");
                return;
            }
            
            _playerInfoList = new List<PlayerInfo>(playerInfoList);
            
            foreach (var playerInfo in playerInfoList)
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
        
        /// <summary>
        /// プレイヤーパネルの位置を更新（ドラッグ後に呼ばれる）
        /// </summary>
        public void UpdatePlayerPosition(int playerId, Vector2 newPosition)
        {
            // マッピングを検索
            var mapping = playerPositionMappings.Find(m => m.playerId == playerId);
            
            if (mapping != null)
            {
                // 既存のマッピングを更新
                mapping.position = newPosition;
                Debug.Log($"PlayerUIPanelManager: PlayerId={playerId} の位置を更新しました: {newPosition}");
            }
            else
            {
                // 新しいマッピングを追加
                playerPositionMappings.Add(new PlayerPositionMapping 
                { 
                    playerId = playerId, 
                    position = newPosition 
                });
                Debug.Log($"PlayerUIPanelManager: PlayerId={playerId} の新しい位置マッピングを追加しました: {newPosition}");
            }
            
            // 自動保存
            if (autoSaveOnDrag)
            {
                SavePositions();
            }
            
            // エディタで変更をマーク（シーン保存時に反映される）
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        /// <summary>
        /// すべてのパネルのドラッグ機能を有効/無効にする
        /// </summary>
        public void SetAllDraggable(bool draggable)
        {
            foreach (var panel in _activePanels)
            {
                if (panel != null)
                {
                    panel.SetDraggable(draggable);
                }
            }
            Debug.Log($"PlayerUIPanelManager: すべてのパネルのドラッグ機能を{(draggable ? "有効" : "無効")}にしました");
        }
        
        /// <summary>
        /// 現在の位置マッピングをログ出力（デバッグ用）
        /// </summary>
        [ContextMenu("Log Current Positions")]
        public void LogCurrentPositions()
        {
            Debug.Log("=== Current Player Positions ===");
            foreach (var mapping in playerPositionMappings)
            {
                Debug.Log($"PlayerId={mapping.playerId}: Position={mapping.position}");
            }
        }
        
        /// <summary>
        /// 位置を保存
        /// </summary>
        [ContextMenu("Save Positions")]
        public void SavePositions()
        {
            switch (saveMethod)
            {
                case SaveMethod.PlayerPrefs:
                    SaveToPlayerPrefs();
                    break;
                case SaveMethod.JsonFile:
                    SaveToJsonFile();
                    break;
            }
        }
        
        /// <summary>
        /// 位置を読み込み
        /// </summary>
        [ContextMenu("Load Positions")]
        public void LoadPositions()
        {
            switch (saveMethod)
            {
                case SaveMethod.PlayerPrefs:
                    LoadFromPlayerPrefs();
                    break;
                case SaveMethod.JsonFile:
                    LoadFromJsonFile();
                    break;
            }
        }
        
        /// <summary>
        /// 保存された位置をクリア
        /// </summary>
        [ContextMenu("Clear Saved Positions")]
        public void ClearSavedPositions()
        {
            switch (saveMethod)
            {
                case SaveMethod.PlayerPrefs:
                    PlayerPrefs.DeleteKey(SAVE_KEY);
                    PlayerPrefs.Save();
                    Debug.Log("PlayerUIPanelManager: 保存された位置をクリアしました（PlayerPrefs）");
                    break;
                case SaveMethod.JsonFile:
                    string path = GetJsonFilePath();
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                        Debug.Log($"PlayerUIPanelManager: 保存された位置をクリアしました（{path}）");
                    }
                    break;
            }
        }
        
        /// <summary>
        /// デフォルト位置に戻す
        /// </summary>
        [ContextMenu("Reset to Default Positions")]
        public void ResetToDefaultPositions()
        {
            ClearSavedPositions();
            
            // デフォルト位置を再適用
            if (useFixedPositions)
            {
                ApplyFixedPositions();
            }
            
            Debug.Log("PlayerUIPanelManager: デフォルト位置に戻しました");
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// パネルを固定位置に配置（プレイヤーIDベースのマッピング使用）
        /// </summary>
        private void ApplyFixedPositions()
        {
            foreach (var kvp in _playerIdToPanel)
            {
                int playerId = kvp.Key;
                PlayerUI panel = kvp.Value;
                
                if (panel == null) continue;
                
                // プレイヤーIDに対応する位置を検索
                var mapping = playerPositionMappings.Find(m => m.playerId == playerId);
                if (mapping == null)
                {
                    Debug.LogWarning($"PlayerUIPanelManager: PlayerId={playerId} の位置マッピングが見つかりません");
                    continue;
                }
                
                RectTransform panelRect = panel.GetComponent<RectTransform>();
                if (panelRect == null) continue;
                
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = mapping.position;
                panelRect.localScale = Vector3.one;
                panelRect.localRotation = Quaternion.identity;
                
                Debug.Log($"PlayerUIPanelManager: PlayerId={playerId} を位置 {mapping.position} に配置しました");
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
        private PlayerUI CreatePanel(int playerId)
        {
            if (panelPrefab == null || panelParent == null)
            {
                Debug.LogError("PlayerUIPanelManager: panelPrefabまたはpanelParentが設定されていません");
                return null;
            }
            
            var panelInstance = Instantiate(panelPrefab, panelParent);
            panelInstance.name = $"PlayerUI_P{playerId}";
            panelInstance.SetManager(this); // Managerへの参照を設定
            _activePanels.Add(panelInstance);
            
            return panelInstance;
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugPositions || !useFixedPositions) return;
            if (panelParent == null) return;

            // UIローカル空間で描画
            Gizmos.matrix = panelParent.localToWorldMatrix;
            for (int i = 0; i < playerPositionMappings.Count; i++)
            {
                var mapping = playerPositionMappings[i];
                var col = debugColors != null && debugColors.Count > 0 ? debugColors[i % debugColors.Count] : Color.yellow;
                Gizmos.color = col;
                Gizmos.DrawWireCube(new Vector3(mapping.position.x, mapping.position.y, 0f), new Vector3(debugGizmoSize, debugGizmoSize * 0.6f, 1f));
#if UNITY_EDITOR
                UnityEditor.Handles.color = col;
                UnityEditor.Handles.Label(new Vector3(mapping.position.x, mapping.position.y, 0f), $"P{mapping.playerId}");
#endif
            }
        }
        
        #region Persistence Methods
        
        private void SaveToPlayerPrefs()
        {
            var data = new PlayerPositionData { mappings = playerPositionMappings };
            string json = JsonUtility.ToJson(data, true);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("PlayerUIPanelManager: 位置をPlayerPrefsに保存しました");
        }
        
        private void LoadFromPlayerPrefs()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                var data = JsonUtility.FromJson<PlayerPositionData>(json);
                if (data != null && data.mappings != null && data.mappings.Count > 0)
                {
                    playerPositionMappings = data.mappings;
                    Debug.Log($"PlayerUIPanelManager: {data.mappings.Count}個の位置をPlayerPrefsから読み込みました");
                }
            }
        }
        
        private void SaveToJsonFile()
        {
            var data = new PlayerPositionData { mappings = playerPositionMappings };
            string json = JsonUtility.ToJson(data, true);
            string path = GetJsonFilePath();
            
            try
            {
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"PlayerUIPanelManager: 位置をJSONファイルに保存しました: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PlayerUIPanelManager: JSONファイルの保存に失敗しました: {e.Message}");
            }
        }
        
        private void LoadFromJsonFile()
        {
            string path = GetJsonFilePath();
            if (System.IO.File.Exists(path))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(path);
                    var data = JsonUtility.FromJson<PlayerPositionData>(json);
                    if (data != null && data.mappings != null && data.mappings.Count > 0)
                    {
                        playerPositionMappings = data.mappings;
                        Debug.Log($"PlayerUIPanelManager: {data.mappings.Count}個の位置をJSONファイルから読み込みました: {path}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"PlayerUIPanelManager: JSONファイルの読み込みに失敗しました: {e.Message}");
                }
            }
        }
        
        private string GetJsonFilePath()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }
        
        #endregion
        
        #endregion
    }
    
    /// <summary>
    /// 保存方法
    /// </summary>
    public enum SaveMethod
    {
        PlayerPrefs,  // PlayerPrefsに保存
        JsonFile      // JSONファイルに保存
    }
    
    /// <summary>
    /// プレイヤーIDと位置のマッピング（インスペクター編集用）
    /// </summary>
    [System.Serializable]
    public class PlayerPositionMapping
    {
        [Tooltip("プレイヤーID (ActorNumber)")]
        public int playerId = 1;
        
        [Tooltip("配置位置")]
        public Vector2 position = Vector2.zero;
    }
    
    /// <summary>
    /// 位置データの保存用クラス
    /// </summary>
    [System.Serializable]
    internal class PlayerPositionData
    {
        public List<PlayerPositionMapping> mappings = new List<PlayerPositionMapping>();
    }
}