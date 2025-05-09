using System;
using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.UI;

namespace Tetrage.Presenters
{
    /// <summary>
    /// CardPileモデルの移動イベントを監視し、対応するCardViewを
    /// 指定されたCardPileViewに親子付け替えるPresenter。
    /// </summary>
    public class CardPilePresenter : IDisposable
    {
        private readonly CardPile _model;
        private readonly CardPileView _view;
        private readonly Dictionary<Card, CardView> _cardViews;

        /// <param name="model">監視対象のCardPileモデル</param>
        /// <param name="view">モデルに対応するCardPileView</param>
        /// <param name="cardViews">CardモデルとCardViewの対応辞書</param>
        public CardPilePresenter(
            CardPile model,
            CardPileView view,
            Dictionary<Card, CardView> cardViews)
        {
            _model = model;
            _view = view;
            _cardViews = cardViews;
            // Viewの破棄を監視し、破棄時に Dispose を呼び出す
            _view.Destroyed += OnViewDestroyed;
            // 初期カード追加イベントを監視
            _model.CardsInitialized += OnCardsInitialized;

            // 移動イベントを監視
            _model.CardTransferred += OnCardTransferred;
        }

        private void OnCardTransferred(Card card, CardPile from, CardPile to)
        {
            // 移動されたカードが存在しない場合は処理しない
            if (!_cardViews.TryGetValue(card, out var cardView)) return;

            // 目的のPileViewを取得して移動
            // 通常は外部でPileViewとモデルの対応が管理されている前提
            // ここでは単一Viewを担当する場合、_viewがselfモデルの場合のみ処理
            if (to == _model)
            {
                _view.AddCardView(cardView);
            }

        }

        /// <summary>
        /// 初期カード設定時に呼び出され、ビューにカードを追加する
        /// </summary>
        private void OnCardsInitialized(IEnumerable<Card> cards)
        {
            foreach (var card in cards)
            {
                if (_cardViews.TryGetValue(card, out var cardView))
                {
                    _view.AddCardView(cardView);
                }
            }
        }

        private void OnViewDestroyed()
        {
            Dispose();
        }

        public void Dispose()
        {
            // イベント購読解除
            _model.CardTransferred -= OnCardTransferred;
            _model.CardsInitialized -= OnCardsInitialized;
            _view.Destroyed -= OnViewDestroyed;
        }
    }
}