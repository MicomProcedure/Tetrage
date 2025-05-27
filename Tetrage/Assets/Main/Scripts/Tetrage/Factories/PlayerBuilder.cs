using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.UI;
using Tetrage.Presenters;
using Tetrage.Core.Constants;
using Tetrage.Core.Enums;

namespace Tetrage.Factories
{
    /// <summary>プレイヤー（初期カードパイル付き）を構築するビルダーパターン実装</summary>
    public class PlayerBuilder
    {
        private readonly PlayerModelFactory _innerFactory;      // プレイヤー生成用基本ファクトリ
        private BasicPlayerView _viewPrefab;                // プレイヤー表示用ビュー
        private Transform _viewParent;                      // プレイヤー表示用ビューの親
        private Dictionary<Card, CardView> _cardViewsDict;  // カード表示用ビューのディクショナリ
        private Dictionary<CardPileType, BasicCardPileView> _cardPileViewsDict; // カードパイル表示用ビューのディクショナリ
        private bool _useView;                             // プレイヤー表示用ビューの使用フラグ
        private string _userId = InGameConsts.DEFAULT_PLAYER_ID;                                        // ユーザーID
        private int _handsCapacity = InGameConsts.DEFAULT_PLAYER_HAND_CAPACITY;                         // 手札の容量
        private int _tmpCapacity = InGameConsts.DEFAULT_PLAYER_TMP_CAPACITY;                            // 一時保持カードの容量
        private int _targetCapacity = InGameConsts.DEFAULT_PLAYER_TARGET_CAPACITY;                      // ターゲットカードの容量
        private float _layoutWidth;                        // カードパイル表示用ビューの幅
        private float _layoutMinSpacing;                   // カードパイル表示用ビューの最小間隔
        private float _layoutMaxSpacing;                   // カードパイル表示用ビューの最大間隔
        private Vector3 _layoutOffset;                     // カードパイル表示用ビューのオフセット
        private static int _playerBuildingCount = 0;       // このPlayerBuilderで生成したPlayerの数

        /// <summary>基礎となる IPlayerFactory を受け取るコンストラクタ</summary>
        public PlayerBuilder(PlayerModelFactory innerFactory)
        {
            Assert.IsNotNull(innerFactory, "innerFactory(PlayerModelFactory) が null です");
            
            _innerFactory = innerFactory;

            // デフォルト設定
            _userId = InGameConsts.DEFAULT_PLAYER_ID + _playerBuildingCount;

            // デフォルトのレイアウト設定を適用
            _layoutWidth = InGameConsts.DEFAULT_CARD_PILE_WIDTH;
            _layoutMinSpacing = InGameConsts.DEFAULT_CARD_VIEW_MIN_SPACING;
            _layoutMaxSpacing = InGameConsts.DEFAULT_CARD_VIEW_MAX_SPACING;
            _layoutOffset = InGameConsts.DEFAULT_CARD_VIEW_POSITION_OFFSET;
            
        }

        /// <summary>View と Presenter を生成するよう設定する</summary>
        public PlayerBuilder UseView(
            BasicPlayerView viewPrefab, 
            Transform parent, 
            Dictionary<Card, CardView> cardViewsDict,
            Dictionary<CardPileType, BasicCardPileView> cardPileViewsDict)
        {
            Assert.IsNotNull(viewPrefab, "viewPrefab が null です");
            Assert.IsNotNull(parent, "parent が null です");
            Assert.IsNotNull(cardViewsDict, "cardViewsDict が null です");
            Assert.IsNotNull(cardPileViewsDict, "cardPileViewsDict が null です");
            
            ValidateCardPileViewDict(cardPileViewsDict);
            
            _useView = true;
            _viewPrefab = viewPrefab;
            _viewParent = parent;
            _cardViewsDict = cardViewsDict;
            _cardPileViewsDict = cardPileViewsDict;
            return this;
        }

        /// <summary>Viewを使用しないよう設定する</summary>
        public PlayerBuilder WithoutView()
        {
            _useView = false;
            return this;
        }

        /// <summary>生成するプレイヤーのユーザーIDを設定する</summary>
        public PlayerBuilder WithUserId(string userId)
        {
            Assert.IsFalse(string.IsNullOrEmpty(userId), "userId が null または空です");
            _userId = userId;
            return this;
        }


