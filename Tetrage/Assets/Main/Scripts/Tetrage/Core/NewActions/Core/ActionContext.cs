using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Contracts;
using Tetrage.Models;
using Tetrage.Network.Gameplay;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクション実行時のコンテキスト実装
    /// </summary>
    public class ActionContext : IActionContext
    {
        public IPlayer RequesterPlayer { get; }
        public Stage CurrentStage { get; }
        public IReadOnlyList<IPlayer> AllPlayers { get; }
        public IReadOnlyList<IPlayer> OtherPlayers { get; }
        public IPlayer CurrentTurnPlayer { get; }
        public IGameContextProvider GameContext { get; }
        public ActionAwaiter ActionAwaiter { get; }
        public INetworkActionContext Network { get; }

        public ActionContext(
            IPlayer requesterPlayer,
            IGameContextProvider gameContext,
            ActionAwaiter actionAwaiter = null,
            INetworkActionContext network = null)
        {
            RequesterPlayer = requesterPlayer;
            GameContext = gameContext;
            ActionAwaiter = actionAwaiter;
            Network = network;
            CurrentStage = gameContext.Stage;
            AllPlayers = gameContext.Players;
            CurrentTurnPlayer = gameContext.CurrentPlayer;

            // リクエスト者以外のプレイヤーを取得
            OtherPlayers = AllPlayers
                .Where(p => !ReferenceEquals(p, requesterPlayer))
                .ToList();
        }
    }
}