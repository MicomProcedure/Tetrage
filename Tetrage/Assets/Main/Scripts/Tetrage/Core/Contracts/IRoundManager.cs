using System;

namespace Tetrage.Core.Contracts
{
    public interface IRoundManager
    {
        event Action RoundStart;
        event Action RoundEnd;
        IDealerStrategy DealerStrategy { get; }

        void StartGame();
        void EndGame();
        void NextTurn();
        void OnRoundStart();
        void OnRoundEnd();


    }
}