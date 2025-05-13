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

            if (TopCardView == null){

            }
        }
    }
}
