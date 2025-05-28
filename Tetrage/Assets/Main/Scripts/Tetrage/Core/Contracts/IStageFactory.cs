using Tetrage.Models;

namespace Tetrage.Core.Contracts
{
    public interface IStageFactory
    {
        public Stage SetupStage();
    }
}