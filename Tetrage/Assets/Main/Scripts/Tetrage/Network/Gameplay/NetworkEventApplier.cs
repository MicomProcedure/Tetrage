using Tetrage.Core.Ids;
using Tetrage.Core.Enums;
using Tetrage.Models;
using Tetrage.Core;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Tetrage.Network.Contracts;
namespace Tetrage.Network.Gameplay
{

    // IGameplayEventBus は GameplayEventBus.cs に統一。
    /// <summary>
    /// 受信イベントをローカルモデルへ適用する責務のクラス（Guest向け）。
    /// </summary>
    public sealed class NetworkEventApplier
    {
        private readonly IdRegistry<CardId, Card> _cardRegistry;
        private readonly IdRegistry<PileId, CardPile> _pileRegistry;
        private readonly IdRegistry<PlayerId, Player> _playerRegistry;
        private readonly IGameplayEventBus _bus;
        private readonly TurnGate _turnGate;
        private readonly IPlayerIdMapper _playerIdMapper;
        private GameContext _gameCtx;

        private int _lastSequence;

        // 受信したプレイヤーの並び順（GameStartedで確定）
        public IReadOnlyList<Player> OrderedPlayers { get; private set; }

        #region ListOrder データ型
        public readonly struct ListOrderData
        {
            public readonly ListOrderIdKind IdKind;
            public readonly int[] OrderedIds;

            public ListOrderData(ListOrderIdKind idKind, int[] orderedIds)
            {
                IdKind = idKind;
                OrderedIds = orderedIds;
            }
        }
        #endregion

        // 任意キーの並び順レジストリ（Id種別付き）
        private readonly Dictionary<ListOrderKey, ListOrderData> _listOrderRegistry = new Dictionary<ListOrderKey, ListOrderData>();

        public NetworkEventApplier(
            IdRegistry<PileId, CardPile> pileRegistry,
            IdRegistry<CardId, Card> cardRegistry,
            IdRegistry<PlayerId, Player> playerRegistry = null,
            IGameplayEventBus bus = null,
            TurnGate turnGate = null,
            GameContext context = null,
            IPlayerIdMapper playerIdMapper = null)
        {
            _pileRegistry = pileRegistry;
            _cardRegistry = cardRegistry;
            _playerRegistry = playerRegistry;
            _bus = bus;
            _turnGate = turnGate;
            _playerIdMapper = playerIdMapper;
            _lastSequence = 0;
            _gameCtx = context;
        }

        public void AttachContext(GameContext context)
        {
            _gameCtx = context;
        }

        private bool ShouldApply(int sequence)
        {
            if (sequence <= _lastSequence) return false; // 重複/古いイベントは無視
            _lastSequence = sequence;
            return true;
        }

        public void Apply(CardMovedEvent e)
        {
            Debug.Log($"NetworkEventApplier: Apply CardMoved, Sequence: {e.sequence}, FromPileId: {e.fromPileId}, ToPileId: {e.toPileId}, CardId: {e.cardId}");
            if (!ShouldApply(e.sequence)) {
                Debug.LogWarning($"NetworkEventApplier: Deny CardMoved, Sequence: {e.sequence}, FromPileId: {e.fromPileId}, ToPileId: {e.toPileId}, CardId: {e.cardId}, Not Applying");
                return;
            }
            if (!_pileRegistry.TryGet(new PileId(e.fromPileId), out var from)) {
                Debug.LogWarning($"NetworkEventApplier: Deny CardMoved, Sequence: {e.sequence}, FromPileId: {e.fromPileId}, ToPileId: {e.toPileId}, CardId: {e.cardId}, FromPile not found");
                return;
            }
            if (!_pileRegistry.TryGet(new PileId(e.toPileId), out var to)) {
                Debug.LogWarning($"NetworkEventApplier: Deny CardMoved, Sequence: {e.sequence}, FromPileId: {e.fromPileId}, ToPileId: {e.toPileId}, CardId: {e.cardId}, ToPile not found");
                return;
            }
            if (!_cardRegistry.TryGet(new CardId(e.cardId), out var card)) {
                Debug.LogWarning($"NetworkEventApplier: Deny CardMoved, Sequence: {e.sequence}, FromPileId: {e.fromPileId}, ToPileId: {e.toPileId}, CardId: {e.cardId}, Card not found");
                return;
            }
            CardPile.TransferService.Transfer(from, to, card);
            _bus?.PublishCardMoved(e);
        }

        public void Apply(CardVisibilityChangedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;
            if (!_cardRegistry.TryGet(new CardId(e.cardId), out var card)) return;
            if (card.IsVisible != e.isVisible)
            {
                card.Flip();
            }
            _bus?.PublishCardVisibilityChanged(e);
        }

        public void Apply(StartScanPhaseEvent e)
        {
            if (!ShouldApply(e.sequence)) return;
            // 空実装: フェーズ開始のUI反映などは将来追加
            _bus?.PublishStartScanPhase(e);
        }

        public void Apply(EndScanPhaseEvent e)
        {
            if (!ShouldApply(e.sequence)) return;
            // 空実装: フェーズ終了のUI反映などは将来追加
            _bus?.PublishEndScanPhase(e);
        }

        public void Apply(FinishingGameEvent e)
        {
            if (!ShouldApply(e.sequence)) return;
            _bus?.PublishFinishingGame(e);
        }

        public void Apply(GameEndedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;
            _bus?.PublishGameEnded(e);
        }

