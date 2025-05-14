using UnityEngine;
using Tetrage.Core;

namespace Tetrage.UI
{
    public class StackCardPileView : BasicCardPileView
    {
        [SerializeField] private Transform _cardPileParent;
        public CardView TopCardView;

        protected override void LayoutCardView()
        {
            // カード表示用ViewのTransformリストを更新
            UpdateCardViewObjects();
            int count = _cardViewObjects.Count;
            if (count == 0) return;

            SetCardViewPositions(count, 0, 0);
        }

        // スタックの場合はカード表示用Viewの座標を0,0,0にする
        protected override void SetCardViewPositionByIndex(int index, float spacing, float centerOffset)
        {
            _cardViewObjects[index].localPosition = new Vector3(0, 0, 0) + _cardViewPositionOffset;
        }
    }
}
