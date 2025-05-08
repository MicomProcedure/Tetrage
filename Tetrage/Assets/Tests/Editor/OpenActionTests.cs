using NUnit.Framework;
using Tetrage.Core.Contracts;
using Tetrage.Actions;
using Tetrage.Models;
using UnityEngine;

namespace Tetrage.Tests.Editor
{
    public class OpenAction_FakeProvider_Tests
    {
        [SetUp]
        public void SetUp()
        {

        }

        [Test]
        public void Validate_ReturnsTrue_WhenOpponentHasFaceDown()
        {
            var _goA = new GameObject("Requester");
            var _goB = new GameObject("Opponent");

            var _card = new GameObject("Club.A");

            // Arrange
            var requester = _goA.AddComponent<Player>();
            var opponent = _goB.AddComponent<Player>();
            opponent.Hands.Add(_card);

            var fakeProv = new FakePlayerProvider
            {
                CurrentPlayer = requester,
                Players = new[] { requester, opponent }
            };

            var action = new OpenAction(requester, fakeProv);

            // Act & Assert
            Assert.IsTrue(action.Validate());
        }

        [Test]
        public void Execute_FlipsFirstFaceDownCard()
        {
            var requester = new Player("A");
            var opponent = new Player("B");
            var faceDown = new Card { isVisible = false };
            opponent.Hands.Add(faceDown);

            var fakeProv = new FakePlayerProvider
            {
                CurrentPlayer = requester,
                Players = new[] { requester, opponent }
            };
            var action = new OpenAction(requester, fakeProv);

            // Act
            action.Execute();

            // Assert
            Assert.IsTrue(faceDown.isVisible);
        }
    }
}
