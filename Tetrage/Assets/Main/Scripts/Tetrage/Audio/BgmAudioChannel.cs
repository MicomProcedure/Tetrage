using UnityEngine;

namespace Tetrage.Audio
{
    /// <summary>
    /// BGM 用 AudioSource のループ再生・停止を担当する。
    /// </summary>
    public sealed class BgmAudioChannel : AudioChannelBase
    {
        #region Public Properties

        /// <summary>
        /// BGM AudioSource の音量。ダック制御で利用する。
        /// </summary>
        public float Volume
        {
            get => AudioSource != null ? AudioSource.volume : 0f;
            set
            {
                if (AudioSource != null)
                {
                    AudioSource.volume = value;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定クリップをループ再生する。
        /// </summary>
        public void PlayLoop(AudioClip clip)
        {
            TryPlay(clip);
        }

        /// <inheritdoc />
        public override void Stop()
        {
            base.Stop();

            if (AudioSource == null)
            {
                return;
            }

            AudioSource.clip = null;
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override void PlayClipInternal(AudioClip clip)
        {
            if (AudioSource.clip == clip && AudioSource.isPlaying)
            {
                return;
            }

            AudioSource.loop = true;
            AudioSource.clip = clip;
            AudioSource.Play();
        }

        #endregion
    }
}
