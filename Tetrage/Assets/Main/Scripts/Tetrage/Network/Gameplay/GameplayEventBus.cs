using R3;
using UnityEngine;
using DomainEvents = Tetrage.Core.Events;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// ゲーム内の「適用後イベント」を配信するイベントバス。
    /// NetworkEventApplier が Publish を呼び、UI/Context はイベントを購読します。
    /// R3の Observable を使用して型安全なイベント購読と自動購読解除を実現。
    /// </summary>
    public interface IGameplayEventBus
    {
        Observable<DomainEvents.GameStartedEvent> GameStarted { get; }
        Observable<DomainEvents.TurnStartedEvent> TurnStarted { get; }
        Observable<DomainEvents.TurnEndedEvent> TurnEnded { get; }
        Observable<DomainEvents.ListOrderDeclaredEvent> ListOrderDeclared { get; }
        Observable<DomainEvents.CardMovedEvent> CardMoved { get; }
        Observable<DomainEvents.CardVisibilityChangedEvent> CardVisibilityChanged { get; }
        Observable<DomainEvents.PileShuffledEvent> PileShuffled { get; }
        Observable<DomainEvents.ActionRequestedEvent> ActionRequested { get; }
        Observable<DomainEvents.ActionResultEvent> ActionResult { get; }
        Observable<DomainEvents.ScanPhaseStartedEvent> ScanPhaseStarted { get; }
        Observable<DomainEvents.ScanPhaseEndedEvent> ScanPhaseEnded { get; }
        Observable<DomainEvents.FinishingGameEvent> FinishingGame { get; }
        Observable<DomainEvents.GameEndedEvent> GameEnded { get; }

        void Publish(DomainEvents.GameStartedEvent e);
        void Publish(DomainEvents.TurnStartedEvent e);
        void Publish(DomainEvents.TurnEndedEvent e);
        void Publish(DomainEvents.ListOrderDeclaredEvent e);
        void Publish(DomainEvents.CardMovedEvent e);
        void Publish(DomainEvents.CardVisibilityChangedEvent e);
        void Publish(DomainEvents.PileShuffledEvent e);
        void Publish(DomainEvents.ActionRequestedEvent e);
        void Publish(DomainEvents.ActionResultEvent e);
        void Publish(DomainEvents.ScanPhaseStartedEvent e);
        void Publish(DomainEvents.ScanPhaseEndedEvent e);
        void Publish(DomainEvents.FinishingGameEvent e);
        void Publish(DomainEvents.GameEndedEvent e);
    }

    #region R3EventBus実装

    /// <summary>
    /// R3を使用したイベントバス実装。Observable/Subjectによる型安全なイベント購読を実現。
    /// メインスレッドでの呼び出しは呼び出し元で保証してください。
    /// </summary>
    public sealed class R3EventBus : IGameplayEventBus
    {
        private readonly Subject<DomainEvents.GameStartedEvent> _gameStarted = new();
        private readonly Subject<DomainEvents.TurnStartedEvent> _turnStarted = new();
        private readonly Subject<DomainEvents.TurnEndedEvent> _turnEnded = new();
        private readonly Subject<DomainEvents.ListOrderDeclaredEvent> _listOrderDeclared = new();
        private readonly Subject<DomainEvents.CardMovedEvent> _cardMoved = new();
        private readonly Subject<DomainEvents.CardVisibilityChangedEvent> _cardVisibilityChanged = new();
        private readonly Subject<DomainEvents.PileShuffledEvent> _pileShuffled = new();
        private readonly Subject<DomainEvents.ActionRequestedEvent> _actionRequested = new();
        private readonly Subject<DomainEvents.ActionResultEvent> _actionResult = new();
        private readonly Subject<DomainEvents.ScanPhaseStartedEvent> _scanPhaseStarted = new();
        private readonly Subject<DomainEvents.ScanPhaseEndedEvent> _scanPhaseEnded = new();
        private readonly Subject<DomainEvents.FinishingGameEvent> _finishingGame = new();
        private readonly Subject<DomainEvents.GameEndedEvent> _gameEnded = new();

        public Observable<DomainEvents.GameStartedEvent> GameStarted => _gameStarted;
        public Observable<DomainEvents.TurnStartedEvent> TurnStarted => _turnStarted;
        public Observable<DomainEvents.TurnEndedEvent> TurnEnded => _turnEnded;
        public Observable<DomainEvents.ListOrderDeclaredEvent> ListOrderDeclared => _listOrderDeclared;
        public Observable<DomainEvents.CardMovedEvent> CardMoved => _cardMoved;
        public Observable<DomainEvents.CardVisibilityChangedEvent> CardVisibilityChanged => _cardVisibilityChanged;
        public Observable<DomainEvents.PileShuffledEvent> PileShuffled => _pileShuffled;
        public Observable<DomainEvents.ActionRequestedEvent> ActionRequested => _actionRequested;
        public Observable<DomainEvents.ActionResultEvent> ActionResult => _actionResult;
        public Observable<DomainEvents.ScanPhaseStartedEvent> ScanPhaseStarted => _scanPhaseStarted;
        public Observable<DomainEvents.ScanPhaseEndedEvent> ScanPhaseEnded => _scanPhaseEnded;
        public Observable<DomainEvents.FinishingGameEvent> FinishingGame => _finishingGame;
        public Observable<DomainEvents.GameEndedEvent> GameEnded => _gameEnded;

        public void Publish(DomainEvents.GameStartedEvent e)
        {
            _gameStarted.OnNext(e);
            Debug.Log($"R3EventBus: GameStarted published, Sequence={e.Sequence}, PlayerIds={string.Join(",", e.PlayerIds)}");
        }

        public void Publish(DomainEvents.TurnStartedEvent e)
        {
            _turnStarted.OnNext(e);
            Debug.Log($"R3EventBus: TurnStarted published, Sequence={e.Sequence}, CurrentPlayerId={e.CurrentPlayerId}");
        }

        public void Publish(DomainEvents.TurnEndedEvent e)
        {
            _turnEnded.OnNext(e);
            Debug.Log($"R3EventBus: TurnEnded published, Sequence={e.Sequence}, PreviousPlayerId={e.PreviousPlayerId}");
        }

        public void Publish(DomainEvents.ListOrderDeclaredEvent e)
        {
            _listOrderDeclared.OnNext(e);
            Debug.Log($"R3EventBus: ListOrderDeclared published, Sequence={e.Sequence}");
        }

        public void Publish(DomainEvents.CardMovedEvent e)
        {
            _cardMoved.OnNext(e);
            Debug.Log($"R3EventBus: CardMoved published, Sequence={e.Sequence}, CardId={e.CardId}, From={e.FromPileId}, To={e.ToPileId}");
        }

        public void Publish(DomainEvents.CardVisibilityChangedEvent e)
        {
            _cardVisibilityChanged.OnNext(e);
            Debug.Log($"R3EventBus: CardVisibilityChanged published, Sequence={e.Sequence}, CardId={e.CardId}, IsVisible={e.IsVisible}");
        }

        public void Publish(DomainEvents.PileShuffledEvent e)
        {
            _pileShuffled.OnNext(e);
            Debug.Log($"R3EventBus: PileShuffled published, Sequence={e.Sequence}, PileId={e.PileId}");
        }

        public void Publish(DomainEvents.ActionRequestedEvent e)
        {
            _actionRequested.OnNext(e);
            Debug.Log($"R3EventBus: ActionRequested published, Sequence={e.Sequence}, ActorPlayerId={e.ActorPlayerId}");
        }

        public void Publish(DomainEvents.ActionResultEvent e)
        {
            _actionResult.OnNext(e);
            Debug.Log($"R3EventBus: ActionResult published, Sequence={e.Sequence}, Accepted={e.Accepted}");
        }

        public void Publish(DomainEvents.ScanPhaseStartedEvent e)
        {
            _scanPhaseStarted.OnNext(e);
            Debug.Log($"R3EventBus: ScanPhaseStarted published, Sequence={e.Sequence}, UserPlayerId={e.UserPlayerId}");
        }

        public void Publish(DomainEvents.ScanPhaseEndedEvent e)
        {
            _scanPhaseEnded.OnNext(e);
            Debug.Log($"R3EventBus: ScanPhaseEnded published, Sequence={e.Sequence}");
        }

        public void Publish(DomainEvents.FinishingGameEvent e)
        {
            _finishingGame.OnNext(e);
            Debug.Log($"R3EventBus: FinishingGame published, Sequence={e.Sequence}, WinnerIds={string.Join(",", e.WinnerPlayerIds)}");
        }

        public void Publish(DomainEvents.GameEndedEvent e)
        {
            _gameEnded.OnNext(e);
            Debug.Log($"R3EventBus: GameEnded published, Sequence={e.Sequence}, WinnerIds={string.Join(",", e.WinnerPlayerIds)}");
        }

        /// <summary>
        /// 全てのSubjectを破棄（テスト用）
        /// </summary>
        public void Dispose()
        {
            _gameStarted.Dispose();
            _turnStarted.Dispose();
            _turnEnded.Dispose();
            _listOrderDeclared.Dispose();
            _cardMoved.Dispose();
            _cardVisibilityChanged.Dispose();
            _pileShuffled.Dispose();
            _actionRequested.Dispose();
            _actionResult.Dispose();
            _scanPhaseStarted.Dispose();
            _scanPhaseEnded.Dispose();
            _finishingGame.Dispose();
            _gameEnded.Dispose();
        }
    }

    #endregion

}


