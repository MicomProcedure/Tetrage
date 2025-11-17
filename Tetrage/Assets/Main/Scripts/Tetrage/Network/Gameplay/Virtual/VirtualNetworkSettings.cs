namespace Tetrage.Network.Gameplay
{
    /// <summary>
    /// VirtualTransportの動作設定。
    /// 将来的に遅延（Latency）やパケットロス（Packet Loss）の注入機能を実装予定。
    /// 
    /// 【注意】現在は空実装。将来拡張用のプレースホルダー。
    /// </summary>
    public sealed class VirtualNetworkSettings
    {
        /// <summary>
        /// シミュレートする遅延（ミリ秒）。デフォルト: 0（即時配送）
        /// </summary>
        public int LatencyMs { get; set; } = 0;

        /// <summary>
        /// パケットロス率（0.0～1.0）。デフォルト: 0.0（ロスなし）
        /// </summary>
        public float PacketLossRate { get; set; } = 0.0f;

        /// <summary>
        /// デフォルト設定を取得
        /// </summary>
        public static VirtualNetworkSettings Default => new VirtualNetworkSettings();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public VirtualNetworkSettings()
        {
            // 将来実装: 遅延注入、パケットロス注入の初期化
        }
    }
}

