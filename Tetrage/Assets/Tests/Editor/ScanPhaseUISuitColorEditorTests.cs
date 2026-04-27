using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Tetrage.Core;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Models;
using Tetrage.UI;

namespace Tetrage.Tests.Editor
{
    public class ScanPhaseUISuitColorEditorTests
    {
        #region Tests

        [TestCase(Suit.Spade, "#000000FF")]
        [TestCase(Suit.Club, "#000000FF")]
        [TestCase(Suit.Heart, "#FF0000FF")]
        [TestCase(Suit.Diamond, "#FF0000FF")]
        public void OnNextButtonClicked_UsesOwnTargetSuitColor(Suit suit, string expectedColor)
        {
            var root = new GameObject("ScanPhaseUI");
            var textObject = new GameObject("NaviText");

            try
            {
                var ui = root.AddComponent<ScanPhaseUI>();
                var naviText = textObject.AddComponent<TextMeshProUGUI>();
                SetPrivateField(ui, "_naviText", naviText);
                SetPrivateField(ui, "_gameContext", CreateGameContext(suit));

                ui.OnNextButtonClicked();

                StringAssert.Contains($"<color={expectedColor}>", naviText.text);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(textObject);
            }
        }

        #endregion

        #region Helpers

        private static IGameContext CreateGameContext(Suit targetSuit)
        {
            var player = CreatePlayer(targetSuit);
            return new GameContext(
                stage: null,
                players: new List<IPlayer> { player },
                userPlayer: player,
                events: null);
        }

        private static IPlayer CreatePlayer(Suit targetSuit)
        {
            var targetCard = new Card(new CardId(1), targetSuit, 1, isVisible: false);
            var targetPile = new CardPile(
                PileIds.PlayerTarget(1),
                "Target-1",
                new[] { targetCard });

            return new Player(
                new PlayerId(1),
                "p1",
                0,
                targetPile,
                new CardPile(PileIds.PlayerHands(1), "Hands-1"),
                new CardPile(PileIds.PlayerTmp(1), "Tmp-1"));
        }

        private static void SetPrivateField<T>(ScanPhaseUI ui, string fieldName, T value)
        {
            var field = typeof(ScanPhaseUI).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{fieldName} field should exist.");
            field.SetValue(ui, value);
        }

        #endregion
    }
}
