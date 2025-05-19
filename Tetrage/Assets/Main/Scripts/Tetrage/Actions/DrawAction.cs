using System.Collections;
using System.Linq;
using Tetrage.Models;
using Tetrage.UI;
using UnityEngine;
using Tetrage.Managers;  // Dealer用に必要
using Tetrage.Core.Contracts;

namespace Tetrage.Actions
{
    public class DrawAction : GameAction
    {
        private readonly Stage _stage;
        private Card selectedCard;

        public DrawAction(IPlayer requester, IGameContextProvider provider = null) : base(requester)
        {
            _stage = provider?.Stage ?? Dealer.Instance.Stage;
        }

        public override bool Validate()
        {
            return _stage.Stack.Count >= 2;
        }

        protected override IEnumerator Run()
        {
            // 1. Tmp に山札から2枚引く
            for (int i = 0; i < 2; i++)
            {
                //var card = _stage.DrawFromStack();
                //if (card == null) yield break;

                if (!_stage.DrawFromStack(_requester.Tmp)) yield break; // カードをスタックからドロー

            }

            // 2. Tmp から1枚選択して残す（残りは Stack に戻す）
            selectedCard = null;
            void OnTmpCardClick(Card clicked)
            {
                if (_requester.Tmp.Contains(clicked))
                {
                    selectedCard = clicked;
                }
            }

            try
            {
                CardClickDispatcher.OnCardClicked += OnTmpCardClick;
                yield return new WaitUntil(() => selectedCard != null);
            }
            finally
            {
                CardClickDispatcher.OnCardClicked -= OnTmpCardClick;
            }

            // 選ばれなかったカードを Stack に戻す
            foreach (var card in _requester.Tmp)
            {
                if (!ReferenceEquals(card, selectedCard))
                {
                    // 選択されなかったカードを山に戻す
                    CardPile.TransferService.Transfer(_requester.Tmp, _stage.Stack, card);
                    break;
                }
            }

            // 3. 手札に空きがあれば追加、なければ捨てるカードを選ぶ
            if (_requester.Hands.Count < 3)
            {
                // 手札に追加
                CardPile.TransferService.Transfer(_requester.Tmp, _requester.Hands, selectedCard);
            }
            else
            {
                // Overflow: Hands(3) + Tmp(1) の4枚から1枚選択して Trash
                selectedCard = null;

                void OnOverflowClick(Card clicked)
                {
                    if (_requester.Hands.Contains(clicked) || _requester.Tmp.Contains(clicked))
                    {
                        selectedCard = clicked;
                    }
                }

                try
                {
                    CardClickDispatcher.OnCardClicked += OnOverflowClick;
                    yield return new WaitUntil(() => selectedCard != null);
                }
                finally
                {
                    CardClickDispatcher.OnCardClicked -= OnOverflowClick;
                }

                if (_requester.Tmp.Contains(selectedCard))
                {
                    // Tmp のカードを Trash
                    _stage.Discard(_requester.Tmp, selectedCard);
                }
                else
                {
                    // Hands のカードを Trash
                    _stage.Discard(_requester.Hands, selectedCard);

                    // Tmp の残ったカードを Hands に追加
                    var tmpCard = _requester.Tmp.First();
                    CardPile.TransferService.Transfer(_requester.Tmp, _requester.Hands, tmpCard);
                }
            }
        }
    }
}
