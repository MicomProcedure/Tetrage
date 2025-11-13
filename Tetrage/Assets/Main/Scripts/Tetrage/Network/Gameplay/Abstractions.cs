using System;
using Tetrage.Core.Ids;
using Tetrage.Models;

namespace Tetrage.Network.Gameplay
{
    public interface ISerializer
    {
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] data);
    }

    public interface INetworkBroadcaster
    {
        void Raise<T>(EventCode code, T payload);
        /// <summary>
        /// 指定したActorNumberのクライアントにのみ送信します。
        /// </summary>
        void RaiseToActors<T>(EventCode code, T payload, int[] targetActorNumbers);
        /// <summary>
        /// 指定したActorNumberの単一クライアントにのみ送信します。
        /// </summary>
        void RaiseToActor<T>(EventCode code, T payload, int targetActorNumber);
    }

    public interface INetworkReceiver
    {
        void On<T>(EventCode code, Action<T> handler);
        void Start();
        void Stop();
    }



}


