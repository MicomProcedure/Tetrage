using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Tetrage.Core.Enums;

namespace Tetrage.Models
{
    public class Card
    {


        public Suit suit
        {
            get => _suit;
            set
            {
                if (_suit != value)
                {
                    _suit = value;
                    OnCardChanged?.Invoke(this); // 情報更新時にCardViewで見た目を変更
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
                    OnCardChanged?.Invoke(this); // 情報更新時にCardViewで見た目を変更
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
                    OnCardChanged?.Invoke(this); // 情報更新時にCardViewで見た目を変更
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
        
        public void Flip()
        {
            isVisible = !isVisible;
        }


    }
}

