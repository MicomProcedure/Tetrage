using System.Collections.Generic;
using R3;
using Tetrage.Core.Ids;

namespace Tetrage.UI
{
    /// <summary>
    /// TetrageMulti フェーズの UI 入力をロジック層へ橋渡しするディスパッチャ。
    /// - 宣言者: 選択開始通知 / 共同プレイヤー選択確定を通知
    /// - 被選択者: 出す/出さない応答を通知
    /// </summary>
    public static class TetrageMultiDispatcher
    {
        #region 宣言者 — プレイヤー選択開始

        private static readonly Subject<Unit> _selectionStarted = new();

        /// <summary>
        /// 宣言者がプレイヤー選択フェーズに入ったときに発行されるストリーム。
        /// UI 側はこれを受けて「確定」ボタンを表示する。
        /// </summary>
        public static Observable<Unit> SelectionStarted => _selectionStarted;

        /// <summary>宣言者がプレイヤー選択を開始したことを通知する。</summary>
        public static void PublishSelectionStarted()
            => _selectionStarted.OnNext(Unit.Default);

        #endregion

        #region 宣言者 — 対象プレイヤー選択確定

        private static readonly Subject<IReadOnlyList<PlayerId>> _selectionConfirmed = new();

        /// <summary>
        /// 宣言者が共同プレイヤーを確定したときに発行されるストリーム。
        /// 値 = 選択したプレイヤーの PlayerId 一覧。
        /// </summary>
        public static Observable<IReadOnlyList<PlayerId>> SelectionConfirmed => _selectionConfirmed;

        /// <summary>宣言者が共同プレイヤー選択を確定したことを通知する。</summary>
        public static void PublishSelectionConfirmed(IReadOnlyList<PlayerId> selectedPlayerIds)
            => _selectionConfirmed.OnNext(selectedPlayerIds);

        #endregion

        #region 被選択者 — 参加応答

        private static readonly Subject<bool> _responseConfirmed = new();

        /// <summary>
        /// 被選択者が参加応答を選択したときに発行されるストリーム。
        /// 値 = true（出す）/ false（出さない）。
        /// </summary>
        public static Observable<bool> ResponseConfirmed => _responseConfirmed;

        /// <summary>被選択者が応答を確定したことを通知する。</summary>
        public static void PublishResponseConfirmed(bool open)
            => _responseConfirmed.OnNext(open);

        #endregion
    }
}
