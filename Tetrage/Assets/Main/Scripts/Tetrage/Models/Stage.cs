using UnityEngine;
using Tetrage.Core.Enums;
using Tetrage.Managers;

namespace Tetrage.Models
{
    public class Stage 
    {

        private CardPile _stack;
        private CardPile _trash;

        // スタックを読み取り専用で公開するプロパティ
        public CardPile Stack => _stack;
        // 捨て札を読み取り専用で公開するプロパティ 
        public CardPile Trash => _trash;

        public Stage(CardPile stack, CardPile trash){
            _stack = stack;
            _trash = trash;
        }

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

            CardTransferService.Transfer(_stack, targetPile, drawnCard);

            Debug.Log("カードを引きました: " + drawnCard.Suit + drawnCard.Number);

            return true;
        }

        // カードを捨て札へ
        public void Discard(CardPile cardPile, Card card)
        {
            if (card == null)
            {
                Debug.LogWarning("無効なカードが指定されました");
                return;
            }

            // stack→trash の移動をサービスで実行
            CardTransferService.Transfer(cardPile, _trash, card);

            Debug.Log("カードを捨てました: " + card.Suit + card.Number);
        }

    }

}