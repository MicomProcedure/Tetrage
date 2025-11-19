using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// Photon実装のNetworkContext。
    /// PhotonNetworkの状態をINetworkContextとして提供する。
    /// </summary>
    public sealed class PhotonNetworkContext : INetworkContext
    {
        /// <summary>ホスト判定</summary>
        public bool IsHost => PhotonNetwork.IsConnectedAndReady && PhotonNetwork.IsMasterClient;

        /// <summary>自ActorNumber</summary>
        public int LocalActorNumber => PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;

        /// <summary>接続状態</summary>
        public bool IsReady => PhotonNetwork.IsConnectedAndReady;

        /// <summary>ルーム参加状態</summary>
        public bool IsInRoom => PhotonNetwork.InRoom;

        /// <summary>ルーム内のプレイヤー数</summary>
        public int PlayerCount => PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;

        /// <summary>
        /// ルーム内のActorNumber一覧を取得
        /// </summary>
        public IReadOnlyList<int> GetActorNumbers()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            {
                return System.Array.Empty<int>();
            }

            return PhotonNetwork.PlayerList
                .Select(p => p.ActorNumber)
                .ToList();
        }
    }
}

