using Tetrage.Core.Enums;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// クライアント側（Guest/Host問わず）から Host へアクション確定リクエストを送るための境界。
    /// Action クラスはこれに依存し、UIの最終確定時に呼び出す。
    /// </summary>
    public interface INetworkActionContext
    {
        void Request(ActionRequestedEvent request);
        int NextClientSequence();
    }
}


