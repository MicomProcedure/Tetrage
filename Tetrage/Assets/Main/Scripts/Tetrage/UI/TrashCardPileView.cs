using UnityEngine;
using Tetrage.Core;

namespace Tetrage.UI
{
    public class TrashCardPileView : BasicCardPileView
    {
        [SerializeField] private Transform _cardPileParent;

        public override void RefreshView()
        {
            base.RefreshView();
            
        }
    }
}
