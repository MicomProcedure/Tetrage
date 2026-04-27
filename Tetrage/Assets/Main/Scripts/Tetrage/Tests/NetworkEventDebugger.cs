using UnityEngine;
using Tetrage.Network.Gameplay;
using Tetrage.Core.DTO;
using Tetrage.Core.Events;
using System;
using System.Reflection;

namespace Tetrage.Tests
{
    public enum EventTargetType
    {
        NetworkBroadcast,
        LocalEventBus
    }

    /// <summary>
    /// インスペクターからネットワークイベントを送信するためのデバッグコンポーネント。
    /// GameSceneDebugEntrySimpleなどにアタッチして使用する。
    /// </summary>
    public class NetworkEventDebugger : MonoBehaviour
    {
        #region Serialized fields
        [Header("Debug Network Event")]
        [SerializeField] private bool _enableDebugEvents = true;
        [SerializeField] private EventTargetType _targetType = EventTargetType.NetworkBroadcast;
        [SerializeField] private EventCode _debugEventCode = EventCode.ActionResult;
        [SerializeField, TextArea(3, 10)] private string _debugJsonPayload = "{}";
        [SerializeField] private bool _sendToAll = true;
        [SerializeField] private int[] _targetActorNumbers;
        [Tooltip("NetworkBroadcast 経由のとき、DTO の sequence を NetworkEventApplier の最終値より大きく自動補正する（古い sequence だと Apply が無視されるため）。")]
        [SerializeField] private bool _autoBumpNetworkSequence = true;
        #endregion

        #region Dependencies
        private IGameplayNetworkController _networkController;
        private DomainEventConverter _converter;
        #endregion

        /// <summary>
        /// ネットワークコントローラーを設定する
        /// </summary>
        public void Setup(IGameplayNetworkController networkController)
        {
            _networkController = networkController;
            if (_networkController?.PlayerIdMapper != null)
            {
                _converter = new DomainEventConverter(_networkController.PlayerIdMapper);
            }
        }

