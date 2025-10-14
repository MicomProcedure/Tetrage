using UnityEngine;
using System;
using Tetrage.Core.Enums;
using Tetrage.Core.Contracts;
using Tetrage.Core.Ids;

namespace Tetrage.Models
{
    public class Card : IIdentifiable<CardId>
    {
        /// <summary>
        /// カード一意ID（不変）
        /// </summary>
        public CardId Id { get; }

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
            // 既存互換用のコンストラクタ。DeckId=1、Suitのenum値をインデックスとみなして決定論的にIDを合成する。
            // TODO: CardIDを合成するヘルパーが出来次第、カードIDの生成をカードファクトリに移行する
            int v = ((1 & 0xFF) << 16) | (((int)suit & 0xFF) << 8) | (Mathf.Max(1, number) & 0xFF);
            Id = new CardId(v);
            _suit = suit;
            _number = Mathf.Max(1, number);
            _isVisible = isVisible;
        }

        /// <summary>
        /// カードを初期化するコンストラクタ（推奨）
        /// </summary>
        public Card(CardId id, Suit suit, int number, bool isVisible)
        {
            Id = id;
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

