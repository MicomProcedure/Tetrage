using System;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ネットワーク経由でイベントを受信するインターフェース
    /// </summary>
    public interface INetworkReceiver
    {
        /// <summary>
        /// 指定したイベントコードに対するハンドラを登録する
        /// </summary>
        void On<T>(EventCode code, Action<T> handler);

        /// <summary>
        /// イベント受信を開始する
        /// </summary>
        void Start();

        /// <summary>
        /// イベント受信を停止する
        /// </summary>
        void Stop();
    }
}

