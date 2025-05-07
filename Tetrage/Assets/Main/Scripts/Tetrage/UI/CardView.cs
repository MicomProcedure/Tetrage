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

        public event Action Clicked;

        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke();

        public void PlayFlipAnimation() => animator.SetTrigger("FlipSuccess");
        public void SetSuitSymbol(string symbol) => suitText.text = symbol;
        public void SetNumber(int number) => numberText.text = number.ToString();

        public void ShowFace()
        {
            spriteRenderer.color = Color.white;
            suitText.enabled = true;
            numberText.enabled = true;
        }

        public void ShowBack()
        {
            spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f);
            suitText.enabled = false;
            numberText.enabled = false;
        }
    }
}