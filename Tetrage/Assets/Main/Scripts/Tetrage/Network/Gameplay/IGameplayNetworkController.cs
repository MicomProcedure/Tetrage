using Tetrage.Core.Ids;
using Tetrage.Models;
using Tetrage.Core;
using Tetrage.Network.Gameplay;
using System;

namespace Tetrage.Network.Gameplay
{
    // ゲームプレイ用のネットワーク制御インターフェース（GameManagerから参照）
    public interface IGameplayNetworkController
    {
        INetworkBroadcaster Broadcaster { get; }
        IGameplayEventBus EventBus { get; }
        TurnGate TurnGate { get; }
        SequenceService Sequence { get; }
        void AttachGameContext(Tetrage.Core.GameContext ctx);
        void Start();
        void Stop();
    }
}