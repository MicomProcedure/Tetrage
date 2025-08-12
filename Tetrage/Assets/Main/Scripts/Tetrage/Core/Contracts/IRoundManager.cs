using System;
using Cysharp.Threading.Tasks;

namespace Tetrage.Core.Contracts
{
    public interface IRoundManager
    {
        event Action TurnStart;
        event Action TurnEnd;
        event Action RoundStart;
        event Action RoundEnd;
        event Action GameStart;
        event Action GameEnd;
        public int TurnCount { get; }
        public int RoundCount { get; }
        IDealerStrategy DealerStrategy { get; }

        UniTask StartGameAsync(float timeoutSeconds = 0);
        void EndGame();
        UniTask StartTurnLoopAsync();
        UniTask StartSingleTurnAsync();
        void OnTurnStart();
        void OnTurnEnd();

        void OnGameStart();
        void OnGameEnd();

        void OnRoundStart();
        void OnRoundEnd();

    }
}