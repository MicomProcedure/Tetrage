using UnityEngine;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// MPPM別プロセス間でイベントログへ送信するVirtualBroadcaster。
    /// </summary>
    public sealed class MppmSharedVirtualBroadcaster : INetworkBroadcaster
    {
        #region Fields

        private readonly ISerializer _serializer;
        private readonly MppmSharedVirtualTransportStore _store;
        private readonly int _senderActorNumber;

        #endregion

        #region Constructor

        /// <summary>
        /// ストアと送信元ActorNumberを指定して生成する。
        /// </summary>
        public MppmSharedVirtualBroadcaster(
            ISerializer serializer,
            MppmSharedVirtualTransportStore store,
            int senderActorNumber)
        {
            _serializer = serializer;
            _store = store;
            _senderActorNumber = senderActorNumber;
        }

        #endregion

        #region INetworkBroadcaster

        public void Raise<T>(EventCode code, T payload)
        {
            var bytes = _serializer.Serialize(payload);
            _store.Append(_senderActorNumber, code, bytes, null);
            Debug.Log($"MppmSharedVirtualBroadcaster: Raise (Code: {code}, Actor: {_senderActorNumber})");
        }

        public void RaiseToActors<T>(EventCode code, T payload, int[] targetActorNumbers)
        {
            if (targetActorNumbers == null || targetActorNumbers.Length == 0)
            {
                return;
            }

            var bytes = _serializer.Serialize(payload);
            _store.Append(_senderActorNumber, code, bytes, targetActorNumbers);
            Debug.Log($"MppmSharedVirtualBroadcaster: RaiseToActors (Code: {code}, Actor: {_senderActorNumber}, Targets: {string.Join(", ", targetActorNumbers)})");
        }

        public void RaiseToActor<T>(EventCode code, T payload, int targetActorNumber)
        {
            RaiseToActors(code, payload, new[] { targetActorNumber });
        }

        #endregion
    }
}
