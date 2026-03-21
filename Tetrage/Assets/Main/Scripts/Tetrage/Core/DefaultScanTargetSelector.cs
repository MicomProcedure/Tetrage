using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;

namespace Tetrage.Core
{
    /// <summary>
    /// 既定の偵察対象選択実装。先頭候補を自動選択する。
    /// </summary>
    public sealed class DefaultScanTargetSelector : IScanTargetSelector
    {
        public UniTask<IPlayer> SelectTargetAsync(IPlayer scanner, IReadOnlyList<IPlayer> candidates)
        {
            if (scanner == null)
            {
                throw new ArgumentNullException(nameof(scanner));
            }

            if (candidates == null || candidates.Count == 0)
            {
                throw new InvalidOperationException("偵察対象候補が存在しません。");
            }

            return UniTask.FromResult(candidates[0]);
        }
    }
}
