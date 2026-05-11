using UnityEngine;
using TMPro;
using Tetrage.Core.DTO;
using Tetrage.Core.Contracts;
using Tetrage.Core.Ids;
using Tetrage.Services;
using UnityEngine.UI;
using System.Collections.Generic;
using Tetrage.Core.Enums;

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
        [SerializeField] private Transform targetMarkerTransform;
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

        private void Start()
        {

            if (TryAlignTargetMarkerTransform(Camera.main, transform.position, -Camera.main.transform.forward))
            {
                Debug.Log("<color=red>PlayerUI: Start: TargetMarkerの座標は" + targetMarkerTransform.position);
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
            // Debug.Log($"[PlayerUI] SetPlayerProfileData呼び出し: PlayerId={id}, IconIndex={iconIndex}");
            
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
                Debug.Log("[PlayerUI] ✅ ProfileDisplayView へ反映しました: PlayerId=" + _playerId + ", IconIndex=" + _iconIndex);
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
        public PlayerId GetPlayerId()
        {
            return _playerId;
        }

        /// <summary>
        /// Targetカード山の同期に使用するマーカーを取得（targetPileMarker から targetMarkerTransform を再射影してから返す）
        /// </summary>
        public bool TryGetTargetPileMarker(out Transform markerTransform)
        {
            markerTransform = targetMarkerTransform;
            return TryAlignTargetMarkerTransform(Camera.main, transform.position, -Camera.main.transform.forward);
        }

        /// <summary>
        /// 再射影しないで targetMarkerTransform を返す。呼び出し側でワールド位置が既に正しいときに TargetSyncUIPileView へ複製する用途。
        /// </summary>
        public bool TryGetTargetPileMarkerTransform(out Transform markerTransform)
        {
            markerTransform = targetMarkerTransform;
            return markerTransform != null;
        }

        /// <summary>
        /// targetPileMarker のUI座標をワールド平面へ射影し、targetMarkerTransform の位置を同期する。
        /// </summary>
        public bool TryAlignTargetMarkerTransform(Camera worldCamera, Vector3 planePoint, Vector3 planeNormal)
        {
            if (targetMarkerTransform == null)
            {
                Debug.LogWarning("PlayerUI: targetMarkerTransform が未設定です。", this);
                return false;
            }

            if (targetPileMarker == null)
            {
                Debug.LogWarning("PlayerUI: targetPileMarker が未設定です。", this);
                return false;
            }

            if (!RectTransformWorldProjectionService.TryProjectMarkerToWorldOnPlane(
                    targetPileMarker,
                    worldCamera,
                    planePoint,
                    planeNormal,
                    out var worldPosition))
            {
                Debug.LogWarning("PlayerUI: targetPileMarker のワールド座標変換に失敗しました。", this);
                return false;
            }

            targetMarkerTransform.position = worldPosition;

            Debug.Log("<color=yellow>PlayerUI: TryAlignTargetMarkerTransform: targetMarkerTransform.position=" + targetMarkerTransform.position);
            return true;
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