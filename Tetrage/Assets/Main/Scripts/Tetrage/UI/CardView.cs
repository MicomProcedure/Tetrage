using UnityEngine;
using TMPro;
using Tetrage.Models;

namespace Tetrage.UI
{
    public class CardView : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer CardSpriteRenderer;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private TextMeshProUGUI suitText;
        [SerializeField] private TextMeshProUGUI numberText;

        [SerializeField] private Card model;

        public void Initialize(Card cardModel)
        {
            model = cardModel;
            model.OnCardChanged += OnModelChanged;  // イベント購読
            RefreshView();  // 初期表示更新
        }

        private void OnDestroy()
        {
            if (model != null)
            {
                model.OnCardChanged -= OnModelChanged;  // イベント購読解除
            }
        }

        void Start()
        {
            Initialize(model);
        }


        private void OnModelChanged(Card updatedCard)
        {
            RefreshView();
        }

        private void RefreshView()
        {
            Debug.Log($"isVisible: {model.isVisible}");  // デバッグログ追加

            if (model.isVisible)
            {
                CardSpriteRenderer.color = Color.white;
                suitText.text = GetSuitSymbol(model.suit);
                numberText.text = model.number.ToString();
                suitText.enabled = true;
                numberText.enabled = true;
            }
            else
            {
                CardSpriteRenderer.color = new Color(0.3f, 0.3f, 0.3f);
                suitText.enabled = false;
                numberText.enabled = false;
            }
        }


        private string GetSuitSymbol(Card.Suit suit)
        {
            switch (suit)
            {
                case Card.Suit.Spade: return "♠";
                case Card.Suit.Heart: return "♥";
                case Card.Suit.Diamond: return "♦";
                case Card.Suit.Club: return "♣";
                default: return "?";
            }
        }
    }
}