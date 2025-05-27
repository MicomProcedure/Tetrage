using UnityEngine;
using Tetrage.Core.Enums;

namespace Tetrage.Core.Constants
{
    public static class InGameConsts
    {
        public const float DEFAULT_CARD_PILE_WIDTH = 10f;   // カード表示用Viewを置いておく幅
        public const float DEFAULT_CARD_VIEW_MIN_SPACING = 0f; // カード表示用Viewの最小間隔
        public const float DEFAULT_CARD_VIEW_MAX_SPACING = float.MaxValue; // カード表示用Viewの最大間隔
        public static readonly Vector3 DEFAULT_CARD_VIEW_POSITION_OFFSET = new Vector3(0, 0, 0); // カード表示用Viewの中心からのオフセット

        public static readonly Suit[] DEFAULT_INITIAL_SUITS = new[] { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club }; // デフォルトの初期カード生成スート
        public const int DEFAULT_INITIAL_COUNT_PER_SUIT = 13; // デフォルトの初期カード生成数
        public const int DEFAULT_CARD_PILE_CAPACITY = 52; // デフォルトのカードパイルの容量

        public const int DEFAULT_PLAYER_HAND_CAPACITY = 3; // デフォルトのプレイヤーの手札の容量
        public const int DEFAULT_PLAYER_TMP_CAPACITY = 2; // デフォルトのプレイヤーの一時的に保持しているカードの容量
        public const int DEFAULT_PLAYER_TARGET_CAPACITY = 1; // デフォルトのプレイヤーのターゲットの容量
        public const string DEFAULT_PLAYER_ID = "DefaultPlayer"; // デフォルトのプレイヤーのID
    }
}
