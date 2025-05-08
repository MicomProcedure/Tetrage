using System.Collections.Generic;
using UnityEngine;
using Tetrage.Models;
using Tetrage.UI;
using Tetrage.Presenters;
using UnityEngine.Assertions;
using Tetrage.Core.Enums;
using Tetrage.Core.Contracts;
namespace Tetrage.Factories
{
    /// <summary>
    /// カードモデル生成後にViewとPresenterを構築するデコレーターファクトリ
    /// </summary>
    public class CardWithViewFactory : ICardFactory
    {
        private readonly ICardFactory _innerFactory;
        private readonly CardView _viewPrefab;
        private readonly Transform _parentTransform;

        /// <param name="innerFactory">モデル生成を委譲するICardFactory</param>
        /// <param name="viewPrefab">カード表示用Viewプレハブ</param>
        /// <param name="parentTransform">生成したViewの親Transform</param>
        public CardWithViewFactory(ICardFactory innerFactory, CardView viewPrefab, Transform parentTransform)
        {
            // 必須パラメータのnullチェック
            // カードモデル生成を委譲するファクトリのnullチェック
            Assert.IsNotNull(innerFactory, "innerFactory が null です");
            // カードビュープレハブのnullチェック
            Assert.IsNotNull(viewPrefab, "CardViewPrefab が null です"); 
            // 生成したビューの親となるTransformのnullチェック
            Assert.IsNotNull(parentTransform, "ParentTransform が null です");
            _innerFactory = innerFactory;
            _viewPrefab = viewPrefab;
            _parentTransform = parentTransform;
        }

        /// <inheritdoc/>
        public Card CreateCard(Suit suit, int number)
        {
            // モデル生成
            var cardModel = _innerFactory.CreateCard(suit, number);

            // カードモデルに対応するViewとPresenterを生成
            CreateViewAndPresenter(cardModel);

            return cardModel;
        }

        /// <inheritdoc/>
        public List<Card> CreateCards(Suit[] suits, int countPerSuit)
        {
            var models = _innerFactory.CreateCards(suits, countPerSuit);
            // 各カードモデルに対してView/Presenterを生成
            foreach (var card in models)
            {
                CreateViewAndPresenter(card);
            }
            return models;

        }

        /// <summary>
        /// カードモデルに対応するViewとPresenterを生成します
        /// </summary>
        /// <param name="card">対象のカードモデル</param>
        private void CreateViewAndPresenter(Card card)
        {
            // View生成
            var view = Object.Instantiate(_viewPrefab, _parentTransform);
            // Presenter生成
            var presenter = new CardPresenter(card, view);
        }
    }
} 