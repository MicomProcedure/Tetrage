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

namespace Tetrage.Factories
{
    /// <summary>
    /// ステージ（初期カード付き）を構築するビルダーパターン実装
    /// PlayerBuilderと同様の設計で、StageModelFactoryを内部利用してViewの生成も可能
    /// </summary>
    public class StageBuilder
    {
        private readonly StageModelFactory _innerFactory;      // ステージモデル生成用基本ファクトリ
        private readonly ICardPileFactory _cardPileFactory;    // カードパイル生成用ファクトリ
        private readonly ICardFactory _cardModelFactory;       // カードモデル生成用ファクトリ
        private CardView _cardViewPrefab;                     // カード表示用ビュープレハブ
        private StageView _stageViewPrefab;                   // ステージ表示用ビュープレハブ
        private Transform _stageParent;                       // ステージ表示用ビューの親
        private Vector3 _stageSpawnPosition;                  // ステージ表示用ビューの生成位置
        private Dictionary<CardPileType, BasicCardPileView> _cardPileViewsDict; // カードパイル表示用ビューのディクショナリ
        private bool _useView;                               // ステージ表示用ビューの使用フラグ
        private int _countPerSuit = InGameConsts.DEFAULT_INITIAL_COUNT_PER_SUIT; // スートごとのカード枚数
        private Dictionary<CardPileType, CardPileLayoutSettings> _cardPileLayoutSettingsDict; // カードパイル表示用ビューのレイアウト設定

        /// <summary>
        /// StageModelFactoryを受け取るコンストラクタ
        /// </summary>
        /// <param name="innerFactory">ステージモデル生成用ファクトリ</param>
        /// <param name="cardPileFactory">カードパイル生成用ファクトリ</param>
        /// <param name="cardModelFactory">カードモデル生成用ファクトリ</param>
        public StageBuilder(StageModelFactory innerFactory, ICardPileFactory cardPileFactory, ICardFactory cardModelFactory)
        {
            Assert.IsNotNull(innerFactory, "innerFactory(StageModelFactory) が null です");
            Assert.IsNotNull(cardPileFactory, "cardPileFactory が null です");
            Assert.IsNotNull(cardModelFactory, "cardModelFactory が null です");
            
            _innerFactory = innerFactory;
            _cardPileFactory = cardPileFactory;
            _cardModelFactory = cardModelFactory;

            // デフォルトのレイアウト設定
            _cardPileLayoutSettingsDict = new Dictionary<CardPileType, CardPileLayoutSettings>
            {
                { CardPileType.Stack, CardPileLayoutSettings.Default },
                { CardPileType.Trash, CardPileLayoutSettings.Default }
            };
        }

        /// <summary>
        /// StageModelFactoryのみを受け取るコンストラクタ（後方互換性のため）
        /// </summary>
        /// <param name="innerFactory">ステージモデル生成用ファクトリ</param>
        public StageBuilder(StageModelFactory innerFactory)
        {
            Assert.IsNotNull(innerFactory, "innerFactory(StageModelFactory) が null です");
            
            _innerFactory = innerFactory;
            // デフォルトのファクトリを使用（後方互換性）
            _cardPileFactory = new CardPileFactory();
            _cardModelFactory = new CardModelFactory();

            // デフォルトのレイアウト設定
            _cardPileLayoutSettingsDict = new Dictionary<CardPileType, CardPileLayoutSettings>
            {
                { CardPileType.Stack, CardPileLayoutSettings.Default },
                { CardPileType.Trash, CardPileLayoutSettings.Default }
            };
        }

