using UnityEngine;

namespace Tetrage.Core.Actions
{
    /// <summary>
    /// アクション実行結果を表現するクラス
    /// </summary>
    public class ActionResult
    {
        /// <summary>
        /// 実行が成功したかどうか
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// エラーメッセージ（失敗時）
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// アクション実行時の詳細情報
        /// </summary>
        public object AdditionalData { get; }

        private ActionResult(bool isSuccess, string errorMessage = null, object additionalData = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            AdditionalData = additionalData;
        }

        /// <summary>
        /// 成功結果を作成
        /// </summary>
        public static ActionResult Success(object additionalData = null)
        {
            return new ActionResult(true, additionalData: additionalData);
        }

        /// <summary>
        /// 失敗結果を作成
        /// </summary>
        public static ActionResult Failure(string errorMessage, object additionalData = null)
        {
            return new ActionResult(false, errorMessage, additionalData);
        }

        #region ログ出力ユーティリティ
        /// <summary>
        /// ActionResult の内容を Unity コンソールに出力する
        /// </summary>
        /// <param name="prefix">ログメッセージの接頭辞（任意）</param>
        public void Log(string prefix = "")
        {
            #if UNITY_EDITOR
            var head = string.IsNullOrEmpty(prefix) ? "" : $"{prefix}: ";

            if (IsSuccess)
            {
                Debug.Log($"{head}Action 成功 - 追加情報: {AdditionalData ?? "なし"}");
            }
            else
            {
                Debug.LogWarning($"{head}Action 失敗 - エラー: {ErrorMessage ?? "不明"}, 追加情報: {AdditionalData ?? "なし"}");
            }
            #endif
        }
        #endregion
    }
}