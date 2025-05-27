using Tetrage.Core.Contracts;
using UnityEngine;
using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.Core.Enums;
using Tetrage.Factories;
using System.Linq;
using Tetrage.UI;
using Tetrage.Core.Constants;

namespace Tetrage.Factories
{
    /// <summary>
    /// ゲームステージ生成用Factory
    /// </summary>
    public class StageFactory : IStageFactory
    {
        private readonly ICardPileFactory _deckFactory;
        private readonly ICardPileFactory _pileFactory;
        private readonly CardModelFactory _cardModelFactory;
        private readonly CardWithViewFactory _cardFactory;
        private readonly CardPileBuilder _pileBuilder;
        private readonly Dictionary<CardPileViewType, GameObject> _pileViewPrefabDict;
        private readonly BasicCardPileView _stackViewPrefab;
        private readonly BasicCardPileView _trashViewPrefab;
        private readonly Transform _cardParent;
        private readonly Transform _pileParent;
        private readonly Dictionary<Card, CardView> _cardViewsDict;
        private readonly CardPileViewType _pileViewType;
        private readonly int _countPerSuit = InGameConsts.DEFAULT_INITIAL_COUNT_PER_SUIT;
        /// <summary>
        /// ステージファクトリのコンストラクタ
        /// </summary>
        /// <param name="deckFactory">デッキ生成用ファクトリ</param>
        /// <param name="pileFactory">山札生成用ファクトリ</param>
        /// <param name="cardsPerSuit">1スートあたりのカード枚数（デフォルト: 13）</param>
        /// <param name="numberOfSuits">使用するスートの種類数（デフォルト: 4）</param>
        public StageFactory(
            ICardPileFactory deckFactory,
            ICardPileFactory pileFactory,
            CardView cardViewPrefab,
            Transform cardParent,
            Transform pileParent,
            BasicCardPileView stackViewPrefab,
            BasicCardPileView trashViewPrefab
            )
        {
            _deckFactory = deckFactory;
            _pileFactory = pileFactory;
            _stackViewPrefab = stackViewPrefab;
            _trashViewPrefab = trashViewPrefab;
            _cardParent = cardParent;
            _pileParent = pileParent;
            _cardViewsDict = new Dictionary<Card, CardView>();

            // モデルファクトリとデコレータファクトリの初期化
            _cardModelFactory = new CardModelFactory();
            _cardFactory = new CardWithViewFactory(_cardModelFactory, cardViewPrefab, cardParent, _cardViewsDict);
            // CardPileBuilder の初期化
            _pileBuilder = new CardPileBuilder(_pileFactory)
                .UseCardFactory(_cardFactory)
                .UseView(_pileViewPrefabDict[_pileViewType].GetComponent<BasicCardPileView>(), pileParent, _cardViewsDict);
        }

        /// <summary>
        /// ステージ(MonoBehaviour)生成と初期配置
        /// </summary>
        /// <summary>
        /// ステージ(MonoBehaviour)生成と初期配置を行います。
        /// </summary>
        /// <returns>生成されたStageManagerインスタンス。</returns>
        public Stage SetupStage()
        {
            //cardpilebuilderを使って山札を作る
            CardPileBuilder builder = new CardPileBuilder(_pileFactory);
            CardPile deck = builder.Build();
            Stage stage = new Stage(BuildStack(), BuildTrash());
            return stage;
        }

        private CardPile BuildStack()
        {
            var suits = new[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };
            var count = suits.Length * _countPerSuit;

            // ビルダーで山札生成（初期カード付き）
            var pile = _pileBuilder
                .UseView(_stackViewPrefab, _pileParent, _cardViewsDict)
                .WithName("Stack")
                .WithMaxCount(count)
                .WithInitialCards(_cardFactory, suits, _countPerSuit)
                .Build();
            return pile;
        }

        private CardPile BuildTrash()
        {
            // ビルダーで山札生成（初期カード付き）
            var pile = _pileBuilder
                .UseView(_trashViewPrefab, _pileParent, _cardViewsDict)
                .WithName("Trash")
                .WithMaxCount(InGameConsts.DEFAULT_CARD_PILE_CAPACITY)
                .Build();
            return pile;
        }
    }
}