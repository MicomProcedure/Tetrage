using Tetrage.Managers;
using Tetrage.Managers.DealerStrategies;
using Tetrage.Core.Enums;

namespace Tetrage.Managers
{
    public static class DealerFactory
    {
        public static Dealer CreateDealer(FieldSetupManager fieldSetupManager, GameMode gameMode)
        {
            if (gameMode == GameMode.Debug)
            {
                return new Dealer(fieldSetupManager.Stage, fieldSetupManager.Players, new FixedPlayerStrategy());
            }
            else
            {
                throw new System.InvalidOperationException("GameModeがDebugではありません");
            }
        }
    }
}