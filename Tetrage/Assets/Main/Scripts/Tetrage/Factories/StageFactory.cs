using Tetrage.Core.Contracts;
using UnityEngine;
using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.Core.Enums;
using Tetrage.Factories;
using System.Linq;
using Tetrage.UI;
using Tetrage.Core.Constants;
using UnityEngine.Assertions;
using Tetrage.Core.DTO;

namespace Tetrage.Factories
{
    /// <summary>
    /// ゲームステージ生成用Factory
    /// </summary>
    public class StageFactory : IStageFactory
    {
        private readonly ICardPileFactory _pileFactory;
        private readonly CardModelFactory _cardModelFactory;
        private readonly CardView _cardViewPrefab;
        private readonly Transform _pileParent;
        private readonly Dictionary<CardPileType, BasicCardPileView> _pileViewPrefabDict;
        private readonly Dictionary<CardPileType, CardPileLayoutSettings> _cardPileLayoutSettingsDict;
        private readonly int _countPerSuit = InGameConsts.DEFAULT_INITIAL_COUNT_PER_SUIT;

        /// <summary>
        /// ステージファクトリのコンストラクタ
        /// </summary>
        /// <param name="pileFactory">カードパイル生成用ファクトリ</param>
        /// <param name="cardViewPrefab">カード表示用Viewプレハブ</param>
        /// <param name="pileParent">カードパイル表示用ビューの親Transform</param>
        /// <param name="pileViewPrefabDict">カードパイルタイプとView Prefabの対応辞書</param>
        public StageFactory(
            ICardPileFactory pileFactory,
            CardView cardViewPrefab,
            Transform pileParent,
            Dictionary<CardPileType, BasicCardPileView> pileViewPrefabDict
            )
        {
            // 必須パラメータのnullチェック
            Assert.IsNotNull(pileFactory, "pileFactory が null です");
            Assert.IsNotNull(cardViewPrefab, "cardViewPrefab が null です");
            Assert.IsNotNull(pileParent, "pileParent が null です");
            Assert.IsNotNull(pileViewPrefabDict, "pileViewPrefabDict が null です");
            ValidatePileViewDict(pileViewPrefabDict);

            _pileFactory = pileFactory;
            _cardViewPrefab = cardViewPrefab;
            _pileParent = pileParent;
            _pileViewPrefabDict = pileViewPrefabDict;

            // モデルファクトリの初期化
            _cardModelFactory = new CardModelFactory();

            // デフォルトのレイアウト設定
            _cardPileLayoutSettingsDict = new Dictionary<CardPileType, CardPileLayoutSettings>
            {
                { CardPileType.Stack, CardPileLayoutSettings.Default },
                { CardPileType.Trash, CardPileLayoutSettings.Default }
            };
        }

        /// <summary>レイアウト設定を追加する</summary>
        public StageFactory WithCardPileLayoutSettings(CardPileType cardPileType, CardPileLayoutSettings layoutSettings)
        {
            _cardPileLayoutSettingsDict[cardPileType] = layoutSettings;
            return this;
        }

        /// <summary>
        /// ステージ(MonoBehaviour)生成と初期配置を行います。
        /// </summary>
        /// <returns>生成されたStageManagerインスタンス。</returns>
        public Stage SetupStage()
        {
            Stage stage = new Stage(BuildStack(), BuildTrash());
            return stage;
        }

        private CardPile BuildStack()
        {
            var suits = new[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };
            var count = suits.Length * _countPerSuit;

            // CardWithViewFactoryを生成
            var cardFactory = new CardWithViewFactory(_cardModelFactory, _cardViewPrefab);

            // ビルダーで山札生成（初期カード付き）
            var builder = new CardPileBuilder(_pileFactory);
            var pile = builder
                .WithName("Stack")
                .WithMaxCount(count)
                .UseView(_pileViewPrefabDict[CardPileType.Stack], _pileParent)
                .WithLayout(_cardPileLayoutSettingsDict[CardPileType.Stack])
                .UseCardFactory(cardFactory)
                .WithInitialCards(cardFactory, suits, _countPerSuit)
                .Build();
            return pile;
        }

        private CardPile BuildTrash()
        {
            // CardWithViewFactoryを生成
            var cardFactory = new CardWithViewFactory(_cardModelFactory, _cardViewPrefab);

            // ビルダーで山札生成
            var builder = new CardPileBuilder(_pileFactory);
            var pile = builder
                .WithName("Trash")
                .WithMaxCount(InGameConsts.DEFAULT_CARD_PILE_CAPACITY)
                .UseView(_pileViewPrefabDict[CardPileType.Trash], _pileParent)
                .WithLayout(_cardPileLayoutSettingsDict[CardPileType.Trash])
                .UseCardFactory(cardFactory)
                .Build();
            return pile;
        }

        /// <summary>
        /// PileViewDictに必要なキーと値が存在するかを検証します
        /// </summary>
        /// <param name="pileViewPrefabDict">検証対象の辞書</param>
        private static void ValidatePileViewDict(Dictionary<CardPileType, BasicCardPileView> pileViewPrefabDict)
        {
            // 必要なCardPileTypeの配列
            var requiredCardPileTypes = new[]
            {
                CardPileType.Stack,
                CardPileType.Trash
            };

            // 各必要キーの存在と値のnullチェック
            foreach (var requiredType in requiredCardPileTypes)
            {
                // キーの存在チェック
                Assert.IsTrue(pileViewPrefabDict.ContainsKey(requiredType),
                    $"pileViewPrefabDict に必要なキー '{requiredType}' が存在しません");

                // 対応する値のnullチェック
                var pileView = pileViewPrefabDict[requiredType];
                Assert.IsNotNull(pileView,
                    $"pileViewPrefabDict のキー '{requiredType}' に対応する値が null です");
            }
        }
    }
}