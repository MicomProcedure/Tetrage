using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Tetrage.Audio
{
    /// <summary>
    /// ジングル再生と BGM ダック・復帰のオーケストレーション。連打時は前の再生を打ち切る。
    /// </summary>
    public sealed class InGameJinglePlayer
    {
        #region Private Fields

        private readonly BgmDuckController _duckController;
        private readonly JingleAudioChannel _jingleChannel;
        private readonly InGameAudioCatalog _catalog;
        private readonly CancellationToken _destroyToken;
        private int _generation;
        private CancellationTokenSource _playCts;

        #endregion

        #region Public Methods

        public InGameJinglePlayer(
            BgmDuckController duckController,
            JingleAudioChannel jingleChannel,
            InGameAudioCatalog catalog,
            CancellationToken destroyToken)
        {
            _duckController = duckController;
            _jingleChannel = jingleChannel;
            _catalog = catalog;
            _destroyToken = destroyToken;
        }

        /// <summary>
        /// 指定 ID のジングルを再生する（非同期・BGM ダック付き）。
        /// </summary>
        public void PlayJingle(JingleClipId id)
        {
            PlayJingleAsync(id).Forget();
        }

        /// <summary>
        /// 再生中のジングルとダック待機を打ち切る。
        /// </summary>
        public void Cancel()
        {
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = null;
            _jingleChannel?.Stop();
            _duckController?.Restore();
        }

        #endregion

        #region Private Methods

        private void CancelPreviousPlayback()
        {
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = null;
        }

        private async UniTaskVoid PlayJingleAsync(JingleClipId id)
        {
            var generation = ++_generation;
            CancelPreviousPlayback();

            _playCts = CancellationTokenSource.CreateLinkedTokenSource(_destroyToken);
            var token = _playCts.Token;

            var clip = _catalog.GetAudioClip(id);
            if (clip == null)
            {
                return;
            }

            _duckController.EnsureDucked();

            try
            {
                if (!_jingleChannel.TryPlay(clip))
                {
                    return;
                }

                var delayMs = _catalog.GetAudioClipLengthMS(id);
                if (delayMs > 0)
                {
                    // クリップ長に合わせてダックを維持する
                    await UniTask.Delay(delayMs, cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
                // 連打による打ち切り
            }
            finally
            {
                // 最新の再生セッションのみがダックを解除する
                if (generation == _generation)
                {
                    _duckController.Restore();
                }
            }
        }

        #endregion
    }
}
