using System;
using System.Collections.Generic;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// リスト順序宣言イベントの共通基底。
    /// </summary>
    public abstract class ListOrderDeclaredEvent : DomainEventBase
    {
        public ListOrderIdKind IdKind { get; }
        public ListOrderKey ListKey { get; }

        protected ListOrderDeclaredEvent(
            int sequence,
            ListOrderIdKind idKind,
            ListOrderKey listKey,
            int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            IdKind = idKind;
            ListKey = listKey;
        }

        public abstract Type IdType { get; }
    }

    /// <summary>
    /// 型付きの順序宣言イベント。
    /// </summary>
    public sealed class ListOrderDeclaredEvent<TId> : ListOrderDeclaredEvent
    {
        public IReadOnlyList<TId> OrderedIds { get; }
        public override Type IdType => typeof(TId);

        public ListOrderDeclaredEvent(
            int sequence,
            ListOrderIdKind idKind,
            ListOrderKey listKey,
            IReadOnlyList<TId> orderedIds,
            int stateVersion = 0)
            : base(sequence, idKind, listKey, stateVersion)
        {
            OrderedIds = orderedIds ?? throw new ArgumentNullException(nameof(orderedIds));
        }
    }
}

