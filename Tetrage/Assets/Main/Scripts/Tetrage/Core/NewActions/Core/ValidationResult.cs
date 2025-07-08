namespace Tetrage.Core.Actions
{
    /// <summary>
    /// 検証結果を表現するクラス
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 検証が成功したかどうか
        /// </summary>
        public bool IsValid { get; }
        
        /// <summary>
        /// 失敗理由（検証失敗時）
        /// </summary>
        public string FailureReason { get; }
        
        private ValidationResult(bool isValid, string failureReason = null)
        {
            IsValid = isValid;
            FailureReason = failureReason;
        }
        
        /// <summary>
        /// 検証成功結果を作成
        /// </summary>
        public static ValidationResult Valid()
        {
            return new ValidationResult(true);
        }
        
        /// <summary>
        /// 検証失敗結果を作成
        /// </summary>
        public static ValidationResult Invalid(string reason)
        {
            return new ValidationResult(false, reason);
        }
    }
} 