using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Constants;
using UnityEngine;

namespace Tetrage.Audio
{
    /// <summary>
    /// SE 用 AudioSource の再生を担当する。
    /// </summary>
    public sealed class SEAudioChannel : AudioChannelBase
    {
        #region Private Fields

        private readonly Dictionary<InGameSEId, bool> _playbackGate = new();
        private int _sePlaybackGateTimeMS = InGameConsts.DEFAULT_SE_PLAYBACK_GATE_TIME_MS;

        #endregion

        #region Public Methods

        /// <summary>
        /// 同時再生を抑制する SE ID を登録する。
        /// </summary>
        public void ConfigureGatedSeIds(IEnumerable<InGameSEId> gatedIds)
        {
            _playbackGate.Clear();
            foreach (var id in gatedIds)
            {
                _playbackGate[id] = true;
            }
        }

        /// <summary>
        /// ゲート付きで SE を再生する。再生できた場合は true。
        /// </summary>
        public bool TryPlayGated(InGameSEId id, AudioClip clip)
        {
            // ゲートが開いていない場合は再生を抑制（同時再生防止）
            if (!_playbackGate.TryGetValue(id, out var isOpen) || !isOpen)
            {
                return false;
            }

            if (!TryPlay(clip)) // Clip再生を試みる
            {
                return false;
            }

            _playbackGate[id] = false;
            UniTask.Delay(_sePlaybackGateTimeMS)  // 設定したゲート時間だけ待ってからゲートを開放
                .ContinueWith(() => _playbackGate[id] = true)
                .Forget();
            return true;
        }

        public void SetSEPlaybackGateTimeMS(int timeMS)
        {
            _sePlaybackGateTimeMS = timeMS;
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override void PlayClipInternal(AudioClip clip)
        {
            AudioSource.PlayOneShot(clip);
        }

        #endregion
    }
}
