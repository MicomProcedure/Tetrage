using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// MPPM別プロセス間のイベントログを購読するVirtualReceiver。
    /// </summary>
    public sealed class MppmSharedVirtualReceiver : INetworkReceiver, IDisposable
    {
        #region Fields

        private readonly ISerializer _serializer;
        private readonly MppmSharedVirtualTransportStore _store;
        private readonly int _actorNumber;
        private readonly Dictionary<byte, Action<byte[]>> _handlers = new();
        private bool _active;
        private bool _disposed;
        private long _lastSequence;

        #endregion

        #region Properties

        public int ActorNumber => _actorNumber;

        #endregion

        #region Constructor

        /// <summary>
        /// ストアと受信ActorNumberを指定して生成する。
        /// </summary>
        public MppmSharedVirtualReceiver(
            ISerializer serializer,
            MppmSharedVirtualTransportStore store,
            int actorNumber)
        {
            _serializer = serializer;
            _store = store;
            _actorNumber = actorNumber;
        }

        #endregion

        #region INetworkReceiver

        public void On<T>(EventCode code, Action<T> handler)
        {
            _handlers[(byte)code] = bytes =>
            {
                var dto = _serializer.Deserialize<T>(bytes);
                handler?.Invoke(dto);
                Debug.Log($"MppmSharedVirtualReceiver({_actorNumber}): イベント受信 (Code: {code})");
            };
        }

        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MppmSharedVirtualReceiver));
            }

            if (_active)
            {
                return;
            }

            _active = true;
            PollEventsAsync().Forget();
            Debug.Log($"MppmSharedVirtualReceiver: 受信開始 (ActorNumber: {_actorNumber})");
        }

        public void Stop()
        {
            _active = false;
            Debug.Log($"MppmSharedVirtualReceiver: 受信停止 (ActorNumber: {_actorNumber})");
        }

        #endregion

        #region IDisposable

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
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid PollEventsAsync()
        {
            while (_active && !_disposed)
            {
                DispatchPendingEvents();
                await UniTask.Delay(50);
            }
        }

        private void DispatchPendingEvents()
        {
            var events = _store.ReadAfter(_lastSequence);
            foreach (var storedEvent in events)
            {
                _lastSequence = Math.Max(_lastSequence, storedEvent.sequence);
                if (!MppmSharedVirtualTransportStore.IsTargetForActor(storedEvent, _actorNumber))
                {
                    continue;
                }

                if (!_handlers.TryGetValue((byte)storedEvent.eventCode, out var handler))
                {
                    continue;
                }

                try
                {
                    var payload = Convert.FromBase64String(storedEvent.payloadBase64 ?? string.Empty);
                    handler(payload);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"MppmSharedVirtualReceiver({_actorNumber}): イベント処理に失敗しました (Code: {storedEvent.eventCode}) {ex.Message}");
                }
            }
        }

        #endregion
    }
}
