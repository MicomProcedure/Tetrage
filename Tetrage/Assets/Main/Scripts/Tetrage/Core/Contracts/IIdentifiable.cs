namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// 一意の識別子を持つオブジェクトのためのインターフェース
    /// </summary>
    /// <typeparam name="TId">識別子の型</typeparam>
    public interface IIdentifiable<TId>
    {
        /// <summary>
        /// 一意の識別子
        /// </summary>
        TId Id { get; }
    }
}