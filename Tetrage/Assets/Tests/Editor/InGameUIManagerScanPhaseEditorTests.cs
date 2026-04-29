using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Tetrage.Core;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Managers;
using Tetrage.Models;
using Tetrage.Network.Gameplay;
using Tetrage.UI;

namespace Tetrage.Tests.Editor
{
    /// <summary>
    /// ScanPhaseの相手ターゲット確認フローを検証する。
    /// </summary>
    public class InGameUIManagerScanPhaseEditorTests
    {
        #region Tests

        /// <summary>
        /// 相手ターゲットは選択時にスートを表示せず、送信後はNextを非表示にする。
        /// </summary>
        [Test]
        public void ScanOpponentTarget_RevealsSuitOnlyAfterNextThenHidesNextButton()
        {
            var managerObject = new GameObject("InGameUIManager");
            var navigationObject = new GameObject("InGameNavigation");
            var textObject = new GameObject("NavigationText");
            var scanObject = new GameObject("ScanPhaseUI");
            var nextButtonObject = new GameObject("NextButton");

            try
            {
                var manager = managerObject.AddComponent<InGameUIManager>();
                var navigation = navigationObject.AddComponent<InGameNavigation>();
                var navigationText = textObject.AddComponent<TextMeshProUGUI>();
                var scanUI = scanObject.AddComponent<ScanPhaseUI>();
                var nextButton = nextButtonObject.AddComponent<Button>();
                var broadcaster = new BroadcasterSpy();
                var network = new GameplayNetworkControllerStub(broadcaster);

                network.PlayerIdMapper.Register(new PlayerId(1), 11);
                network.PlayerIdMapper.Register(new PlayerId(2), 22);
                network.PlayerIdMapper.Register(new PlayerId(3), 33);

                SetPrivateField(navigation, "_navigationText", navigationText);
                SetPrivateField(scanUI, "_nextButton", nextButton);
                SetPrivateField(manager, "_inGameNavigation", navigation);
                SetPrivateField(manager, "_ScanUIController", scanUI);
                SetPrivateField(manager, "_gameContext", CreateGameContext());
                SetPrivateField(manager, "_gameplayNetwork", network);
                SetPrivateField(manager, "_scanOwnTargetConfirmed", true);

                InvokePrivate(manager, "OnOpponentTargetSelected", new PlayerId(2), Suit.Heart);

                Assert.AreEqual("2Pのターゲットのスートを確認しますか？", navigation.GetNavigationText());
                StringAssert.DoesNotContain(Suit.Heart.GetKatakanaName(), navigation.GetNavigationText());

                InvokePrivate(manager, "OnOpponentTargetSelected", new PlayerId(3), Suit.Club);

                Assert.AreEqual("3Pのターゲットのスートを確認しますか？", navigation.GetNavigationText());

                InvokePrivate(manager, "OnScanPhaseNextClicked");

                Assert.AreEqual("3Pのターゲットカードのスートはクラブです。", navigation.GetNavigationText());
                Assert.IsNull(broadcaster.LastCode);
                Assert.IsTrue(nextButtonObject.activeSelf);

                InvokePrivate(manager, "OnOpponentTargetSelected", new PlayerId(2), Suit.Heart);

                Assert.AreEqual("3Pのターゲットカードのスートはクラブです。", navigation.GetNavigationText());

                InvokePrivate(manager, "OnScanPhaseNextClicked");

                Assert.AreEqual(EventCode.ScanTargetSelected, broadcaster.LastCode);
                Assert.AreEqual("他のプレイヤーを待っています", navigation.GetNavigationText());
                Assert.IsFalse(nextButtonObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(navigationObject);
                Object.DestroyImmediate(textObject);
                Object.DestroyImmediate(scanObject);
                Object.DestroyImmediate(nextButtonObject);
            }
        }

        #endregion

        #region Helpers

        private static IGameContext CreateGameContext()
        {
            var self = CreatePlayer(1, Suit.Spade);
            var opponent = CreatePlayer(2, Suit.Heart);
            return new GameContext(
                stage: null,
                players: new List<IPlayer> { self, opponent },
                userPlayer: self,
                events: null);
        }

        private static IPlayer CreatePlayer(int playerNumber, Suit targetSuit)
        {
            var targetCard = new Card(new CardId(playerNumber), targetSuit, 1, isVisible: false);
            var targetPile = new CardPile(
                PileIds.PlayerTarget(playerNumber),
                $"Target-{playerNumber}",
                new[] { targetCard });

            return new Player(
                new PlayerId(playerNumber),
                $"p{playerNumber}",
                0,
                targetPile,
                new CardPile(PileIds.PlayerHands(playerNumber), $"Hands-{playerNumber}"),
                new CardPile(PileIds.PlayerTmp(playerNumber), $"Tmp-{playerNumber}"));
        }

        private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            var field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{fieldName} field should exist.");
            field.SetValue(target, value);
        }

        private static void InvokePrivate(InGameUIManager manager, string methodName, params object[] args)
        {
            var method = typeof(InGameUIManager).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"{methodName} method should exist.");
            method.Invoke(manager, args);
        }

        private sealed class BroadcasterSpy : INetworkBroadcaster
        {
            public EventCode? LastCode { get; private set; }

            public void Raise<T>(EventCode code, T payload)
            {
                LastCode = code;
            }

            public void RaiseToActors<T>(EventCode code, T payload, int[] targetActorNumbers)
            {
            }

            public void RaiseToActor<T>(EventCode code, T payload, int targetActorNumber)
            {
            }
        }

        private sealed class GameplayNetworkControllerStub : IGameplayNetworkController
        {
            public GameplayNetworkControllerStub(INetworkBroadcaster broadcaster)
            {
                Broadcaster = broadcaster;
                Sequence = new SequenceService();
                PlayerIdMapper = new PlayerIdMapper();
            }

            public INetworkBroadcaster Broadcaster { get; }
            public IGameplayEventBus EventBus => null;
            public TurnGate TurnGate => null;
            public IGameContext GameContext => null;
            public SequenceService Sequence { get; }
            public IPlayerIdMapper PlayerIdMapper { get; }
            public int LastAppliedNetworkSequence => 0;

            public void Start()
            {
            }

            public void Stop()
            {
            }
        }

        #endregion
    }
}
