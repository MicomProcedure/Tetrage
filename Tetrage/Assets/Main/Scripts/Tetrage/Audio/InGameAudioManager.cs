using UnityEngine;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Contracts;
using Tetrage.Core.Constants;

namespace Tetrage.Audio
{
    /// <summary>
    /// InGame 音声の Composition Root。チャンネル・Presenter の配線とライフサイクルを担当する。
    /// </summary>
    public sealed class InGameAudioManager : MonoBehaviour, ISEAudioService, IJingleAudioService
    {
        #region Serialized Fields

        [Header("Catalog")]
        [SerializeField] private InGameAudioCatalog _catalog;

        [Header("Channels")]
        [SerializeField] private SEAudioChannel _seChannel;
        [SerializeField] private BgmAudioChannel _bgmChannel;
        [SerializeField] private JingleAudioChannel _jingleChannel;

        #endregion

        #region Private Fields

        private InGameAudioEventPresenter _presenter;
        private InGameBgmStateMachine _bgmStateMachine;
        private InGameJinglePlayer _jinglePlayer;
        private bool _isInitialized;

        #endregion

        #region Public Properties

        public bool IsInitialized => _isInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            _presenter?.Unbind();
            _jinglePlayer?.Cancel();
            _bgmStateMachine?.Stop();
            _jingleChannel?.Stop();
        }

        #endregion

        #region Public Methods

        public void Initialize(IGameContext gameContext)
        {
            if (!ValidateInitialized(gameContext))
            {
                return;
            }

            // 同時再生を抑制する SE ID を登録する。
            _seChannel.ConfigureGatedSeIds(new[]
            {
                SEClipId.CardMove,
                SEClipId.CardFlip,
                SEClipId.ButtonClick,
            });

            // SE 再生ゲートの時間を設定する。
            _seChannel.SetSEPlaybackGateTimeMS(InGameConsts.DEFAULT_SE_PLAYBACK_GATE_TIME_MS);

            // BGM 状態マシンを初期化する。
            _bgmStateMachine = new InGameBgmStateMachine(_bgmChannel, _catalog);

            // BGM ダックコントローラを初期化する。
            var duckController = new BgmDuckController(_bgmChannel);

            // Jingle プレイヤーを初期化する。
            _jinglePlayer = new InGameJinglePlayer(
                duckController,
                _jingleChannel,
                _catalog,
                this.GetCancellationTokenOnDestroy());
            _presenter = new InGameAudioEventPresenter();
            _presenter.Bind(
                gameContext.Events,
                gameContext.Players,
                _catalog,
                _seChannel,
                _bgmStateMachine,
                this.GetCancellationTokenOnDestroy());

            _isInitialized = true;
        }

        /// <inheritdoc />
        public void PlaySE(SEClipId id)
        {
            if (!ValidatePlaybackReady())
            {
                return;
            }

            var clip = _catalog.GetAudioClip(id);
            if (id == SEClipId.GameStart)
            {
                _seChannel.TryPlay(clip);
                return;
            }

            _seChannel.TryPlayGated(id, clip);
        }

        /// <inheritdoc />
        public void PlayJingle(JingleClipId id)
        {
            if (!ValidateJinglePlaybackReady())
            {
                return;
            }

            _jinglePlayer.PlayJingle(id);
        }

        #endregion

        #region Private Methods

        private bool ValidatePlaybackReady()
        {
            if (_isInitialized && _catalog != null && _seChannel != null)
            {
                return true;
            }

            Debug.LogWarning("InGameAudioManager: 初期化されていません。SE再生をスキップします。", this);
            return false;
        }

        private bool ValidateJinglePlaybackReady()
        {
            if (_isInitialized && _catalog != null && _jingleChannel != null)
            {
                return true;
            }

            Debug.LogWarning("InGameAudioManager: 初期化されていません。Jingle再生をスキップします。", this);
            return false;
        }

        private bool ValidateInitialized(IGameContext gameContext)
        {
            if (gameContext?.Events == null)
            {
                Debug.LogWarning("InGameAudioManager: GameContext または EventBus が無効です。", this);
                return false;
            }

            return ValidateReferences();
        }

        private bool ValidateReferences()
        {
            if (_catalog == null)
            {
                Debug.LogWarning("InGameAudioManager: InGameAudioCatalog が未設定です。", this);
                return false;
            }

            var isValid = true;
            if (!_catalog.HasClipSets)
            {
                Debug.LogWarning("InGameAudioManager: InGameAudioCatalog のクリップセットが未設定です。", this);
                isValid = false;
            }
            else
            {
                // 未設定 Clip は Validate 内で警告済み。戻り値は初期化可否に使わない。
                _catalog.ValidateReferences();
            }
            if (_seChannel != null)
            {
                isValid &= _seChannel.ValidateReferences();
            }
            else
            {
                Debug.LogWarning("InGameAudioManager: SeAudioChannel が未設定です。", this);
                isValid = false;
            }

            if (_bgmChannel != null)
            {
                isValid &= _bgmChannel.ValidateReferences();
            }
            else
            {
                Debug.LogWarning("InGameAudioManager: BgmAudioChannel が未設定です。", this);
                isValid = false;
            }

            if (_jingleChannel != null)
            {
                isValid &= _jingleChannel.ValidateReferences();
            }
            else
            {
                Debug.LogWarning("InGameAudioManager: JingleAudioChannel が未設定です。", this);
                isValid = false;
            }

            return isValid;
        }

        #endregion
    }
}
