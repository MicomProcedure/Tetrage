using Tetrage.Core.Contracts;
using UnityEngine;
using System.Collections.Generic; // 必要に応じて追加
using Tetrage.Models;

namespace Tetrage.Factories
{
    /// <summary>
    /// ゲームステージ生成用Factory
    /// </summary>
    public class StageFactory
    {
        private readonly ICardPileFactory _deckFactory;
        private readonly ICardPileFactory _pileFactory;

        public StageFactory(ICardPileFactory deckFactory, ICardPileFactory pileFactory)
        {
            _deckFactory = deckFactory;
            _pileFactory = pileFactory;
        }

        /// <summary>
        /// ステージ(MonoBehaviour)生成と初期配置
        /// </summary>
        /// <summary>
        /// ステージ(MonoBehaviour)生成と初期配置を行います。
        /// </summary>
        /// <returns>生成されたStageManagerインスタンス。</returns>
        public StageManager SetupStage()
        {
            // 1. StageManagerコンポーネントを持つGameObjectを生成
            GameObject stageGameObject = new GameObject("GameStageManager");
            StageManager stageManager = stageGameObject.AddComponent<StageManager>();
            Debug.Log("StageManager GameObject とコンポーネントを生成しました。");

            // 2. デッキと捨て山をFactoryで生成
            // ここで ICardFactory などを使って具体的なカードのリストを生成することもできます。
            // 例: ICardFactory cardFactory = new CardModelFactory();
            //     IEnumerable<Card> initialDeckCards = cardFactory.CreateCards(new Suit[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club }, 13);
            //     CardPile deck = _deckFactory.CreatePile("Deck", initialDeckCards, 52);

            // 今回は簡略化し、初期カードなしで空の山札を生成します。
            // 必要に応じて、上記コメントアウトした部分のように具体的なカードを生成して渡すことができます。
            CardPile deck = _deckFactory.CreatePile("Deck", 52); // CreatePile メソッドを使用
            Debug.Log($"デッキ '{deck.Name}' を生成しました。");

            // 捨て山の生成
            CardPile discardPile = _pileFactory.CreatePile("DiscardPile", int.MaxValue);
            Debug.Log($"捨て山 '{discardPile.Name}' を生成しました。");

            // 3. 生成したデッキと捨て山をStageManagerに紐付け(これによりゲーム状で山札とかを操作するときはStageMangerにアクセスすれば良くなる)
            stageManager.Initialize(deck, discardPile);

            Debug.Log("ステージのセットアップが完了しました。");

            return stageManager;
        }
    }
} 