using R3;
using System.Collections.Generic;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Models;
using Tetrage.Network.Gameplay;
using UnityEngine;
using DomainEvents = Tetrage.Core.Events;

namespace Tetrage.Core
{
    /// <summary>
    /// DomainEventを購読し、ドメインロジック（モデル更新）を実行するハンドラ。
    /// NetworkEventApplierから責務を分離。
    /// </summary>
    public sealed class GameplayDomainEventHandler
    {
        private readonly IdRegistry<CardId, Card> _cardRegistry;
        private readonly IdRegistry<PileId, CardPile> _pileRegistry;
        private readonly IdRegistry<PlayerId, Player> _playerRegistry;
        private readonly TurnGate _turnGate;
        private readonly GameContext _gameContext;
        private CompositeDisposable _disposables = new();

        // 受信したプレイヤーの並び順（GameStartedで確定）
        public IReadOnlyList<Player> OrderedPlayers { get; private set; }

        public GameplayDomainEventHandler(
            IdRegistry<CardId, Card> cardRegistry,
            IdRegistry<PileId, CardPile> pileRegistry,
            IdRegistry<PlayerId, Player> playerRegistry,
            TurnGate turnGate,
            GameContext gameContext,
            IGameplayEventBus eventBus)
        {
            _cardRegistry = cardRegistry;
            _pileRegistry = pileRegistry;
            _playerRegistry = playerRegistry;
            _turnGate = turnGate;
            _gameContext = gameContext;

            Initialize(eventBus);
        }

        /// <summary>
        /// IGameplayEventBusからDomainEventを購読開始
        /// </summary>
        private void Initialize(IGameplayEventBus eventBus)
        {
            eventBus.GameStarted
                .Subscribe(OnGameStarted)
                .AddTo(_disposables);

            eventBus.TurnStarted
                .Subscribe(OnTurnStarted)
                .AddTo(_disposables);

            eventBus.TurnEnded
                .Subscribe(OnTurnEnded)
                .AddTo(_disposables);

            eventBus.CardMoved
                .Subscribe(OnCardMoved)
                .AddTo(_disposables);

            eventBus.CardVisibilityChanged
                .Subscribe(OnCardVisibilityChanged)
                .AddTo(_disposables);

            eventBus.PileShuffled
                .Subscribe(OnPileShuffled)
                .AddTo(_disposables);

            eventBus.ListOrderDeclared
                .Subscribe(OnListOrderDeclared)
                .AddTo(_disposables);

            eventBus.ActionResult
                .Subscribe(OnActionResult)
                .AddTo(_disposables);

            eventBus.ScanPhaseStarted
                .Subscribe(OnScanPhaseStarted)
                .AddTo(_disposables);

            eventBus.ScanPhaseEnded
                .Subscribe(OnScanPhaseEnded)
                .AddTo(_disposables);

            eventBus.FinishingGame
                .Subscribe(OnFinishingGame)
                .AddTo(_disposables);

            eventBus.GameEnded
                .Subscribe(OnGameEnded)
                .AddTo(_disposables);
        }

        /// <summary>
        /// 購読解除
        /// </summary>
        public void Dispose()
        {
            _disposables.Dispose();
        }

        #region ドメインイベントハンドラ

        private void OnGameStarted(DomainEvents.GameStartedEvent e)
        {
            // GameContextのターンインデックスをリセット
            _gameContext?.ResetTurnIndexInternal();

            if (e.PlayerIds == null || e.PlayerIds.Count == 0)
            {
                return;
            }

            // プレイヤー順序を確定
            var ordered = new List<Player>(e.PlayerIds.Count);
            foreach (var playerId in e.PlayerIds)
            {
                if (_playerRegistry.TryGet(playerId, out var player))
                {
                    ordered.Add(player);
                }
                else
                {
                    Debug.LogWarning($"GameplayDomainEventHandler: PlayerId {playerId} が見つかりません");
                }
            }

            OrderedPlayers = ordered;
            _gameContext?.SetPlayersInternal(ordered);
        }

        private void OnTurnStarted(DomainEvents.TurnStartedEvent e)
        {
            // プレイヤーを取得
            if (!_playerRegistry.TryGet(e.CurrentPlayerId, out var player))
            {
                Debug.LogWarning($"GameplayDomainEventHandler: PlayerId {e.CurrentPlayerId} が見つかりません");
                return;
            }

            // GameContextに現在プレイヤーを設定
            _gameContext?.SetCurrentPlayerInternal(player);
            _gameContext?.IncrementTurnIndexInternal();

            // TurnGate解放
            _turnGate?.Release(e.CurrentPlayerId);
        }

