using System;

namespace Tetrage.Core.Ids
{
    /// <summary>
    /// CardId の合成/分解ヘルパ。
    /// deck(8bit) | suit(8bit) | number(8bit) の24bitで表現します。
    /// </summary>
    public static class CardIdComposer
    {
        #region Compose
        /// <summary>
        /// DeckId・スートインデックス・数字から決定論的に CardId を合成します。
        /// </summary>
        public static CardId Compose(DeckId deckId, int suitIndex, int number)
        {
            int d = deckId.Value & 0xFF;
            int s = suitIndex & 0xFF;
            int n = Math.Max(1, number) & 0xFF;
            int v = (d << 16) | (s << 8) | n;
            return new CardId(v);
        }
        #endregion

        #region Decompose
        /// <summary>
        /// CardId を (deckId, suitIndex, number) に分解します。
        /// </summary>
        public static void Decompose(CardId cardId, out int deckId, out int suitIndex, out int number)
        {
            int v = cardId.Value;
            deckId = (v >> 16) & 0xFF;
            suitIndex = (v >> 8) & 0xFF;
            number = v & 0xFF;
        }
        #endregion
    }
}


