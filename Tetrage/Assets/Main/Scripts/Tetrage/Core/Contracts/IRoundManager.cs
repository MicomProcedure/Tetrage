using System;
using Cysharp.Threading.Tasks;

namespace Tetrage.Core.Contracts
{
    public interface IRoundManager
    {
        event Action RoundStart;
        event Action RoundEnd;
        public int RoundCount { get; }
        IDealerStrategy DealerStrategy { get; }

        UniTask StartGameAsync(float timeoutSeconds = 0);
        void EndGame();
        UniTask StartRoundLoopAsync();
        UniTask StartSingleRoundAsync();
        void OnRoundStart();
        void OnRoundEnd();


    }
}