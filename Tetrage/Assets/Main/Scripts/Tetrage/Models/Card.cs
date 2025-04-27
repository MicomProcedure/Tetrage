using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace Tetrage.Models
{
    public class Card : MonoBehaviour, IPointerClickHandler
    {
        //カードの持ち主情報を記録
        public Player owner;
        //アニメーション
        Animator animator;

        public Suit suit
        {
            get => _suit;
            set
            {
                if (_suit != value)
                {
                    _suit = value;
                    OnCardChanged?.Invoke(this);
                }
            }
        }
        private Suit _suit;

        public bool isVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnCardChanged?.Invoke(this);
                }
            }
        }
        private bool _isVisible = true;
        private int _number;
        public int number
        {
            get => _number;
            set
            {
                int clamped = Mathf.Max(1, value);
                if (_number != clamped)
                {
                    _number = clamped;
                    OnCardChanged?.Invoke(this);
                }
            }
        }

        //タッチした時に裏表を逆にできるかどうか
        public bool canFlip = true;

        public event Action<Card> OnCardChanged;

        public void Initialize(Suit suit, int number, bool isVisible)
        {
            this.suit = suit;
            this.number = number;
            this.isVisible = isVisible;
        }

        public void Start()
        {
            animator = GetComponent<Animator>();
        }
        
        public void Flip()
        {
            isVisible = !isVisible;
        }

        //知識:OnPointerClickという関数名で実装すると，このクラスを継承しているオブジェクトがタップされたときにOnPointerClick関数が自動で実行される
        public void OnPointerClick(PointerEventData eventData)
        {
            //カードを生成するときに持ち主を記録しておく，または自分のカード以外をcanFlip = falseにする必要がありそう（memo by Manri）
            if (canFlip)
            {
                Flip();
                animator.SetTrigger("FlipSuccess");
            }
        }

        public enum Suit
        {
            Spade,
            Heart,
            Diamond,
            Club
        }
    }
}

