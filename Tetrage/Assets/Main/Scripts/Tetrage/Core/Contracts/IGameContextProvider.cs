using UnityEngine;
using Tetrage.Models;
using System.Collections.Generic;
using Tetrage.Network.Gameplay;

namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// ゲームに参加しているプレイヤー情報を読み取り専用で提供するインターフェース
    /// </summary>
    public interface IGameContextProvider
    {
        /// <summary>現在の手番プレイヤー（存在しない場合は null）</summary>
        IPlayer CurrentPlayer { get; }

        /// <summary>ユーザーが操作しているプレイヤー（存在しない場合は null）</summary>
        IPlayer UserPlayer { get; }

        /// <summary>参加している全プレイヤーを取得</summary>
        IReadOnlyList<IPlayer> Players { get; }

        /// <summary>
        /// ステージを取得
        /// </summary>
        Stage Stage { get; }

        /// <summary>適用後イベントを購読できるイベントバス</summary>
        IGameplayEventBus Events { get; }
    }
}
