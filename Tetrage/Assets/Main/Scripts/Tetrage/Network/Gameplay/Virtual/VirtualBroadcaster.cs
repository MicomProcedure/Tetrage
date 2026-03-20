using UnityEngine;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// 同一プロセス内でネットワークイベントをブロードキャストする仮想Broadcaster。
    /// VirtualTransportHubを経由してイベントを配信する。
    /// </summary>
    public sealed class VirtualBroadcaster : INetworkBroadcaster
    {
        private readonly ISerializer _serializer;
        private readonly VirtualTransportHub _hub;

        public VirtualBroadcaster(ISerializer serializer, VirtualTransportHub hub)
        {
            _serializer = serializer;
            _hub = hub;
        }

        /// <summary>
        /// 全クライアントにイベントをブロードキャストする
        /// </summary>
        /// <typeparam name="T">DTO型</typeparam>
        /// <param name="code">イベントコード</param>
        /// <param name="payload">ペイロード</param>
        public void Raise<T>(EventCode code, T payload)
        {
            var bytes = _serializer.Serialize(payload);
            _hub.BroadcastToAll((byte)code, bytes);
            Debug.Log($"VirtualBroadcaster: Raise (Code: {code}, Payload: {typeof(T).Name})");
        }

        /// <summary>
        /// 指定したActorNumberのクライアントにイベントを送信する
        /// </summary>
        /// <typeparam name="T">DTO型</typeparam>
        /// <param name="code">イベントコード</param>
        /// <param name="payload">ペイロード</param>
        /// <param name="targetActorNumbers">送信先ActorNumber配列</param>
        public void RaiseToActors<T>(EventCode code, T payload, int[] targetActorNumbers)
        {
            if (targetActorNumbers == null || targetActorNumbers.Length == 0)
            {
                return;
            }

            var bytes = _serializer.Serialize(payload);
            _hub.BroadcastToActors((byte)code, bytes, targetActorNumbers);
            Debug.Log($"VirtualBroadcaster: RaiseToActors (Code: {code}, Targets: {string.Join(", ", targetActorNumbers)})");
        }

        /// <summary>
        /// 指定したActorNumberの単一クライアントにイベントを送信する
        /// </summary>
        /// <typeparam name="T">DTO型</typeparam>
        /// <param name="code">イベントコード</param>
        /// <param name="payload">ペイロード</param>
        /// <param name="targetActorNumber">送信先ActorNumber</param>
        public void RaiseToActor<T>(EventCode code, T payload, int targetActorNumber)
        {
            RaiseToActors(code, payload, new int[] { targetActorNumber });
        }
    }
}

