using UnityEngine;
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
        public bool DrawFromStack(CardPile targetPile)
        {
            if (_stack.Count == 0)
            {
                Debug.LogWarning("スタックが空です");
                return false;
            }

            // 先頭のカードを取得して、StackのCardPile から削除
            Card drawnCard = _stack.Peek(1)[0];

            _stack.TransferTo(targetPile, drawnCard); // カードを狙ったカードパイルへ移動する

            Debug.Log("カードを引きました: " + drawnCard.suit + drawnCard.number);

            return true;
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


            Debug.Log("カードを捨てました: " + card.suit + card.number);
        }
        
    }

}