using System.Collections.Generic;
using Tetrage.Core.Contracts;
using Tetrage.Models;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクション実行時のコンテキストを提供するインターフェース
    /// </summary>
    public interface IActionContext
    {
        /// <summary>
        /// アクションを実行するプレイヤー
        /// </summary>
        IPlayer RequesterPlayer { get; }

        /// <summary>
        /// 現在のステージ情報
        /// </summary>
        Stage CurrentStage { get; }

        /// <summary>
        /// 参加している全プレイヤー
        /// </summary>
        IReadOnlyList<IPlayer> AllPlayers { get; }

        /// <summary>
        /// 他のプレイヤー（リクエスター以外）
        /// </summary>
        IReadOnlyList<IPlayer> OtherPlayers { get; }

        /// <summary>
        /// 現在のターンプレイヤー
        /// </summary>
        IPlayer CurrentTurnPlayer { get; }

        /// <summary>
        /// ゲームコンテキストプロバイダー
        /// </summary>
        IGameContextProvider GameContext { get; }

        /// <summary>
        /// アクション待機・キャンセル制御を担当するActionAwaiter
        /// ExecutorがCancellationTokenにアクセスするために使用
        /// </summary>
        ActionAwaiter ActionAwaiter { get; }

        /// <summary>
        /// ネットワーク送信コンテキスト
        /// </summary>
        Tetrage.Network.Gameplay.INetworkActionContext Network { get; }
    }
}