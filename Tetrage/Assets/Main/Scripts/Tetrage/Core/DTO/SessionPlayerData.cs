using System;

namespace Tetrage.Core.DTO
{
    /// <summary>
    /// セッション内で扱うプレイヤー情報。
    /// ネットワーク実装に依存しない共通表現として利用する。
    /// </summary>
    [Serializable]
    public record SessionPlayerData
    {
        #region Public Fields
        public int SessionId { get; init; }
        public string PlayerName { get; init; }
        public int IconIndex { get; init; }
        public bool IsLocal { get; init; }
        #endregion
    }
}
