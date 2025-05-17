using System;
using Tetrage.UI; // use concrete view
using Tetrage.Core.Enums;
using Tetrage.Models;
using UnityEngine;
using UnityEngine.EventSystems;

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
            // Subscribe to animation events
            _view.FlipAnimationHalfway += OnFlipAnimationHalfway;

            // Initial sync
            RefreshView();
        }

        private void OnModelChanged(Card updatedCard)
        {
            RefreshView();
        }

        private void OnViewClicked()
        {
            // クリックされたカードをCardClickDispatcherに通知。Actionの実行に使われる。
            CardClickDispatcher.Invoke(_model);

            // クリックされた際にカードを裏返す
            if (_model.CanFlip)
            {
                // アニメーションを開始
                _view.PlayFlipAnimation();
                // 実際のFlipはアニメーションの途中で行う
            }
            // TODO: dispatch click to application logic if needed
        }

        private void OnFlipAnimationHalfway()
        {
            Debug.Log("CardPresenter: OnFlipAnimationHalfway called");
            // アニメーションの途中でカードの状態を変更
            _model.Flip();
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

            // ハイライト状態の更新
            if (_model.IsHighlighted)
            {
                _view.Highlight();
            }
            else
            {
                _view.Unhighlight();
            }
        }

        private string GetSuitSymbol(Suit suit) => suit switch
        {
            Suit.Spade => "<color=black>♠</color>",
            Suit.Heart => "<color=red>♥</color>",
            Suit.Diamond => "<color=red>♦</color>",
            Suit.Club => "<color=black>♣</color>",
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
            _view.FlipAnimationHalfway -= OnFlipAnimationHalfway;
        }
    }
}