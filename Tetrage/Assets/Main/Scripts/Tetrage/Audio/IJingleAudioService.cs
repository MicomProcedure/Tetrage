namespace Tetrage.Audio
{
    /// <summary>
    /// Jingle 再生 API。UI 等から Manager 参照経由で利用する。
    /// </summary>
    public interface IJingleAudioService
    {
        /// <summary>
        /// 指定 ID の Jingle を再生する。
        /// </summary>
        void PlayJingle(JingleClipId id);
    }
}
  