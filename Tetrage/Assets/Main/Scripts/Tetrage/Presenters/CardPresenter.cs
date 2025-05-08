using System;
using Tetrage.UI; // use concrete view
using Tetrage.Core.Enums;
using Tetrage.Models;

namespace Tetrage.Presenters
{
    /// <summary>
    /// Presenter that synchronizes a card model (ICardModel) with its view (ICardView).
    /// </summary>
    public class CardPresenter : IDisposable
    {
        private readonly Card _model;
        private readonly CardView _view;

        public CardPresenter(Card model, CardView view)
        {
            _model = model;
            _view = view;
            // Viewの破棄を監視し、破棄時にDisposeを呼び出す
            _view.Destroyed += OnViewDestroyed;

            // Subscribe to model changes
            _model.CardChanged += OnModelChanged;
            // Subscribe to view clicks
            _view.Clicked += OnViewClicked;

            // Initial sync
            RefreshView();
        }

        private void OnModelChanged(Card updatedCard)
        {
            RefreshView();
        }

        private void OnViewClicked()
        {
            if (_model.CanFlip)
            {
                _model.Flip();
                _view.PlayFlipAnimation();
            }
            // TODO: dispatch click to application logic if needed
        }

        private void RefreshView()
        {
            if (_model.IsVisible)
            {
                _view.ShowFace();
                _view.SetSuitSymbol(GetSuitSymbol(_model.Suit));
                _view.SetNumber(_model.Number);
            }
            else
            {
                _view.ShowBack();
            }
        }

        private string GetSuitSymbol(Suit suit) => suit switch
        {
            Suit.Spade => "♠",
            Suit.Heart => "♥",
            Suit.Diamond => "♦",
            Suit.Club => "♣",
            _ => "?"
        };

        private void OnViewDestroyed()
        {
            Dispose();
        }

        public void Dispose()
        {
            // イベント購読解除
            _model.CardChanged -= OnModelChanged;
            _view.Clicked -= OnViewClicked;
            _view.Destroyed -= OnViewDestroyed;
        }
    }
}