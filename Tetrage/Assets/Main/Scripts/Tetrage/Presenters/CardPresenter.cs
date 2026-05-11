using System;
using Tetrage.UI; // use concrete view
using Tetrage.Core.Enums;
using Tetrage.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using Tetrage.Services;
using R3;

namespace Tetrage.Presenters
{
    /// <summary>
    /// Presenter that synchronizes a card model (ICardModel) with its view (ICardView).
    /// </summary>
    public class CardPresenter : IDisposable
    {
        private readonly Card _model;
        private readonly CardView _view;
        private readonly CompositeDisposable _disposables = new();
        private bool _lastIsFaceUp = false;

        public CardPresenter(Card model, CardView view)
        {
            _model = model;
            _view = view;

            // カードモデルとビューの対応をグローバルな辞書に登録
            CardViewRegistry.Register(_model, _view);

            // Viewの破棄を監視し、破棄時にDisposeを呼び出す
            _view.Destroyed.Subscribe(_ => OnViewDestroyed())
                .AddTo(_disposables);

            // カードデータを設定
            _view.SetCardData(_model.Suit, _model.Number);

            // Subscribe to model changes
            _model.CardChanged.Subscribe(_ => OnModelChanged(_))
                .AddTo(_disposables);
                
            _model.IsFaceUpChanged.Subscribe(isFaceUp => OnModelIsFaceUpChanged(isFaceUp))
                .AddTo(_disposables);

            // Subscribe to view clicks
            _view.Clicked
                .Subscribe(_ => OnViewClicked())
                .AddTo(_disposables);
            // Subscribe to animation events
            _view.FlipAnimationHalfway.Subscribe(_ => OnFlipAnimationHalfway())
                .AddTo(_disposables);
            _view.FlipAnimationCompleted.Subscribe(_ => OnFlipAnimationCompleted())
                .AddTo(_disposables);


            // Initial sync
            RefreshView();
            _lastIsFaceUp = _model.IsFaceUp;
        }

        private void OnModelChanged(Card updatedCard)
        {
            RefreshView();
        }

        private void OnModelIsFaceUpChanged(bool isFaceUp)
        {
            if (_lastIsFaceUp != isFaceUp)
            {
                _view.PlayFlipAnimation();
            }

        }

        private void OnViewClicked()
        {
            // クリックされたカードをCardClickDispatcherに通知。Actionの実行に使われる。
            CardClickDispatcher.Publish(_model);

            // 反転アニメーションはモデル状態の変化時のみ実行する
        }

        private void OnFlipAnimationHalfway()
        {
            Debug.Log("CardPresenter: OnFlipAnimationHalfway called");
            RefreshView();
            _view.SetFlip(_model.IsFaceUp, _model.IsSuitVisible);
        }

        private void OnFlipAnimationCompleted(){
            RefreshView();
            _view.SetFlip(_model.IsFaceUp, _model.IsSuitVisible);
            _lastIsFaceUp = _model.IsFaceUp;
        }

        private void RefreshView()
        {
            _view.SetCardData(_model.Suit, _model.Number);

            if (!_view.IsFlipAnimationInProgress)
            {
                _view.SetFlip(_model.IsFaceUp, _model.IsSuitVisible);
            }
            else
            {

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

        private void OnViewDestroyed()
        {
            Dispose();
        }

        public void Dispose()
        {
            // カードモデルとビューの対応をグローバルな辞書から削除
            CardViewRegistry.Unregister(_model);

            // 購読解除
            _disposables.Dispose();
        }
    }
}