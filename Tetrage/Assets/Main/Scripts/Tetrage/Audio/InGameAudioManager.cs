using UnityEngine;
using Tetrage.Core.Contracts;
using Tetrage.Core.Events;
using R3;
using System.Linq;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Constants;
using System.Collections.Generic;
using Tetrage.Core.Ids;
using Tetrage.Core.Enums;

namespace Tetrage.Audio
{
    /// <summary>
    /// InGame中のSE・BGM再生を担当するAudioManager。
    /// シングルトンを使わず、参照を持つ側から呼び出して利用する。
    /// </summary>
    public sealed class InGameAudioManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Catalog")]
        [SerializeField] private InGameAudioCatalog _catalog;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource SEAudioSource;
        [SerializeField] private AudioSource BGMAudioSource;

        #endregion

        #region 管理対象インスタンス

        private IGameContext _gameContext;

        #endregion

        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        private bool _isValid = false;
        private bool _isAfterReachBgm = false;
        private Dictionary<InGameSEId, bool> _sePlaybackGate = new();
        private CompositeDisposable _disposables = new();

        private readonly List<PileId> _targetPiles = new();

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateAudioReferences();
        }

        #endregion

        #region Public Methods

        public void Initialize(IGameContext gameContext)
        {
            _gameContext = gameContext;

            // Subscribeの前に実行する必要あり
            // CardMovedEventのToPileIdがPlayerTargetのPileIdであるかを判定するために、PlayerTargetのPileIdをリストに追加する。
            _targetPiles.Clear();
            foreach (var player in _gameContext.Players)
            {
                _targetPiles.Add(PileIds.PlayerTarget(player.Id));
            }

            InitializeSEPlaybackGate();
            SubscribeToEvents();
            _isInitialized = true;
        }

        /// <summary>
        /// ゲーム開始演出用SEを再生する。
        /// </summary>
        public void PlayGameStartSE()
        {
            PlaySE(InGameSEId.GameStart);
        }

        /// <summary>
        /// カード移動SEを再生する。
        /// </summary>
        public void PlayCardMoveSE()
        {
            PlayGatedSe(InGameSEId.CardMove);
        }

        /// <summary>
        /// カード反転SEを再生する。
        /// </summary>
        public void PlayCardFlipSE()
        {
            PlayGatedSe(InGameSEId.CardFlip);
        }

        /// <summary>
        /// ボタンクリックSEを再生する。
        /// </summary>
        public void PlayButtonClickSE()
        {
            PlayGatedSe(InGameSEId.ButtonClick);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reach 前 BGM をループ再生する。
        /// </summary>
        private void PlayBeforeReachBGM()
        {
            _isAfterReachBgm = false;
            PlayBGM(InGameBgmId.BeforeReach);
        }

        /// <summary>
        /// 初回 Reach 成功時に Reach 後 BGM へ切り替える。
        /// </summary>
        private void PlayAfterReachBGM()
        {
            if (_isAfterReachBgm)
            {
                return;
            }

            _isAfterReachBgm = true;
            PlayBGM(InGameBgmId.AfterReach);
        }

        private void StopBGM()
        {
            if (BGMAudioSource == null)
            {
                return;
            }

            if (BGMAudioSource.isPlaying)
            {
                BGMAudioSource.Stop();
            }

            BGMAudioSource.clip = null;
        }

        /// <summary>
        /// ゲート付き SE を再生する。
        /// </summary>
        private void PlayGatedSe(InGameSEId id)
        {
            if (!_sePlaybackGate.TryGetValue(id, out var isOpen) || !isOpen)
            {
                return;
            }

            if (!PlaySE(id))
            {
                return;
            }

            _sePlaybackGate[id] = false;
            UniTask.Delay(InGameConsts.DEFAULT_SE_PLAYBACK_GATE_TIME)
                .ContinueWith(() => _sePlaybackGate[id] = true)
                .Forget();
        }

        /// <summary>
        /// 指定 ID の SE を再生する。再生できた場合は true。
        /// </summary>
        private bool PlaySE(InGameSEId id)
        {
            if (_catalog == null)
            {
                return false;
            }

            var clip = _catalog.GetSe(id);
            if (SEAudioSource == null || clip == null)
            {
                return false;
            }

            if (!_isInitialized)
            {
                Debug.LogWarning("InGameAudioManager: 初期化されていません。SE再生をスキップします。", this);
                return false;
            }

            SEAudioSource.PlayOneShot(clip);
            return true;
        }

        /// <summary>
        /// 指定 ID の BGM をループ再生する。
        /// </summary>
        private void PlayBGM(InGameBgmId id)
        {
            if (_catalog == null)
            {
                return;
            }

            var clip = _catalog.GetBgm(id);
            if (BGMAudioSource == null || clip == null)
            {
                return;
            }

            if (!_isInitialized)
            {
                Debug.LogWarning("InGameAudioManager: 初期化されていません。BGM再生をスキップします。", this);
                return;
            }

            if (BGMAudioSource.clip == clip && BGMAudioSource.isPlaying)
            {
                return;
            }

            BGMAudioSource.loop = true;
            BGMAudioSource.clip = clip;
            BGMAudioSource.Play();
        }

        /// <summary>
        /// Initialize時に必要な参照が設定されているか検証する。
        /// </summary>
        private void ValidateAudioReferences()
        {
            if (_catalog == null)
            {
                Debug.LogWarning("InGameAudioManager: InGameAudioCatalog が未設定です。音声再生をスキップします。", this);
                return;
            }

            if (SEAudioSource == null)
            {
                Debug.LogWarning("InGameAudioManager: seAudioSource が未設定です。SE再生をスキップします。", this);
            }
            else if (BGMAudioSource == null)
            {
                Debug.LogWarning("InGameAudioManager: BGMAudioSource が未設定です。BGM再生をスキップします。", this);
            }
            else if (_catalog.ValidateReferences())
            {
                _isValid = true;
            }
        }

        private void SubscribeToEvents()
        {
            // Card移動時に音声を鳴らす
            // ただし、移動先がPlayerTargetのPileIdである場合は音声を鳴らさない（フィールド初期化時は音を無らしたくない）。
            if (_catalog.GetSe(InGameSEId.CardMove) != null)
            {
                _gameContext.Events.CardMoved
                    .Where(e => !_targetPiles.Contains(e.ToPileId))
                    .Subscribe(_ => PlayCardMoveSE())
                    .AddTo(_disposables);
            }

            // ScanPhase 終了時（ゲーム開始演出と同タイミング）に SE・Reach 前 BGM を再生する
            _gameContext.Events.ScanPhaseEnded
                .Subscribe(_ => OnScanPhaseEnded())
                .AddTo(_disposables);

            // 誰かが Reach に成功したら BGM を切り替える
            if (_catalog.GetBgm(InGameBgmId.AfterReach) != null && BGMAudioSource != null)
            {
                _gameContext.Events.ActionResult
                    .Where(e => e.ActionType == ActionType.Reach && e.Accepted)
                    .Subscribe(_ => PlayAfterReachBGM())
                    .AddTo(_disposables);
            }
        }

        /// <summary>
        /// ScanPhase 終了時の音声処理。ゲーム開始 SE 再生後、その長さだけ遅らせて Reach 前 BGM を再生する。
        /// </summary>
        private void OnScanPhaseEnded()
        {
            OnScanPhaseEndedAsync().Forget();
        }

        private async UniTaskVoid OnScanPhaseEndedAsync()
        {
            PlayGameStartSE();

            // gameStartSE の再生時間が終わってから BGM を開始する
            var delayMs = _catalog != null
                ? _catalog.GetSeLengthMilliseconds(InGameSEId.GameStart)
                : 0;
            if (delayMs > 0)
            {
                await UniTask.Delay(delayMs, cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            PlayBeforeReachBGM();
        }

        private void InitializeSEPlaybackGate()
        {
            _sePlaybackGate[InGameSEId.CardMove] = true;
            _sePlaybackGate[InGameSEId.CardFlip] = true;
            _sePlaybackGate[InGameSEId.ButtonClick] = true;
        }

        private void OnDestroy()
        {
            StopBGM();
            _disposables.Dispose();
        }

        #endregion
    }
}
