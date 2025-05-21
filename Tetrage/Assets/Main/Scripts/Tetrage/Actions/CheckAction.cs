using UnityEngine;
using Tetrage.Models;
using Tetrage.Managers;
using Tetrage.UI;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Contracts;
using System.Collections;

namespace Tetrage.Actions
{
    public class CheckAction : GameAction
    {
        private IReadOnlyList<IPlayer> _others; //他のプレイヤークラスが入ってるリスト
        private Card _card;
        private IGameContextProvider _provider; // 基本はDealer、テスト用にそれ以外

        public CheckAction(IPlayer requester, IGameContextProvider provider = null) : base(requester) // providerはデフォルト引数なので省略可能
        {
            _provider = provider ?? (IGameContextProvider)Dealer.Instance; // providerを受け取るが、デフォルトではDealerの単一なインスタンスとなる

            // provider（Dealer）を使って他プレイヤーの参照を書き込み
            _others = _provider.Players
                                .Where(p => !ReferenceEquals(p, requester)).ToList();
        }

        public override bool Validate()
        {
            //全てのカードが一致しているかを返す
            return _requester.Hands.All(c => c.Suit == _requester.Hands.First().Suit);
        }
        protected override IEnumerator Run()
        {
            var selectable = _others
                .Select(p => p.Target as Card) // 各プレイヤーのターゲットを取得し Card にキャスト
                .Where(c => c != null && c.IsVisible) // Nullや表向きカードは除外
                .ToList();

            // クリック待ちの処理
            Card clickedCard = null;
            try
            {
                CardClickDispatcher.OnCardClicked += OnClick;

                yield return new WaitUntil(() => clickedCard != null); // クリックされるまで毎フレームチェック
            }
            finally
            {
                // try.finallyを使うことで、待機中にエラーが発生しても必ずイベントを解除する
                CardClickDispatcher.OnCardClicked -= OnClick;
            }
            //ここでclickedCardにクリックしたカードが入ってるからそのカードと，自分の手札3枚が一致してるかを確かめる
            if (_requester.Hands.First().Suit == clickedCard.Suit)
            {
                //チェック成功
                Debug.Log("チェック成功");
            }
            else
            {
                //チェック失敗
                Debug.Log("チェック失敗");
            }


            // ローカル関数
            void OnClick(Card c)
            {
                if (selectable.Contains(c)) clickedCard = c;
            }
        }
    }
}