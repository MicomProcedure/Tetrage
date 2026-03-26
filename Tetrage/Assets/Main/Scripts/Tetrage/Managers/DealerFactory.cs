using Tetrage.Managers;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;
using UnityEngine;
using Tetrage.Network.Gameplay;
using Tetrage.Managers.DealerStrategies;
using Tetrage.Core;

namespace Tetrage.Managers
{
    public static class DealerFactory
    {
        public static Dealer CreateDealer(GameMode gameMode, IGameContext gameContext, INetworkContext networkContext, IGameplayNetworkController netCtl)
        {

            try
            {


                var messenger = new DealerNetworkMessenger(netCtl.Broadcaster, netCtl.Sequence, netCtl.PlayerIdMapper);
                if (gameMode == GameMode.Debug)
                {
                    var dealer = new Dealer(gameContext, new RealDealerPlanner(), networkContext, netCtl.PlayerIdMapper);
                    if (networkContext.IsHost)
                    {
                        dealer.SetMessenger(messenger);
                        dealer.SetTurnGate(netCtl.TurnGate);
                    }
                    return dealer;
                }
                else if (gameMode == GameMode.Release)
                {
                    var dealer = new Dealer(gameContext, new RealDealerPlanner(), networkContext, netCtl.PlayerIdMapper);
                    if (networkContext.IsHost)
                    {
                        dealer.SetMessenger(messenger);
                        dealer.SetTurnGate(netCtl.TurnGate);
                    }
                    return dealer;
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