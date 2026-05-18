namespace Tetrage.Audio
{
    /// <summary>
    /// InGame の SE 再生 API。UI 等から Manager 参照経由で利用する。
    /// </summary>
    public interface ISEAudioService
    {
        bool IsInitialized { get; }

        /// <summary>
        /// 指定 ID の SE を再生する。
        /// </summary>
        void PlaySE(SEClipId id);
    }
}
  