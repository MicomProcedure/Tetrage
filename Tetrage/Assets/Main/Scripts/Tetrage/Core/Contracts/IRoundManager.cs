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
        IDealerPlanner DealerPlanner { get; }
        IEventEmitter<DealerPlan> DealerPlanEmitter { get; }
        UniTask StartGameAsync(float timeoutSeconds = 0, CancellationToken gameCts = default);
        void EndGame();
        UniTask StartTurnLoopAsync();
        UniTask StartSingleTurnAsync();

    }
}