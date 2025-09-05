using Tetrage.Managers;
using Tetrage.Managers.DealerStrategies;
using Tetrage.Core.Enums;
using UnityEngine;

namespace Tetrage.Managers
{
    public static class DealerFactory
    {
        public static Dealer CreateDealer(FieldSetupManager fieldSetupManager, GameMode gameMode)
        {
            
            try
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
            catch (System.InvalidOperationException ex)
            {
                Debug.LogError($"DealerFactory: Dealerの生成に失敗しました: {ex.Message}");
                throw;
            }
        }
    }
}