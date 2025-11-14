using System.Collections.Generic;
using Tetrage.Core.Ids;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// ゲーム終了処理開始イベント
    /// </summary>
    public sealed class FinishingGameEvent : DomainEventBase
    {
        public IReadOnlyList<PlayerId> WinnerPlayerIds { get; } // -1相当の場合は空リスト

        public FinishingGameEvent(int sequence, IReadOnlyList<PlayerId> winnerPlayerIds, int stateVersion = 0)
            : base(sequence, stateVersion)
        {
            WinnerPlayerIds = winnerPlayerIds;
        }
    }
}