        [ContextMenu("Send Network Event")]
        public void SendNetworkEvent()
        {
            if (!_enableDebugEvents)
            {
                Debug.LogWarning("[NetworkEventDebugger] Debug events are disabled.");
                return;
            }

            if (_networkController == null)
            {
                Debug.LogWarning("[NetworkEventDebugger] NetworkController is not set. Call Setup() first.");
                return;
            }

            Type payloadType = GetTypeForEventCode(_debugEventCode);
            if (payloadType == null)
            {
                Debug.LogError($"[NetworkEventDebugger] Unsupported EventCode for debugging: {_debugEventCode}");
                return;
            }

            try
            {
                // JSONからオブジェクトを復元
                object payload = JsonUtility.FromJson(_debugJsonPayload, payloadType);
                if (payload == null)
                {
                    Debug.LogError("[NetworkEventDebugger] JSON parse failed (result is null)");
                    return;
                }

                if (_targetType == EventTargetType.NetworkBroadcast)
                {
                    SendViaNetwork(payload, payloadType);
                }
                else if (_targetType == EventTargetType.LocalEventBus)
                {
                    PublishToLocalEventBus(_debugEventCode, payload);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkEventDebugger] Error sending event: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void SendViaNetwork(object payload, Type payloadType)
        {
            if (_autoBumpNetworkSequence)
            {
                payload = NormalizeNetworkSequenceIfNeeded(payload, payloadType);
            }

            // INetworkBroadcasterのメソッドをリフレクションで取得して実行
            var broadcasterType = typeof(INetworkBroadcaster);
            string payloadJson = JsonUtility.ToJson(payload);

            if (_sendToAll)
            {
                MethodInfo raiseMethod = broadcasterType.GetMethod("Raise")?.MakeGenericMethod(payloadType);
                if (raiseMethod != null)
                {
                    raiseMethod.Invoke(_networkController.Broadcaster, new object[] { _debugEventCode, payload });
                    Debug.Log($"[NetworkEventDebugger] Sent {_debugEventCode} to All via Network:\n{payloadJson}");
                }
            }
            else
            {
                MethodInfo raiseToActorsMethod = broadcasterType.GetMethod("RaiseToActors")?.MakeGenericMethod(payloadType);
                if (raiseToActorsMethod != null)
                {
                    int[] targets = _targetActorNumbers ?? Array.Empty<int>();
                    raiseToActorsMethod.Invoke(_networkController.Broadcaster, new object[] { _debugEventCode, payload, targets });
                    Debug.Log($"[NetworkEventDebugger] Sent {_debugEventCode} to Actors [{string.Join(",", targets)}] via Network:\n{payloadJson}");
                }
            }
        }

        /// <summary>
        /// NetworkEventApplier は sequence が単調増加でないイベントを破棄するため、
        /// インスペクタ JSON のままだとゲーム進行後にデバッグ送信が無視される。補正する。
        /// </summary>
        private object NormalizeNetworkSequenceIfNeeded(object payload, Type payloadType)
        {
            if (payload == null || payloadType == null || _networkController == null)
            {
                return payload;
            }

            FieldInfo seqField = payloadType.GetField("sequence", BindingFlags.Instance | BindingFlags.Public);
            if (seqField == null || seqField.FieldType != typeof(int))
            {
                return payload;
            }

            int lastApplied = _networkController.LastAppliedNetworkSequence;
            int current = (int)seqField.GetValue(payload);
            int adjusted = Math.Max(current, lastApplied + 1);
            if (adjusted != current)
            {
                seqField.SetValue(payload, adjusted);
                Debug.Log($"[NetworkEventDebugger] Network 用に sequence を {current} → {adjusted} に補正（LastAppliedNetworkSequence={lastApplied}）。");
            }

            return payload;
        }

        private void PublishToLocalEventBus(EventCode code, object dtoPayload)
        {
            if (_networkController?.EventBus == null)
            {
                Debug.LogWarning("[NetworkEventDebugger] EventBus is not ready.");
                return;
            }

            if (_converter == null)
            {
                Debug.LogWarning("[NetworkEventDebugger] DomainEventConverter is not ready (PlayerIdMapper missing?).");
                return;
            }

            var bus = _networkController.EventBus;

            switch (code)
            {
                case EventCode.GameStarted:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.GameStartedEvent)dtoPayload));
                    break;
                case EventCode.TurnStarted:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.TurnStartedEvent)dtoPayload));
                    break;
                case EventCode.TurnEnded:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.TurnEndedEvent)dtoPayload));
                    break;
                case EventCode.ListOrderDeclared:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.ListOrderDeclaredEvent)dtoPayload));
                    break;
                case EventCode.CardMoved:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.CardMovedEvent)dtoPayload));
                    break;
                case EventCode.CardVisibilityChanged:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.CardVisibilityChangedEvent)dtoPayload));
                    break;
                case EventCode.StartScanPhase:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.StartScanPhaseEvent)dtoPayload));
                    break;
                case EventCode.EndScanPhase:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.EndScanPhaseEvent)dtoPayload));
                    break;
                case EventCode.FinishingGame:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.FinishingGameEvent)dtoPayload));
                    break;
                case EventCode.GameEnded:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.GameEndedEvent)dtoPayload));
                    break;
                case EventCode.PileShuffledWithSeed:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.PileShuffledWithSeedEvent)dtoPayload));
                    break;
                case EventCode.ActionRequested:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.ActionRequestedEvent)dtoPayload));
                    break;
                case EventCode.ActionResult:
                    bus.Publish(_converter.ToDomain((Tetrage.Network.Gameplay.ActionResultEvent)dtoPayload));
                    break;
                default:
                    Debug.LogWarning($"[NetworkEventDebugger] Local publish not implemented for {code}");
                    return;
            }
            Debug.Log($"[NetworkEventDebugger] Published {_debugEventCode} to Local EventBus:\n{_debugJsonPayload}");
        }

        /// <summary>
        /// EventCodeに対応するDTO型を返す
        /// </summary>
        private Type GetTypeForEventCode(EventCode code)
        {
            return code switch
            {
                EventCode.GameStarted => typeof(Tetrage.Network.Gameplay.GameStartedEvent),
                EventCode.TurnStarted => typeof(Tetrage.Network.Gameplay.TurnStartedEvent),
                EventCode.TurnEnded => typeof(Tetrage.Network.Gameplay.TurnEndedEvent),
                EventCode.ListOrderDeclared => typeof(Tetrage.Network.Gameplay.ListOrderDeclaredEvent),
                EventCode.CardMoved => typeof(Tetrage.Network.Gameplay.CardMovedEvent),
                EventCode.CardVisibilityChanged => typeof(Tetrage.Network.Gameplay.CardVisibilityChangedEvent),
                EventCode.StartScanPhase => typeof(Tetrage.Network.Gameplay.StartScanPhaseEvent),
                EventCode.EndScanPhase => typeof(Tetrage.Network.Gameplay.EndScanPhaseEvent),
                EventCode.FinishingGame => typeof(Tetrage.Network.Gameplay.FinishingGameEvent),
                EventCode.GameEnded => typeof(Tetrage.Network.Gameplay.GameEndedEvent),
                EventCode.PileShuffledWithSeed => typeof(Tetrage.Network.Gameplay.PileShuffledWithSeedEvent),
                EventCode.ActionRequested => typeof(Tetrage.Network.Gameplay.ActionRequestedEvent),
                EventCode.ActionResult => typeof(Tetrage.Network.Gameplay.ActionResultEvent),
                _ => null
            };
        }
    }
}
