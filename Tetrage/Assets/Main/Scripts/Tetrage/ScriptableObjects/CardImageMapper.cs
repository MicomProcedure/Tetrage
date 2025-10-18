using UnityEngine;
using Tetrage.Core.Enums;
using System;
using System.Collections.Generic;

namespace Tetrage.Data
{
    /// <summary>
    /// カード画像とスート・数字を紐付けるScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "CardImageMapper", menuName = "Tetrage/CardImageMapper")]
    public class CardImageMapper : ScriptableObject
    {
        #region Serialized Fields

        [Header("Card Sprites")]
        [SerializeField] private List<CardSpriteData> cardSprites = new List<CardSpriteData>();

        [Header("Card Back Sprite")]
        [SerializeField] private Sprite cardBackSprite;

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定したスートと数字に対応するスプライトを取得
        /// </summary>
        /// <param name="suit">スート</param>
        /// <param name="number">数字（1-13）</param>
        /// <returns>対応するスプライト（見つからない場合はnull）</returns>
        public Sprite GetCardSprite(Suit suit, int number)
        {
            var data = cardSprites.Find(x => x.suit == suit && x.number == number);
            return data?.sprite;
        }

        /// <summary>
        /// カードの裏面スプライトを取得
        /// </summary>
        public Sprite GetCardBackSprite()
        {
            return cardBackSprite;
        }

        /// <summary>
        /// スートに対応するシンボル文字列を取得
        /// </summary>
        public string GetSuitSymbol(Suit suit)
        {
            return suit switch
            {
                Suit.Spade => "♠",
                Suit.Heart => "♥",
                Suit.Diamond => "♦",
                Suit.Club => "♣",
                _ => "?"
            };
        }

        /// <summary>
        /// スートに対応する色を取得
        /// </summary>
        public Color GetSuitColor(Suit suit)
        {
            return suit switch
            {
                Suit.Heart => Color.red,
                Suit.Diamond => Color.red,
                Suit.Spade => Color.black,
                Suit.Club => Color.black,
                _ => Color.white
            };
        }

        #endregion

        #region Editor Utility

#if UNITY_EDITOR
        /// <summary>
        /// エディタ上で全カードデータを自動生成するメソッド
        /// </summary>
        [ContextMenu("Generate All Card Data")]
        public void GenerateAllCardData()
        {
            cardSprites.Clear();
            
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (int number = 1; number <= 13; number++)
                {
                    cardSprites.Add(new CardSpriteData
                    {
                        suit = suit,
                        number = number,
                        sprite = null
                    });
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"Generated {cardSprites.Count} card data entries.");
        }
#endif

        #endregion
    }

    /// <summary>
    /// カードスプライトデータのシリアライズ可能なクラス
    /// </summary>
    [Serializable]
    public class CardSpriteData
    {
        [Header("Card Identity")]
        public Suit suit;
        [Range(1, 13)]
        public int number;

        [Header("Visual")]
        public Sprite sprite;

        /// <summary>
        /// エディタ表示用の名前
        /// </summary>
        public string DisplayName => $"{suit} {GetNumberName()}";

        private string GetNumberName()
        {
            return number switch
            {
                1 => "A",
                11 => "J",
                12 => "Q",
                13 => "K",
                _ => number.ToString()
            };
        }
    }
}