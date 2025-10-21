using Tetrage.Core.Ids;
using Tetrage.Core.Enums;
using Tetrage.Models;
using System.Collections.Generic;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// 受信イベントをローカルモデルへ適用する責務のクラス（Guest向け）。
    /// </summary>
    public sealed class NetworkEventApplier
    {
        private readonly IdRegistry<CardId, Card> _cardRegistry;
        private readonly IdRegistry<PileId, CardPile> _pileRegistry;
        private readonly IdRegistry<PlayerId, Player> _playerRegistry;

        private int _lastSequence;

        // 受信したプレイヤーの並び順（GameStartedで確定）
        public System.Collections.Generic.IReadOnlyList<Player> OrderedPlayers { get; private set; }

        // 任意キーの並び順レジストリ
        private readonly Dictionary<string, int[]> _listOrderRegistry = new Dictionary<string, int[]>();

        public NetworkEventApplier(
            IdRegistry<PileId, CardPile> pileRegistry,
            IdRegistry<CardId, Card> cardRegistry,
            IdRegistry<PlayerId, Player> playerRegistry = null)
        {
            _pileRegistry = pileRegistry;
            _cardRegistry = cardRegistry;
            _playerRegistry = playerRegistry;
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
            if (!_pileRegistry.TryGet(new PileId(e.fromPileId), out var from)) return;
            if (!_pileRegistry.TryGet(new PileId(e.toPileId), out var to)) return;
            if (!_cardRegistry.TryGet(new CardId(e.cardId), out var card)) return;
            CardPile.TransferService.Transfer(from, to, card);
        }

        public void Apply(CardVisibilityChangedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;
            if (!_cardRegistry.TryGet(new CardId(e.cardId), out var card)) return;
            if (card.IsVisible != e.isVisible)
            {
                card.Flip();
            }
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
                            if (_cardRegistry.TryGet(e.targetCardIds[i], out var card))
                            {
                                if (!card.IsVisible) card.Flip();
                            }
                        }
                    }
                    break;

                case ActionType.Reach:
                    if (_playerRegistry != null && _playerRegistry.TryGet(new PlayerId(e.actorPlayerId), out var player))
                    {
                        if (!player.IsReach) player.Reach();
                    }
                    break;

                case ActionType.Check:
                    // モデル変更は不要（情報提示のみ）。UI層に委譲する場合はここにフックを追加。
                    break;

                case ActionType.TetrageSolo:
                case ActionType.TetrageMulti:
                    // 勝利判定の結果は別イベント（GameEnded 等）で反映する想定。
                    break;

                default:
                    // Draw/Passなど、モデル変更不要なものは無処理。
                    break;
            }
        }

        public void Apply(PileShuffledWithSeedEvent e)
        {
            if (!ShouldApply(e.sequence)) return;
            if (!_pileRegistry.TryGet(new PileId(e.pileId), out var pile)) return;
            // 決定論的シャッフル（同じseedで同一順序）
            pile.RandomShuffle(e.seed);
        }

        /// <summary>
        /// GameStarted: プレイヤー順の確定など初期同期の適用。
        /// </summary>
        public void Apply(GameStartedEvent e)
        {
            // 初期同期のため、連番はリセットして良い
            ResetSequences();

            if (e.playerActorNumbers == null || e.playerActorNumbers.Length == 0)
            {
                return;
            }

            var ordered = new List<Player>(e.playerActorNumbers.Length);
            for (int i = 0; i < e.playerActorNumbers.Length; i++)
            {
                var actor = e.playerActorNumbers[i];
                if (_playerRegistry.TryGet(new PlayerId(actor), out var p))
                {
                    ordered.Add(p);
                }
            }
            OrderedPlayers = ordered;
        }


        public void Apply(ListOrderDeclaredEvent e)
        {
            if (!ShouldApply(e.sequence)) return;
            if (string.IsNullOrEmpty(e.listKey) || e.orderedIds == null) return;
            _listOrderRegistry[e.listKey] = e.orderedIds;
        }

        public bool TryGetListOrder(string listKey, out int[] orderedIds)
        {
            return _listOrderRegistry.TryGetValue(listKey, out orderedIds);
        }

        public void ResetSequences()
        {
            _lastSequence = 0;
        }
    }
}


