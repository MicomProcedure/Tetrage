namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// 型 T のイベントを発行するための汎用インターフェース。
    /// </summary>
    public interface IEventEmitter<T>
    {
        void Emit(T payload);
    }
}


