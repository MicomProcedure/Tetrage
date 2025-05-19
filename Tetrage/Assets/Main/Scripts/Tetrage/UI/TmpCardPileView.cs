using UnityEngine;

namespace Tetrage.UI
{
    public class TmpCardPileView : BasicCardPileView
    {
        [SerializeField] private Transform _cardPileParent;

        public override void RefreshView()
        {
            base.RefreshView();
            
        }
    }
}
