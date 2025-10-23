using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Tetrage.Core.DTO;

namespace Tetrage.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class PlayerUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Sprite[] availableIcons;
        
        [Header("Optional")]
        [SerializeField] private TextMeshProUGUI playerNumberText;
        [SerializeField] private TextMeshProUGUI currentplayerText;
        
        [Header("Drag Settings")]
        [SerializeField] private bool isDraggable = false;  // ドラッグ可能かどうか
        [SerializeField] private bool savePositionOnDrag = true;  // ドラッグ後の位置を保存するか
        
        #endregion
        
        #region Private Fields
        
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private Vector2 _originalPosition;
        private Vector2 _dragOffset;
        private int _playerId;
        private PlayerUIPanelManager _manager;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _originalPosition = _rectTransform.anchoredPosition;
            
            // UI参照の検証
            if (playerNameText == null)
            {
                Debug.LogError("[PlayerUI] playerNameTextが設定されていません！Inspectorで設定してください。", this);
            }
            
            if (iconImage == null)
            {
                Debug.LogError("[PlayerUI] iconImageが設定されていません！Inspectorで設定してください。", this);
            }
            
            if (availableIcons == null || availableIcons.Length == 0)
            {
                Debug.LogError("[PlayerUI] availableIconsが設定されていません！Inspectorで設定してください。", this);
            }
            else
            {
                Debug.Log($"[PlayerUI] 利用可能なアイコン数: {availableIcons.Length}");
            }
        }
        
        #endregion
        public void SetPlayerInfo(PlayerInfo playerInfo)
        {
            Debug.Log($"[PlayerUI] ===== SetPlayerInfo開始 ===== GameObject={gameObject.name}");
            Debug.Log($"[PlayerUI] SetPlayerInfo呼び出し: PlayerId={playerInfo.Id.Value}, UserId={playerInfo.UserId}, IconIndex={playerInfo.PlayerIconIndex}");
            
            _playerId = playerInfo.Id.Value;
            
            // playerNameTextの確認
            if (playerNameText == null)
            {
                Debug.LogError($"[PlayerUI] playerNameTextがnullです (PlayerId={_playerId})");
            }
            else
            {
                Debug.Log($"[PlayerUI] 変更前: playerNameText.text = '{playerNameText.text}'");
                playerNameText.text = playerInfo.UserId;
                Debug.Log($"[PlayerUI] 変更後: playerNameText.text = '{playerNameText.text}'");
                
                // 本当に変更されたか確認
                if (playerNameText.text == playerInfo.UserId)
                {
                    Debug.Log($"[PlayerUI] ✅ テキスト変更成功");
                }
                else
                {
                    Debug.LogError($"[PlayerUI] ❌ テキスト変更失敗！期待値='{playerInfo.UserId}', 実際='{playerNameText.text}'");
                }
            }
            
            // iconImageとavailableIconsの確認
            if (iconImage == null)
            {
                Debug.LogError($"[PlayerUI] iconImageがnullです (PlayerId={_playerId})");
            }
            else if (availableIcons == null || availableIcons.Length == 0)
            {
                Debug.LogError($"[PlayerUI] availableIconsが設定されていません (PlayerId={_playerId})");
            }
            else if (playerInfo.PlayerIconIndex < 0 || playerInfo.PlayerIconIndex >= availableIcons.Length)
            {
                Debug.LogError($"[PlayerUI] IconIndex={playerInfo.PlayerIconIndex}が範囲外です。利用可能: 0-{availableIcons.Length - 1} (PlayerId={_playerId})");
            }
            else
            {
                var oldSprite = iconImage.sprite;
                Debug.Log($"[PlayerUI] 変更前: iconImage.sprite = {oldSprite?.name}");
                
                iconImage.sprite = availableIcons[playerInfo.PlayerIconIndex];
                
                Debug.Log($"[PlayerUI] 変更後: iconImage.sprite = {iconImage.sprite?.name}");
                
                if (iconImage.sprite == availableIcons[playerInfo.PlayerIconIndex])
                {
                    Debug.Log($"[PlayerUI] ✅ アイコン変更成功");
                }
                else
                {
                    Debug.LogError($"[PlayerUI] ❌ アイコン変更失敗！");
                }
            }
            
            Debug.Log($"[PlayerUI] ===== SetPlayerInfo完了 =====");
        }
        public void SetCurrentPlayer(bool isCurrentPlayer)
        {
            currentplayerText.text = isCurrentPlayer ? "Now" : "";
        }

        public void SetPlayerIcon(int iconIndex)
        {
            // アイコンを設定
            if (iconImage != null && availableIcons != null && 
                iconIndex >= 0 && iconIndex < availableIcons.Length)
            {
                iconImage.sprite = availableIcons[iconIndex];
            }
            else if (iconIndex < 0 || iconIndex >= (availableIcons?.Length ?? 0))
            {
                Debug.LogWarning($"PlayerUI: IconIndex={iconIndex} が範囲外です (利用可能: 0-{availableIcons?.Length - 1 ?? 0})");
            }

        }

            
        #region Drag Handlers
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable) return;
            
            // ドラッグ開始時のオフセットを計算
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );
            _dragOffset = _rectTransform.anchoredPosition - localPoint;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!isDraggable) return;
            
            // ドラッグ中の位置を更新
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            {
                _rectTransform.anchoredPosition = localPoint + _dragOffset;
            }
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDraggable) return;
            
            if (savePositionOnDrag)
            {
                Vector2 newPosition = _rectTransform.anchoredPosition;
                Debug.Log($"PlayerUI: PlayerId={_playerId} の新しい位置 = {newPosition}");
                
                // PlayerUIPanelManagerに位置を通知
                if (_manager != null)
                {
                    _manager.UpdatePlayerPosition(_playerId, newPosition);
                }
                else
                {
                    Debug.LogWarning("PlayerUI: PlayerUIPanelManagerへの参照が設定されていません");
                }
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// ドラッグ可能状態を設定
        /// </summary>
        public void SetDraggable(bool draggable)
        {
            isDraggable = draggable;
        }
        
        /// <summary>
        /// 元の位置にリセット
        /// </summary>
        public void ResetPosition()
        {
            _rectTransform.anchoredPosition = _originalPosition;
        }
        
        /// <summary>
        /// 現在の位置を取得
        /// </summary>
        public Vector2 GetCurrentPosition()
        {
            return _rectTransform.anchoredPosition;
        }
        
        /// <summary>
        /// 位置を設定
        /// </summary>
        public void SetPosition(Vector2 position)
        {
            _rectTransform.anchoredPosition = position;
        }
        
        /// <summary>
        /// PlayerUIPanelManagerへの参照を設定
        /// </summary>
        public void SetManager(PlayerUIPanelManager manager)
        {
            _manager = manager;
        }
        
        /// <summary>
        /// PlayerIdを取得
        /// </summary>
        public int GetPlayerId()
        {
            return _playerId;
        }
        
        #endregion
    }
}