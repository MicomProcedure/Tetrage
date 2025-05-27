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
        public IPlayer CreatePlayer(string userId, Card target, CardPile hands, CardPile tmp)
        {
            IPlayer player = new Player(userId, target, hands, tmp);

            return player;
        }

    }
}