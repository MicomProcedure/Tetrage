using Tetrage.Core.Constants;

namespace Tetrage.Audio
{
    /// <summary>
    /// ジングル再生中に BGM の音量を下げ、終了後に復帰する。BGM の Stop や曲切替は行わない。
    /// </summary>
    public sealed class BgmDuckController
    {
        #region Private Fields

        private readonly BgmAudioChannel _channel;
        private bool _isDucked;
        private float _savedVolume;

        #endregion

        #region Public Methods

        public BgmDuckController(BgmAudioChannel channel)
        {
            _channel = channel;
        }

        /// <summary>
        /// 未ダック時のみ音量を保存してダックする。連打時の二重ダックを防ぐ。
        /// </summary>
        public void EnsureDucked()
        {
            if (_isDucked || _channel == null)
            {
                return;
            }

            _savedVolume = _channel.Volume;
            _channel.Volume = _savedVolume * InGameConsts.Audio.JINGLE_BGM_DUCK_VOLUME_MULTIPLIER;
            _isDucked = true;
        }

        /// <summary>
        /// ダック前の音量へ復帰する。
        /// </summary>
        public void Restore()
        {
            if (!_isDucked)
            {
                return;
            }

            if (_channel != null)
            {
                _channel.Volume = _savedVolume;
            }

            _isDucked = false;
        }

        #endregion
    }
}
