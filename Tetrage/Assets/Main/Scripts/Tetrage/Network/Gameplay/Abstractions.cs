using System;

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
    }

    public interface INetworkReceiver
    {
        void On<T>(EventCode code, Action<T> handler);
        void Start();
        void Stop();
    }
}


