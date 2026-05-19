namespace Tetrage.Core.Constants
{
    /// <summary>アプリケーションライフサイクル関連の定数</summary>
    public static class ApplicationConsts
    {
        /// <summary>Photon 部屋退出完了待ちのタイムアウト（秒）</summary>
        public const float PHOTON_LEAVE_ROOM_TIMEOUT_SECONDS = 10f;

        /// <summary>Photon 切断完了待ちのタイムアウト（秒）</summary>
        public const float PHOTON_DISCONNECT_TIMEOUT_SECONDS = 10f;
    }
}
