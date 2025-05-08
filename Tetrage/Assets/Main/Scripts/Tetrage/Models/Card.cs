using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Tetrage.Core.Enums;
using Tetrage.Core.Contracts;

namespace Tetrage.Models
{
    public class Card
    {
        private Suit _suit;
        public Suit Suit
        {
            get => _suit;
            set
            {
                if (_suit != value)
                {
                    _suit = value;
                    OnCardChanged(this);
                }
            }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnCardChanged(this);
                }
            }
        }

        private int _number;
        public int Number
        {
            get => _number;
            set
            {
                int clamped = Mathf.Max(1, value);
                if (_number != clamped)
                {
                    _number = clamped;
                    OnCardChanged(this);
                }
            }
        }

        public bool CanFlip { get; set; } = true;

        public event Action<Card> CardChanged;

        private void OnCardChanged(Card card)
        {
            CardChanged?.Invoke(this);
        }

        /// <summary>
        /// カードを初期化するコンストラクタ
        /// </summary>
        public Card(Suit suit, int number, bool isVisible)
        {
            _suit = suit;
            _number = Mathf.Max(1, number);
            _isVisible = isVisible;
        }

        public void Flip()
        {
            IsVisible = !IsVisible;
        }
    }
}

