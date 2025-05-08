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

        public void Initialize(Suit suit, int number, bool isVisible)
        {
            Suit = suit;
            Number = number;
            IsVisible = isVisible;
        }

        public void Flip()
        {
            IsVisible = !IsVisible;
        }
    }
}

