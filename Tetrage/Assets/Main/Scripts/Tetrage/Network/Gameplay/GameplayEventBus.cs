using System;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ゲーム内の「適用後イベント」を配信するイベントバス。
    /// NetworkEventApplier が Publish を呼び、UI/Context はイベントを購読します。
    /// </summary>
    public interface IGameplayEventBus
    {
        event Action<GameStartedEvent> GameStartedApplied;
        event Action<TurnStartedEvent> TurnStartedApplied;
        event Action<TurnEndedEvent> TurnEndedApplied;
        event Action<ListOrderDeclaredEvent> ListOrderDeclaredApplied;
        event Action<CardMovedEvent> CardMovedApplied;
        event Action<CardVisibilityChangedEvent> CardVisibilityChangedApplied;
        event Action<PileShuffledWithSeedEvent> PileShuffledApplied;
        event Action<ActionResultEvent> ActionResultApplied;

        void PublishGameStarted(GameStartedEvent e);
        void PublishTurnStarted(TurnStartedEvent e);
        void PublishTurnEnded(TurnEndedEvent e);
        void PublishListOrderDeclared(ListOrderDeclaredEvent e);
        void PublishCardMoved(CardMovedEvent e);
        void PublishCardVisibilityChanged(CardVisibilityChangedEvent e);
        void PublishPileShuffled(PileShuffledWithSeedEvent e);
        void PublishActionResult(ActionResultEvent e);
    }

    /// <summary>
    /// シンプルなイベントバス実装。メインスレッドでの呼び出しは呼び出し元で保証してください。
    /// </summary>
    public sealed class SimpleGameplayEventBus : IGameplayEventBus
    {
        public event Action<GameStartedEvent> GameStartedApplied;
        public event Action<TurnStartedEvent> TurnStartedApplied;
        public event Action<TurnEndedEvent> TurnEndedApplied;
        public event Action<ListOrderDeclaredEvent> ListOrderDeclaredApplied;
        public event Action<CardMovedEvent> CardMovedApplied;
        public event Action<CardVisibilityChangedEvent> CardVisibilityChangedApplied;
        public event Action<PileShuffledWithSeedEvent> PileShuffledApplied;
        public event Action<ActionResultEvent> ActionResultApplied;

        public void PublishGameStarted(GameStartedEvent e) => GameStartedApplied?.Invoke(e);
        public void PublishTurnStarted(TurnStartedEvent e) => TurnStartedApplied?.Invoke(e);
        public void PublishTurnEnded(TurnEndedEvent e) => TurnEndedApplied?.Invoke(e);
        public void PublishListOrderDeclared(ListOrderDeclaredEvent e) => ListOrderDeclaredApplied?.Invoke(e);
        public void PublishCardMoved(CardMovedEvent e) => CardMovedApplied?.Invoke(e);
        public void PublishCardVisibilityChanged(CardVisibilityChangedEvent e) => CardVisibilityChangedApplied?.Invoke(e);
        public void PublishPileShuffled(PileShuffledWithSeedEvent e) => PileShuffledApplied?.Invoke(e);
        public void PublishActionResult(ActionResultEvent e) => ActionResultApplied?.Invoke(e);
    }
}


