using System.Collections.Generic;

namespace Tetrage.Core.Enums
{
    /// <summary>
    /// Suit enumの拡張メソッド
    /// </summary>
    public static class SuitExtensions
    {
        #region カスタム比較順序定義

        /// <summary>
        /// カスタム比較用の順序マッピング
        /// 必要に応じて順序を変更可能
        /// </summary>
        private static readonly Dictionary<Suit, int> CustomOrder = new Dictionary<Suit, int>
        {
            { Suit.Spade, 0 },
            { Suit.Heart, 1 },
            { Suit.Diamond, 2 },
            { Suit.Club, 3 }
        };

        #endregion

        #region 比較メソッド

        /// <summary>
        /// カスタム順序でSuitを比較する
        /// </summary>
        /// <param name="suit">比較元のスート</param>
        /// <param name="other">比較先のスート</param>
        /// <returns>比較結果（-1: より小さい, 0: 等しい, 1: より大きい）</returns>
        public static int CompareToCustom(this Suit suit, Suit other)
        {
            return CustomOrder[suit].CompareTo(CustomOrder[other]);
        }

        /// <summary>
        /// トランプの一般的な順序でSuitを比較する
        /// スペード < ハート < ダイヤ < クラブ
        /// </summary>
        /// <param name="suit">比較元のスート</param>
        /// <param name="other">比較先のスート</param>
        /// <returns>比較結果</returns>
        public static int CompareByTrumpOrder(this Suit suit, Suit other)
        {
            return suit.CompareTo(other); // デフォルトのenum順序を使用
        }


        #endregion

        #region ユーティリティメソッド

        /// <summary>
        /// スートの表示用シンボルを取得する
        /// </summary>
        /// <param name="suit">対象のスート</param>
        /// <returns>スートシンボル</returns>
        public static string GetSymbol(this Suit suit) => suit switch
        {
            Suit.Spade => "♠",
            Suit.Heart => "♥",
            Suit.Diamond => "♦",
            Suit.Club => "♣",
            _ => "?"
        };

        /// <summary>
        /// スートの色を取得する
        /// </summary>
        /// <param name="suit">対象のスート</param>
        /// <returns>true: 赤色, false: 黒色</returns>
        public static bool IsRed(this Suit suit) => suit == Suit.Heart || suit == Suit.Diamond;

        /// <summary>
        /// スートの色を取得する
        /// </summary>
        /// <param name="suit">対象のスート</param>
        /// <returns>true: 黒色, false: 赤色</returns>
        public static bool IsBlack(this Suit suit) => !suit.IsRed();

        #endregion
    }
}