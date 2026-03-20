namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// DomainEventを直接注入するためのインターフェース。
    /// ネットワーク層をバイパスし、LogicInjectionモードで使用する。
    /// チュートリアル、部分再現、リプレイ再生に利用。
    /// </summary>
    public interface IVirtualLogicFeeder
    {
        /// <summary>
        /// DomainEventを直接EventBusに発行する
        /// </summary>
        void Feed<TEvent>(TEvent domainEvent) where TEvent : class;

        /// <summary>
        /// EventBusへの参照を取得
        /// </summary>
        IGameplayEventBus EventBus { get; }
    }
}

