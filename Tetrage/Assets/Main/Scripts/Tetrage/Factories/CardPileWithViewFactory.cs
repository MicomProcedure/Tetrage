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
    /// CardPileモデル生成後にViewとPresenterを構築するデコレーターファクトリ
    /// </summary>
    public class CardPileWithViewFactory : ICardPileFactory
    {
        private readonly ICardPileFactory _innerFactory;
        private readonly CardPileView _viewPrefab;
        private readonly Transform _parentTransform;
        private readonly Dictionary<Card, CardView> _cardViews;

        /// <param name="innerFactory">モデル生成を委譲するICardPileFactory</param>
        /// <param name="viewPrefab">カード山表示用Viewプレハブ</param>
        /// <param name="parentTransform">生成したPileViewの親Transform</param>
        /// <param name="cardViews">CardモデルとCardViewの対応辞書</param>
        public CardPileWithViewFactory(
            ICardPileFactory innerFactory,
            CardPileView viewPrefab,
            Transform parentTransform,
            Dictionary<Card, CardView> cardViews)
        {
            Assert.IsNotNull(innerFactory, "innerFactory が null です");
            Assert.IsNotNull(viewPrefab, "CardPileView prefab が null です");
            Assert.IsNotNull(parentTransform, "parentTransform が null です");
            Assert.IsNotNull(cardViews, "cardViews が null です");
            _innerFactory = innerFactory;
            _viewPrefab = viewPrefab;
            _parentTransform = parentTransform;
            _cardViews = cardViews;
        }

        /// <inheritdoc/>
        public CardPile CreatePile(string name, int maxCount)
        {
            // モデル生成
            var pileModel = _innerFactory.CreatePile(name, maxCount);

            // View生成
            var pileView = Object.Instantiate(_viewPrefab, _parentTransform);

            // Presenter生成
            var presenter = new CardPilePresenter(pileModel, pileView, _cardViews);

            return pileModel;
        }
    }
} 