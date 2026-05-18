using UnityEngine;
using Tetrage.Core.Enums;

namespace Tetrage.Core.Constants
{
    public static class InGameConsts
    {
        /// <summary>Unity組み込みタグ（シーン解決用）。マジック文字列を避ける。</summary>
        public static class UnityBuiltInTags
        {
            public const string MainCamera = "MainCamera";
        }

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
        public const string DEFAULT_PLAYER_ID = "DefaultPlayerName"; // デフォルトのプレイヤーのID
        public const int DEFAULT_PLAYER_ICON_INDEX = 0; // デフォルトのプレイヤーのアイコンインデックス
        public const int DEFAULT_GAME_PLAYER_COUNT = 4; // デフォルトのゲームのプレイヤー数

        public const int DEFAULT_DECK_ID = 1; // デフォルトのデッキのID

        /// <summary>通常表示時の CardView の sortingOrder（SpriteRenderer）。</summary>
        public const int CARD_VIEW_DEFAULT_SORTING_ORDER = 0;

        /// <summary>Tmp パイル配下に置いたときの CardView の sortingOrder。他の山より手前に出す。</summary>
        public const int CARD_VIEW_TMP_PILE_SORTING_ORDER = 10;

        /// <summary>裏向き表示時にハイライト色を SpriteRenderer.color へ乗算する係数。</summary>
        public const float CARD_VIEW_BACK_HIGHLIGHT_COLOR_MULTIPLIER = 0.3f;

        public const int DEFAULT_SE_PLAYBACK_GATE_TIME_MS = 100; // SE再生ゲートの時間(ミリ秒)

        /// <summary>カットイン演出の時間設定。</summary>
        public static class CutInAnimationDuration
        {
            public const int MILLISECONDS_PER_SECOND = 1000; // 秒からミリ秒への変換係数
            public const float ENTRY_DURATION_SECONDS = 0.5f; // カットインが入る時間
            public const float EXIT_DURATION_SECONDS = 0.3f; // カットインが抜ける時間
            public const int TETRAGE_SOLO_TOTAL_DURATION_MS = 1500; // TetrageSoloカットインの合計時間
            public const int TETRAGE_MULTI_TOTAL_DURATION_MS = 1500; // TetrageMultiカットインの合計時間
            public const int TETRAGE_REACH_TOTAL_DURATION_MS = 1500; // TetrageReachカットインの合計時間
        }

        /// <summary>
        /// TetrageMulti の ActionRequested/ActionResult で使う actionStatusInt 値。
        /// 0/1 は既存の成功/失敗と互換を保つ。
        /// </summary>
        public static class TetrageMultiStatus
        {
            /// <summary>Requester→Host: 宣言開始（targetCardIds = 選択した参加者の Target カード ID）</summary>
            public const int StartRequest = 10;
            /// <summary>Host→All: 選択プレイヤーへの参加応答要求通知</summary>
            public const int ResponseRequested = 20;
            /// <summary>Guest→Host: 参加応答 — 出す</summary>
            public const int ResponseOpen = 21;
            /// <summary>Guest→Host: 参加応答 — 出さない（明示的不参加）</summary>
            public const int ResponseDecline = 22;
            // 最終結果は既存の 0=失敗 / 1=成功 を流用
        }

        /// <summary>ScanPhase中にInGameNavigationへ表示する文言。</summary>
        public static class ScanPhaseNavigationText
        {
            public const string SelectOpponentTarget = "次に、他の人のターゲットをスキャンします。好きな人のカードをタッチしてください";
            public const string ConfirmOpponentTargetSuitFormat = "「Next」を押すと{0}Pのターゲットのスートを確認できます。";
            public const string OpponentTargetSuitRevealedFormat = "{0}Pのターゲットは{1}です。ターゲットを覚えて、そのまま「Next」を押してください";
            public const string WaitingOtherPlayers = "他のプレイヤーを待っています……";
        }
    }
}
