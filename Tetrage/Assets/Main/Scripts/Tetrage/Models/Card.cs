using UnityEngine;
using System;
using Tetrage.Core.Enums;

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
                    OnCardChanged(this); // カードのスートが変更されたことをPresenterに通知
                }
            }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            private set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnCardChanged(this); // カードの表示状態が変更されたことをPresenterに通知
                }
            }
        }

        private bool _isHighlighted = false;
        public bool IsHighlighted
        {
            get => _isHighlighted;
            private set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    OnCardChanged(this); // カードのハイライト状態が変更されたことをPresenterに通知
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
                    OnCardChanged(this); // カードの数字が変更されたことをPresenterに通知
                }
            }
        }

        public bool CanFlip { get; set; } = false;

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

        /// <summary>
        /// カードをハイライト状態にする
        /// </summary>
        public void Highlight()
        {
            IsHighlighted = true;
        }

        /// <summary>
        /// カードのハイライト状態を解除する
        /// </summary>
        public void Unhighlight()
        {
            IsHighlighted = false;
        }

        public override string ToString()
        {
            return $"{Suit} {Number}";
        }
    }
}

