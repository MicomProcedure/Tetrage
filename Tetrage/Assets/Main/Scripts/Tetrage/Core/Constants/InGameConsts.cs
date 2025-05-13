using UnityEngine;

namespace Tetrage.Core.Constants
{
    public static class InGameConsts
    {
        public const float DEFAULT_CARD_PILE_WIDTH = 10f;   // カード表示用Viewを置いておく幅
        public const float DEFAULT_CARD_VIEW_MIN_SPACING = 0f; // カード表示用Viewの最小間隔
        public const float DEFAULT_CARD_VIEW_MAX_SPACING = float.MaxValue; // カード表示用Viewの最大間隔
        public static readonly Vector3 DEFAULT_CARD_VIEW_POSITION_OFFSET = new Vector3(0, 0, 0); // カード表示用Viewの中心からのオフセット
    }
}
