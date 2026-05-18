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

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定 ID の SE クリップを取得する。未設定時は null。
        /// </summary>
        public AudioClip GetSe(InGameSEId id)
        {
            if (_seClips == null)
            {
                return null;
            }

            return _seClips.GetClip(id);
        }

        /// <summary>
        /// 指定 ID の BGM クリップを取得する。未設定時は null。
        /// </summary>
        public AudioClip GetBgm(InGameBgmId id)
        {
            if (_bgmClips == null)
            {
                return null;
            }

            return _bgmClips.GetClip(id);
        }

        /// <summary>
        /// SE クリップの再生時間をミリ秒で返す。未設定時は 0。
        /// </summary>
        public int GetSeLengthMilliseconds(InGameSEId id)
        {
            var clip = GetSe(id);
            if (clip == null)
            {
                return 0;
            }

            return Mathf.CeilToInt(clip.length * InGameConsts.CutInAnimationDuration.MILLISECONDS_PER_SECOND);
        }

        /// <summary>
        /// Catalog 参照と各 Clip の設定漏れを検証する。
        /// </summary>
        public bool ValidateReferences()
        {
            if (_seClips == null || _bgmClips == null)
            {
                Debug.LogWarning("InGameAudioCatalog: SE/BGM クリップセットが未設定です。", this);
                return false;
            }

            var isValid = true;
            isValid &= _seClips.Validate(this);
            isValid &= _bgmClips.Validate(this);
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

    /// <summary>
    /// InGame SE クリップの一覧。
    /// </summary>
    [Serializable]
    public sealed class InGameSEClipSet
    {
        [SerializeField] private AudioClip gameStart;
        [SerializeField] private AudioClip cardMove;
        [SerializeField] private AudioClip cardFlip;
        [SerializeField] private AudioClip buttonClick;

        public AudioClip GetClip(InGameSEId id)
        {
            return id switch
            {
                InGameSEId.GameStart => gameStart,
                InGameSEId.CardMove => cardMove,
                InGameSEId.CardFlip => cardFlip,
                InGameSEId.ButtonClick => buttonClick,
                _ => null,
            };
        }

        public bool Validate(UnityEngine.Object context)
        {
            var isValid = true;
            isValid &= ValidateClip(context, InGameSEId.GameStart, gameStart);
            isValid &= ValidateClip(context, InGameSEId.CardMove, cardMove);
            isValid &= ValidateClip(context, InGameSEId.CardFlip, cardFlip);
            isValid &= ValidateClip(context, InGameSEId.ButtonClick, buttonClick);
            return isValid;
        }

        private static bool ValidateClip(UnityEngine.Object context, InGameSEId id, AudioClip clip)
        {
            if (clip != null)
            {
                return true;
            }

            Debug.LogWarning($"InGameAudioCatalog: SE '{id}' が未設定です。", context);
            return false;
        }
    }

    /// <summary>
    /// InGame BGM クリップの一覧。
    /// </summary>
    [Serializable]
    public sealed class InGameBgmClipSet
    {
        [SerializeField] private AudioClip beforeReach;
        [SerializeField] private AudioClip afterReach;

        public AudioClip GetClip(InGameBgmId id)
        {
            return id switch
            {
                InGameBgmId.BeforeReach => beforeReach,
                InGameBgmId.AfterReach => afterReach,
                _ => null,
            };
        }

        public bool Validate(UnityEngine.Object context)
        {
            var isValid = true;
            isValid &= ValidateClip(context, InGameBgmId.BeforeReach, beforeReach);
            isValid &= ValidateClip(context, InGameBgmId.AfterReach, afterReach);
            return isValid;
        }

        private static bool ValidateClip(UnityEngine.Object context, InGameBgmId id, AudioClip clip)
        {
            if (clip != null)
            {
                return true;
            }

            Debug.LogWarning($"InGameAudioCatalog: BGM '{id}' が未設定です。", context);
            return false;
        }
    }

    #endregion
}
