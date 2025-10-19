using System;

namespace Tetrage.Network.Gameplay
{
    public interface IGameplayEventHandler
    {
        void OnGameStarted(GameStartedEvent e);
        void OnTurnStarted(TurnStartedEvent e);
        void OnCardMoved(CardMovedEvent e);
        void OnListOrderDeclared(ListOrderDeclaredEvent e);
        void OnCardVisibilityChanged(CardVisibilityChangedEvent e);
        void OnPileShuffledWithSeed(PileShuffledWithSeedEvent e);
        void OnActionRequested(ActionRequestedEvent e);
        void OnActionResult(ActionResultEvent e);
    }

    /// <summary>
    /// 既定のイベントハンドラ実装。モデル反映は NetworkEventApplier に委譲。
    /// </summary>
    public sealed class DefaultGameplayEventHandler : IGameplayEventHandler
    {
        private readonly NetworkEventApplier _applier;
        private readonly bool _isHost;
        private readonly Action<ActionRequestedEvent> _onActionRequestedHost;
        private readonly Action<GameStartedEvent> _onGameStartedUi;
        private readonly Action<TurnStartedEvent> _onTurnStartedUi;

        public DefaultGameplayEventHandler(
            NetworkEventApplier applier,
            bool isHost,
            Action<ActionRequestedEvent> onActionRequestedHost,
            Action<GameStartedEvent> onGameStartedUi = null,
            Action<TurnStartedEvent> onTurnStartedUi = null)
        {
            _applier = applier;
            _isHost = isHost;
            _onActionRequestedHost = onActionRequestedHost;
            _onGameStartedUi = onGameStartedUi;
            _onTurnStartedUi = onTurnStartedUi;
        }

        public void OnGameStarted(GameStartedEvent e)
        {
            _onGameStartedUi?.Invoke(e);
            // 初期同期（プレイヤー順など）を適用
            _applier.Apply(e);
        }
        public void OnListOrderDeclared(ListOrderDeclaredEvent e)
        {
            _applier.Apply(e);
        }

        public void OnTurnStarted(TurnStartedEvent e)
        {
            _onTurnStartedUi?.Invoke(e);
        }

        public void OnCardMoved(CardMovedEvent e)
        {
            _applier.Apply(e);
        }

        public void OnCardVisibilityChanged(CardVisibilityChangedEvent e)
        {
            _applier.Apply(e);
        }

        public void OnPileShuffledWithSeed(PileShuffledWithSeedEvent e)
        {
            _applier.Apply(e);
        }

        public void OnActionRequested(ActionRequestedEvent e)
        {
            if (_isHost)
            {
                _onActionRequestedHost?.Invoke(e);
            }
        }

        public void OnActionResult(ActionResultEvent e)
        {
            _applier.Apply(e);
        }
    }
}



