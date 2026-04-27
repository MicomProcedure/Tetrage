using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tetrage.Network.Gameplay;
using Tetrage.Core.DTO;

namespace Tetrage.Core.Contracts
{
    public interface IRoundManager
    {
        public int TurnCount { get; }
        public int RoundCount { get; }
        IDealerPlanner DealerPlanner { get; set; }
        DealerNetworkMessenger Messenger { get; }
        UniTask StartGameAsync(float timeoutSeconds = 0, CancellationToken gameCts = default);
        UniTask StartTurnLoopAsync();
        UniTask StartSingleTurnAsync();


    }
}