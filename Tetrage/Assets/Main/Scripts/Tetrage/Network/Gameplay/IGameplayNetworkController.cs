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
        /// <summary>
        /// NetworkEventApplier が最後に受理した sequence。デバッグ用ネットワーク送信の sequence 補正に利用。
        /// </summary>
        int LastAppliedNetworkSequence { get; }
        void Start();
        void Stop();
    }
}