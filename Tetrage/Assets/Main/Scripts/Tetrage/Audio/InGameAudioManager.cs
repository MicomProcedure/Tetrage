using UnityEngine;
using Tetrage.Core.Contracts;
using Tetrage.Core.Events;
using R3;
using System.Linq;
using Cysharp.Threading.Tasks;
using Tetrage.Core.Constants;
using System.Collections.Generic;

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
        private Dictionary<AudioClip, bool> _SEPlaybackGate = new(); // SE再生ゲートを表す変数Dictionary        
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
            InitializeSEPlaybackGate();

            _isInitialized = true;
        }

        /// <summary>
        /// カード移動SEを再生する。
        /// </summary>
        public void PlayCardMoveSE()
        {
            if (!_SEPlaybackGate[cardMoveSE])
            {
                return;
            }           
            PlaySE(cardMoveSE);
            _SEPlaybackGate[cardMoveSE] = false;    // 同時再生を防ぐためにゲートを閉じる
            // 指定した時間後にゲートを開く
            UniTask.Delay(InGameConsts.DEFAULT_SE_PLAYBACK_GATE_TIME).ContinueWith(() => _SEPlaybackGate[cardMoveSE] = true).Forget();
        }

        /// <summary>
        /// カード反転SEを再生する。
        /// </summary>
        public void PlayCardFlipSE()
        {
            if (!_SEPlaybackGate[cardFlipSE])
            {
                return;
            }
            PlaySE(cardFlipSE);
            _SEPlaybackGate[cardFlipSE] = false;    // 同時再生を防ぐためにゲートを閉じる
            UniTask.Delay(InGameConsts.DEFAULT_SE_PLAYBACK_GATE_TIME).ContinueWith(() => _SEPlaybackGate[cardFlipSE] = true).Forget();
        }

        /// <summary>
        /// ボタンクリックSEを再生する。
        /// </summary>
        public void PlayButtonClickSE()
        {
            if (!_SEPlaybackGate[buttonClickSE])
            {
                return;
            }
            PlaySE(buttonClickSE);
            _SEPlaybackGate[buttonClickSE] = false;    // 同時再生を防ぐためにゲートを閉じる
            UniTask.Delay(InGameConsts.DEFAULT_SE_PLAYBACK_GATE_TIME).ContinueWith(() => _SEPlaybackGate[buttonClickSE] = true).Forget();
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
            // if (cardFlipSE != null){
            //     _gameContext.Events.CardStateChanged
            //                                 .Where(e => e.StateType == CardStateType.FaceUp)
            //                                 .Subscribe(_ => PlayCardFlipSE());
            // }

            if (cardMoveSE != null){
                _gameContext.Events.CardMoved.Subscribe(_ => PlayCardMoveSE());
            }

        }

        private void InitializeSEPlaybackGate()
        {
            _SEPlaybackGate.TryAdd(cardMoveSE, true);
            _SEPlaybackGate.TryAdd(cardFlipSE, true);
            _SEPlaybackGate.TryAdd(buttonClickSE, true);
            
        }       

        #endregion
    }



}