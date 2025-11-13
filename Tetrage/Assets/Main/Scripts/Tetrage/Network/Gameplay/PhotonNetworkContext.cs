using Photon.Pun;
using Tetrage.Network.Contracts;

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
    }
}

