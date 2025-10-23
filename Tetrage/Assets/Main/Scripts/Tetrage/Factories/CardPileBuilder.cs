using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.UI;
using Tetrage.Presenters;
using Tetrage.Core.Constants;
using Tetrage.Core.Enums;
using Tetrage.Core.DTO;
using Tetrage.Core.Ids;

namespace Tetrage.Factories
{
    /// <summary>カード山（初期カード付き）を構築するビルダーパターン実装</summary>
    public class CardPileBuilder
    {
        private readonly ICardPileFactory _innerFactory;    // 山札生成用基本ファクトリ
        private List<BasicCardPileView> _viewPrefabs;             // 山札表示用ビューのリスト
        private BasicCardPileView _viewPrefab;              // 山札表示用ビュー
        private Transform _viewParent;                     // 山札表示用ビューの親
        private bool _useView;                             // 山札表示用ビューの使用フラグ
        private string _name;                              // 山札の名前
        private int _maxCount;                             // 山札の最大枚数
        private PileId _pileId;                             // 山札のID
        private CardPileLayoutSettings _layoutSettings;     // 山札表示用ビューのレイアウト設定
        private ICardFactory _cardFactory;                 // カード生成用ファクトリ (初期カード生成に使用)
        private bool _useInitialCards;                     // 初期カード生成フラグ
        private Suit[] _initialSuits;                      // 初期カード生成スート
        private int _initialCountPerSuit;                  // 初期カード生成枚数
        
        // パラメータ変更検知用フラグ
        private bool _hasParametersChanged;                // パラメータが初期値から変更されたかどうか




        /// <summary>基礎となる ICardPileFactory を受け取るコンストラクタである</summary>
        public CardPileBuilder(ICardPileFactory innerFactory)
        {
            Assert.IsNotNull(innerFactory, "innerFactory が null です");
            _innerFactory = innerFactory;
            _name = "CardPile";
            _maxCount = int.MaxValue;
            // デフォルトのレイアウト設定を適用
            _layoutSettings = CardPileLayoutSettings.Default;
            // デフォルトのカードファクトリ設定
            _cardFactory = new CardModelFactory();
            // フラグを初期化
            _hasParametersChanged = false;
        }

        /// <summary>View と Presenter を生成するよう設定する</summary>
        /// <param name="viewPrefab">BasicCardPileView のインスタンス</param>
        /// <param name="parent">BasicCardPileView の親</param>
        /// <param name="cardViewsDict">Card と CardView のディクショナリ</param>
        /// <returns>CardPileBuilder のインスタンス</returns>
        public CardPileBuilder UseView(BasicCardPileView viewPrefab, Transform parent)
        {
            Assert.IsNotNull(viewPrefab, "viewPrefab が null です");
            Assert.IsNotNull(parent, "parent が null です");
            _useView = true;
            _viewPrefab = viewPrefab;
            _viewParent = parent;
            _hasParametersChanged = true; // パラメータ変更フラグを立てる
            return this;
        }

        public CardPileBuilder WithoutView()
        {
            _useView = false;
            _hasParametersChanged = true; // パラメータ変更フラグを立てる
            return this;
        }

        /// <summary>生成する山札の名前を設定する</summary>
        public CardPileBuilder WithName(string name)
        {
            _name = name;
            _hasParametersChanged = true; // パラメータ変更フラグを立てる
            return this;
        }

        /// <summary>生成する山札の最大枚数を設定する</summary>
        public CardPileBuilder WithMaxCount(int maxCount)
        {
            _maxCount = maxCount;
            _hasParametersChanged = true; // パラメータ変更フラグを立てる
            return this;
        }


        /// <summary>CardPileView のレイアウト情報を設定する</summary>
        /// <param name="pileWidth">山札の幅（デフォルト: DEFAULT_CARD_PILE_WIDTH）</param>
        /// <param name="minSpacing">山札の最小間隔（デフォルト: DEFAULT_CARD_VIEW_MIN_SPACING）</param>
        /// <param name="maxSpacing">山札の最大間隔（デフォルト: DEFAULT_CARD_VIEW_MAX_SPACING）</param>
        /// <param name="positionOffset">山札の位置オフセット（デフォルト: DEFAULT_CARD_VIEW_POSITION_OFFSET）</param>
        /// <returns>CardPileBuilder のインスタンス</returns>
        public CardPileBuilder WithLayout(
            float pileWidth = InGameConsts.DEFAULT_CARD_PILE_WIDTH,
            float minSpacing = InGameConsts.DEFAULT_CARD_VIEW_MIN_SPACING,
            float maxSpacing = InGameConsts.DEFAULT_CARD_VIEW_MAX_SPACING,
            Vector3 positionOffset = default)
        {
            _layoutSettings = new CardPileLayoutSettings(
                pileWidth,
                minSpacing,
                maxSpacing,
                positionOffset == default ? InGameConsts.DEFAULT_CARD_VIEW_POSITION_OFFSET : positionOffset
            );
            _hasParametersChanged = true; // パラメータ変更フラグを立てる
            return this;
        }

