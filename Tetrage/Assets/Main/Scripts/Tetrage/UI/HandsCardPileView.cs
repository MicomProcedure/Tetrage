using UnityEngine;
using Tetrage.Core;

namespace Tetrage.UI
{
    public class HandsCardPileView : BasicCardPileView
    {
        [SerializeField] private Transform _cardPileParent;

        public override void RefreshView()
        {
            base.RefreshView();
            
        }
    }
}