        private void OnTurnEnded(DomainEvents.TurnEndedEvent e)
        {
            // 現状モデルの直接更新は不要
            // 必要に応じてターン履歴などを更新
        }

        private void OnCardMoved(DomainEvents.CardMovedEvent e)
        {
            // カードとパイルを取得
            if (!_cardRegistry.TryGet(e.CardId, out var card))
            {
                Debug.LogWarning($"GameplayDomainEventHandler: CardId {e.CardId} が見つかりません");
                return;
            }

            if (!_pileRegistry.TryGet(e.FromPileId, out var fromPile))
            {
                Debug.LogWarning($"GameplayDomainEventHandler: FromPileId {e.FromPileId} が見つかりません");
                return;
            }

            if (!_pileRegistry.TryGet(e.ToPileId, out var toPile))
            {
                Debug.LogWarning($"GameplayDomainEventHandler: ToPileId {e.ToPileId} が見つかりません");
                return;
            }

            // カード移動を実行
            CardPile.TransferService.Transfer(fromPile, toPile, card);
        }

        private void OnCardVisibilityChanged(DomainEvents.CardVisibilityChangedEvent e)
        {
            if (!_cardRegistry.TryGet(e.CardId, out var card))
            {
                Debug.LogWarning($"GameplayDomainEventHandler: CardId {e.CardId} が見つかりません");
                return;
            }

            // 可視性が変更されていればFlip
            if (card.IsVisible != e.IsVisible)
            {
                card.Flip();
            }
        }

        private void OnPileShuffled(DomainEvents.PileShuffledEvent e)
        {
            if (!_pileRegistry.TryGet(e.PileId, out var pile))
            {
                Debug.LogWarning($"GameplayDomainEventHandler: PileId {e.PileId} が見つかりません");
                return;
            }

            // 決定論的シャッフル（同じseedで同一順序）
            pile.RandomShuffle(e.Seed);
        }

        private void OnListOrderDeclared(DomainEvents.ListOrderDeclaredEvent e)
        {
            // プレイヤー手番の宣言であれば、OrderedPlayersを更新
            if (e.IdKind == ListOrderIdKind.PlayerId && e.ListKey == ListOrderKey.TurnOrder)
            {
                var ordered = new List<Player>(e.OrderedIds.Count);
                foreach (var id in e.OrderedIds)
                {
                    var playerId = new PlayerId(id);
                    if (_playerRegistry.TryGet(playerId, out var player))
                    {
                        ordered.Add(player);
                    }
                }

                if (ordered.Count > 0)
                {
                    OrderedPlayers = ordered;
                    _gameContext?.SetPlayersInternal(ordered);
                }
            }
        }

        private void OnActionResult(DomainEvents.ActionResultEvent e)
        {
            switch (e.ActionType)
            {
                case ActionType.Open:
                    // カードを公開
                    if (e.TargetCardIds != null)
                    {
                        foreach (var cardId in e.TargetCardIds)
                        {
                            if (_cardRegistry.TryGet(cardId, out var card))
                            {
                                if (!card.IsVisible)
                                {
                                    card.Flip();
                                }
                            }
                        }
                    }
                    break;

                case ActionType.Reach:
                    // Reach宣言
                    if (_playerRegistry.TryGet(e.ActorPlayerId, out var player))
                    {
                        if (!player.IsReach)
                        {
                            player.Reach();
                        }
                    }
                    break;

                case ActionType.Check:
                    // モデル変更は不要（情報提示のみ）
                    break;

                case ActionType.TetrageSolo:
                    // 勝利判定の結果は別イベント（GameEnded等）で反映する想定
                    break;

                case ActionType.TetrageMulti:
                    // 勝利判定の結果は別イベント（GameEnded等）で反映する想定
                    break;

                default:
                    // Draw/Passなど、モデル変更不要なものは無処理
                    break;
            }
        }

        private void OnScanPhaseStarted(DomainEvents.ScanPhaseStartedEvent e)
        {
            // 空実装: フェーズ開始のUI反映などは将来追加
        }

        private void OnScanPhaseEnded(DomainEvents.ScanPhaseEndedEvent e)
        {
            // 空実装: フェーズ終了のUI反映などは将来追加
        }

        private void OnFinishingGame(DomainEvents.FinishingGameEvent e)
        {
            // 空実装: ゲーム終了処理開始のロジックは将来追加
        }

        private void OnGameEnded(DomainEvents.GameEndedEvent e)
        {
            // 空実装: ゲーム終了のロジックは将来追加
        }

        #endregion
    }
}

