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
    }
} 