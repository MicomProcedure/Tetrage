using UnityEngine;
using Tetrage.Core;

namespace Tetrage.UI
{  
    public class StackCardPileView : BasicCardPileView
    {
        [SerializeField] private Transform _cardPileParent;

        public override void RefreshView()
        {
            base.RefreshView();
            
        }

    }
}
