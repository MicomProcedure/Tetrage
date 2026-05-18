namespace Tetrage.Audio
{
    /// <summary>
    /// InGame BGM の Reach 前後切り替え状態を管理する。
    /// </summary>
    public sealed class InGameBgmStateMachine
    {
        #region Private Fields

        private readonly BgmAudioChannel _channel;
        private readonly InGameAudioCatalog _catalog;
        private bool _isAfterReachBgm;

        #endregion

        public InGameBgmStateMachine(BgmAudioChannel channel, InGameAudioCatalog catalog)
        {
            _channel = channel;
            _catalog = catalog;
        }

        #region Public Methods

        /// <summary>
        /// Reach 前 BGM を再生する。
        /// </summary>
        public void PlayBeforeReach()
        {
            _isAfterReachBgm = false;
            _channel.PlayLoop(_catalog.GetBgm(InGameBgmId.BeforeReach));
        }

        /// <summary>
        /// 初回 Reach 成功時に Reach 後 BGM へ切り替える。
        /// </summary>
        public void TrySwitchToAfterReach()
        {
            if (_isAfterReachBgm)
            {
                return;
            }

            _isAfterReachBgm = true;
            _channel.PlayLoop(_catalog.GetBgm(InGameBgmId.AfterReach));
        }

        /// <summary>
        /// BGM を停止する。
        /// </summary>
        public void Stop()
        {
            _isAfterReachBgm = false;
            _channel.Stop();
        }

        #endregion
    }
}