        /// <summary>CardPileView のレイアウト情報を設定する</summary>
        /// <param name="layoutSettings">CardPileLayoutSettings の構造体インスタンス</param>
        /// <returns>CardPileBuilder のインスタンス</returns>
        public CardPileBuilder WithLayout(CardPileLayoutSettings layoutSettings)
        {
            _layoutSettings = layoutSettings;
            _hasParametersChanged = true; // パラメータ変更フラグを立てる
            return this;
        }

        /// <summary>SuitとcountPerSuitを省略したデフォルト初期カード設定</summary>
        /// <returns>CardPileBuilder のインスタンス</returns>
        public CardPileBuilder WithInitialCards()
        {
            _useInitialCards = true;
            _initialSuits = InGameConsts.DEFAULT_INITIAL_SUITS;
            _initialCountPerSuit = InGameConsts.DEFAULT_INITIAL_COUNT_PER_SUIT;
            _hasParametersChanged = true; // パラメータ変更フラグを立てる
            return this;
        }

        /// <summary>初期カードとして生成するスート配列と枚数を設定する</summary>
        /// <param name="cardFactory">ICardFactory の実装</param>
        /// <param name="suits">初期カードのスート配列</param>
        /// <param name="countPerSuit">初期カードの枚数</param>
        /// <returns>CardPileBuilder のインスタンス</returns>
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
            _hasParametersChanged = true; // パラメータ変更フラグを立てる
            return this;
        }

        /// <summary>ICardFactory を設定し、初期カード生成を可能にする</summary>
        /// <param name="cardFactory">ICardFactory の実装</param>
        /// <returns>CardPileBuilder のインスタンス</returns>
        public CardPileBuilder UseCardFactory(ICardFactory cardFactory)
        {
            Assert.IsNotNull(cardFactory, "cardFactory が null です");
            _cardFactory = cardFactory;
            _hasParametersChanged = true; // パラメータ変更フラグを立てる
            return this;
        }

        public CardPileBuilder WithPileId(PileId id)
        {
            _pileId = id;
            _hasParametersChanged = true; // パラメータ変更フラグを立てる
            return this;
        }


        /// <summary>山札を生成する（初期カード指定なし）</summary>
        /// <returns>生成された CardPile のインスタンス</returns>
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
        /// <param name="initialCards">初期カードのコレクション</param>
        /// <returns>生成された CardPile のインスタンス</returns>
        public CardPile Build(IEnumerable<Card> initialCards)
        {
            // 初期パラメータのまま使用された場合の警告
            if (!_hasParametersChanged)
            {
                Debug.LogWarning("[CardPileBuilder] パラメータが初期値のままBuild()が実行されました。" +
                    "適切なパラメータ設定（WithName, WithMaxCount, UseView など）を行うことを推奨します。");
            }

            // モデル生成
            // IDはビルダー段階では未確定のため暫定0で生成（後段でレジストリ付与/再割当を許容）
            CardPile pile = initialCards == null
                ? _innerFactory.CreatePile(_pileId, _name, _maxCount)
                : _innerFactory.CreatePile(_pileId, _name, initialCards, _maxCount);

            if (_useView)
            {
                // View を生成
                ICardPileView view = Object.Instantiate(_viewPrefab, _viewParent);
                // レイアウト設定を反映
                view.SetCardViewLayoutInfo(_layoutSettings);
                // Presenter を生成
                var presenter = new CardPilePresenter(pile, view);
            }

            ResetParameters();

            return pile;
        }

        private void ResetParameters()
        {
            _useView = false;
            _useInitialCards = false;
            _name = "CardPile";
            _maxCount = int.MaxValue;
            _initialSuits = InGameConsts.DEFAULT_INITIAL_SUITS;
            _initialCountPerSuit = InGameConsts.DEFAULT_INITIAL_COUNT_PER_SUIT;
            _layoutSettings = CardPileLayoutSettings.Default;
            _hasParametersChanged = false; // フラグもリセット
        }
    }
}