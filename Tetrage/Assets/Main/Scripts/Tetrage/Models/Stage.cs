using System.Collections.Generic;
using UnityEngine;
using Tetrage.Models;
using Tetrage.Core.Enums;

namespace Tetrage.Models
{
    public class Stage : MonoBehaviour
    {
        [SerializeField] private Transform stackContainer;
        [SerializeField] private Transform trashContainer;

        private CardPile _stack = new CardPile(name: "Stack", ownerType: CardOwner.Stage);
        private CardPile _trash = new CardPile(name: "Trash", ownerType: CardOwner.Stage);

        public CardPile Stack => _stack;
        public CardPile Trash => _trash;

        // スタックからカードを1枚引く
        public Card DrawFromStack()
        {
            if (_stack.Count == 0)
            {
                Debug.LogWarning("スタックが空です");
                return null;
            }

                // 先頭のカードを取得して、StackのCardPile から削除
                Card drawnCard = _stack.Peek(1)[0];
                _stack.Remove(drawnCard);

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

            // stack→trash の移動を一度に行う
            _stack.TransferTo(_trash, card);

            // シーン上の親も切り替え
            card.transform.SetParent(trashContainer);

            Debug.Log("カードを捨てました: " + card.name);
        }
        
    }

}