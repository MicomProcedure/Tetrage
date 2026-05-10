using R3;
using System;
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
using Tetrage.Services;

namespace Tetrage.Core
{
    /// <summary>
    /// DomainEventを購読し、ドメインロジック（モデル更新）を実行するハンドラ。
    /// NetworkEventApplierから責務を分離。
    /// </summary>
    public sealed class GameplayDomainEventHandler : IDisposable
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
                .Select(e => e as DomainEvents.ListOrderDeclaredEvent<PlayerId>)
                .Subscribe(PlayerOrderUpdate)
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
            // // GameContextのターンインデックスをリセット
            // _gameContext?.ResetTurnIndexInternal();

            // if (e.PlayerIds == null || e.PlayerIds.Count == 0)
            // {
            //     return;
            // }

            // // プレイヤー順序を確定
            // var ordered = new List<Player>(e.PlayerIds.Count);
            // foreach (var playerId in e.PlayerIds)
            // {
            //     if (_playerRegistry.TryGet(playerId, out var player))
            //     {
            //         ordered.Add(player);
            //     }
            //     else
            //     {
            //         Debug.LogWarning($"GameplayDomainEventHandler: PlayerId {playerId} が見つかりません");
            //     }
            // }

            // OrderedPlayers = ordered;
            // _gameContext?.SetPlayersInternal(ordered);
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
            // カード移動が失敗した場合は、以降の表示状態更新も行わない。
            if (!CardPile.TransferService.Transfer(fromPile, toPile, card))
            {
                return;
            }

            UpdateFaceUpStateForTmpTransition(fromPile, toPile, card);

