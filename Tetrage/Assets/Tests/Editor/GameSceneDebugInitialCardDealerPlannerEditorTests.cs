using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Factories;
using Tetrage.Models;
using Tetrage.Tests;
using Tetrage.Tests.Data;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// GameSceneデバッグ用の初期カードPlannerを検証する。
    /// </summary>
    public class GameSceneDebugInitialCardDealerPlannerEditorTests
    {
        #region Tests

        [Test]
        public void PlanTargetSetup_UsesConfiguredTargetAndHandCards()
        {
            var stack = CreateStack();
            var players = CreatePlayers();
            var debugInfos = new List<DebugPlayerInfo>
            {
                new()
                {
                    PlayerId = 1,
                    SetInitialCards = true,
                    TargetCard = new CardSpec { Suit = Suit.Heart, Number = 1 },
                    HandCards = new List<CardSpec>
                    {
                        new() { Suit = Suit.Spade, Number = 2 },
                        new() { Suit = Suit.Club, Number = 3 }
                    }
                },
                new()
            };
            var planner = new GameSceneDebugInitialCardDealerPlanner(debugInfos);

            var plan = planner.PlanTargetSetup(players, stack);
            var moves = plan.Moves.ToList();

            var heartOne = CardIdComposer.Compose(new DeckId(1), (int)Suit.Heart, 1);
            var spadeTwo = CardIdComposer.Compose(new DeckId(1), (int)Suit.Spade, 2);
            var clubThree = CardIdComposer.Compose(new DeckId(1), (int)Suit.Club, 3);

            Assert.IsTrue(moves.Any(move => move.CardId.Equals(heartOne) && move.ToPileId.Equals(players[0].Target.Id)));
            Assert.IsTrue(moves.Any(move => move.CardId.Equals(spadeTwo) && move.ToPileId.Equals(players[0].Hands.Id)));
            Assert.IsTrue(moves.Any(move => move.CardId.Equals(clubThree) && move.ToPileId.Equals(players[0].Hands.Id)));
            Assert.AreEqual(moves.Count, moves.Select(move => move.CardId).Distinct().Count());
        }

        #endregion

        #region Helpers

        private static CardPile CreateStack()
        {
            var factory = new CardModelFactory();
            var cards = factory.CreateCards(new[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club }, 13);
            return new CardPile(PileIds.Stack, "Stack", cards);
        }

        private static List<Player> CreatePlayers()
        {
            return new List<Player>
            {
                CreatePlayer(1),
                CreatePlayer(2)
            };
        }

        private static Player CreatePlayer(int playerNumber)
        {
            return new Player(
                new PlayerId(playerNumber),
                $"p{playerNumber}",
                0,
                new CardPile(PileIds.PlayerTarget(playerNumber), $"Target-{playerNumber}"),
                new CardPile(PileIds.PlayerHands(playerNumber), $"Hands-{playerNumber}"),
                new CardPile(PileIds.PlayerTmp(playerNumber), $"Tmp-{playerNumber}"));
        }

        #endregion
    }
}
