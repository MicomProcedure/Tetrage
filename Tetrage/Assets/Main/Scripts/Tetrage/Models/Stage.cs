using System.Collections.Generic;
using UnityEngine;

namespace Tetrage.Models
{
    public class Stage : MonoBehaviour
    {
        [SerializeField] private Transform stackContainer;
        [SerializeField] private Transform trashContainer;

        private List<Card> stack = new List<Card>();
        private List<Card> trash = new List<Card>();

        // スタックからカードを1枚引く
        public Card DrawFromStack()
        {
            if (stack.Count == 0)
            {
                Debug.LogWarning("スタックが空です");
                return null;
            }

            Card drawnCard = stack[0];
            stack.RemoveAt(0);
            Debug.Log("カードを引きました: " + drawnCard.name);
            return drawnCard;
        }

        // カードを捨て札へ
        public void Discard(Card card)
        {
            if (card == null)
            {
                Debug.LogWarning("無効なカードが指定されました");
                return;
            }

            trash.Add(card);
            card.transform.SetParent(trashContainer);
            Debug.Log("カードを捨てました: " + card.name);
        }
    }
}