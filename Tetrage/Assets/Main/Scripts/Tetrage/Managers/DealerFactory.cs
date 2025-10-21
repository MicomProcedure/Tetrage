using Tetrage.Managers;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using UnityEngine;
using Tetrage.Network.Gameplay;
using Tetrage.Managers.DealerStrategies;

namespace Tetrage.Managers
{
    public static class DealerFactory
    {
        public static Dealer CreateDealer(FieldSetupManager fieldSetupManager, GameMode gameMode, INetworkBroadcaster broadcaster = null)
        {

            try
            {


                var emitter = broadcaster != null ? new DealerPlanEmitter(broadcaster) : null;
                if (gameMode == GameMode.Debug)
                {
                    return new Dealer(fieldSetupManager.Stage, fieldSetupManager.Players, new RealDealerPlanner(), emitter);
                }
                else if (gameMode == GameMode.Release)
                {
                    return new Dealer(fieldSetupManager.Stage, fieldSetupManager.Players, new RealDealerPlanner(), emitter);
                }
                else
                {
                    throw new System.InvalidOperationException("GameModeがDebugまたはReleaseではありません");
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