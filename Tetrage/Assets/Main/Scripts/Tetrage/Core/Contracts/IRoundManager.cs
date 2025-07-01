using System;

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
        void NextTurn();
        void OnRoundStart();
        void OnRoundEnd();


    }
}