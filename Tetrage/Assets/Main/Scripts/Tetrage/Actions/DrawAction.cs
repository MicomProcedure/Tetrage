using Cysharp.Threading.Tasks; // UniTask を使うために追加
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

        public DrawAction(IPlayer requester, IGameContextProvider provider = null) : base(requester)
        {
            _stage = provider?.Stage ?? Dealer.Instance.Stage;
        }

        public override bool Validate()
        {
            // 山札に2枚以上のカードがあることを確認
            return _stage.Stack.Count >= 2;
        }

        protected override async UniTask Run()
        {
            // 1. Tmp に山札から2枚引く
            for (int i = 0; i < 2; i++)
            {
                // カードをスタックからドロー
                if (!_stage.DrawFromStack(_requester.Tmp)) 
                {
                    Debug.LogWarning("山札からカードを引けませんでした。DrawActionを終了します。");
                    return;
                }
            }

            // 2. Tmp から1枚選択して残す（残りは Stack に戻す）
            selectedCard = null;

            // ローカル関数として定義し、await に対応させる
            // CardClickDispatcher が非同期のイベント発行をしている場合、これを待機する
            // もし CardClickDispatcher が同期的にイベントを発行している場合、
            // UniTask.WhenAll(CardClickDispatcher.OnCardClicked.ToUniTask()) のような変換はできないため、
            // 以下のようにフラグとループで待機する形が一般的です。
            
            // UniTask でイベントを待つためのカスタムヘルパー関数を定義することもできますが、
            // 今回は既存のイベントハンドラロジックを極力維持します。
            
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
                // yield return new WaitUntil(() => selectedCard != null); の代替
                // selectedCard が null でなくなるまで、毎フレーム待機
                await UniTask.WaitUntil(() => selectedCard != null); 
                // または while ループで実装:
                // while (selectedCard == null)
                // {
                //     await UniTask.Yield(); // 1フレーム待機
                // }
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
                    // ここで break するのは、Tmp には常に2枚あり、1枚選ばれたら残り1枚が選ばれなかったカードだから
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
                    // yield return new WaitUntil(() => selectedCard != null); の代替
                    await UniTask.WaitUntil(() => selectedCard != null);
                    // または while ループで実装:
                    // while (selectedCard == null)
                    // {
                    //     await UniTask.Yield(); // 1フレーム待機
                    // }
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
                else // _requester.Hands.Contains(selectedCard)
                {
                    // Hands のカードを Trash
                    _stage.Discard(_requester.Hands, selectedCard);

                    // Tmp の残ったカードを Hands に追加 (Tmp には常に1枚残っているはず)
                    var tmpCard = _requester.Tmp.First();
                    CardPile.TransferService.Transfer(_requester.Tmp, _requester.Hands, tmpCard);
                }
            }
        }
    }
}