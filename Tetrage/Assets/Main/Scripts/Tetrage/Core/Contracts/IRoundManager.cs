using System;

namespace Tetrage.Core.Contracts
{
    public interface IRoundManager
    {
        event Action RoundStart;
        event Action RoundEnd;

        void OnRoundStart();
        void OnRoundEnd();


    }
}