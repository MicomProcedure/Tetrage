using UnityEngine;
using Tetrage.Core.Constants;

namespace Tetrage.Core.Settings
{
    /// <summary>
    /// カードパイルのレイアウト設定を表す不変な構造体
    /// </summary>
    /// <remarks>
    /// この構造体は不変であり、変更可能なプロパティはありません。
    /// 新しい設定を作成する場合は、With系メソッドを使用して新しいインスタンスを生成してください。
    /// </remarks>
    public readonly struct CardPileLayoutSettings
    {
        public readonly float PileWidth;
        public readonly float MinSpacing;
        public readonly float MaxSpacing;
        public readonly Vector3 PositionOffset;

        // コンストラクタ
        public CardPileLayoutSettings(
            float pileWidth = InGameConsts.DEFAULT_CARD_PILE_WIDTH,
            float minSpacing = InGameConsts.DEFAULT_CARD_VIEW_MIN_SPACING,
            float maxSpacing = InGameConsts.DEFAULT_CARD_VIEW_MAX_SPACING,
            Vector3 positionOffset = default)
        {
            PileWidth = pileWidth;
            MinSpacing = minSpacing;
            MaxSpacing = maxSpacing;
            PositionOffset = positionOffset == default 
                ? InGameConsts.DEFAULT_CARD_VIEW_POSITION_OFFSET 
                : positionOffset;
        }

        // よく使うプリセット（staticプロパティ）
        public static CardPileLayoutSettings Default => new CardPileLayoutSettings();
        
        // public static CardPileLayoutSettings Hands => new CardPileLayoutSettings(
        //     pileWidth: 15f, 
        //     minSpacing: 1f, 
        //     maxSpacing: 3f);
            
        // public static CardPileLayoutSettings Stack => new CardPileLayoutSettings(
        //     pileWidth: 5f, 
        //     minSpacing: 0f, 
        //     maxSpacing: 0f);
            
        // public static CardPileLayoutSettings Tmp => new CardPileLayoutSettings(
        //     pileWidth: 8f, 
        //     minSpacing: 0.5f, 
        //     maxSpacing: 2f);

        // With系メソッドで部分変更（不変性を保ちつつ）
        public CardPileLayoutSettings WithWidth(float width) => new CardPileLayoutSettings(
            width, MinSpacing, MaxSpacing, PositionOffset);
            
        public CardPileLayoutSettings WithSpacing(float min, float max) => new CardPileLayoutSettings(
            PileWidth, min, max, PositionOffset);
            
        public CardPileLayoutSettings WithOffset(Vector3 offset) => new CardPileLayoutSettings(
            PileWidth, MinSpacing, MaxSpacing, offset);
    }
}