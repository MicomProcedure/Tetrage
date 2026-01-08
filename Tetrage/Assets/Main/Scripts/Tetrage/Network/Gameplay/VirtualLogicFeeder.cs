using UnityEngine;
using DomainEvents = Tetrage.Core.Events;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// DomainEventを直接EventBusに注入する実装。
    /// ネットワーク層（DTO変換、送受信）を完全にバイパスする。
    /// </summary>
    public sealed class VirtualLogicFeeder : IVirtualLogicFeeder
    {
        private readonly IGameplayEventBus _eventBus;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="eventBus">イベント注入先のEventBus</param>
        public VirtualLogicFeeder(IGameplayEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
            Debug.Log("VirtualLogicFeeder: 初期化完了");
        }

        /// <summary>
        /// EventBusへの参照
        /// </summary>
        public IGameplayEventBus EventBus => _eventBus;

        /// <summary>
        /// DomainEventを直接EventBusに発行
        /// </summary>
        public void Feed<TEvent>(TEvent domainEvent) where TEvent : class
        {
            if (domainEvent == null)
            {
                throw new System.ArgumentNullException(nameof(domainEvent));
            }

            // 型に応じてPublishメソッドを呼び出し
            switch (domainEvent)
            {
                case DomainEvents.GameStartedEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: GameStartedEvent注入 (Seq: {e.Sequence}, Players: {e.PlayerIds.Count})");
                    break;
                case DomainEvents.TurnStartedEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: TurnStartedEvent注入 (Seq: {e.Sequence}, CurrentPlayer: {e.CurrentPlayerId})");
                    break;
                case DomainEvents.TurnEndedEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: TurnEndedEvent注入 (Seq: {e.Sequence})");
                    break;
                case DomainEvents.CardMovedEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: CardMovedEvent注入 (Seq: {e.Sequence}, Card: {e.CardId}, From: {e.FromPileId} -> To: {e.ToPileId})");
                    break;
                case DomainEvents.CardVisibilityChangedEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: CardVisibilityChangedEvent注入 (Seq: {e.Sequence})");
                    break;
                case DomainEvents.GameEndedEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: GameEndedEvent注入 (Seq: {e.Sequence}, Winners: {e.WinnerPlayerIds.Count})");
                    break;
                case DomainEvents.ScanPhaseStartedEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: ScanPhaseStartedEvent注入 (Seq: {e.Sequence})");
                    break;
                case DomainEvents.ScanPhaseEndedEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: ScanPhaseEndedEvent注入 (Seq: {e.Sequence})");
                    break;
                case DomainEvents.FinishingGameEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: FinishingGameEvent注入 (Seq: {e.Sequence})");
                    break;
                case DomainEvents.ActionRequestedEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: ActionRequestedEvent注入 (Seq: {e.Sequence})");
                    break;
                case DomainEvents.ActionResultEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: ActionResultEvent注入 (Seq: {e.Sequence})");
                    break;
                case DomainEvents.PileShuffledEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: PileShuffledEvent注入 (Seq: {e.Sequence})");
                    break;
                case DomainEvents.ListOrderDeclaredEvent e:
                    _eventBus.Publish(e);
                    Debug.Log($"VirtualLogicFeeder: ListOrderDeclaredEvent注入 (Seq: {e.Sequence})");
                    break;
                default:
                    throw new System.ArgumentException($"未対応のDomainEvent型: {domainEvent.GetType().Name}");
            }
        }
    }
}

