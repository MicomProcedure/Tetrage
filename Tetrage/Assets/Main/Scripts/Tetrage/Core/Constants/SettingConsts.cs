namespace Tetrage.Core.Constants
{
    public static class SettingConsts
    {
        public const int DEFAULT_CARD_PILE_ID_OFFSET = 1000;
        public const int MAX_PLAYER_COUNT = 6;
        public const int MIN_PLAYER_COUNT = 3;

        public const float SCAN_SELECTION_TIMEOUT_SECONDS = 300f;

        /// <summary>TetrageMulti の提出応答タイムアウト（秒）。未応答は親=提出・子=提出しない。</summary>
        public const float TETRAGE_MULTI_RESPONSE_TIMEOUT_SECONDS = 60f;
    }
}