        /// <summary>
        /// ViewとPresenterを生成するよう設定する
        /// </summary>
        /// <param name="stageViewPrefab">ステージ表示用ビューのプレハブ</param>
        /// <param name="stageSpawnPosition">ステージ表示用ビューの生成位置</param>
        /// <param name="stageParent">ステージ表示用ビューの親</param>
        /// <param name="cardViewPrefab">カード表示用ビューのプレハブ</param>
        /// <param name="cardPileViewsDict">カードパイル表示用ビューのディクショナリ</param>
        /// <returns>このStageBuilderインスタンス</returns>
        public StageBuilder UseView(
            StageView stageViewPrefab,
            Vector3 stageSpawnPosition,
            Transform stageParent,
            CardView cardViewPrefab, 
            Dictionary<CardPileType, BasicCardPileView> cardPileViewsDict)
        {
            Assert.IsNotNull(stageViewPrefab, "stageViewPrefab が null です");
            Assert.IsNotNull(stageParent, "stageParent が null です");
            Assert.IsNotNull(cardViewPrefab, "cardViewPrefab が null です");
            Assert.IsNotNull(cardPileViewsDict, "cardPileViewsDict が null です");
            ValidateCardPileViewDict(cardPileViewsDict);
            
            _useView = true;
            _stageViewPrefab = stageViewPrefab;
            _stageSpawnPosition = stageSpawnPosition;
            _stageParent = stageParent;
            _cardViewPrefab = cardViewPrefab;
            _cardPileViewsDict = cardPileViewsDict;
            
            return this;
        }

        /// <summary>
        /// Viewを使用しないよう設定する
        /// </summary>
        /// <returns>このStageBuilderインスタンス</returns>
        public StageBuilder WithoutView()
        {
            _useView = false;
            return this;
        }

        /// <summary>
        /// スートごとのカード枚数を設定する
        /// </summary>
        /// <param name="countPerSuit">スートごとのカード枚数</param>
        /// <returns>このStageBuilderインスタンス</returns>
        public StageBuilder WithCountPerSuit(int countPerSuit)
        {
            Assert.IsTrue(countPerSuit > 0, "countPerSuit は0より大きい値である必要があります");
            _countPerSuit = countPerSuit;
            return this;
        }

        /// <summary>
        /// カードパイルのレイアウト設定を設定する
        /// </summary>
        /// <param name="cardPileType">カードパイルのタイプ</param>
        /// <param name="layoutSettings">カードパイルのレイアウト設定</param>
        /// <returns>このStageBuilderインスタンス</returns>
        public StageBuilder WithCardPileLayoutSettings(CardPileType cardPileType, CardPileLayoutSettings layoutSettings)
        {
            // ステージ関連のCardPileTypeのみ受け入れる
            Assert.IsTrue(cardPileType == CardPileType.Stack || cardPileType == CardPileType.Trash,
                $"StageBuilderは {CardPileType.Stack} と {CardPileType.Trash} のみサポートします");
            
            _cardPileLayoutSettingsDict[cardPileType] = layoutSettings;
            return this;
        }

        /// <summary>
        /// ステージを生成する
        /// </summary>
        /// <returns>生成されたStageのインスタンス</returns>
        public Stage Build()
        {
            // カードパイル生成用ビルダーを作成
            var cardPileBuilder = new CardPileBuilder(_cardPileFactory);

            // ステージ View を格納する変数の用意（if構文をまたぐためにif文の外に出しておく）
            StageView stageView = null;

            // 各カードパイルを生成
            CardPile stack = null;
            CardPile trash = null;

            if (_useView)
            {
                // ステージ View を生成（StageViewプレハブが指定されている場合のみ）

                stageView = Object.Instantiate(_stageViewPrefab, _stageSpawnPosition, Quaternion.identity, _stageParent);


                // CardWithViewFactoryを作成（依存性注入されたCardFactoryを使用）
                var cardWithViewFactory = new CardWithViewFactory(_cardModelFactory, _cardViewPrefab, stageView != null ? stageView.transform : _stageParent);

                // View付きでカードパイルを生成
                stack = BuildStackWithView(cardPileBuilder, cardWithViewFactory);
                trash = BuildTrashWithView(cardPileBuilder, cardWithViewFactory);
            }
            else
            {
                // Viewなしでカードパイルを生成
                stack = BuildStackWithoutView(cardPileBuilder);
                trash = BuildTrashWithoutView(cardPileBuilder);
            }

            // StageModelFactoryを使用してモデルを生成
            Stage stage = _innerFactory.SetupStage(stack, trash);

            if (_useView)
            {
                // ステージ Presenter を生成
                var presenter = new StagePresenter(stageView, stage);
            }

            return stage;
        }

