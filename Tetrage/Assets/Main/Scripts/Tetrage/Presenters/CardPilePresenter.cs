using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly Dictionary<Card, CardView> _cardViewsDict; // カードモデルとビューの対応辞書。このCardPileに入っているCardModelとCardViewだけでなく、Factoryで生成されたCardModelとCardViewも含めてGlobalに管理する

        /// <param name="model">監視対象のCardPileモデル</param>
        /// <param name="view">モデルに対応するCardPileView</param>
        /// <param name="cardViewsDict">CardモデルとCardViewの対応辞書</param>
        public CardPilePresenter(
            CardPile model,
            CardPileView view,
            Dictionary<Card, CardView> cardViewsDict)
        {
            _model = model;
            _view = view;
            _cardViewsDict = cardViewsDict;
            // Viewの破棄を監視し、破棄時に Dispose を呼び出す
            _view.Destroyed += OnViewDestroyed;
            // 初期カード追加イベントを監視
            // 現状意味なし。なぜならmodel生成後にpresenterを生成するため、初期カード追加イベントが既に発行されてしまっているから
            // _model.CardsInitialized += OnCardsInitialized;

            // 移動イベントを監視
            _model.CardTransferred += OnCardTransferred;

            // オブジェクト名を変更
            _view.RenameObject(model.Name);
            // Presenterのコンストラクタでも実行（）
            OnCardsInitialized(_model.Cards);
        }

        private void OnCardTransferred(Card card, CardPile from, CardPile to)
        {
            // 移動されたカードが存在しない場合は処理しない
            if (!_cardViewsDict.TryGetValue(card, out var cardView)) {
                UnityEngine.Debug.Log("指定されたカードはCardPilePresenterの辞書に登録されていません。");
                return;
            }
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
            UnityEngine.Debug.Log($"CardPilePresenter: OnCardsInitialized {_model.Name}");

            foreach (var card in cards)
            {
                if (_cardViewsDict.TryGetValue(card, out var cardView))
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