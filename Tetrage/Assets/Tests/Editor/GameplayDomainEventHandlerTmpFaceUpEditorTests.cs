using System.Collections.Generic;
using NUnit.Framework;
using Tetrage.Core;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Models;
using Tetrage.Network.Gameplay;
using DomainEvents = Tetrage.Core.Events;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// GameplayDomainEventHandler の Tmp 入退場時の表裏制御を検証するテストクラス
    /// </summary>
    public class GameplayDomainEventHandlerTmpFaceUpEditorTests
    {
        #region Tmp FaceUp Tests
        [Test]
        public void CardMoved_IntoTmp_SetsFaceUpTrue()
        {
            var card = CreateCard(cardValue: 21, isFaceUp: false);
            var from = new CardPile(PileIds.PlayerHands(1), "Hands-1", new[] { card });
            var tmp = new CardPile(PileIds.PlayerTmp(1), "Tmp-1");
            var bus = new R3EventBus();
            using var handler = CreateHandler(bus, card, from, tmp);

            bus.Publish(new DomainEvents.CardMovedEvent(1, card.Id, from.Id, tmp.Id));

            Assert.IsTrue(card.IsFaceUp, "Tmp に入ったカードは IsFaceUp=true になるべき");
        }

        [Test]
        public void CardMoved_OutFromTmp_SetsFaceUpFalse()
        {
            var card = CreateCard(cardValue: 22, isFaceUp: true);
            var tmp = new CardPile(PileIds.PlayerTmp(1), "Tmp-1", new[] { card });
            var to = new CardPile(PileIds.PlayerTarget(1), "Target-1");
            var bus = new R3EventBus();
            using var handler = CreateHandler(bus, card, tmp, to);

            bus.Publish(new DomainEvents.CardMovedEvent(1, card.Id, tmp.Id, to.Id));

            Assert.IsFalse(card.IsFaceUp, "Tmp から出たカードは IsFaceUp=false になるべき");
        }

        [Test]
        public void CardMoved_BetweenNonTmpPiles_DoesNotChangeFaceUp()
        {
            var card = CreateCard(cardValue: 23, isFaceUp: true);
            var from = new CardPile(PileIds.PlayerTarget(1), "Target-1", new[] { card });
            var to = new CardPile(PileIds.PlayerHands(1), "Hands-1");
            var bus = new R3EventBus();
            using var handler = CreateHandler(bus, card, from, to);

            bus.Publish(new DomainEvents.CardMovedEvent(1, card.Id, from.Id, to.Id));

            Assert.IsTrue(card.IsFaceUp, "Tmp と無関係な移動では IsFaceUp を変更しないべき");
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// テスト用カードを生成します。
        /// </summary>
        private static Card CreateCard(int cardValue, bool isFaceUp)
        {
            return new Card(new CardId(cardValue), Suit.Spade, number: 1, isFaceUp);
        }

        /// <summary>
        /// CardMovedEvent を適用するためのハンドラを生成します。
        /// </summary>
        private static GameplayDomainEventHandler CreateHandler(R3EventBus bus, Card card, params CardPile[] piles)
        {
            var cardRegistry = new IdRegistry<CardId, Card>();
            var pileRegistry = new IdRegistry<PileId, CardPile>();
            var playerRegistry = new IdRegistry<PlayerId, Player>();
            var gameContext = new GameContext(stage: null, players: new List<IPlayer>(), userPlayer: null, events: bus);

            cardRegistry.Register(card);
            foreach (var pile in piles)
            {
                pileRegistry.Register(pile);
            }

            return new GameplayDomainEventHandler(
                cardRegistry,
                pileRegistry,
                playerRegistry,
                new TurnGate(),
                gameContext,
                new PlayerIdMapper(),
                new SequenceService(),
                isHost: true);
        }
        #endregion
    }
}
