using UnityEngine;
using Tetrage.Core.Constants;

[CreateAssetMenu(fileName = "CardPileLayoutConfig", menuName = "Scriptable Objects/CardPileLayoutConfig")]
public class CardPileLayoutConfig : ScriptableObject
{
    public float PileWidth = InGameConsts.DEFAULT_CARD_PILE_WIDTH;
    public float MinSpacing = InGameConsts.DEFAULT_CARD_VIEW_MIN_SPACING;
    public float MaxSpacing = InGameConsts.DEFAULT_CARD_VIEW_MAX_SPACING;
    public Vector3 PositionOffset = InGameConsts.DEFAULT_CARD_VIEW_POSITION_OFFSET;
}
