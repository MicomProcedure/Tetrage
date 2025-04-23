using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace Tetrage.Models
{
    public class Card : MonoBehaviour, IPointerClickHandler
    {
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

        public bool canFlip = true;

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

        public event Action<Card> OnCardChanged;

        public Card(Suit suit, int number, bool isVisible)
        {
            this.suit = suit;
            this.number = number;
            this.isVisible = isVisible;
        }

        public void Flip()
        {
            isVisible = !isVisible;
            Debug.Log($"今は {(isVisible ? "表" : "裏")}.");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (canFlip)
            {
                Flip();
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

