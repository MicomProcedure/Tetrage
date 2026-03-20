using System;

namespace Tetrage.Core.DTO
{
    /// <summary>
    /// セッション内で扱うプレイヤー情報。
    /// ネットワーク実装に依存しない共通表現として利用する。
    /// </summary>
    [Serializable]
    public class SessionPlayerData
    {
        #region Public Fields
        public int SessionId { get; set; }
        public string PlayerName { get; set; }
        public int IconIndex { get; set; }
        public bool IsLocal { get; set; }
        #endregion
    }
}
