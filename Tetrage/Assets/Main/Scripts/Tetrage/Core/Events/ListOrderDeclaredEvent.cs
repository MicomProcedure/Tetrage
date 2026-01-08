using System.Collections.Generic;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// リスト順序宣言イベント
    /// </summary>
    public sealed class ListOrderDeclaredEvent : DomainEventBase
    {
        public ListOrderIdKind IdKind { get; }
        public ListOrderKey ListKey { get; }
        public IReadOnlyList<int> OrderedIds { get; } // IDラッパーの実体値（PlayerId/CardId/PileId等）

        public ListOrderDeclaredEvent(
            int sequence,
            ListOrderIdKind idKind,
            ListOrderKey listKey,
            IReadOnlyList<int> orderedIds,
            int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            IdKind = idKind;
            ListKey = listKey;
            OrderedIds = orderedIds;
        }
    }
}

