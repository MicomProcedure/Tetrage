using UnityEngine;

namespace Tetrage.Audio
{
    /// <summary>
    /// Jingle 用 AudioSource の非ループ再生を担当する。
    /// </summary>
    public sealed class JingleAudioChannel : AudioChannelBase
    {
        #region Public Properties

        /// <summary>
        /// ジングルが再生中かどうか。
        /// </summary>
        public bool IsPlaying => AudioSource != null && AudioSource.isPlaying;

        #endregion

        #region Public Methods

        /// <summary>
        /// 再生中のジングルを止めてから、指定クリップを再生する。
        /// </summary>
        public void PlayOneShot(AudioClip clip)
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
            // 前のジングルを止めてから新しいクリップを頭出し再生する
            if (AudioSource.isPlaying)
            {
                AudioSource.Stop();
            }

            AudioSource.loop = false;
            AudioSource.clip = clip;
            AudioSource.Play();
        }

        #endregion
    }
}
