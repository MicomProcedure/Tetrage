using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.UI;
using Tetrage.Presenters;
using Tetrage.Core.Constants;

namespace Tetrage.Factories
{
    /// <summary>カード山（初期カード付き）を構築するビルダーパターン実装である</summary>
    public class CardPileBuilder
    {
        private readonly ICardPileFactory _innerFactory;    // 山札生成用基本ファクトリ
        private BasicCardPileView _viewPrefab;             // 山札表示用ビュー
        private Transform _viewParent;                     // 山札表示用ビューの親
        private Dictionary<Card, CardView> _cardViewsDict;  // カード表示用ビューのディクショナリ
        private bool _useView;                             // 山札表示用ビューの使用フラグ
        private string _name;                              // 山札の名前
        private int _maxCount;                             // 山札の最大枚数
        private float _layoutWidth;                        // 山札表示用ビューの幅
        private float _layoutMinSpacing;                   // 山札表示用ビューの最小間隔
        private float _layoutMaxSpacing;                   // 山札表示用ビューの最大間隔
        private Vector3 _layoutOffset;                     // 山札表示用ビューのオフセット
        private ICardFactory _cardFactory;                 // カード生成用ファクトリ (初期カード生成に使用)
        private bool _useInitialCards;                     // 初期カード生成フラグ
        private Suit[] _initialSuits;                      // 初期カード生成スート
        private int _initialCountPerSuit;                  // 初期カード生成枚数




        /// <summary>基礎となる ICardPileFactory を受け取るコンストラクタである</summary>
        public CardPileBuilder(ICardPileFactory innerFactory)
        {
            Assert.IsNotNull(innerFactory, "innerFactory が null です");
            _innerFactory = innerFactory;
            _name = "CardPile";
            _maxCount = int.MaxValue;
            // デフォルトのレイアウト設定を適用
            _layoutWidth = InGameConsts.DEFAULT_CARD_PILE_WIDTH;
            _layoutMinSpacing = InGameConsts.DEFAULT_CARD_VIEW_MIN_SPACING;
            _layoutMaxSpacing = InGameConsts.DEFAULT_CARD_VIEW_MAX_SPACING;
            _layoutOffset = InGameConsts.DEFAULT_CARD_VIEW_POSITION_OFFSET;
            // デフォルトのカードファクトリ設定
            _cardFactory = new CardModelFactory();
        }

        /// <summary>View と Presenter を生成するよう設定する</summary>
        public CardPileBuilder UseView(BasicCardPileView viewPrefab, Transform parent, Dictionary<Card, CardView> cardViewsDict)
        {
            Assert.IsNotNull(viewPrefab, "viewPrefab が null です");
            Assert.IsNotNull(parent, "parent が null です");
            Assert.IsNotNull(cardViewsDict, "cardViewsDict が null です");
            _useView = true;
            _viewPrefab = viewPrefab;
            _viewParent = parent;
            _cardViewsDict = cardViewsDict;
            return this;
        }

        /// <summary>生成する山札の名前を設定する</summary>
        public CardPileBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        /// <summary>生成する山札の最大枚数を設定する</summary>
        public CardPileBuilder WithMaxCount(int maxCount)
        {
            _maxCount = maxCount;
            return this;
        }

        /// <summary>CardPileView のレイアウト情報を設定する</summary>
        public CardPileBuilder WithLayout(float pileWidth, float minSpacing, float maxSpacing, Vector3 positionOffset)
        {
            _layoutWidth = pileWidth;
            _layoutMinSpacing = minSpacing;
            _layoutMaxSpacing = maxSpacing;
            _layoutOffset = positionOffset;
            return this;
        }

        /// <summary>SuitとcountPerSuitを省略したデフォルト初期カード設定</summary>
        public CardPileBuilder WithInitialCards()
        {
            _useInitialCards = true;
            _initialSuits = InGameConsts.DEFAULT_INITIAL_SUITS;
            _initialCountPerSuit = InGameConsts.DEFAULT_INITIAL_COUNT_PER_SUIT;
            return this;
        }

        /// <summary>ICardFactory を設定し、初期カード生成を可能にする</summary>
        /// <param name="cardFactory">ICardFactory の実装</param>
        /// <returns>CardPileBuilder のインスタンス</returns>
        public CardPileBuilder UseCardFactory(ICardFactory cardFactory)
        {
            Assert.IsNotNull(cardFactory, "cardFactory が null です");
            _cardFactory = cardFactory;
            return this;
        }

        /// <summary>初期カードとして生成するスート配列と枚数を設定する</summary>
        public CardPileBuilder WithInitialCards(
            ICardFactory cardFactory,
            Suit[] suits,
            int countPerSuit)
        {
            Assert.IsNotNull(cardFactory, "cardFactory が null です");
            Assert.IsNotNull(suits, "suits が null です");
            Assert.IsTrue(countPerSuit > 0, "countPerSuit は正数を指定してください");
            _cardFactory = cardFactory;
            _useInitialCards = true;
            _initialSuits = suits;
            _initialCountPerSuit = countPerSuit;
            return this;
        }

        /// <summary>山札を生成する（初期カード指定なし）</summary>
        public CardPile Build()
        {
            IEnumerable<Card> cards = null;
            if (_useInitialCards)
            {
                Assert.IsNotNull(_cardFactory, "UseCardFactory を呼び出して ICardFactory を設定してください");
                cards = _cardFactory.CreateCards(_initialSuits, _initialCountPerSuit);
            }
            return Build(cards);
        }

        /// <summary>山札を生成する（初期カード指定あり）</summary>
        public CardPile Build(IEnumerable<Card> initialCards)
        {
            // モデル生成
            CardPile pile = initialCards == null
                ? _innerFactory.CreatePile(_name, _maxCount)
                : _innerFactory.CreatePile(_name, initialCards, _maxCount);

            if (_useView)
            {
                // View を生成
                ICardPileView view = Object.Instantiate(_viewPrefab, _viewParent);
                // レイアウト設定を反映
                view.SetCardViewLayoutInfo(_layoutWidth, _layoutMinSpacing, _layoutMaxSpacing, _layoutOffset);
                // Presenter を生成
                var presenter = new CardPilePresenter(pile, view, _cardViewsDict);
            }

            return pile;
        }
    }
} 