        /// <summary>
        /// View付きでStackカードパイルを構築する
        /// </summary>
        /// <param name="cardPileBuilder">カードパイルビルダー</param>
        /// <param name="cardWithViewFactory">カード&ビューファクトリ</param>
        /// <returns>View付きのStackカードパイル</returns>
        private CardPile BuildStackWithView(CardPileBuilder cardPileBuilder, CardWithViewFactory cardWithViewFactory)
        {
            var suits = new[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };
            var maxCount = suits.Length * _countPerSuit;
            
            return cardPileBuilder
                .WithName("Stack")
                .WithMaxCount(maxCount)
                .UseView(_cardPileViewsDict[CardPileType.Stack], _stageParent)
                .WithLayout(_cardPileLayoutSettingsDict[CardPileType.Stack])
                .UseCardFactory(cardWithViewFactory)
                .WithInitialCards(cardWithViewFactory, suits, _countPerSuit)
                .Build();
        }

        /// <summary>
        /// View付きでTrashカードパイルを構築する
        /// </summary>
        /// <param name="cardPileBuilder">カードパイルビルダー</param>
        /// <param name="cardWithViewFactory">カード&ビューファクトリ</param>
        /// <returns>View付きのTrashカードパイル</returns>
        private CardPile BuildTrashWithView(CardPileBuilder cardPileBuilder, CardWithViewFactory cardWithViewFactory)
        {
            return cardPileBuilder
                .WithName("Trash")
                .WithMaxCount(InGameConsts.DEFAULT_CARD_PILE_CAPACITY)
                .UseView(_cardPileViewsDict[CardPileType.Trash], _stageParent)
                .WithLayout(_cardPileLayoutSettingsDict[CardPileType.Trash])
                .UseCardFactory(cardWithViewFactory)
                .Build(); // 空のカードパイル
        }

        /// <summary>
        /// ViewなしでStackカードパイルを構築する
        /// </summary>
        /// <param name="cardPileBuilder">カードパイルビルダー</param>
        /// <returns>ViewなしのStackカードパイル</returns>
        private CardPile BuildStackWithoutView(CardPileBuilder cardPileBuilder)
        {
            var suits = new[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };
            var maxCount = suits.Length * _countPerSuit;
            
            return cardPileBuilder
                .WithName("Stack")
                .WithMaxCount(maxCount)
                .WithoutView()
                .UseCardFactory(_cardModelFactory)
                .WithInitialCards(_cardModelFactory, suits, _countPerSuit)
                .Build();
        }

        /// <summary>
        /// ViewなしでTrashカードパイルを構築する
        /// </summary>
        /// <param name="cardPileBuilder">カードパイルビルダー</param>
        /// <returns>ViewなしのTrashカードパイル</returns>
        private CardPile BuildTrashWithoutView(CardPileBuilder cardPileBuilder)
        {
            return cardPileBuilder
                .WithName("Trash")
                .WithMaxCount(InGameConsts.DEFAULT_CARD_PILE_CAPACITY)
                .WithoutView()
                .UseCardFactory(_cardModelFactory)
                .Build(); // 空のカードパイル
        }

        /// <summary>
        /// CardPileViewDictに必要なキーと値が存在するかを検証します
        /// </summary>
        /// <param name="cardPileViewsDict">検証対象の辞書</param>
        private static void ValidateCardPileViewDict(Dictionary<CardPileType, BasicCardPileView> cardPileViewsDict)
        {
            // 必要なCardPileTypeの配列（ステージ用）
            var requiredCardPileTypes = new[]
            {
                CardPileType.Stack,
                CardPileType.Trash
            };

            // 各必要キーの存在と値のnullチェック
            foreach (var requiredType in requiredCardPileTypes)
            {
                // キーの存在チェック
                Assert.IsTrue(cardPileViewsDict.ContainsKey(requiredType),
                    $"cardPileViewsDict に必要なキー '{requiredType}' が存在しません");

                // 対応する値のnullチェック
                var cardPileView = cardPileViewsDict[requiredType];
                Assert.IsNotNull(cardPileView,
                    $"cardPileViewsDict のキー '{requiredType}' に対応する値が null です");
            }
        }
    }
} 