        // /// <summary>CardPileView のレイアウト情報を設定する</summary>
        // /// <param name="pileWidth">カードパイルの幅（デフォルト: DEFAULT_CARD_PILE_WIDTH）</param>
        // /// <param name="minSpacing">カードパイルの最小間隔（デフォルト: DEFAULT_CARD_VIEW_MIN_SPACING）</param>
        // /// <param name="maxSpacing">カードパイルの最大間隔（デフォルト: DEFAULT_CARD_VIEW_MAX_SPACING）</param>
        // /// <param name="positionOffset">カードパイルの位置オフセット（デフォルト: DEFAULT_CARD_VIEW_POSITION_OFFSET）</param>
        // public PlayerBuilder WithLayout(
        //     float pileWidth = InGameConsts.DEFAULT_CARD_PILE_WIDTH,
        //     float minSpacing = InGameConsts.DEFAULT_CARD_VIEW_MIN_SPACING,
        //     float maxSpacing = InGameConsts.DEFAULT_CARD_VIEW_MAX_SPACING,
        //     Vector3 positionOffset = default)
        // {
        //     _layoutWidth = pileWidth;
        //     _layoutMinSpacing = minSpacing;
        //     _layoutMaxSpacing = maxSpacing;
        //     _layoutOffset = positionOffset == default ? InGameConsts.DEFAULT_CARD_VIEW_POSITION_OFFSET : positionOffset;
        //     return this;
        // }



        /// <summary>プレイヤーを生成する</summary>
        /// <returns>生成された Player のインスタンス</returns>
        public IPlayer Build()
        {
            // カードパイル生成用ビルダーを作成
            var cardPileBuilder = new CardPileBuilder(new CardPileFactory());
            BasicPlayerView playerView = null;

            // 各カードパイルを生成
            CardPile target = null;
            CardPile hands = null;
            CardPile tmp = null;

            if (_useView)
            {
              // プレイヤー View を生成
                playerView = Object.Instantiate(_viewPrefab, _viewParent);

                // View付きでカードパイルを生成
                target = cardPileBuilder
                    .WithName(CardPileType.Target.ToString())
                    .WithMaxCount(_targetCapacity)
                    .UseView(_cardPileViewsDict[CardPileType.Target], _viewParent, _cardViewsDict)
                    .WithLayout(_layoutWidth, _layoutMinSpacing, _layoutMaxSpacing, _layoutOffset)
                    .Build();

                hands = cardPileBuilder
                    .WithName(CardPileType.Hands.ToString())
                    .WithMaxCount(_handsCapacity)
                    .UseView(_cardPileViewsDict[CardPileType.Hands], _viewParent, _cardViewsDict)
                    .WithLayout(_layoutWidth, _layoutMinSpacing, _layoutMaxSpacing, _layoutOffset)
                    .Build();

                tmp = cardPileBuilder
                    .WithName(CardPileType.Tmp.ToString())
                    .WithMaxCount(_tmpCapacity)
                    .UseView(_cardPileViewsDict[CardPileType.Tmp], _viewParent, _cardViewsDict)
                    .WithLayout(_layoutWidth, _layoutMinSpacing, _layoutMaxSpacing, _layoutOffset)
                    .Build();
            }
            else
            {
                // Viewなしでカードパイルを生成
                target = cardPileBuilder
                    .WithName(CardPileType.Target.ToString())
                    .WithMaxCount(_targetCapacity)
                    .WithoutView()
                    .Build();

                hands = cardPileBuilder
                    .WithName(CardPileType.Hands.ToString())
                    .WithMaxCount(_handsCapacity)
                    .WithoutView()
                    .Build();

                tmp = cardPileBuilder
                    .WithName(CardPileType.Tmp.ToString())
                    .WithMaxCount(_tmpCapacity)
                    .WithoutView()
                    .Build();
            }

            // プレイヤーモデル生成
            IPlayer player = _innerFactory.CreatePlayer(_userId, target, hands, tmp);

            if (_useView)
            {

                // プレイヤー Presenter を生成
                var presenter = new PlayerPresenter(playerView, player);
            }

            _playerBuildingCount++; // 名前を設定しなかった場合のデフォルトの名前を設定するためのカウンタなのであんまり気にしなくてよい

            return player;
        }

        /// <summary>
        /// CardPileViewDictに必要なキーと値が存在するかを検証します
        /// </summary>
        /// <param name="cardPileViewsDict">検証対象の辞書</param>
        private static void ValidateCardPileViewDict(Dictionary<CardPileType, BasicCardPileView> cardPileViewsDict)
        {
            // 必要なCardPileTypeの配列
            var requiredCardPileTypes = new[]
            {
                CardPileType.Target,
                CardPileType.Hands,
                CardPileType.Tmp
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