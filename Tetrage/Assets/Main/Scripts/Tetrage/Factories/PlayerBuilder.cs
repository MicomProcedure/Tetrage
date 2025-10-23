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
    /// <summary>プレイヤー（初期カードパイル付き）を構築するビルダーパターン実装</summary>
    public class PlayerBuilder
    {
        private readonly IPlayerFactory _innerFactory;      // プレイヤーモデル生成用基本ファクトリ
        private BasicPlayerView _viewPrefab;                // プレイヤー表示用ビュー
        private Transform _viewParent;                      // プレイヤー表示用ビューの親
        private Vector3 _viewSpawnPosition;                 // プレイヤー表示用ビューの生成位置
        private Dictionary<CardPileType, BasicCardPileView> _cardPileViewsDict; // カードパイル表示用ビューのディクショナリ
        private bool _useView;                             // プレイヤー表示用ビューの使用フラグ
        private PlayerId _id;                               // プレイヤーID
        private string _userId = InGameConsts.DEFAULT_PLAYER_ID;                                        // ユーザーID
        private int _iconIndex = InGameConsts.DEFAULT_PLAYER_ICON_INDEX;                                        // アイコンインデックス
        private int _handsCapacity = InGameConsts.DEFAULT_PLAYER_HAND_CAPACITY + 1;                         // 手札の容量（一枚のみキャパオーバーを許容する）
        private int _tmpCapacity = InGameConsts.DEFAULT_PLAYER_TMP_CAPACITY;                            // 一時保持カードの容量
        private int _targetCapacity = InGameConsts.DEFAULT_PLAYER_TARGET_CAPACITY;                      // ターゲットカードの容量
        private Dictionary<CardPileType, CardPileLayoutSettings> _cardPileLayoutSettingsDict; // カードパイル表示用ビューのレイアウト設定
        private int _playerBuildingCount = 0;       // このPlayerBuilderで生成したPlayerの数
        private PlayerType _playerType;

        /// <summary>基礎となる IPlayerFactory を受け取るコンストラクタ</summary>
        public PlayerBuilder(IPlayerFactory innerFactory)
        {
            Assert.IsNotNull(innerFactory, "innerFactory(PlayerModelFactory) が null です");

            _innerFactory = innerFactory;

            // デフォルト設定
            _userId = InGameConsts.DEFAULT_PLAYER_ID + _playerBuildingCount;

            _cardPileLayoutSettingsDict = new Dictionary<CardPileType, CardPileLayoutSettings>
            {
                { CardPileType.Target, CardPileLayoutSettings.Default },
                { CardPileType.Hands, CardPileLayoutSettings.Default },
                { CardPileType.Tmp, CardPileLayoutSettings.Default }
            };


        }

        /// <summary>View と Presenter を生成するよう設定する</summary>
        /// <param name="viewPrefab">プレイヤー表示用ビューのプレハブ</param>
        /// <param name="spawnPosition">プレイヤー表示用ビューの生成位置</param>
        /// <param name="parent">プレイヤー表示用ビューの親</param>
        /// <param name="cardPileViewsDict">カードパイル表示用ビューのディクショナリ</param>
        public PlayerBuilder UseView(
            BasicPlayerView viewPrefab,
            Vector3 spawnPosition,
            Transform parent,
            Dictionary<CardPileType, BasicCardPileView> cardPileViewsDict)
        {
            Assert.IsNotNull(viewPrefab, "viewPrefab が null です");
            Assert.IsNotNull(parent, "parent が null です");
            Assert.IsNotNull(cardPileViewsDict, "cardPileViewsDict が null です");
            ValidateCardPileViewDict(cardPileViewsDict);

            _useView = true;
            _viewPrefab = viewPrefab;
            _viewSpawnPosition = spawnPosition;
            _viewParent = parent;
            _cardPileViewsDict = cardPileViewsDict;
            return this;
        }

        public PlayerBuilder WithPlayerId(PlayerId id)
        {
            _id = id;
            return this;
        }

        public PlayerBuilder WithIconIndex(int iconIndex)
        {
            _iconIndex = iconIndex;
            return this;
        }

        /// <summary>Viewを使用しないよう設定する</summary>
        public PlayerBuilder WithoutView()
        {
            _useView = false;
            return this;
        }

        /// <summary>生成するプレイヤーのユーザーIDを設定する</summary>
        /// <param name="userId">ユーザーID</param>
        public PlayerBuilder WithUserId(string userId)
        {
            Assert.IsFalse(string.IsNullOrEmpty(userId), "userId が null または空です");
            _userId = userId;
            return this;
        }

        /// <summary>生成するプレイヤーのプレイヤータイプを設定する</summary>
        /// <param name="playerType">プレイヤータイプ</param>
        public PlayerBuilder WithPlayerType(PlayerType playerType)
        {
            _playerType = playerType;
            // 現在は特定の設定はなし。
            return this;
        }

        /// <summary>生成するプレイヤーのカードパイルのレイアウト設定を設定する</summary>
        /// <param name="cardPileType">カードパイルのタイプ</param>
        /// <param name="layoutSettings">カードパイルのレイアウト設定</param>
        public PlayerBuilder WithCardPileLayoutSettings(CardPileType cardPileType, CardPileLayoutSettings layoutSettings)
        {
            _cardPileLayoutSettingsDict[cardPileType] = layoutSettings;
            return this;
        }


        /// <summary>プレイヤーを生成する</summary>
        /// <returns>生成された Player のインスタンス</returns>
        public IPlayer Build()
        {
            if (_id == default(PlayerId))
            {
                Debug.LogError("PlayerBuilder: id が null です。UserIdを使用して生成してください。");
            }

            // カードパイル生成用ビルダーを作成
            var cardPileBuilder = new CardPileBuilder(new CardPileFactory());

            // プレイヤー View を格納する変数の用意（if構文をまたぐためにif文の外に出しておく）
            BasicPlayerView playerView = null;

            // 各カードパイルを生成
            CardPile target = null;
            CardPile hands = null;
            CardPile tmp = null;

            if (_useView)
            {
                // プレイヤー View を生成
                playerView = Object.Instantiate(_viewPrefab, _viewSpawnPosition, Quaternion.identity, _viewParent); // Quaternion.identityは回転なしの意味

                // View付きでカードパイルを生成
                target = cardPileBuilder
                    .WithName(CardPileType.Target.ToString())
                    .WithMaxCount(_targetCapacity)
                    .UseView(_cardPileViewsDict[CardPileType.Target], playerView.TargetRoot)
                    .WithLayout(_cardPileLayoutSettingsDict[CardPileType.Target])
                    .WithPileId(PileIds.PlayerTarget(_id.Value))
                    .Build();

                hands = cardPileBuilder
                    .WithName(CardPileType.Hands.ToString())
                    .WithMaxCount(_handsCapacity)
                    .UseView(_cardPileViewsDict[CardPileType.Hands], playerView.HandsRoot)
                    .WithLayout(_cardPileLayoutSettingsDict[CardPileType.Hands])
                    .WithPileId(PileIds.PlayerHands(_id.Value))
                    .Build();

                tmp = cardPileBuilder
                    .WithName(CardPileType.Tmp.ToString())
                    .WithMaxCount(_tmpCapacity)
                    .UseView(_cardPileViewsDict[CardPileType.Tmp], playerView.TmpRoot)
                    .WithLayout(_cardPileLayoutSettingsDict[CardPileType.Tmp])
                    .WithPileId(PileIds.PlayerTmp(_id.Value))
                    .Build();
            }
            else
            {
                // Viewなしでカードパイルを生成
                target = cardPileBuilder
                    .WithName(CardPileType.Target.ToString())
                    .WithMaxCount(_targetCapacity)
                    .WithoutView()
                    .WithPileId(PileIds.PlayerTarget(_id.Value))
                    .Build();

                hands = cardPileBuilder
                    .WithName(CardPileType.Hands.ToString())
                    .WithMaxCount(_handsCapacity)
                    .WithoutView()
                    .WithPileId(PileIds.PlayerHands(_id.Value))
                    .Build();

                tmp = cardPileBuilder
                    .WithName(CardPileType.Tmp.ToString())
                    .WithMaxCount(_tmpCapacity)
                    .WithoutView()
                    .WithPileId(PileIds.PlayerTmp(_id.Value))
                    .Build();
            }

            // プレイヤーモデル生成
            IPlayer player = _innerFactory.CreatePlayer(_id, _userId, _iconIndex, target, hands, tmp);

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