using System;
using UnityEngine;

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
        event Action<StartScanPhaseEvent> StartScanPhaseApplied;
        event Action<EndScanPhaseEvent> EndScanPhaseApplied;
        event Action<FinishingGameEvent> FinishingGameApplied;
        event Action<GameEndedEvent> GameEndedApplied;

        void PublishGameStarted(GameStartedEvent e);
        void PublishTurnStarted(TurnStartedEvent e);
        void PublishTurnEnded(TurnEndedEvent e);
        void PublishListOrderDeclared(ListOrderDeclaredEvent e);
        void PublishCardMoved(CardMovedEvent e);
        void PublishCardVisibilityChanged(CardVisibilityChangedEvent e);
        void PublishPileShuffled(PileShuffledWithSeedEvent e);
        void PublishActionResult(ActionResultEvent e);
        void PublishStartScanPhase(StartScanPhaseEvent e);
        void PublishEndScanPhase(EndScanPhaseEvent e);
        void PublishFinishingGame(FinishingGameEvent e);
        void PublishGameEnded(GameEndedEvent e);
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
        public event Action<StartScanPhaseEvent> StartScanPhaseApplied;
        public event Action<EndScanPhaseEvent> EndScanPhaseApplied;
        public event Action<FinishingGameEvent> FinishingGameApplied;
        public event Action<GameEndedEvent> GameEndedApplied;

        public void PublishGameStarted(GameStartedEvent e) { 
            GameStartedApplied?.Invoke(e); 
            Debug.Log($"SimpleGameplayEventBus: PublishGameStarted, PlayerActorNumbers: {string.Join(", ", e.playerActorNumbers)}");
            Debug.Log($"SimpleGameplayEventBus: ActorPlayerId: {Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber}");
        }
        public void PublishTurnStarted(TurnStartedEvent e) { 
            TurnStartedApplied?.Invoke(e); 
            Debug.Log($"SimpleGameplayEventBus: PublishTurnStarted, Sequence: {e.sequence}, StateVersion: {e.stateVersion}");
            Debug.Log($"SimpleGameplayEventBus: PublishTurnStarted, CurrentPlayerActorNumber: {e.currentPlayerActorNumber}");
        }
        public void PublishTurnEnded(TurnEndedEvent e) { 
            TurnEndedApplied?.Invoke(e); 
            Debug.Log($"SimpleGameplayEventBus: PublishTurnEnded, Sequence: {e.sequence}, StateVersion: {e.stateVersion}");
        }
        public void PublishListOrderDeclared(ListOrderDeclaredEvent e) { 
            ListOrderDeclaredApplied?.Invoke(e); 
            Debug.Log($"SimpleGameplayEventBus: PublishListOrderDeclared, Sequence: {e.sequence}, StateVersion: {e.stateVersion}");
        }
        public void PublishCardMoved(CardMovedEvent e) { CardMovedApplied?.Invoke(e); 
            Debug.Log($"SimpleGameplayEventBus: PublishCardMoved, Sequence: {e.sequence}, StateVersion: {e.stateVersion}");
            Debug.Log($"SimpleGameplayEventBus: PublishCardMoved, CardId: {e.cardId}, FromPileId: {e.fromPileId}, ToPileId: {e.toPileId}");
        }
        public void PublishCardVisibilityChanged(CardVisibilityChangedEvent e) { CardVisibilityChangedApplied?.Invoke(e); 
            Debug.Log($"SimpleGameplayEventBus: PublishCardVisibilityChanged, Sequence: {e.sequence}, StateVersion: {e.stateVersion}");
            Debug.Log($"SimpleGameplayEventBus: PublishCardVisibilityChanged, CardId: {e.cardId}, IsVisible: {e.isVisible}");
        }
        public void PublishPileShuffled(PileShuffledWithSeedEvent e) { PileShuffledApplied?.Invoke(e); 
            Debug.Log($"SimpleGameplayEventBus: PublishPileShuffled, Sequence: {e.sequence}, StateVersion: {e.stateVersion}");
            Debug.Log($"SimpleGameplayEventBus: PublishPileShuffled, PileId: {e.pileId}, Seed: {e.seed}");
            }
        public void PublishActionResult(ActionResultEvent e) { ActionResultApplied?.Invoke(e); 
            Debug.Log($"SimpleGameplayEventBus: PublishActionResult, Sequence: {e.sequence}, Accepted: {e.accepted}, Reason: {e.reason}");
        }

        public void PublishStartScanPhase(StartScanPhaseEvent e) { StartScanPhaseApplied?.Invoke(e);
            Debug.Log($"SimpleGameplayEventBus: PublishStartScanPhase, Sequence: {e.sequence}, StateVersion: {e.stateVersion}");
        }

        public void PublishEndScanPhase(EndScanPhaseEvent e) { EndScanPhaseApplied?.Invoke(e);
            Debug.Log($"SimpleGameplayEventBus: PublishEndScanPhase, Sequence: {e.sequence}, StateVersion: {e.stateVersion}");
        }

        public void PublishFinishingGame(FinishingGameEvent e) { FinishingGameApplied?.Invoke(e);
            Debug.Log($"SimpleGameplayEventBus: PublishFinishingGame, Sequence: {e.sequence}, WinnerActor={string.Join(", ", e.winnerActorNumbers)}");
        }

        public void PublishGameEnded(GameEndedEvent e) { GameEndedApplied?.Invoke(e);
            Debug.Log($"SimpleGameplayEventBus: PublishGameEnded, Sequence: {e.sequence}, WinnerActor={string.Join(", ", e.winnerActorNumbers)}");
        }
    }
}


