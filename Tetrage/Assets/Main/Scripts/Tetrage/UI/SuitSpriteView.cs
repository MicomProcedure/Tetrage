using Tetrage.Core.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Tetrage.UI
{
    /// <summary>
    /// UI Image にスートスプライトを表示する View。
    /// </summary>
    public class SuitSpriteView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private Image _suitImage;

        [Header("Suit Sprites")]
        [SerializeField] private Sprite _spadeSprite;
        [SerializeField] private Sprite _heartSprite;
        [SerializeField] private Sprite _diamondSprite;
        [SerializeField] private Sprite _clubSprite;

        #endregion

        #region Public Methods

        /// <summary>
        /// 表示用 Image を外部から注入する。
        /// </summary>
        public void ConfigureBindings(Image suitImage)
        {
            _suitImage = suitImage;
        }

        /// <summary>
        /// スートに応じて表示スプライトを切り替える。
        /// </summary>
        public void SetSuit(Suit suit)
        {
            if (_suitImage == null)
            {
                Debug.LogWarning("SuitSpriteView: _suitImage が未設定です。");
                return;
            }

            var sprite = ResolveSprite(suit);
            if (sprite == null)
            {
                Debug.LogWarning($"SuitSpriteView: Suit={suit} のスプライトが未設定です。");
                return;
            }

            _suitImage.sprite = sprite;
        }

        #endregion

        #region Private Methods

        private Sprite ResolveSprite(Suit suit) => suit switch
        {
            Suit.Spade => _spadeSprite,
            Suit.Heart => _heartSprite,
            Suit.Diamond => _diamondSprite,
            Suit.Club => _clubSprite,
            _ => null,
        };

        #endregion
    }
}
