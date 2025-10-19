using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// 簡易シリアライザ（JsonUtilityの代替として byte[] をそのまま運ぶ用途）
    /// ここではシリアライズ層は最小化し、PUNの RaiseEvent に object を渡す。
    /// </summary>
    public sealed class PhotonJsonSerializer : ISerializer
    {
        public byte[] Serialize<T>(T obj)
        {
            // Unity AOTと相性を考慮し JsonUtility を使う場合は string→byte[]
            var json = UnityEngine.JsonUtility.ToJson(obj);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            return UnityEngine.JsonUtility.FromJson<T>(json);
        }
    }

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
    }

    public sealed class PhotonActionContext : INetworkActionContext
    {
        private readonly INetworkBroadcaster _broadcaster;
        public PhotonActionContext(INetworkBroadcaster broadcaster)
        {
            _broadcaster = broadcaster;
        }
        public void Request(ActionRequestedEvent request)
        {
            _broadcaster.Raise(EventCode.ActionRequested, request);
        }
    }

    public sealed class PhotonReceiver : INetworkReceiver, IOnEventCallback, IDisposable
    {
        private readonly ISerializer _serializer;
        private readonly Dictionary<byte, Action<byte[]>> _handlers = new Dictionary<byte, Action<byte[]>>();
        private bool _active;
        private bool _disposed;

        public PhotonReceiver(ISerializer serializer)
        {
            _serializer = serializer;
        }

        /// <summary>
        /// ネットワークイベントのハンドラを登録
        /// </summary>
        /// <typeparam name="T">DTO型</typeparam>
        /// <param name="code">イベントコード</param>
        /// <param name="handler">ハンドラ</param>
        public void On<T>(EventCode code, Action<T> handler)
        {
            _handlers[(byte)code] = (bytes) =>
            {
                var dto = _serializer.Deserialize<T>(bytes);
                handler?.Invoke(dto);
            };
        }

        #region Start/Stop
        /// <summary>
        /// ネットワーク受信の開始（冪等）
        /// </summary>
        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PhotonReceiver));
            if (_active) return;
            PhotonNetwork.AddCallbackTarget(this);
            _active = true;
        }

        /// <summary>
        /// ネットワーク受信の停止（冪等）
        /// </summary>
        public void Stop()
        {
            if (!_active) return;
            PhotonNetwork.RemoveCallbackTarget(this);
            _active = false;
        }
        #endregion

        /// <summary>
        /// ネットワークイベントの受信
        /// </summary>
        public void OnEvent(EventData photonEvent)
        {
            if (_handlers.TryGetValue(photonEvent.Code, out var h))
            {
                if (photonEvent.CustomData is byte[] bytes)
                {
                    h(bytes);
                }
            }
        }

        #region IDisposable
        /// <summary>
        /// 受信停止を保証して破棄します。
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            Stop();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}


