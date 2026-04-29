using UnityEngine;
using TMPro;
using Tetrage.Core.DTO;
using Tetrage.Core.Contracts;
using Tetrage.Core.Ids;

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
        [SerializeField] private RectTransform targetPileMarker;
        
        #endregion
        
        #region Private Fields
        
        private PlayerId _playerId;
        private int _iconIndex;
        private TextMeshProUGUI _turnMarkerText;
        private int _playerNumber;
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            EnsureProfileDisplayView();
            CacheTurnMarkerText();

            // UI参照の検証
            if (profileDisplayView == null)
            {
                Debug.LogError("[PlayerUI] profileDisplayViewが設定されていません！Inspectorで設定してください。", this);
            }
        }
        
        #endregion

        #region Public Methods

        /// <summary>
        /// プレイヤーのプロファイルデータを設定
        /// </summary>
        /// <param name="id">プレイヤーのID</param>
        /// <param name="iconIndex">プレイヤーのアイコンのインデックス</param>
        /// <param name="playerNumber">プレイヤー番号（ターン順）</param>
        public void SetPlayerProfileData(PlayerId id, int iconIndex, int playerNumber)
        {
            Debug.Log($"[PlayerUI] SetPlayerInfo呼び出し: PlayerId={id}, IconIndex={iconIndex}");
            
            _playerId = id;
            _iconIndex = iconIndex;
            _playerNumber = playerNumber;

            if (playerNumberText != null)
            {
                playerNumberText.text = playerNumber.ToString()+"P";    // プレイヤー番号を設定する。
            }
            EnsureProfileDisplayView();
            
            if (profileDisplayView == null)
            {
                Debug.LogError($"[PlayerUI] profileDisplayViewがnullです (PlayerId={_playerId})");
            }
            else
            {
                profileDisplayView.SetProfile(_iconIndex, _playerId.ToString());
                Debug.Log("[PlayerUI] ✅ ProfileDisplayView へ反映しました");
            }
            
        }

        /// <summary>
        /// プレイヤーのプロファイルデータを設定
        /// </summary>
        /// <param name="playerInfo">プレイヤーの情報</param>
        public void SetPlayerProfileData(PlayerInfo playerInfo, int playerNumber){
            SetPlayerProfileData(playerInfo.Id, playerInfo.PlayerIconIndex, playerNumber);
        }

        /// <summary>
        /// プレイヤーのプロファイルデータを設定
        /// </summary>
        /// <param name="player">プレイヤー</param>
        public void SetPlayerProfileData(IPlayer player, int playerNumber){
            SetPlayerProfileData(player.Id, player.IconIndex, playerNumber);
        }


        /// <summary>
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

        /// <summary>
        /// Targetカード山の同期に使用するマーカーを取得
        /// </summary>
        public bool TryGetTargetPileMarker(out RectTransform marker)
        {
            marker = targetPileMarker;
            return marker != null;
        }

        public void HideAllTurnMarker()
        {
            if (turnMarker == null)
            {
                return;
            }

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
        

        private void EnableTurnMarker()
        {
            if (turnMarker == null)
            {
                Debug.LogWarning("PlayerUI: turnMarker が未設定です。", this);
                return;
            }

            turnMarker.SetActive(true);
            if (_turnMarkerText != null)
            {
                _turnMarkerText.text = "Now";
            }
        }

        private void DisableTurnMarker()
        {
            if (turnMarker == null)
            {
                return;
            }

            turnMarker.SetActive(false);
            if (_turnMarkerText != null)
            {
                _turnMarkerText.text = "";
            }
        }

        /// <summary>
        /// TurnMarker配下のテキスト参照をキャッシュする
        /// </summary>
        private void CacheTurnMarkerText()
        {
            if (turnMarker == null)
            {
                return;
            }

            _turnMarkerText = turnMarker.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_turnMarkerText == null)
            {
                Debug.LogWarning("PlayerUI: turnMarker 配下に TextMeshProUGUI が見つかりません。", this);
            }
        }


        #endregion
    }
}