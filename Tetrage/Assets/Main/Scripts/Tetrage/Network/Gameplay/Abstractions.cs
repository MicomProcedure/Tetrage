using System;
using Tetrage.Core.Ids;
using Tetrage.Models;

namespace Tetrage.Network.Gameplay
{
    public interface ISerializer
    {
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] data);
    }

    public interface INetworkBroadcaster
    {
        void Raise<T>(EventCode code, T payload);
    }

    public interface INetworkReceiver
    {
        void On<T>(EventCode code, Action<T> handler);
        void Start();
        void Stop();
    }

    // ゲームプレイ用のネットワーク制御インターフェース（GameManagerから参照）
    public interface IGameplayNetworkController
    {
        INetworkBroadcaster Broadcaster { get; }
        IGameplayEventBus EventBus { get; }
        TurnGate TurnGate { get; }
        void AttachGameContext(Tetrage.Core.GameContext ctx);
        void Initialize(
            bool isHost,
            IdRegistry<PileId, CardPile> pileRegistry,
            IdRegistry<CardId, Card> cardRegistry,
            IdRegistry<PlayerId, Player> playerRegistry,
            Action<ActionRequestedEvent> onActionRequestedHost,
            Action<GameStartedEvent> onGameStartedOptional = null
        );
        void Start();
        void Stop();
    }
}


