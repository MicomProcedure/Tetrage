using UnityEngine;
using Tetrage.Core.Contracts;
using Tetrage.Core.Events;
using R3;
using System.Linq;

namespace Tetrage.Audio{
    /// <summary>
    /// InGame中のSE再生を担当するAudioManager。
    /// シングルトンを使わず、参照を持つ側から呼び出して利用する。
    /// </summary>
    public sealed class InGameAudioManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Audio Sources")]
        [SerializeField] private AudioSource SEAudioSource;

        [Header("SE Clips")]
        [SerializeField] private AudioClip cardMoveSE;
        [SerializeField] private AudioClip cardFlipSE;
        [SerializeField] private AudioClip buttonClickSE;

        #endregion

        #region 管理対象インスタンス

        private IGameContext _gameContext;

        #endregion

        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        private bool _isValid = false;
        private CompositeDisposable _disposables = new();

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

            SubscribeToEvents();

            _isInitialized = true;
        }

        /// <summary>
        /// カード移動SEを再生する。
        /// </summary>
        public void PlayCardMoveSE()
        {
            PlaySE(cardMoveSE);
        }

        /// <summary>
        /// カード反転SEを再生する。
        /// </summary>
        public void PlayCardFlipSE()
        {
            PlaySE(cardFlipSE);
        }

        /// <summary>
        /// ボタンクリックSEを再生する。
        /// </summary>
        public void PlayButtonClickSE()
        {
            PlaySE(buttonClickSE);
        }


        #endregion

        #region Private Methods
        
        /// <summary>
        /// 指定したSEを再生する。
        /// </summary>
        private void PlaySE(AudioClip clip)
        {
            if (SEAudioSource == null || clip == null)
            {
                return;
            }

            if (!_isInitialized)
            {
                Debug.LogWarning("InGameAudioManager: 初期化されていません。SE再生をスキップします。", this);
                return;
            }

            SEAudioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Initialize時に必要な参照が設定されているか検証する。
        /// </summary>
        private void ValidateAudioReferences()
        {
            if (SEAudioSource == null)
            {
                Debug.LogWarning("InGameAudioManager: seAudioSource が未設定です。SE再生をスキップします。", this);
            }
            else if (cardFlipSE == null)
            {
                Debug.LogWarning("InGameAudioManager: cardFlipSe が未設定です。SE再生をスキップします。", this);
            }
            else if (cardMoveSE == null)
            {
                Debug.LogWarning("InGameAudioManager: cardMoveSe が未設定です。SE再生をスキップします。", this);
            }
            else if (buttonClickSE == null)
            {
                Debug.LogWarning("InGameAudioManager: buttonClickSe が未設定です。SE再生をスキップします。", this);
            }
            else
            {
                _isValid = true;
            }
        }

        private void SubscribeToEvents()
        {
            if (cardFlipSE != null){
                _gameContext.Events.CardStateChanged
                                            .Where(e => e.StateType == CardStateType.FaceUp)
                                            .Subscribe(_ => PlayCardFlipSE());
            }

            if (cardMoveSE != null){
                _gameContext.Events.CardMoved.Subscribe(_ => PlayCardMoveSE());
            }

        }

        #endregion
    }



}