using UnityEngine;
using Tetrage.Models;
using System.Collections.Generic;

namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// ゲームに参加しているプレイヤー情報を読み取り専用で提供するインターフェース
    /// </summary>
    public interface IGameContextProvider
    {
        /// <summary>現在の手番プレイヤー（存在しない場合は null）</summary>
        Player CurrentPlayer { get; }

        /// <summary>参加している全プレイヤーを取得</summary>
        IReadOnlyList<Player> Players { get; }

        /// <summary>
        /// ステージを取得
        /// </summary>
        Stage Stage { get; }
    }
}
