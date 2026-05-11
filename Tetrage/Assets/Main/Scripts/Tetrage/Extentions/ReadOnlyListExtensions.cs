using System.Collections.Generic;

namespace Tetrage.Extentions
{
    public static class ReadOnlyListExtensions
    {
        public static int IndexOf<T>(this IReadOnlyList<T> list, T target)
        {
            if (list == null) return -1;

            var comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < list.Count; i++)
            {
                if (comparer.Equals(list[i], target))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}