        public void Apply(ActionResultEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            switch (e.actionType)
            {
                case ActionType.Open:
                    if (e.targetCardIds != null)
                    {
                        for (int i = 0; i < e.targetCardIds.Length; i++)
                        {
                            if (_cardRegistry.TryGet(new CardId(e.targetCardIds[i]), out var card))
                            {
                                if (!card.IsVisible) card.Flip();
                            }
                        }
                    }
                    break;

                case ActionType.Reach:
                    // ActorNumber → PlayerId変換
                    if (_playerIdMapper?.TryGetPlayerId(e.actorPlayerId, out var playerId) ?? false)
                    {
                        if (_playerRegistry != null && _playerRegistry.TryGet(playerId, out var player))
                        {
                            if (!player.IsReach) player.Reach();
                        }
                    }
                    break;

                case ActionType.Check:
                    // モデル変更は不要（情報提示のみ）。UI層に委譲する場合はここにフックを追加。
                    break;

                case ActionType.TetrageSolo:
                    if (e.actionStatusInt == 1)
                    {
                        // 勝利
                        
                    }
                    else
                    {
                        // 敗北
                    }
                    break;
                case ActionType.TetrageMulti:
                    // 勝利判定の結果は別イベント（GameEnded 等）で反映する想定。
                    break;

                default:
                    // Draw/Passなど、モデル変更不要なものは無処理。
                    break;
            }
            _bus?.PublishActionResult(e);
        }

        public void Apply(PileShuffledWithSeedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;
            if (!_pileRegistry.TryGet(new PileId(e.pileId), out var pile)) return;
            // 決定論的シャッフル（同じseedで同一順序）
            pile.RandomShuffle(e.seed);
            _bus?.PublishPileShuffled(e);
        }

        /// <summary>
        /// GameStarted: プレイヤー順の確定など初期同期の適用。
        /// </summary>
        public void Apply(GameStartedEvent e)
        {
            // 初期同期のため、連番はリセットして良い
            ResetSequences();
            _gameCtx?.ResetTurnIndexInternal();

            if (e.playerActorNumbers == null || e.playerActorNumbers.Length == 0)
            {
                return;
            }

            var ordered = new List<Player>(e.playerActorNumbers.Length);
            for (int i = 0; i < e.playerActorNumbers.Length; i++)
            {
                var actorNumber = e.playerActorNumbers[i];
                // ActorNumber → PlayerId変換
                if (_playerIdMapper?.TryGetPlayerId(actorNumber, out var playerId) ?? false)
                {
                    if (_playerRegistry.TryGet(playerId, out var p))
                    {
                        ordered.Add(p);
                    }
                }
                else
                {
                    Debug.LogWarning($"NetworkEventApplier: ActorNumber {actorNumber} のマッピングが見つかりません");
                }
            }
            OrderedPlayers = ordered;
            _gameCtx?.SetPlayersInternal(ordered);
            _bus?.PublishGameStarted(e);
        }


        public void Apply(ListOrderDeclaredEvent e)
        {
            if (!ShouldApply(e.sequence)) return;
            if (e.orderedIds == null) return;
            _listOrderRegistry[e.listKey] = new ListOrderData(e.idKind, e.orderedIds);

            // プレイヤー手番の宣言であれば、OrderedPlayers も更新する
            if (_playerRegistry != null && e.idKind == ListOrderIdKind.PlayerId && e.listKey == ListOrderKey.TurnOrder)
            {
                var ordered = new List<Player>(e.orderedIds.Length);
                for (int i = 0; i < e.orderedIds.Length; i++)
                {
                    var actor = e.orderedIds[i];
                    if (_playerRegistry.TryGet(new PlayerId(actor), out var p))
                    {
                        ordered.Add(p);
                    }
                }
                if (ordered.Count > 0)
                {
                    OrderedPlayers = ordered;
                    _gameCtx?.SetPlayersInternal(ordered);
                }
            }
            _bus?.PublishListOrderDeclared(e);
        }

        #region TurnStartedEvent適用

        /// <summary>
        /// ターン開始イベントをモデルに適用
        /// </summary>
        public void Apply(TurnStartedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;

            // playerIdのスコープを事前に宣言してエラー回避
            PlayerId playerId = default;

            if (_playerIdMapper == null || !_playerIdMapper.TryGetPlayerId(e.currentPlayerActorNumber, out playerId))
            {
                Debug.LogWarning($"NetworkEventApplier: ActorNumber {e.currentPlayerActorNumber} のマッピングが見つかりません");
                return;
            }
            
            // 対応するPlayerが得られるかを確認しつつ反映
            Player player = null;
            if (_playerRegistry != null)
            {
                _playerRegistry.TryGet(playerId, out player);
            }

            // GameContextとPlayerが取得できた場合のみUI・内部状態に伝播
            if (_playerRegistry != null && _gameCtx != null && player != null)
            {
                _gameCtx.SetCurrentPlayerInternal(player);
                _gameCtx.IncrementTurnIndexInternal();
            }

            // TurnStartedの通知
            _bus?.PublishTurnStarted(e);

            // TurnGate.Releaseは現状ActorNumberで呼び出す
            _turnGate?.Release(e.currentPlayerActorNumber);
        }

        #endregion

        public void Apply(TurnEndedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;
            // 現状モデルの直接更新は不要。必要に応じてターン履歴などを更新する。
            _bus?.PublishTurnEnded(e);
        }

        public bool TryGetListOrder(ListOrderKey listKey, out int[] orderedIds)
        {
            if (_listOrderRegistry.TryGetValue(listKey, out var data))
            {
                orderedIds = data.OrderedIds;
                return true;
            }
            orderedIds = null;
            return false;
        }

        public bool TryGetListOrder(ListOrderKey listKey, out ListOrderData data)
        {
            return _listOrderRegistry.TryGetValue(listKey, out data);
        }

        public void ResetSequences()
        {
            _lastSequence = 0;
        }
    }
}


