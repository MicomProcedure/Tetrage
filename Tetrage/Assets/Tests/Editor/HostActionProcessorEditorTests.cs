using System.Collections.Generic;
using NUnit.Framework;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Models;
using Tetrage.Network.Gameplay;

namespace Tetrage.Tests.Editor
{
    public class HostActionProcessorEditorTests
    {
        private sealed class BroadcasterSpy : INetworkBroadcaster
        {
            public List<(EventCode code, object payload, int[] actors)> RaiseToActorsCalls { get; } = new();
            public List<(EventCode code, object payload)> RaiseCalls { get; } = new();

            public void Raise<T>(EventCode code, T payload)
            {
                RaiseCalls.Add((code, payload));
            }

            public void RaiseToActors<T>(EventCode code, T payload, int[] targetActorNumbers)
            {
                RaiseToActorsCalls.Add((code, payload, targetActorNumbers));
            }

            public void RaiseToActor<T>(EventCode code, T payload, int targetActorNumber)
            {
            }
        }

        private sealed class FakeGameContext : IGameContext
        {
            public IPlayer CurrentPlayer => null;
            public IPlayer UserPlayer => null;
            public IReadOnlyList<IPlayer> Players { get; }
            public Stage Stage => null;
            public IGameplayEventBus Events => null;

            public FakeGameContext(IReadOnlyList<IPlayer> players)
            {
                Players = players;
            }

            public int GetTurnOrderNumber(PlayerId playerId)
            {
                if (Players == null)
                {
                    return 0;
                }

                for (int i = 0; i < Players.Count; i++)
                {
                    var player = Players[i];
                    if (player != null && player.Id == playerId)
                    {
                        return i + 1;
                    }
                }

                return 0;
            }

            public int GetTurnOrderNumber(IPlayer player)
            {
                if (Players == null || player == null)
                {
                    return 0;
                }

                for (int i = 0; i < Players.Count; i++)
                {
                    if (ReferenceEquals(Players[i], player))
                    {
                        return i + 1;
                    }
                }

                return 0;
            }
        }

        private sealed class FakeController : IGameplayNetworkController
        {
            public INetworkBroadcaster Broadcaster { get; }
            public IGameplayEventBus EventBus => null;
            public TurnGate TurnGate => null;
            public IGameContext GameContext { get; }
            public SequenceService Sequence { get; } = new();
            public IPlayerIdMapper PlayerIdMapper { get; }
            public int LastAppliedNetworkSequence => 0;

            public FakeController(INetworkBroadcaster broadcaster, IGameContext gameContext, IPlayerIdMapper mapper)
            {
                Broadcaster = broadcaster;
                GameContext = gameContext;
                PlayerIdMapper = mapper;
            }

            public void Start()
            {
            }

            public void Stop()
            {
            }
        }

        [Test]
        public void ProcessDraw_UsesPlayerIdForPileIdsWhenActorNumberDiffers()
        {
            var mapper = new PlayerIdMapper();
            mapper.Register(new PlayerId(1), 10);
            mapper.Register(new PlayerId(2), 20);

            var actorTarget = new CardPile(PileIds.PlayerTarget(1), "Target-1");
            var otherTarget = new CardPile(PileIds.PlayerTarget(2), "Target-2");
            var actor = new Player(new PlayerId(1), "p1", 0, actorTarget, new CardPile(PileIds.PlayerHands(1), "Hands-1"), new CardPile(PileIds.PlayerTmp(1), "Tmp-1"));
            var other = new Player(new PlayerId(2), "p2", 0, otherTarget, new CardPile(PileIds.PlayerHands(2), "Hands-2"), new CardPile(PileIds.PlayerTmp(2), "Tmp-2"));
            var context = new FakeGameContext(new List<IPlayer> { actor, other });
            var broadcaster = new BroadcasterSpy();
            var controller = new FakeController(broadcaster, context, mapper);
            var processor = new DefaultHostActionProcessor(controller, mapper);

            var request = new ActionRequestedEventPacket
            {
                actorPlayerId = 10,
                actionType = ActionType.Draw,
                targetIds = new[] { 101, 102, 103 },
                clientSequence = 1
            };

            processor.Process(request);

            Assert.AreEqual(2, broadcaster.RaiseToActorsCalls.Count);
            var firstMove = (CardMovedEventPacket)broadcaster.RaiseToActorsCalls[0].payload;
            var secondMove = (CardMovedEventPacket)broadcaster.RaiseToActorsCalls[1].payload;
            Assert.AreEqual(PileIds.PlayerHands(1).Value, firstMove.toPileId);
            Assert.AreEqual(PileIds.PlayerHands(1).Value, secondMove.fromPileId);
        }
    }
}
