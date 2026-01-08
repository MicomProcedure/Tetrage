namespace Tetrage.Network
{
    /// <summary>
    /// ゲームの動作モードを定義する列挙型。
    /// 起動時に決定し、GameManager.Initialize()後はロックされる。
    /// </summary>
    public enum NetworkMode
    {
        /// <summary>
        /// Photonネットワーク経由での本番プレイモード
        /// </summary>
        RealPhoton = 0,

        /// <summary>
        /// 同一プロセス内仮想通信モード（PlayMode E2Eテスト用）
        /// </summary>
        VirtualTransport = 1,

        /// <summary>
        /// DomainEvent直接注入モード（部分再現、チュートリアル用）
        /// </summary>
        LogicInjection = 2,

        /// <summary>
        /// ローカル vs ボットモード（オフライン練習用、将来実装）
        /// </summary>
        LocalVsBot = 3
    }
}

