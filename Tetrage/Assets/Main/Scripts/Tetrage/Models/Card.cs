using UnityEngine;
using System;
using Tetrage.Core.Enums;
using Tetrage.Core.Contracts;
using Tetrage.Core.Ids;
using R3;

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

        private bool _isSuitVisible = false;
        /// <summary>
        /// カードが可視状態かどうか
        /// </summary>
        public bool IsSuitVisible
        {
            get => _isSuitVisible;
            private set
            {
                if (_isSuitVisible != value)
                {
                    _isSuitVisible = value;
                    OnCardChanged(this); // カードの表示状態が変更されたことをPresenterに通知
                }
            }
        }

        private readonly ReactiveProperty<bool> _isFaceUp = new(false);
        /// <summary>
        /// カードが表向きかどうか
        /// </summary>
        public bool IsFaceUp
        {
            get => _isFaceUp.Value;
            private set => _isFaceUp.Value = value;
        }

        public Observable<bool> IsFaceUpChanged => _isFaceUp;

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
        private readonly Subject<Card> _cardChanged = new();

        public Observable<Card> CardChanged => _cardChanged;

        private void OnCardChanged(Card card)
        {
            _cardChanged.OnNext(this);
        }

        /// <summary>
        /// カードを初期化するコンストラクタ（推奨）
        /// </summary>
        public Card(CardId id, Suit suit, int number, bool isFaceUp)
        {
            Id = id;
            _suit = suit;
            _number = Mathf.Max(1, number);
            _isFaceUp.Value = isFaceUp;
        }

        public void Flip()
        {
            IsFaceUp = !IsFaceUp;
        }

        /// <summary>
        /// スート可視状態を更新する
        /// </summary>
        public void SetSuitVisible(bool isVisible)
        {
            IsSuitVisible = isVisible;
        }

        /// <summary>
        /// パイル移動時のカード状態をログ出力する(Debug用)
        /// </summary>
        public void LogStateOnPileTransfer(PileId fromPileId, PileId toPileId)
        {
            Debug.Log(
                $"[Card] Transfer Id={Id.Value} From={fromPileId.Value} To={toPileId.Value} " +
                $"Suit={Suit} Number={Number} IsFaceUp={IsFaceUp} IsSuitVisible={IsSuitVisible} " +
                $"IsHighlighted={IsHighlighted} CanFlip={CanFlip}");
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
            return  $"[Card] Suit={Suit} Number={Number} IsFaceUp={IsFaceUp} IsSuitVisible={IsSuitVisible}IsHighlighted={IsHighlighted} CanFlip={CanFlip}";
        }
    }
}

