using System;
using System.Collections.Generic;
using Tetrage.Core.Ids;

namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// IIdentifiable 向けの並び替えユーティリティ。
    /// orderedIds は int 実体（PlayerId/CardId/PileId などの中身）を想定。
    /// </summary>
    public static class ListOrderUtil
    {
        #region 汎用: 任意のID型 -> int 実体
        public static void ApplyOrderInPlace<TItem, TId>(
            List<TItem> items,
            int[] orderedIds,
            Func<TItem, TId> getId,
            Func<TId, int> idToInt
        )
        {
            if (items == null || orderedIds == null || getId == null || idToInt == null) return;

            var idToIndex = new Dictionary<int, int>(orderedIds.Length);
            for (int i = 0; i < orderedIds.Length; i++)
            {
                idToIndex[orderedIds[i]] = i;
            }

            items.Sort((a, b) =>
            {
                var aInt = idToInt(getId(a));
                var bInt = idToInt(getId(b));
                var aHas = idToIndex.TryGetValue(aInt, out var aIdx);
                var bHas = idToIndex.TryGetValue(bInt, out var bIdx);
                if (aHas && bHas) return aIdx.CompareTo(bIdx);
                if (aHas) return -1;
                if (bHas) return 1;
                return 0;
            });
        }
        #endregion

        #region 便利オーバーロード: int Id
        public static void ApplyOrderInPlace<TItem>(
            List<TItem> items,
            int[] orderedIds,
            Func<TItem, int> getId
        )
        {
            if (getId == null) return;
            ApplyOrderInPlace(items, orderedIds, getId, id => id);
        }
        #endregion

        #region 便利オーバーロード: PlayerId/CardId/PileId
        public static void ApplyOrderByPlayerId<TItem>(
            List<TItem> items,
            int[] orderedIds,
            Func<TItem, PlayerId> getId
        )
        {
            ApplyOrderInPlace(items, orderedIds, getId, id => id.Value);
        }

        public static void ApplyOrderByCardId<TItem>(
            List<TItem> items,
            int[] orderedIds,
            Func<TItem, CardId> getId
        )
        {
            ApplyOrderInPlace(items, orderedIds, getId, id => id.Value);
        }

        public static void ApplyOrderByPileId<TItem>(
            List<TItem> items,
            int[] orderedIds,
            Func<TItem, PileId> getId
        )
        {
            ApplyOrderInPlace(items, orderedIds, getId, id => id.Value);
        }
        #endregion
    }
}


