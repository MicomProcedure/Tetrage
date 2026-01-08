using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// 同一プロセス内で複数のPeer（VirtualReceiver）を管理し、イベントをルーティングする中央ハブ。
    /// ActorNumberの割り当てと、Peer間のメッセージ配送を担当する。
    /// </summary>
    public sealed class VirtualTransportHub
    {
        private static VirtualTransportHub _instance;
        private readonly Dictionary<int, VirtualReceiver> _receivers = new Dictionary<int, VirtualReceiver>();
        private int _nextActorNumber = 1;
        private readonly object _lock = new object();

        /// <summary>
        /// シングルトンインスタンスを取得
        /// </summary>
        public static VirtualTransportHub Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VirtualTransportHub();
                }
                return _instance;
            }
        }

        private VirtualTransportHub()
        {
            Debug.Log("VirtualTransportHub: インスタンス作成");
        }

        /// <summary>
        /// 新しいReceiverを登録し、ActorNumberを割り当てる
        /// </summary>
        /// <param name="receiver">登録するVirtualReceiver</param>
        /// <returns>割り当てられたActorNumber</returns>
        public int RegisterReceiver(VirtualReceiver receiver)
        {
            lock (_lock)
            {
                int actorNumber = _nextActorNumber++;
                _receivers[actorNumber] = receiver;
                Debug.Log($"VirtualTransportHub: Receiver登録 (ActorNumber: {actorNumber}, 総Peer数: {_receivers.Count})");
                return actorNumber;
            }
        }

        /// <summary>
        /// Receiverの登録を解除する
        /// </summary>
        /// <param name="actorNumber">解除するReceiverのActorNumber</param>
        public void UnregisterReceiver(int actorNumber)
        {
            lock (_lock)
            {
                if (_receivers.Remove(actorNumber))
                {
                    Debug.Log($"VirtualTransportHub: Receiver登録解除 (ActorNumber: {actorNumber}, 残りPeer数: {_receivers.Count})");
                }
            }
        }

        /// <summary>
        /// 全てのReceiverにイベントをブロードキャストする
        /// </summary>
        /// <param name="eventCode">イベントコード</param>
        /// <param name="payload">ペイロード（byte配列）</param>
        public void BroadcastToAll(byte eventCode, byte[] payload)
        {
            lock (_lock)
            {
                Debug.Log($"VirtualTransportHub: BroadcastToAll (Code: {eventCode}, Peer数: {_receivers.Count})");
                foreach (var receiver in _receivers.Values)
                {
                    try
                    {
                        receiver.OnEventReceived(eventCode, payload);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"VirtualTransportHub: Receiver処理中にエラー: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 指定したActorNumberのReceiverにイベントを送信する
        /// </summary>
        /// <param name="eventCode">イベントコード</param>
        /// <param name="payload">ペイロード（byte配列）</param>
        /// <param name="targetActorNumbers">送信先ActorNumber配列</param>
        public void BroadcastToActors(byte eventCode, byte[] payload, int[] targetActorNumbers)
        {
            if (targetActorNumbers == null || targetActorNumbers.Length == 0)
            {
                return;
            }

            lock (_lock)
            {
                Debug.Log($"VirtualTransportHub: BroadcastToActors (Code: {eventCode}, Targets: {string.Join(", ", targetActorNumbers)})");
                foreach (int actorNumber in targetActorNumbers)
                {
                    if (_receivers.TryGetValue(actorNumber, out var receiver))
                    {
                        try
                        {
                            receiver.OnEventReceived(eventCode, payload);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"VirtualTransportHub: Receiver({actorNumber})処理中にエラー: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"VirtualTransportHub: ActorNumber {actorNumber} のReceiverが見つかりません");
                    }
                }
            }
        }

        /// <summary>
        /// 全てのReceiverを登録解除し、ActorNumberカウンタをリセットする（テスト用）
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _receivers.Clear();
                _nextActorNumber = 1;
                Debug.Log("VirtualTransportHub: リセット完了");
            }
        }

        /// <summary>
        /// 現在登録されているPeer数を取得
        /// </summary>
        public int PeerCount
        {
            get
            {
                lock (_lock)
                {
                    return _receivers.Count;
                }
            }
        }

        /// <summary>
        /// シングルトンインスタンスを破棄（テスト用）
        /// </summary>
        public static void DestroyInstance()
        {
            _instance?.Reset();
            _instance = null;
            Debug.Log("VirtualTransportHub: インスタンス破棄");
        }
    }
}