            if (_gameContext.UserPlayer != null)
            {
                var userTmpPileId = PileIds.PlayerTmp(_gameContext.UserPlayer.Id);
                var userHandsPileId = PileIds.PlayerHands(_gameContext.UserPlayer.Id);

                // UserPlayerのHandsに入ったときはスート可視を有効化
                if (toPile.Id.Equals(userHandsPileId))
                {
                    card.SetSuitVisible(true);
                }

                // UserPlayerのHandsから出るときはスート可視を無効化
                if (fromPile.Id.Equals(userHandsPileId) && !toPile.Id.Equals(userHandsPileId))
                {
                    card.SetSuitVisible(false);
                }

                Debug.Log(
                    $"<color=red>GameplayDomainEventHandler: カード移動完了 - Card={card}, FromPile={fromPile.Name}, ToPile={toPile.Name}, UserPlayerTmp={userTmpPileId}, UserPlayerHands={userHandsPileId}");
            }
        }

        private void OnCardVisibilityChanged(DomainEvents.CardSideChangedEvent e)
        {
            if (!_cardRegistry.TryGet(e.CardId, out var card))
            {
                Debug.LogWarning($"GameplayDomainEventHandler: CardId {e.CardId} が見つかりません");
                return;
            }

            // 可視性が変更されていればFlip
            if (card.IsFaceUp != e.IsFaceUp)
            {
                card.Flip();
            }
        }

        #region Tmp FaceUp Control
        /// <summary>
        /// Tmp 入退場時の表裏状態を更新します。
        /// </summary>
        private static void UpdateFaceUpStateForTmpTransition(CardPile from, CardPile to, Card card)
        {
            var isMovingIntoTmp = !IsTmpPile(from) && IsTmpPile(to);
            if (isMovingIntoTmp)
            {
                // Tmp に入るカードは常に表向きにする。
                SetFaceUp(card, isFaceUp: true);
                return;
            }

            var isMovingOutFromTmp = IsTmpPile(from) && !IsTmpPile(to);
            if (isMovingOutFromTmp)
            {
                // Tmp から出るカードは常に裏向きにする。
                SetFaceUp(card, isFaceUp: false);
            }
        }

        /// <summary>
        /// カードの表裏が指定値と異なる場合だけ反転します。
        /// </summary>
        private static void SetFaceUp(Card card, bool isFaceUp)
        {
            if (card.IsFaceUp == isFaceUp)
            {
                return;
            }

            card.Flip();
        }

        /// <summary>
        /// 指定したパイルが Tmp かどうかを判定します。
        /// </summary>
        private static bool IsTmpPile(CardPile pile)
        {
            return pile != null && pile.Type == CardPileType.Tmp;
        }
        #endregion

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

        private void PlayerOrderUpdate(DomainEvents.ListOrderDeclaredEvent e)
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

            // プレイヤー順序が更新されたら、プレイヤーのビュー位置を更新する
            PlayerViewPositionUpdate(e);
            
        }

        /// <summary>
        /// ターン順序決定時にUserPlayerを中心にプレイヤーのビュー位置を更新する
        /// </summary>
        /// <param name="e">ListOrderDeclaredEvent</param>
        private void PlayerViewPositionUpdate(DomainEvents.ListOrderDeclaredEvent e)
        {
            if (e is not DomainEvents.ListOrderDeclaredEvent<PlayerId> playerOrderEvent)
            {
                Debug.LogError($"GameplayDomainEventHandler: ListOrderDeclaredEvent が PlayerId ではありません");
                return;
            }

            Debug.Log($"<color=green>GameplayDomainEventHandler: PlayerViewPositionUpdate呼び出し - ListOrderDeclaredEvent={e}</color>");

            var orderedPlayerIds = playerOrderEvent.OrderedIds;
            if (orderedPlayerIds == null || orderedPlayerIds.Count == 0)
            {
                Debug.LogError($"GameplayDomainEventHandler: OrderedPlayerIds が null または空です");
                return;
            }

            var userPlayerId = _gameContext.UserPlayer.Id;
            if (!orderedPlayerIds.Contains(userPlayerId))
            {
                Debug.LogError($"GameplayDomainEventHandler: UserPlayerId {userPlayerId} が ListOrderDeclaredEvent に含まれていません");
                return;
            }

            // orderedPlayerIds を userPlayerId を中心に回転させる（ユーザーがindex 0 になるように）
            var rotatedPlayerIds = orderedPlayerIds.RotateFrom(userPlayerId);
            Debug.Log(
                $"<color=green>GameplayDomainEventHandler: TurnOrder解析 ordered=[{string.Join(",", orderedPlayerIds.Select(id => id.Value))}] " +
                $"rotated=[{string.Join(",", rotatedPlayerIds.Select(id => id.Value))}] user={userPlayerId.Value}</color>");

            var playerViewsByPlayerId = new Dictionary<PlayerId, GameObject>(orderedPlayerIds.Count);
            for (int i = 0; i < orderedPlayerIds.Count; i++)
            {
                var playerId = orderedPlayerIds[i];
                var playerView = GameObject.Find($"PlayerView_{playerId.Value}");
                if (playerView == null)
                {
                    Debug.LogWarning($"GameplayDomainEventHandler: PlayerView_{playerId.Value} がシーンから見つかりません");
                    return;
                }

                playerViewsByPlayerId[playerId] = playerView;
            }

            // 再配置前の各PlayerViewの位置・兄弟順を記録する（UIとの突き合わせ用）。
            var beforeStates = new List<string>(orderedPlayerIds.Count);
            for (int i = 0; i < orderedPlayerIds.Count; i++)
            {
                var playerId = orderedPlayerIds[i];
                var transform = playerViewsByPlayerId[playerId].transform;
                beforeStates.Add(
                    $"P{playerId.Value}:sib={transform.GetSiblingIndex()},local={transform.localPosition},world={transform.position}");
            }
            Debug.Log($"GameplayDomainEventHandler: PlayerView再配置前 {string.Join(" | ", beforeStates)}");

            var reorderedPlayerViews = new List<GameObject>(rotatedPlayerIds.Count);
            for (int i = 0; i < rotatedPlayerIds.Count; i++)
            {
                var targetPlayerId = rotatedPlayerIds[i];
                if (!playerViewsByPlayerId.TryGetValue(targetPlayerId, out var targetView))
                {
                    Debug.LogError($"GameplayDomainEventHandler: PlayerView_{targetPlayerId.Value} の対応が見つかりません");
                    return;
                }

                reorderedPlayerViews.Add(targetView);
            }

            // 兄弟順スロットのローカル配置を、rotated順のPlayerViewへ再割り当てする。
            if (!TransformReorderPlacementService.TryReassignLocalPlacementsBySiblingOrder(reorderedPlayerViews))
            {
                Debug.LogError("GameplayDomainEventHandler: PlayerView位置の再割り当てに失敗しました");
            }
            else
            {
                var afterStates = new List<string>(orderedPlayerIds.Count);
                for (int i = 0; i < orderedPlayerIds.Count; i++)
                {
                    var playerId = orderedPlayerIds[i];
                    var transform = playerViewsByPlayerId[playerId].transform;
                    afterStates.Add(
                        $"P{playerId.Value}:sib={transform.GetSiblingIndex()},local={transform.localPosition},world={transform.position}");
                }

                Debug.Log($"GameplayDomainEventHandler: PlayerView再配置後 {string.Join(" | ", afterStates)}");
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
                                if (!card.IsFaceUp)
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
            Debug.Log($"GameplayDomainEventHandler: ScanPhase開始（Host={_isHost}）。対象選択は ScanPhaseUI から ScanTargetSelected を送信する。");
        }

        private void OnScanPhaseEnded(DomainEvents.ScanPhaseEndedEvent e)
        {
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
            _pendingWinnerPlayerIds = e.WinnerPlayerIds ?? _pendingWinnerPlayerIds;
            Debug.Log($"GameplayDomainEventHandler: GameEnded受信 勝者数={_pendingWinnerPlayerIds?.Count ?? 0}");
            // 結果UIは InGameUIManager（FinishingGame）→ ResultUI。タイトルへは ResultUI から ApplicationManager.GoToTitle。
        }

        #endregion
    }
}

