using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tetrage.Models;
using Tetrage.UI;
using Tetrage.Core.Contracts;
using Tetrage.Services;

namespace Tetrage.Presenters
{
    /// <summary>
    /// CardPileモデルの移動イベントを監視し、対応するCardViewを
    /// 指定されたCardPileViewに親子付け替えるPresenter。
    /// </summary>
    public class CardPilePresenter : IDisposable
    {
        private readonly CardPile _model;
        private readonly ICardPileView _view;

        /// <param name="model">監視対象のCardPileモデル</param>
        /// <param name="view">モデルに対応する ICardPileView</param>
        public CardPilePresenter(
            CardPile model,
            ICardPileView view)
        {
            _model = model;
            _view = view;

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
            if (!CardViewRegistry.TryGetView(card, out var cardView))
            {
                UnityEngine.Debug.LogError("指定されたカードはCardPilePresenterの辞書に登録されていません。");
                return;
            }

            // このPresenterが担当するPileから出る場合は、必ずView側の除去処理を行う。
            // TmpCardPileViewではここでサイズ復元とキャッシュ掃除を実施する。
            if (from == _model)
            {
                _view.RemoveCardView(cardView);
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
            // UnityEngine.Debug.Log($"CardPilePresenter: OnCardsInitialized {_model.Name}");

            foreach (var card in cards)
            {
                // カードが存在しない場合は処理しない
                if (!CardViewRegistry.TryGetView(card, out var cardView))
                {
                    UnityEngine.Debug.LogError("指定されたカードはCardPilePresenterの辞書に登録されていません。");
                    return;
                }
                _view.AddCardView(cardView, animate: false);
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