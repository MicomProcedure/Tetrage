using UnityEngine;
using Tetrage.UI;
using System;

namespace Tetrage.UI
{
    public class CardPileView : MonoBehaviour
    {
        /// <summary>
        /// このビューが破棄されたときに発行されるイベント。
        /// Presenter はここを購読し Dispose を呼びます。
        /// </summary>
        public event Action Destroyed;

        private void OnDestroy()
        {
            Destroyed?.Invoke();
        }

        /// <summary>カード表示用ViewをこのPileViewの子に設定します。</summary>
        public void AddCardView(CardView cardView)
        {
            cardView.transform.SetParent(transform, worldPositionStays: true);
        }

        /// <summary>カード表示用ViewをこのPileViewから外します。</summary>
        public void RemoveCardView(CardView cardView)
        {
            cardView.transform.SetParent(null);
        }
    }
}
