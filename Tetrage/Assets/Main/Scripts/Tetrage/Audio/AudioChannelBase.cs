using UnityEngine;

namespace Tetrage.Audio
{
    /// <summary>
    /// AudioSource を持つ音声チャンネルの基底クラス。再生・停止の共通処理を提供する。
    /// </summary>
    public abstract class AudioChannelBase : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private AudioSource _audioSource;

        #endregion

        #region Protected Properties

        protected AudioSource AudioSource => _audioSource;

        #endregion

        #region Public Methods

        /// <summary>
        /// クリップを再生する。再生できた場合は true。
        /// </summary>
        public bool TryPlay(AudioClip clip)
        {
            if (!CanPlayClip(clip))
            {
                return false;
            }

            PlayClipInternal(clip);
            return true;
        }

        /// <summary>
        /// 再生を停止する。
        /// </summary>
        public virtual void Stop()
        {
            if (_audioSource == null)
            {
                return;
            }

            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }

        /// <summary>
        /// Inspector 参照の設定漏れを検証する。
        /// </summary>
        public virtual bool ValidateReferences()
        {
            if (_audioSource != null)
            {
                return true;
            }

            Debug.LogWarning($"{GetType().Name}: AudioSource が未設定です。", this);
            return false;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 派生クラスでクリップの再生方式を実装する。
        /// </summary>
        protected abstract void PlayClipInternal(AudioClip clip);

        /// <summary>
        /// クリップ再生可能か判定する。
        /// </summary>
        protected bool CanPlayClip(AudioClip clip)
        {
            return _audioSource != null && clip != null;
        }

        #endregion
    }
}
