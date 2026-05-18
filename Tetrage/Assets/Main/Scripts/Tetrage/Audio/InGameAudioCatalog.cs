using System;
using UnityEngine;
using Tetrage.Core.Constants;

namespace Tetrage.Audio
{
    /// <summary>
    /// InGame 用 SE・BGM クリップの目録。再生ロジックは持たず Clip 解決のみ担当する。
    /// </summary>
    [CreateAssetMenu(fileName = "InGameAudioCatalog", menuName = "Tetrage/InGameAudioCatalog")]
    public sealed class InGameAudioCatalog : ScriptableObject
    {
        #region Serialized Fields

        [Header("SE")]
        [SerializeField] private InGameSEClipSet _seClips;

        [Header("BGM")]
        [SerializeField] private InGameBgmClipSet _bgmClips;

        [Header("JINGLE")]
        [SerializeField] private InGameJingleClipSet _jingleClips;

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定 ID の AudioClip取得する。未設定時は null。
        /// </summary>
        public AudioClip GetAudioClip<TId>(TId id)
        {
            switch (id)
            {
                case SEClipId seId:
                    return _seClips?.GetClip(seId);
                case BgmClipId bgmId:
                    return _bgmClips?.GetClip(bgmId);
                case JingleClipId jingleId:
                    return _jingleClips?.GetClip(jingleId);
                default:
                    return null;
            }
        }

        public int GetAudioClipLengthMS<TId>(TId id)
        {
            var clip = GetAudioClip(id);
            if (clip == null)
            {
                return 0;
            }
            return Mathf.CeilToInt(clip.length * InGameConsts.MILLISECONDS_PER_SECOND);
        }

        /// <summary>
        /// SE/BGM/Jingle のクリップセット参照が揃っているか。
        /// </summary>
        public bool HasClipSets =>
            _seClips != null && _bgmClips != null && _jingleClips != null;

        /// <summary>
        /// Catalog 参照と各 Clip の設定漏れを検証する。
        /// </summary>
        public bool ValidateReferences()
        {
            if (!HasClipSets)
            {
                Debug.LogWarning("InGameAudioCatalog: SE/BGM/Jingle クリップセットが未設定です。", this);
                return false;
            }

            var isValid = true;
            isValid &= _seClips.Validate(this);
            isValid &= _bgmClips.Validate(this);
            isValid &= _jingleClips.Validate(this);
            return isValid;
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateReferences();
        }
#endif
    }

    #region Clip Sets

    public abstract class AudioClipSetBase<T>
    {
        public abstract AudioClip GetClip(T id);
        public abstract bool Validate(UnityEngine.Object context);

        protected static bool ValidateClip(UnityEngine.Object context, T id, AudioClip clip)
        {
            if (clip != null)
            {
                return true;
            }

            Debug.LogWarning($"{context.GetType().Name}: '{id}' が未設定です。", context);
            return false;
        }
    }

    /// <summary>
    /// InGame SE クリップの一覧。
    /// </summary>
    [Serializable]
    public sealed class InGameSEClipSet : AudioClipSetBase<SEClipId>
    {
        [SerializeField] private AudioClip gameStart;
        [SerializeField] private AudioClip cardMove;
        [SerializeField] private AudioClip cardFlip;
        [SerializeField] private AudioClip buttonClick;

        // <inheritdoc />
        public override AudioClip GetClip(SEClipId id)
        {
            return id switch
            {
                SEClipId.GameStart => gameStart,
                SEClipId.CardMove => cardMove,
                SEClipId.CardFlip => cardFlip,
                SEClipId.ButtonClick => buttonClick,
                _ => null,
            };
        }

        // <inheritdoc />
        public override bool Validate(UnityEngine.Object context)
        {
            var isValid = true;
            isValid &= ValidateClip(context, SEClipId.GameStart, gameStart);
            isValid &= ValidateClip(context, SEClipId.CardMove, cardMove);
            isValid &= ValidateClip(context, SEClipId.CardFlip, cardFlip);
            isValid &= ValidateClip(context, SEClipId.ButtonClick, buttonClick);
            return isValid;
        }

    }

    /// <summary>
    /// InGame BGM クリップの一覧。
    /// </summary>
    [Serializable]
    public sealed class InGameBgmClipSet : AudioClipSetBase<BgmClipId>
    {
        [SerializeField] private AudioClip _inGameNormalBgm;
        [SerializeField] private AudioClip _inGameAfterReachBgm;
        [SerializeField] private AudioClip _inGameScanPhaseBgm;

        public override AudioClip GetClip(BgmClipId id)
        {
            return id switch
            {
                BgmClipId.Normal => _inGameNormalBgm,
                BgmClipId.AfterReach => _inGameAfterReachBgm,
                BgmClipId.ScanPhase => _inGameScanPhaseBgm,
                _ => null,
            };
        }

        public override bool Validate(UnityEngine.Object context)
        {
            var isValid = true;
            isValid &= ValidateClip(context, BgmClipId.Normal, _inGameNormalBgm);
            isValid &= ValidateClip(context, BgmClipId.AfterReach, _inGameAfterReachBgm);
            isValid &= ValidateClip(context, BgmClipId.ScanPhase, _inGameScanPhaseBgm);
            return isValid;
        }

    }

    /// <summary>
    /// InGame Jingle クリップの一覧。
    /// </summary>
    [Serializable]
    public sealed class InGameJingleClipSet : AudioClipSetBase<JingleClipId>
    {
        [SerializeField] private AudioClip _loseClip;
        [SerializeField] private AudioClip _winClip;

        public override AudioClip GetClip(JingleClipId id)
        {
            return id switch
            {
                JingleClipId.Lose => _loseClip,
                JingleClipId.Win => _winClip,
                _ => null,
            };
        }

        public override bool Validate(UnityEngine.Object context)
        {
            var isValid = true;
            isValid &= ValidateClip(context, JingleClipId.Lose, _loseClip);
            isValid &= ValidateClip(context, JingleClipId.Win, _winClip);
            return isValid;
        }
        
    }

    #endregion
}
