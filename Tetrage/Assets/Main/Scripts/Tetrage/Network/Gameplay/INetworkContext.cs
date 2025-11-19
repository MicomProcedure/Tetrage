using System.Collections.Generic;

namespace Tetrage.Network.Gameplay
{

    /// <summary>
    /// ネットワーク状態の抽象化インターフェース。
    /// Photon固有の情報を抽象化し、VirtualTransportでも使用可能にする。
    /// </summary>
    public interface INetworkContext
    {
        /// <summary>ホスト判定</summary>
        bool IsHost { get; }

        /// <summary>自ActorNumber</summary>
        int LocalActorNumber { get; }

        /// <summary>接続状態</summary>
        bool IsReady { get; }

        /// <summary>ルーム参加状態</summary>
        bool IsInRoom { get; }

        /// <summary>ルーム内のプレイヤー数</summary>
        int PlayerCount { get; }

        /// <summary>
        /// ルーム内のActorNumber一覧を取得
        /// ゲーム開始前の検証やマッピング用途に使用
        /// </summary>
        IReadOnlyList<int> GetActorNumbers();
    }

}