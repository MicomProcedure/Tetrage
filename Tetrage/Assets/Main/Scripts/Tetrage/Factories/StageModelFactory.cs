using System.Collections.Generic;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.Core.Enums;
using Tetrage.Core.Constants;
using UnityEngine.Assertions;
using Tetrage.Core.Ids;

namespace Tetrage.Factories
{
    /// <summary>
    /// ステージモデル生成用Factory（Viewの生成は行わない）
    /// PlayerModelFactoryと同様の設計パターンでシンプルなモデル生成を担当
    /// </summary>
    public class StageModelFactory : IStageFactory
    {
        private readonly ICardPileFactory _pileFactory;
        private readonly ICardFactory _cardModelFactory;
        private readonly int _countPerSuit = InGameConsts.DEFAULT_INITIAL_COUNT_PER_SUIT;

        /// <summary>
        /// ステージモデルファクトリのコンストラクタ
        /// </summary>
        /// <param name="pileFactory">カードパイル生成用ファクトリ</param>
        /// <param name="cardModelFactory">カードモデル生成用ファクトリ</param>
        public StageModelFactory(ICardPileFactory pileFactory, ICardFactory cardModelFactory)
        {
            Assert.IsNotNull(pileFactory, "pileFactory が null です");
            Assert.IsNotNull(cardModelFactory, "cardModelFactory が null です");
            
            _pileFactory = pileFactory;
            _cardModelFactory = cardModelFactory;
        }

        /// <summary>
        /// ステージモデルを生成します（デフォルトのCardPileを使用）
        /// </summary>
        /// <returns>生成されたStageインスタンス</returns>
        public Stage SetupStage()
        {
            var stack = CreateDefaultStack();
            var trash = CreateDefaultTrash();
            return new Stage(stack, trash);
        }

        /// <summary>
        /// 指定したカード設定でステージモデルを生成します（デフォルトのCardPileを使用）
        /// </summary>
        /// <param name="countPerSuit">スートごとのカード枚数</param>
        /// <returns>生成されたStageインスタンス</returns>
        public Stage SetupStage(int countPerSuit)
        {
            var stack = CreateDefaultStack(countPerSuit);
            var trash = CreateDefaultTrash();
            return new Stage(stack, trash);
        }

        /// <summary>
        /// 指定したCardPileでステージモデルを生成します
        /// </summary>
        /// <param name="stack">Stackカードパイル</param>
        /// <param name="trash">Trashカードパイル</param>
        /// <returns>生成されたStageインスタンス</returns>
        public Stage SetupStage(CardPile stack, CardPile trash)
        {
            Assert.IsNotNull(stack, "stack が null です");
            Assert.IsNotNull(trash, "trash が null です");

            return new Stage(stack, trash);
        }

        /// <summary>
        /// デフォルトのStackカードパイルを生成
        /// </summary>
        /// <returns>生成されたCardPile</returns>
        private CardPile CreateDefaultStack()
        {
            return CreateDefaultStack(_countPerSuit);
        }

        /// <summary>
        /// デフォルトのStackカードパイルを生成（指定枚数）
        /// </summary>
        /// <param name="countPerSuit">スートごとのカード枚数</param>
        /// <returns>生成されたCardPile</returns>
        private CardPile CreateDefaultStack(int countPerSuit)
        {
            var suits = new[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };
            var maxCount = suits.Length * countPerSuit;

            // 初期カードを生成
            var initialCards = _cardModelFactory.CreateCards(suits, countPerSuit);

            // スタック用カードパイルを生成
            return _pileFactory.CreatePile(PileIds.Stack, "Stack", initialCards, maxCount);
        }

        /// <summary>
        /// デフォルトのTrashカードパイルを生成
        /// </summary>
        /// <returns>生成されたCardPile</returns>
        private CardPile CreateDefaultTrash()
        {
            // トラッシュは初期カードなしで生成
            return _pileFactory.CreatePile(PileIds.Trash, "Trash", InGameConsts.DEFAULT_CARD_PILE_CAPACITY);
        }
    }
} 