using UnityEngine; // MonoBehaviourを扱うため追加
using System.Collections.Generic;
using Tetrage.Models;
using Tetrage.Core.Contracts;
using Tetrage.Factories;

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
        public IPlayer CreatePlayer(string userId, Card target)
        {
            var CardPileBuilder = new CardPileBuilder(new CardPileFactory());
            var hands = CardPileBuilder.WithName("Hand").WithMaxCount(3).Build();
            var tmp = CardPileBuilder.WithName("Tmp").WithMaxCount(2).Build();

            IPlayer player = new Player(userId, target, hands, tmp);

            return player;
        }

    }
}