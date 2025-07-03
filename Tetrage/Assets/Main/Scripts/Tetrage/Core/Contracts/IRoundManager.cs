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

        void StartGame();
        void EndGame();
        void StartRoundLoop();
        UniTaskVoid StartRound();
        void NextRound();
        void OnRoundStart();
        void OnRoundEnd();


    }
}