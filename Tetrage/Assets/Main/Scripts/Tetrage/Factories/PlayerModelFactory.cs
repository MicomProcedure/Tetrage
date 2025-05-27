using UnityEngine; // MonoBehaviourを扱うため追加
using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.Core.Contracts;

namespace Tetrage.Factories
{
    /// <summary>
    /// プレイヤー生成用Factory
    /// </summary>
    public class PlayerModelFactory : IPlayerFactory
    {
        /// <summary>
        /// 指定した人数のプレイヤーモデルを生成します。
        /// </summary>
        /// <param name="playerCount">生成するプレイヤーの人数。</param>
        /// <returns>生成されたIPlayerインターフェースのリスト。</returns>
        /// 
        public IPlayer CreatePlayer()
        {
            var player = new Player();

            return player;
        }
        public List<IPlayer> CreatePlayers(int playerCount)
        {
            var players = new List<IPlayer>();

            for (int i = 0; i < playerCount; i++)
            {
                players.Add(CreatePlayer());
            }

            return players;
        }
    }
}