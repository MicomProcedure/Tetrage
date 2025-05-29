using Tetrage.Models;

namespace Tetrage.Core.Contracts
{
    public interface IStageFactory
    {
        public Stage SetupStage();
        public Stage SetupStage(int countPerSuit);
        public Stage SetupStage(CardPile stack, CardPile trash);
    }
}