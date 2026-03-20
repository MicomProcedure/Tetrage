using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// 同一プロセス内でネットワークイベントを受信する仮想Receiver。
    /// VirtualTransportHubに登録され、イベントを受け取る。
    /// </summary>
    public sealed class VirtualReceiver : INetworkReceiver, IDisposable
    {
        private readonly ISerializer _serializer;
        private readonly VirtualTransportHub _hub;
        private readonly Dictionary<byte, Action<byte[]>> _handlers = new Dictionary<byte, Action<byte[]>>();
        private int _actorNumber = -1;
        private bool _active = false;
        private bool _disposed = false;

        public VirtualReceiver(ISerializer serializer, VirtualTransportHub hub)
        {
            _serializer = serializer;
            _hub = hub;
        }

        /// <summary>
        /// このReceiverに割り当てられたActorNumberを取得
        /// </summary>
        public int ActorNumber => _actorNumber;

        /// <summary>
        /// イベントハンドラを登録する
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
                Debug.Log($"VirtualReceiver({_actorNumber}): イベント受信 (Code: {code}, Handler: {handler.Method.Name})");
            };
        }

        /// <summary>
        /// イベント受信を開始する（VirtualTransportHubに登録）
        /// </summary>
        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(VirtualReceiver));
            }

            if (_active)
            {
                return;
            }

            // VirtualTransportHubに登録してActorNumberを取得
            _actorNumber = _hub.RegisterReceiver(this);
            _active = true;
            Debug.Log($"VirtualReceiver: 受信開始 (ActorNumber: {_actorNumber})");
        }

        /// <summary>
        /// イベント受信を停止する（VirtualTransportHubから登録解除）
        /// </summary>
        public void Stop()
        {
            if (!_active)
            {
                return;
            }

            if (_actorNumber >= 0)
            {
                _hub.UnregisterReceiver(_actorNumber);
            }

            _active = false;
            Debug.Log($"VirtualReceiver: 受信停止 (ActorNumber: {_actorNumber})");
        }

        /// <summary>
        /// VirtualTransportHubから呼び出されるイベント受信メソッド（内部用）
        /// </summary>
        /// <param name="eventCode">イベントコード</param>
        /// <param name="payload">ペイロード（byte配列）</param>
        internal void OnEventReceived(byte eventCode, byte[] payload)
        {
            if (!_active)
            {
                return;
            }

            if (_handlers.TryGetValue(eventCode, out var handler))
            {
                try
                {
                    handler(payload);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"VirtualReceiver({_actorNumber}): ハンドラ実行中にエラー (Code: {eventCode}): {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"VirtualReceiver({_actorNumber}): 未登録のイベントコード: {eventCode}");
            }
        }

        /// <summary>
        /// リソースを解放する
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Stop();
            _handlers.Clear();
            _disposed = true;
            GC.SuppressFinalize(this);
            Debug.Log($"VirtualReceiver({_actorNumber}): Dispose完了");
        }
    }
}

