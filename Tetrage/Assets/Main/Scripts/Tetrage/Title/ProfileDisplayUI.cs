using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;

namespace Tetrage.Title
{
    /// <summary>
    /// プレイヤー情報の表示UI（ローカルプロファイルとPhotonプレイヤーの両方に対応）
    /// 待機部屋、ゲームシーンなど様々な場面で使用可能
    /// </summary>
    public class ProfileDisplayUI : MonoBehaviour
    {
        #region Enums
        
        /// <summary>
        /// 表示モード
        /// </summary>
        public enum DisplayMode
        {
            LocalProfile,   // ローカルプロファイルを表示
            PhotonPlayer    // Photonプレイヤーを表示
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("UI References")]
        [SerializeField] private Image iconImage;                 // アイコン表示用
        [SerializeField] private TextMeshProUGUI nameText;        // 名前表示用
        [SerializeField] private Sprite[] availableIcons;         // 使用可能なアイコン一覧

        [Header("Display Settings")]
        [SerializeField] private DisplayMode displayMode = DisplayMode.LocalProfile;  // 表示モード
        [SerializeField] private PlayerProfileManager profileManager;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;      // デバッグログを有効にするか
        
        #endregion

        #region Private Fields
        
        private Player currentPhotonPlayer;  // 現在表示中のPhotonプレイヤー
        private bool isInitialized = false;  // 初期化済みフラグ
        
        #endregion

        #region Unity Lifecycle
        
        void Start()
        {
            InitializeDisplay();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 表示を初期化
        /// </summary>
        public void InitializeDisplay()
        {
            if (isInitialized) return;
            
            switch (displayMode)
            {
                case DisplayMode.LocalProfile:
                    UpdateFromLocalProfile();
                    break;
                case DisplayMode.PhotonPlayer:
                    // Photonプレイヤーが設定されるまで待機
                    break;
            }
            
            isInitialized = true;
        }
        
        /// <summary>
        /// 表示モードを設定
        /// </summary>
        /// <param name="mode">表示モード</param>
        public void SetDisplayMode(DisplayMode mode)
        {
            displayMode = mode;
            
            switch (mode)
            {
                case DisplayMode.LocalProfile:
                    UpdateFromLocalProfile();
                    break;
                case DisplayMode.PhotonPlayer:
                    if (currentPhotonPlayer != null)
                    {
                        UpdateFromPhotonPlayer(currentPhotonPlayer);
                    }
                    break;
            }
        }
        
        #endregion

        #region Local Profile Methods
        
        /// <summary>
        /// ローカルプロファイルから表示を更新
        /// </summary>
        public void UpdateFromLocalProfile()
        {
            // プロフィールマネージャーの参照が無ければ自動検索
            if (profileManager == null)
            {
                profileManager = FindFirstObjectByType<PlayerProfileManager>();
                if (enableDebugLog)
                {
                    Debug.Log($"PlayerProfileManagerを自動検索: {(profileManager != null ? "成功" : "失敗")}");
                }
            }

            var data = profileManager?.Data;
            if (data == null)
            {
                Debug.LogWarning("プロフィールデータが存在しません。");
                return;
            }

            if (enableDebugLog)
            {
                Debug.Log($"ローカルプロファイルから表示更新: IconIndex={data.IconIndex}, PlayerName={data.PlayerName}");
            }
            
            UpdateDisplay(data.IconIndex, data.PlayerName);
            currentPhotonPlayer = null; // ローカルプロファイル使用時はPhotonプレイヤーをクリア
        }

        /// <summary>
        /// PlayerProfileManager のデータをもとに UI を更新（後方互換性のため残す）
        /// </summary>
        public void UpdateDisplay()
        {
            UpdateFromLocalProfile();
        }
        
        #endregion

        #region Photon Player Methods
        
        /// <summary>
        /// Photonプレイヤーから表示を更新
        /// </summary>
        /// <param name="player">表示するプレイヤー</param>
        public void UpdateFromPhotonPlayer(Player player)
        {
            if (player == null)
            {
                Debug.LogWarning("Playerがnullです");
                return;
            }

            currentPhotonPlayer = player; // 現在のPhotonプレイヤーを保存

            // CustomPropertiesからIconIndexを取得（なければ0）
            int iconIndex = player.CustomProperties.ContainsKey("IconIndex") 
                ? (int)player.CustomProperties["IconIndex"] 
                : 0;

            if (enableDebugLog)
            {
                Debug.Log($"Photonプレイヤーから表示更新: {player.NickName}, IconIndex={iconIndex}");
            }

            UpdateDisplay(iconIndex, player.NickName);
        }

        /// <summary>
        /// Photonプレイヤー情報を設定（PlayerItemUIとの互換性のため）
        /// </summary>
        /// <param name="player">表示するプレイヤー</param>
        public void SetPlayerData(Player player)
        {
            UpdateFromPhotonPlayer(player);
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// 共通の表示更新ロジック
        /// </summary>
        /// <param name="iconIndex">アイコンのインデックス</param>
        /// <param name="playerName">プレイヤー名</param>
        private void UpdateDisplay(int iconIndex, string playerName)
        {
            // アイコン設定
            if (iconImage != null && availableIcons != null && iconIndex >= 0 && iconIndex < availableIcons.Length)
            {
                iconImage.sprite = availableIcons[iconIndex];
                if (enableDebugLog)
                {
                    Debug.Log($"アイコンを設定: Index={iconIndex}");
                }
            }
            else if (iconIndex < 0 || iconIndex >= (availableIcons?.Length ?? 0))
            {
                Debug.LogWarning($"IconIndex={iconIndex} が範囲外です (利用可能範囲: 0-{availableIcons?.Length - 1 ?? 0})");
            }

            // 名前設定
            if (nameText != null)
            {
                nameText.text = playerName ?? "Unknown Player";
                if (enableDebugLog)
                {
                    Debug.Log($"プレイヤー名を設定: {playerName}");
                }
            }
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// 現在表示中のプレイヤー情報を取得
        /// </summary>
        /// <returns>現在のPhotonプレイヤー（ローカルプロファイルの場合はnull）</returns>
        public Player GetCurrentPlayer()
        {
            return currentPhotonPlayer;
        }
        
        /// <summary>
        /// 表示モードを取得
        /// </summary>
        /// <returns>現在の表示モード</returns>
        public DisplayMode GetDisplayMode()
        {
            return displayMode;
        }
        
        /// <summary>
        /// デバッグログの有効/無効を切り替え
        /// </summary>
        /// <param name="enabled">有効にするか</param>
        public void SetDebugLogEnabled(bool enabled)
        {
            enableDebugLog = enabled;
        }
        
        #endregion
    }
}
