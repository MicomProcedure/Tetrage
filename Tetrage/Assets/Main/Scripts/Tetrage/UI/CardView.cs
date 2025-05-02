using UnityEngine;
using TMPro;
using Tetrage.Models;
using Tetrage.Core.Enums;
using UnityEngine.EventSystems;

namespace Tetrage.UI
{
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer CardSpriteRenderer;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private TextMeshProUGUI suitText;
        [SerializeField] private TextMeshProUGUI numberText;

        [SerializeField] private Animator animator;

        [SerializeField] private Card _model;

        public void Initialize(Card cardModel)
        {
            _model = cardModel;
            _model.CardChanged += OnModelChanged;  // イベント購読
            RefreshView();  // 初期表示
        }

        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.CardChanged -= OnModelChanged;  // イベント購読解除
            }
        }

        void Start()
        {
            Initialize(_model);

            Animator animator = GetComponent<Animator>();
        }


        private void OnModelChanged(Card updatedCard)
        {
            RefreshView();
        }

        private void RefreshView()
        {
            if (_model.isVisible)
            {
                // カードの見た目を表にする
                CardSpriteRenderer.color = Color.white;
                suitText.text = GetSuitSymbol(_model.suit);
                numberText.text = _model.number.ToString();
                suitText.enabled = true;
                numberText.enabled = true;
            }
            else
            {
                // カードの見た目を裏にする
                CardSpriteRenderer.color = new Color(0.3f, 0.3f, 0.3f);
                suitText.enabled = false;
                numberText.enabled = false;
            }
        }

        private string GetSuitSymbol(Suit suit)
        {
            switch (suit)
            {
                case Suit.Spade: return "♠";
                case Suit.Heart: return "♥";
                case Suit.Diamond: return "♦";
                case Suit.Club: return "♣";
                default: return "?";
            }
        }

        //知識:OnPointerClickという関数名で実装すると，このクラスを継承しているオブジェクトがタップされたときにOnPointerClick関数が自動で実行される
        public void OnPointerClick(PointerEventData eventData)
        {
            //カードを生成するときに持ち主を記録しておく，または自分のカード以外をcanFlip = falseにする必要がありそう（memo by Manri）
            if (_model.canFlip)
            {
                _model.Flip();
                animator.SetTrigger("FlipSuccess");
            }
            // クリック処理（各アクションなど）
            CardClickDispatcher.Invoke(_model);
        }
    }
}