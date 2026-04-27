using UnityEngine;
using TMPro;
using Tetrage.Core.DTO;

namespace Tetrage.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class PlayerUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [SerializeField] private ProfileDisplayView profileDisplayView;
        
        [Header("Optional")]
        [SerializeField] private TextMeshProUGUI playerNumberText;
        [SerializeField] private GameObject turnMarker;
        
        #endregion
        
        #region Private Fields
        
        private int _playerId;
        private TextMeshProUGUI _turnMarkerText;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            EnsureProfileDisplayView();

            // UI参照の検証
            if (profileDisplayView == null)
            {
                Debug.LogError("[PlayerUI] profileDisplayViewが設定されていません！Inspectorで設定してください。", this);
            }
        }
        
        #endregion

        #region Public Methods

        public void SetPlayerInfo(PlayerInfo playerInfo)
        {
            Debug.Log($"[PlayerUI] ===== SetPlayerInfo開始 ===== GameObject={gameObject.name}");
            Debug.Log($"[PlayerUI] SetPlayerInfo呼び出し: PlayerId={playerInfo.Id.Value}, UserId={playerInfo.UserId}, IconIndex={playerInfo.PlayerIconIndex}");
            
            _playerId = playerInfo.Id.Value;
            EnsureProfileDisplayView();
            
            if (profileDisplayView == null)
            {
                Debug.LogError($"[PlayerUI] profileDisplayViewがnullです (PlayerId={_playerId})");
            }
            else
            {
                profileDisplayView.SetProfile(playerInfo.PlayerIconIndex, playerInfo.UserId);
                Debug.Log("[PlayerUI] ✅ ProfileDisplayView へ反映しました");
            }
            
            Debug.Log($"[PlayerUI] ===== SetPlayerInfo完了 =====");
        }
        public void SetCurrentPlayer(bool isCurrentPlayer)
        {
            if (isCurrentPlayer)
            {
                EnableTurnMarker();
            }
            else
            {
                DisableTurnMarker();
            }
        }

        public void SetPlayerIcon(int iconIndex)
        {
            EnsureProfileDisplayView();
            if (profileDisplayView == null)
            {
                Debug.LogWarning("PlayerUI: profileDisplayView が未設定です");
                return;
            }
            
            profileDisplayView.SetIcon(iconIndex);
        }
        
        /// <summary>
        /// PlayerIdを取得
        /// </summary>
        public int GetPlayerId()
        {
            return _playerId;
        }

        public void HideAllTurnMarker()
        {
            turnMarker.SetActive(false);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ProfileDisplayViewが設定されているか確認して、設定されていない場合は自動でProfileDisplayViewを取得する
        /// </summary>
        private void EnsureProfileDisplayView()
        {
            if (profileDisplayView == null)
            {
                profileDisplayView = GetComponent<ProfileDisplayView>();
            }

            if (profileDisplayView == null)
            {
                Debug.LogWarning("PlayerUI: ProfileDisplayView が見つかりません。");
            }
        }
        

        private void EnableTurnMarker(){

            turnMarker.SetActive(true);
            _turnMarkerText.text = "Now";
        }

        private void DisableTurnMarker(){
            turnMarker.SetActive(false);
            _turnMarkerText.text = "";
        }


        #endregion
    }
}