using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System;

namespace Tetrage.UI
{
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// このビューが破棄されたときに発行されるイベント。
        /// Presenter はここを購読して Dispose などの後片付けを行います。
        /// </summary>
        public event Action Destroyed;

        private void OnDestroy()
        {
            Destroyed?.Invoke();
        }

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TextMeshProUGUI suitText;
        [SerializeField] private TextMeshProUGUI numberText;
        [SerializeField] private Animator animator;

        [Header("Highlight Settings")]
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // 黄色っぽい色
        [SerializeField] private float highlightIntensity = 1.2f; // ハイライト時の明るさ倍率(まだ使ってない)

        private Color _originalColor;
        private bool _isHighlighted = false;

        private void Awake()
        {
            _originalColor = spriteRenderer.color;
        }

        public event Action Clicked;
        public event Action FlipAnimationHalfway;

        public void OnPointerClick(PointerEventData e) {
            Debug.Log($"CardView: OnPointerClick {e.pointerId}");
            Clicked?.Invoke();
        }

        public void PlayFlipAnimation() => animator.SetTrigger("FlipSuccess");

        // アニメーションイベントから呼び出されるメソッド
        public void OnFlipAnimationHalfway()
        {
            Debug.Log("OnFlipAnimationHalfway called");
            FlipAnimationHalfway?.Invoke();
        }

        public void SetSuitSymbol(string symbol) => suitText.text = symbol;
        public void SetNumber(int number) => numberText.text = number.ToString();

        public void ShowFace()
        {
            spriteRenderer.color = _isHighlighted ? highlightColor : Color.white;
            suitText.enabled = true;
            numberText.enabled = true;
        }

        public void ShowBack()
        {
            spriteRenderer.color = _isHighlighted ? highlightColor * 0.3f : new Color(0.3f, 0.3f, 0.3f);
            suitText.enabled = false;
            numberText.enabled = false;
        }

        /// <summary>
        /// カードをハイライト状態にする
        /// </summary>
        public void Highlight()
        {
            _isHighlighted = true;
            UpdateVisuals();
        }

        /// <summary>
        /// カードのハイライト状態を解除する
        /// </summary>
        public void Unhighlight()
        {
            _isHighlighted = false;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (suitText.enabled) // 表向きの場合
            {
                spriteRenderer.color = _isHighlighted ? highlightColor : Color.white;
            }
            else // 裏向きの場合
            {
                spriteRenderer.color = _isHighlighted ? highlightColor * 0.3f : new Color(0.3f, 0.3f, 0.3f);
            }
        }
    }
}