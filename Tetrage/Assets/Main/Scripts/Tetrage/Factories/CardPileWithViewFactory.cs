using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Tetrage.Models;
using Tetrage.UI;
using Tetrage.Presenters;
using Tetrage.Core.Contracts;

namespace Tetrage.Factories
{
    /// <summary>
    /// CardPileモデル生成後にViewとPresenterを構築するデコレーターファクトリ。現在は使い道がないので、CardPileBuilderでは使用しません。
    /// </summary>
    public class CardPileWithViewFactory : ICardPileFactory
    {
        private readonly ICardPileFactory _innerFactory;
        private readonly BasicCardPileView _viewPrefab;
        private readonly Transform _parentTransform;
        private readonly Dictionary<Card, CardView> _cardViewsDict;

        /// <param name="innerFactory">モデル生成を委譲するICardPileFactory</param>
        /// <param name="viewPrefab">カード山表示用Viewプレハブ</param>
        /// <param name="parentTransform">生成したPileViewの親Transform</param>
        /// <param name="cardViewsDict">CardモデルとCardViewの対応辞書</param>
        public CardPileWithViewFactory(
            ICardPileFactory innerFactory,
            BasicCardPileView viewPrefab,
            Transform parentTransform,
            Dictionary<Card, CardView> cardViewsDict)
        {
            Assert.IsNotNull(innerFactory, "innerFactory が null です");
            Assert.IsNotNull(viewPrefab, "CardPileView prefab が null です");
            Assert.IsNotNull(parentTransform, "parentTransform が null です");
            Assert.IsNotNull(cardViewsDict, "cardViews が null です");
            _innerFactory = innerFactory;
            _viewPrefab = viewPrefab;
            _parentTransform = parentTransform;
            _cardViewsDict = cardViewsDict;
        }

        /// <inheritdoc/>
        public CardPile CreatePile(string name, int maxCount)
        {
            // モデル生成
            var pileModel = _innerFactory.CreatePile(name, maxCount);

            // Viewを生成し、ICardPileViewとして扱う
            ICardPileView pileView = Object.Instantiate(_viewPrefab, _parentTransform);

            // Presenter生成
            var presenter = new CardPilePresenter(pileModel, pileView, _cardViewsDict);

            return pileModel;
        }

        /// <summary>
        /// 新しいカード山(Pile)を生成します（初期カード指定付き）。
        /// </summary>
        /// <param name="name">カード山の名前</param>
        /// <param name="initialCards">生成時に含めるカードのコレクション</param>
        /// <param name="maxCount">カード山の最大枚数</param>
        public CardPile CreatePile(string name, IEnumerable<Card> initialCards, int maxCount)
        {
            // モデル生成（初期カード指定付き）
            var pileModel = _innerFactory.CreatePile(name, initialCards, maxCount);

            // Viewを生成し、ICardPileViewとして扱う
            ICardPileView pileView = Object.Instantiate(_viewPrefab, _parentTransform);

            // Presenter生成
            var presenter = new CardPilePresenter(pileModel, pileView, _cardViewsDict);

            return pileModel;
        }
    }
} 