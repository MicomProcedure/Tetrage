using Tetrage.Core.Ids;
using Tetrage.Core.Constants;

namespace Tetrage.Core.Ids
{
    /// <summary>
    /// 予約済みの PileId を定義するヘルパ。
    /// </summary>
    public static class PileIds
    {
        #region 固定パイル（グローバル）
        /// <summary>ステージの山札</summary>
        public static readonly PileId Stack = new PileId(1);
        /// <summary>捨て札</summary>
        public static readonly PileId Trash = new PileId(2);
        #endregion

        #region プレイヤー固有パイル（PlayerId.Value 由来）
        /// <summary>各プレイヤーの手札</summary>
        public static PileId PlayerHands(int playerId) => new PileId(SettingConsts.DEFAULT_CARD_PILE_ID_OFFSET + playerId);
        /// <summary>各プレイヤーのターゲット</summary>
        public static PileId PlayerTarget(int playerId) => new PileId(SettingConsts.DEFAULT_CARD_PILE_ID_OFFSET + 1000 + playerId);
        /// <summary>各プレイヤーの一時置き場</summary>
        public static PileId PlayerTmp(int playerId) => new PileId(SettingConsts.DEFAULT_CARD_PILE_ID_OFFSET + 2000 + playerId);
        #endregion
    }
}


