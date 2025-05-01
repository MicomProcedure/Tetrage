using System.Collections;
using System.Linq;
using Tetrage.Models;
using Tetrage.UI;
using UnityEngine;
using Tetrage.Managers;
using Tetrage.Core.Contracts;

namespace Tetrage.Actions
{
    public class DrawAction : GameAction
    {
        private readonly Stage _stage;
        private Card selectedCard;

        public DrawAction(Player requester, IGameContextProvider provider = null) : base(requester)
        {
            _stage = provider.Stage ?? Dealer.Instance.Stage;
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
                var card = _stage.DrawFromStack();
                if (card == null) yield break;

                _requester.Tmp.Add(card);
                card.transform.SetParent(_requester.transform);
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
                    _requester.Tmp.TransferTo(_stage.Stack, card);
                    //card.transform.SetParent(_stage.transform);
                    break;
                }
            }

            // 3. 手札に空きがあれば追加、なければ捨てるカードを選ぶ
            if (_requester.Hands.Count < 3)
            {
                _requester.Tmp.TransferTo(_requester.Hands, selectedCard);
                //selectedCard.transform.SetParent(_requester.transform);
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
                    _requester.Tmp.TransferTo(_stage.Trash, selectedCard);
                    //selectedCard.transform.SetParent(_stage.transform);
                }
                else
                {
                    // Hands のカードを Trash
                    _requester.Hands.TransferTo(_stage.Trash, selectedCard);
                    //selectedCard.transform.SetParent(_stage.transform);

                    // Tmp の残ったカードを Hands に追加
                    var tmpCard = _requester.Tmp.First();
                    _requester.Tmp.TransferTo(_requester.Hands, tmpCard);
                    //tmpCard.transform.SetParent(_requester.transform);
                }
            }
        }
    }
}
