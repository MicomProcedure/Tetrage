using UnityEngine;
using Tetrage.Core.Constants;
using Tetrage.Core.DTO;

[CreateAssetMenu(fileName = "CardPileLayoutAsset", menuName = "Scriptable Objects/CardPileLayoutAsset")]
public class CardPileLayoutAsset : ScriptableObject
{
    public float PileWidth = InGameConsts.DEFAULT_CARD_PILE_WIDTH;
    public float MinSpacing = InGameConsts.DEFAULT_CARD_VIEW_MIN_SPACING;
    public float MaxSpacing = InGameConsts.DEFAULT_CARD_VIEW_MAX_SPACING;
    public Vector3 PositionOffset = InGameConsts.DEFAULT_CARD_VIEW_POSITION_OFFSET;
}

/// <summary>
/// CardPileLayoutAsset の拡張メソッドを提供するクラス
/// </summary>
public static class CardPileLayoutAssetExtensions
{
    /// <summary>
    /// CardPileLayoutAsset を CardPileLayoutSettings に変換する
    /// </summary>
    /// <param name="asset">変換元のCardPileLayoutAsset（nullの場合はデフォルト設定を返す）</param>
    /// <returns>CardPileLayoutSettings</returns>
    public static CardPileLayoutSettings ToLayoutSettings(this CardPileLayoutAsset asset)
    {
        if (asset == null)
        {
            Debug.LogWarning("CardPileLayoutAssetがnullです。デフォルト設定を使用します。");
            return new CardPileLayoutSettings(
                InGameConsts.DEFAULT_CARD_PILE_WIDTH,
                InGameConsts.DEFAULT_CARD_VIEW_MIN_SPACING,
                InGameConsts.DEFAULT_CARD_VIEW_MAX_SPACING,
                InGameConsts.DEFAULT_CARD_VIEW_POSITION_OFFSET
            );
        }

        return new CardPileLayoutSettings(asset.PileWidth, asset.MinSpacing, asset.MaxSpacing, asset.PositionOffset);
    }

    /// <summary>
    /// CardPileLayoutAsset が null または未設定の場合のフォールバック付き変換
    /// </summary>
    /// <param name="asset">変換元のCardPileLayoutAsset</param>
    /// <param name="fallback">フォールバック設定</param>
    /// <returns>CardPileLayoutSettings</returns>
    public static CardPileLayoutSettings ToLayoutSettings(this CardPileLayoutAsset asset, CardPileLayoutSettings fallback)
    {
        if (asset == null)
        {
            Debug.LogWarning("CardPileLayoutAssetがnullです。フォールバック設定を使用します。");
            return fallback;
        }

        return asset.ToLayoutSettings();
    }
}
