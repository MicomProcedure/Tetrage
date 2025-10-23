using UnityEngine; // MonoBehaviourを扱うため追加
using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.Core.Contracts;
using Tetrage.Core.Constants;
using Tetrage.Factories;
using UnityEngine.Assertions;
using Tetrage.Core.Ids;

namespace Tetrage.Factories
{
    /// <summary>
    /// プレイヤー生成用Factory
    /// StageModelFactoryと同様の設計パターンでファクトリを受け取って使用
    /// </summary>
    public class PlayerModelFactory : IPlayerFactory
    {
        private readonly ICardPileFactory _cardPileFactory;
        private readonly ICardFactory _cardModelFactory;

        /// <summary>
        /// プレイヤーモデルファクトリのコンストラクタ
        /// </summary>
        /// <param name="cardPileFactory">カードパイル生成用ファクトリ</param>
        /// <param name="cardModelFactory">カードモデル生成用ファクトリ</param>
        public PlayerModelFactory(ICardPileFactory cardPileFactory, ICardFactory cardModelFactory)
        {
            Assert.IsNotNull(cardPileFactory, "cardPileFactory が null です");
            Assert.IsNotNull(cardModelFactory, "cardModelFactory が null です");

            _cardPileFactory = cardPileFactory;
            _cardModelFactory = cardModelFactory;
        }

        /// <summary>
        /// 指定したユーザーIDのプレイヤーモデルを生成します（デフォルトのCardPileを使用）。
        /// </summary>
        /// <param name="userId">プレイヤーのユーザーID</param>
        /// <returns>生成されたIPlayerインターフェースのインスタンス。</returns>
        public IPlayer CreatePlayer(PlayerId id, string userId, int iconIndex)
        {
            Assert.IsFalse(string.IsNullOrEmpty(userId), "userId が null または空です");

            // デフォルトのCardPileを作成
            var hands = CreateDefaultHands(id);
            var tmp = CreateDefaultTmp(id);
            var target = CreateDefaultTarget(id);

            // プレイヤーモデルを生成
            IPlayer player = new Player(id, userId, iconIndex, target, hands, tmp);
            return player;
        }

        /// <summary>
        /// 指定したプレイヤーモデルを生成します。
        /// </summary>
        /// <param name="userId">ユーザーID</param>
        /// <param name="target">ターゲットカードパイル</param>
        /// <param name="hands">手札カードパイル</param>
        /// <param name="tmp">一時保管カードパイル</param>
        /// <returns>生成されたIPlayerインターフェースのインスタンス</returns>
        public IPlayer CreatePlayer(PlayerId id, string userId, int iconIndex, CardPile target, CardPile hands, CardPile tmp)
        {
            Assert.IsFalse(string.IsNullOrEmpty(userId), "userId が null または空です");
            Assert.IsNotNull(target, "target が null です");
            Assert.IsNotNull(hands, "hands が null です");
            Assert.IsNotNull(tmp, "tmp が null です");

            IPlayer player = new Player(id, userId, iconIndex, target, hands, tmp);
            return player;
        }

        /// <summary>
        /// デフォルトの手札カードパイルを作成
        /// </summary>
        /// <returns>生成されたCardPile</returns>
        private CardPile CreateDefaultHands(PlayerId id)
        {
            // IDは呼び出し側で確定できないため、暫定0（未割当）で生成
            return _cardPileFactory.CreatePile(PileIds.PlayerHands(id.Value), "Hands", InGameConsts.DEFAULT_PLAYER_HAND_CAPACITY);
        }

        /// <summary>
        /// デフォルトの一時保管カードパイルを作成
        /// </summary>
        /// <returns>生成されたCardPile</returns>
        private CardPile CreateDefaultTmp(PlayerId id)
        {
            return _cardPileFactory.CreatePile(PileIds.PlayerTmp(id.Value), "Tmp", InGameConsts.DEFAULT_PLAYER_TMP_CAPACITY);
        }

        /// <summary>
        /// デフォルトのターゲットカードパイルを作成
        /// </summary>
        /// <returns>生成されたCardPile</returns>
        private CardPile CreateDefaultTarget(PlayerId id)
        {
            return _cardPileFactory.CreatePile(PileIds.PlayerTarget(id.Value), "Target", InGameConsts.DEFAULT_PLAYER_TARGET_CAPACITY);
        }
    }
}