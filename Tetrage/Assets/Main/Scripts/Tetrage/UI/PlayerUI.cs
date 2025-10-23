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
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _originalPosition = _rectTransform.anchoredPosition;
        }
        
        #endregion
        public void SetPlayerInfo(PlayerInfo playerInfo)
        {
            //playerNumberText.text = playerInfo.Id.ToString();
            playerNameText.text = playerInfo.UserId;
            iconImage.sprite = availableIcons[playerInfo.PlayerIconIndex];
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
                Debug.Log($"PlayerUI: 新しい位置 = {_rectTransform.anchoredPosition}");
                // 必要に応じて位置をPlayerPrefsなどに保存
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
        
        #endregion
    }
}