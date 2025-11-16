using Tetrage.Core.Events;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// NetworkDTO → DomainEvent 変換を担当するBoundary層クラス。
    /// ドメインロジックの実行は GameplayDomainEventHandler に委譲。
    /// </summary>
    public sealed class NetworkEventApplier
    {
        private readonly IGameplayEventBus _eventBus;
        private readonly DomainEventConverter _converter;
        private int _lastSequence;

        public NetworkEventApplier(
            IGameplayEventBus eventBus,
            DomainEventConverter converter)
        {
            _eventBus = eventBus;
            _converter = converter;
            _lastSequence = 0;
        }

        private bool ShouldApply(int sequence)
        {
            if (sequence <= _lastSequence) return false; // 重複/古いイベントは無視
            _lastSequence = sequence;
            return true;
        }

        public void Apply(CardMovedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }

        public void Apply(CardVisibilityChangedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }

        public void Apply(StartScanPhaseEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }

        public void Apply(EndScanPhaseEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }

        public void Apply(FinishingGameEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }

        public void Apply(GameEndedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }

        public void Apply(ActionResultEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }

        public void Apply(PileShuffledWithSeedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }

        /// <summary>
        /// GameStarted: DTO→DomainEvent変換して発行。
        /// 初期同期のため、連番はリセット。
        /// </summary>
        public void Apply(GameStartedEvent e)
        {
            ResetSequences();

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }


        public void Apply(ListOrderDeclaredEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }

        public void Apply(TurnStartedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }

        public void Apply(TurnEndedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            var domainEvent = _converter.ToDomain(e);
            _eventBus.Publish(domainEvent);
        }

        public void ResetSequences()
        {
            _lastSequence = 0;
        }
    }
}


