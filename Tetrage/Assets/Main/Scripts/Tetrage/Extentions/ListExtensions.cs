using System;
using System.Collections.Generic;

namespace Tetrage.Extentions
{
    public static class ListExtensions
    {
        public static List<T> RotateFromIndex<T>(this IReadOnlyList<T> list, int startIndex)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0) return new List<T>();

            int count = list.Count;

            // 範囲外でも回転として扱う（負数も対応）
            startIndex = ((startIndex % count) + count) % count;

            var result = new List<T>(count);

            for (int i = 0; i < count; i++)
            {
                int index = (startIndex + i) % count;
                result.Add(list[index]);
            }

            return result;
        }

        public static List<T> RotateFrom<T>(this IReadOnlyList<T> list, T target)
        {
            return list.RotateFromIndex(list.IndexOf(target));
        }
    }
}