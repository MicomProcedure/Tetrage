using R3;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Models;
using Tetrage.Network.Gameplay;
using UnityEngine;
using DomainEvents = Tetrage.Core.Events;
using Tetrage.Core.Contracts;
using Tetrage.Extentions;
using Cysharp.Threading.Tasks;

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
        private readonly IPlayerIdMapper _playerIdMapper;
        private readonly SequenceService _sequence;
        private readonly bool _isHost;
        private CompositeDisposable _disposables = new();
        private bool _isScanPhaseActive;
        private IReadOnlyList<PlayerId> _pendingWinnerPlayerIds = new List<PlayerId>();

        // 受信したプレイヤーの並び順（GameStartedで確定）
        public IReadOnlyList<Player> OrderedPlayers { get; private set; }

        public GameplayDomainEventHandler(
            IdRegistry<CardId, Card> cardRegistry,
            IdRegistry<PileId, CardPile> pileRegistry,
            IdRegistry<PlayerId, Player> playerRegistry,
            TurnGate turnGate,
            IGameContext gameContext,
            IPlayerIdMapper playerIdMapper,
            SequenceService sequence,
            bool isHost)
        {
            _cardRegistry = cardRegistry;
            _pileRegistry = pileRegistry;
            _playerRegistry = playerRegistry;
            _turnGate = turnGate;
            _gameContext = gameContext as GameContext;
            _playerIdMapper = playerIdMapper;
            _sequence = sequence;
            _isHost = isHost;

            Initialize(_gameContext.Events);
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

            eventBus.ListOrderDeclared
                .Select(e => e as DomainEvents.ListOrderDeclaredEvent<PlayerId>)
                .Subscribe(PlayerViewPositionUpdate)
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

            eventBus.ScanTargetSelected
                .Subscribe(OnScanTargetSelected)
                .AddTo(_disposables);

            eventBus.ScanResultReceived
                .Subscribe(OnScanResultReceived)
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
            // Debug.Log($"GameplayDomainEventHandler: OnCardMoved呼び出し - CardId={e.CardId}, FromPileId={e.FromPileId}, ToPileId={e.ToPileId}, Sequence={e.Sequence}");

            // カードとパイルを取得
            if (!_cardRegistry.TryGet(e.CardId, out var card))
            {
                var cardCount = _cardRegistry.Entries.Count();
                Debug.LogWarning($"GameplayDomainEventHandler: CardId {e.CardId} が見つかりません。レジストリ内のカード数: {cardCount}");
                // レジストリの内容をログ出力
                foreach (var entry in _cardRegistry.Entries)
                {
                    Debug.Log($"  - 登録済みCardId: {entry.Key}");
                }
                return;
            }

            if (!_pileRegistry.TryGet(e.FromPileId, out var fromPile))
            {
                var pileCount = _pileRegistry.Entries.Count();
                Debug.LogWarning($"GameplayDomainEventHandler: FromPileId {e.FromPileId} が見つかりません。レジストリ内のパイル数: {pileCount}");
                // レジストリの内容をログ出力
                foreach (var entry in _pileRegistry.Entries)
                {
                    Debug.Log($"  - 登録済みPileId: {entry.Key}");
                }
                return;
            }

            if (!_pileRegistry.TryGet(e.ToPileId, out var toPile))
            {
                var pileCount = _pileRegistry.Entries.Count();
                Debug.LogWarning($"GameplayDomainEventHandler: ToPileId {e.ToPileId} が見つかりません。レジストリ内のパイル数: {pileCount}");
                // レジストリの内容をログ出力
                foreach (var entry in _pileRegistry.Entries)
                {
                    Debug.Log($"  - 登録済みPileId: {entry.Key}");
                }
                return;
            }

            // Debug.Log($"GameplayDomainEventHandler: カード移動を実行 - Card={card}, FromPile={fromPile.Name}, ToPile={toPile.Name}");
            // カード移動を実行
            CardPile.TransferService.Transfer(fromPile, toPile, card);
            // Debug.Log($"GameplayDomainEventHandler: カード移動完了 - FromPile.Cards.Count={fromPile.Cards.Count}, ToPile.Cards.Count={toPile.Cards.Count}");
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
            if (e.ListKey == ListOrderKey.TurnOrder && e is DomainEvents.ListOrderDeclaredEvent<PlayerId> playerOrderEvent)
            {
                var ordered = new List<Player>(playerOrderEvent.OrderedIds.Count);
                foreach (var playerId in playerOrderEvent.OrderedIds)
                {
                    if (_playerRegistry.TryGet(playerId, out var player))
                    {
                        ordered.Add(player);
                    }
                    else
                    {
                        Debug.LogWarning($"GameplayDomainEventHandler: ListOrderDeclaredでPlayerId {playerId} が見つかりません");
                    }
                }

                if (ordered.Count > 0)
                {
                    OrderedPlayers = ordered;
                    _gameContext?.SetPlayersInternal(ordered);
                }
            }

            // 
            
        }

        /// <summary>
        /// ターン順序決定時にUserPlayerを中心にプレイヤーのビュー位置を更新する
        /// </summary>
        /// <param name="e">ListOrderDeclaredEvent</param>
        private async void PlayerViewPositionUpdate(DomainEvents.ListOrderDeclaredEvent e)
        {
            if (e is not DomainEvents.ListOrderDeclaredEvent<PlayerId> playerOrderEvent) return;
            Debug.Log($"<color=green>GameplayDomainEventHandler: PlayerViewPositionUpdate呼び出し - ListOrderDeclaredEvent={e}</color>");
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);  // 1F待つ

            var orderedPlayerIds = playerOrderEvent.OrderedIds;
            if (orderedPlayerIds == null || orderedPlayerIds.Count == 0) return;

            var userPlayerId = _gameContext.UserPlayer.Id;
            var rotatedPlayerIds = orderedPlayerIds.RotateFrom(userPlayerId);

            var playerViewsByPlayerId = new Dictionary<PlayerId, GameObject>();
            foreach (var playerId in orderedPlayerIds)
            {
                if (_playerRegistry.TryGet(playerId, out var player))
                {
                    var playerView = GameObject.Find($"PlayerView_{playerId.Value}");
                    if (playerView != null)
                    {
                        playerViewsByPlayerId[playerId] = playerView;
                    }
                    else
                    {
                        Debug.LogWarning($"GameplayDomainEventHandler: PlayerView_{playerId.Value} が見つかりません");
                    }
                }
            }

            // 現在のターン順の座標をスナップショット保存
            var positionsByTurnOrder = new List<Vector3>();
            for (int i = 0; i < orderedPlayerIds.Count; i++)
            {
                var sourcePlayerId = orderedPlayerIds[i];
                if (!playerViewsByPlayerId.TryGetValue(sourcePlayerId, out var sourceView))
                {
                    continue;
                }

                positionsByTurnOrder.Add(sourceView.transform.position);
            }

            // ordered の i 番目の座標を rotated の i 番目プレイヤーへ割り当てる
            for (int i = 0; i < orderedPlayerIds.Count; i++)
            {
                if (i >= positionsByTurnOrder.Count)
                {
                    break;
                }

                var targetPlayerId = rotatedPlayerIds[i];
                if (!playerViewsByPlayerId.TryGetValue(targetPlayerId, out var targetView))
                {
                    continue;
                }
                targetView.GetComponent<IPlayerView>().SetPosition(positionsByTurnOrder[i]);    // IPlayerViewのSetPositionメソッドを呼び出して位置を設定
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
            _isScanPhaseActive = true;
            Debug.Log($"GameplayDomainEventHandler: ScanPhase開始（Host={_isHost}）。対象選択は ScanPhaseUI から ScanTargetSelected を送信する。");
        }

        private void OnScanPhaseEnded(DomainEvents.ScanPhaseEndedEvent e)
        {
            _isScanPhaseActive = false;
            Debug.Log("GameplayDomainEventHandler: ScanPhase終了");
        }

        private void OnScanTargetSelected(DomainEvents.ScanTargetSelectedEvent e)
        {
            if (!_isHost) return;
            Debug.Log($"GameplayDomainEventHandler: ScanTargetSelected受信 actor={e.ActorPlayerId}, target={e.SelectedTargetPlayerId}");
        }

        private void OnScanResultReceived(DomainEvents.ScanResultReceivedEvent e)
        {
            if (_isHost) return;
            Debug.Log($"GameplayDomainEventHandler: ScanResult受信 target={e.TargetPlayerId}, suit={e.TargetSuit}");
        }

        private void OnFinishingGame(DomainEvents.FinishingGameEvent e)
        {
            _pendingWinnerPlayerIds = e.WinnerPlayerIds ?? new List<PlayerId>();
            Debug.Log($"GameplayDomainEventHandler: FinishingGame受信 勝者候補数={_pendingWinnerPlayerIds.Count}");
            // TODO(UI): リザルト演出開始前のフェード/カットインをここで開始する。
        }

        private void OnGameEnded(DomainEvents.GameEndedEvent e)
        {
            _isScanPhaseActive = false;
            _pendingWinnerPlayerIds = e.WinnerPlayerIds ?? _pendingWinnerPlayerIds;
            Debug.Log($"GameplayDomainEventHandler: GameEnded受信 勝者数={_pendingWinnerPlayerIds?.Count ?? 0}");
            // 結果UIは InGameUIManager（FinishingGame）→ ResultUI。タイトルへは ResultUI から ApplicationManager.GoToTitle。
        }

        #endregion
    }
}

