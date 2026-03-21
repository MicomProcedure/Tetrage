using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// ScanPhase で偵察対象プレイヤーを選択する抽象インターフェース。
    /// </summary>
    public interface IScanTargetSelector
    {
        UniTask<IPlayer> SelectTargetAsync(IPlayer scanner, IReadOnlyList<IPlayer> candidates);
    }
}
