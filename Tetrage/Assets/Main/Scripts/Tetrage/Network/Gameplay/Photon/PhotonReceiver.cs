using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// Photonネットワーク経由でイベントを受信する実装
    /// </summary>
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
                Debug.Log($"PhotonReceiver: On, Code: {code}, Handler: {handler.Method.Name}");
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
            #if UNITY_EDITOR
            Debug.Log($"PhotonReceiver: OnEvent code={photonEvent.Code} dataType={(photonEvent?.CustomData != null ? photonEvent.CustomData.GetType().Name : "null")}");
            #endif
            if (_handlers.TryGetValue(photonEvent.Code, out var h))
            {
                if (photonEvent.CustomData is byte[] bytes)
                {
                    h(bytes);
                }
                else
                {
                    Debug.LogWarning($"PhotonReceiver: Unsupported CustomData type for code={photonEvent.Code}");
                }
            }
            else
            {
                Debug.LogWarning($"PhotonReceiver: No handler for code={photonEvent.Code}");
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

