using UnityEngine;
using Tetrage.Models;

namespace Tetrage.Managers
{
    /// <summary>
    /// Domain service for transferring cards between piles.
    /// </summary>
    public static class CardTransferService
    {
        /// <summary>
        /// Removes a card from one pile and adds it to another, firing related events.
        /// </summary>
        /// <returns>True if transfer succeeded; false otherwise.</returns>
        public static bool Transfer(CardPile from, CardPile to, Card card)
        {
            if (!from.Remove(card))
            {
                Debug.LogWarning($"[CardTransferService] Failed to remove card from pile '{from.Name}'");
                return false;
            }

            bool added = to.Add(card);
            if (!added)
            {
                Debug.LogWarning($"[CardTransferService] Failed to add card to pile '{to.Name}'");
                return false;
            }

            // 移動完了を通知（CardTransferredのみ）
            to.NotifyCardTransferred(card, from, to);

            return true;
        }
    }
}