namespace Tetrage.Core.Events
{
    /// <summary>
    /// DomainEventの基底クラス。共通のsequenceとstateVersionを提供。
    /// </summary>
    public abstract class DomainEventBase
    {
        /// <summary>イベントのシーケンス番号</summary>
        public int Sequence { get; }
        
        /// <summary>状態バージョン（オプション）</summary>
        public int StateVersion { get; }

        protected DomainEventBase(int sequence, int stateVersion = 0)
        {
            Sequence = sequence;
            StateVersion = stateVersion;
        }
    }
}

