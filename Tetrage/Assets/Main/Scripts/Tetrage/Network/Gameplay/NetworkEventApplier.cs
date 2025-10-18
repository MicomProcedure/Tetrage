using Tetrage.Core.Ids;
using Tetrage.Core.Enums;
using Tetrage.Models;

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

        public void ResetSequences()
        {
            _lastSequence = 0;
        }
    }
}


