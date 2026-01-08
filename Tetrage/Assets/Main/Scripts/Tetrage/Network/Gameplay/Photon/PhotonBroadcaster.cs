using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// Photonネットワーク経由でイベントをブロードキャストする実装
    /// </summary>
    public sealed class PhotonBroadcaster : INetworkBroadcaster
    {
        private readonly ISerializer _serializer;
        private readonly RaiseEventOptions _optionsAll = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        private readonly SendOptions _sendOptions = new SendOptions { Reliability = true };

        public PhotonBroadcaster(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public void Raise<T>(EventCode code, T payload)
        {
            var bytes = _serializer.Serialize(payload);
            PhotonNetwork.RaiseEvent((byte)code, bytes, _optionsAll, _sendOptions);
        }

        public void RaiseToActors<T>(EventCode code, T payload, int[] targetActorNumbers)
        {
            if (targetActorNumbers == null || targetActorNumbers.Length == 0) return;
            var bytes = _serializer.Serialize(payload);
            var opts = new RaiseEventOptions { TargetActors = targetActorNumbers };
            PhotonNetwork.RaiseEvent((byte)code, bytes, opts, _sendOptions);
            Debug.Log($"PhotonBroadcaster: RaiseToActors, Code: {code}, TargetActorNumbers: {string.Join(", ", targetActorNumbers)}");
        }

        public void RaiseToActor<T>(EventCode code, T payload, int targetActorNumber)
        {
            RaiseToActors(code, payload, new int[] { targetActorNumber });
        }

        // RaiseToOthersExcept は実装しない（呼び出し側でTargetActorsを算出して RaiseToActors を使用）
    }
}

