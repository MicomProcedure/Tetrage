using Tetrage.Core.Ids;
using Tetrage.Models;
using Tetrage.Core;
using Tetrage.Network.Gameplay;
using System;
using Tetrage.Core.Contracts;

namespace Tetrage.Network.Gameplay
{
    // ゲームプレイ用のネットワーク制御インターフェース（GameManagerから参照）
    public interface IGameplayNetworkController
    {
        INetworkBroadcaster Broadcaster { get; }
        IGameplayEventBus EventBus { get; }
        TurnGate TurnGate { get; }
        IGameContext GameContext { get; }
        SequenceService Sequence { get; }
        IPlayerIdMapper PlayerIdMapper { get; }
        void Start();
        void Stop();
    }
}